using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace dennokoworks.FolderCleaner
{
    /// <summary>
    /// 指定したテクスチャフォルダ内のテクスチャのうち、参照元フォルダ配下の
    /// 全アセットから参照されていないものを検出・削除する。
    /// UI は dennokoworks フローティングデザイン (UI Toolkit: UXML/USS) に準拠する。
    /// </summary>
    public class FolderCleanerWindow : EditorWindow
    {
        private const string UXML_GUID = "d3c710f69a5e4b9cb7f1839a48e71234";
        private const string USS_THEME_GUID = "f4a819b2c3d445e698123456789abcde";
        private const string USS_WINDOW_GUID = "c1a2b3c4d5e6f708192a3b4c5d6e7f80";

        public enum StatusType { Info, Success, Error }

        // ─── 設定状態 ────────────────────────────────────────────────────────
        private DefaultAsset _textureFolder;
        private readonly List<DefaultAsset> _sourceFolders          = new List<DefaultAsset> { null };
        private readonly List<DefaultAsset> _excludedFolders        = new List<DefaultAsset>();
        private readonly List<DefaultAsset> _excludedTextureFolders = new List<DefaultAsset>();
        private bool _moveToTrash = true;

        // ─── スキャン結果 ────────────────────────────────────────────────────
        private readonly List<string> _unreferenced = new List<string>();
        private readonly HashSet<string> _selectedPaths = new HashSet<string>();
        private bool _hasScanned;

        // ─── UI 要素参照 ────────────────────────────────────────────────────
        private VisualElement _root;
        private Label _titleLabel;
        private Label _versionLabel;
        private Button _versionReloadBtn;
        private Button _langBtn;

        private Label _sectionTargetTitle;
        private Label _targetFolderLabel;
        private Label _targetFolderHint;
        private ObjectField _targetFolderField;
        private Label _targetFolderError;
        private Label _protectedSubfoldersTitle;
        private Label _protectedSubfoldersHint;
        private VisualElement _protectedSubfoldersContainer;
        private Button _addProtectedSubfolderBtn;

        private Label _sectionReferencesTitle;
        private Label _sourceFoldersTitle;
        private Label _sourceFoldersHint;
        private VisualElement _sourceFoldersContainer;
        private Button _addSourceFolderBtn;
        private Label _excludedFoldersTitle;
        private Label _excludedFoldersHint;
        private VisualElement _excludedFoldersContainer;
        private Button _addExcludedFolderBtn;

        private Label _sectionOptionsTitle;
        private Button _optionTrashBtn;
        private Button _optionDeleteBtn;
        private Label _optionHintLabel;

        private Label _sectionResultsTitle;
        private Label _resultsPlaceholderLabel;
        private VisualElement _resultsContentContainer;
        private Label _resultsSummaryLabel;
        private Button _selectAllBtn;
        private Button _deselectAllBtn;
        private ScrollView _resultsScroll;

        private Button _scanBtn;
        private Button _deleteBtn;
        private Label _statusLabel;
        private IVisualElementScheduledItem _statusResetSchedule;

        private DennokoVersionChecker.Result _versionResult =
            new DennokoVersionChecker.Result { State = DennokoVersionChecker.State.Checking, LocalVersion = "1.0.0" };

        [MenuItem("dennokoworks/Folder Cleaner")]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderCleanerWindow>("Folder Cleaner");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            FolderCleanerLoc.OnLanguageChanged += RefreshLocalization;
        }

        private void OnDisable()
        {
            FolderCleanerLoc.OnLanguageChanged -= RefreshLocalization;
        }

        public void CreateGUI()
        {
            _root = rootVisualElement;
            _root.AddToClassList("dennoko-root");
            _root.style.backgroundColor = (Color)new Color32(0x12, 0x12, 0x12, 0xFF);
            _root.style.flexGrow = 1;

            DennokoUIFont.Apply(_root);

            LoadStylesheets();

            var uxml = LoadAssetByGuidOrPath<VisualTreeAsset>(UXML_GUID, "Editor/UI/FolderCleanerWindow.uxml");
            if (uxml == null)
            {
                _root.Add(new Label("Error: FolderCleanerWindow.uxml not found."));
                return;
            }
            uxml.CloneTree(_root);

            BindElements();
            SetupEventHandlers();
            RefreshLocalization();
            StartVersionCheck();
            UpdateActionButtonsState();
        }

        private void LoadStylesheets()
        {
            var themeUss = LoadAssetByGuidOrPath<StyleSheet>(USS_THEME_GUID, "Editor/UI/DennokoTheme.uss");
            if (themeUss != null) _root.styleSheets.Add(themeUss);

            var windowUss = LoadAssetByGuidOrPath<StyleSheet>(USS_WINDOW_GUID, "Editor/UI/FolderCleanerWindow.uss");
            if (windowUss != null) _root.styleSheets.Add(windowUss);
        }

        private static T LoadAssetByGuidOrPath<T>(string guid, string relativeSuffix) where T : UnityEngine.Object
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null) return asset;
                }
            }

            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(relativeSuffix));
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(relativeSuffix.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<T>(p);
                    if (asset != null) return asset;
                }
            }
            return null;
        }

        private void BindElements()
        {
            _titleLabel = _root.Q<Label>("title-label");
            _versionLabel = _root.Q<Label>("version-label");
            _versionReloadBtn = _root.Q<Button>("version-reload-button");
            _langBtn = _root.Q<Button>("lang-button");

            _sectionTargetTitle = _root.Q<Label>("section-target-title");
            _targetFolderLabel = _root.Q<Label>("target-folder-label");
            _targetFolderHint = _root.Q<Label>("target-folder-hint");
            _targetFolderField = _root.Q<ObjectField>("target-folder-field");
            _targetFolderError = _root.Q<Label>("target-folder-error");
            _protectedSubfoldersTitle = _root.Q<Label>("protected-subfolders-title");
            _protectedSubfoldersHint = _root.Q<Label>("protected-subfolders-hint");
            _protectedSubfoldersContainer = _root.Q<VisualElement>("protected-subfolders-container");
            _addProtectedSubfolderBtn = _root.Q<Button>("add-protected-subfolder-btn");

            _sectionReferencesTitle = _root.Q<Label>("section-references-title");
            _sourceFoldersTitle = _root.Q<Label>("source-folders-title");
            _sourceFoldersHint = _root.Q<Label>("source-folders-hint");
            _sourceFoldersContainer = _root.Q<VisualElement>("source-folders-container");
            _addSourceFolderBtn = _root.Q<Button>("add-source-folder-btn");
            _excludedFoldersTitle = _root.Q<Label>("excluded-folders-title");
            _excludedFoldersHint = _root.Q<Label>("excluded-folders-hint");
            _excludedFoldersContainer = _root.Q<VisualElement>("excluded-folders-container");
            _addExcludedFolderBtn = _root.Q<Button>("add-excluded-folder-btn");

            _sectionOptionsTitle = _root.Q<Label>("section-options-title");
            _optionTrashBtn = _root.Q<Button>("option-trash-btn");
            _optionDeleteBtn = _root.Q<Button>("option-delete-btn");
            _optionHintLabel = _root.Q<Label>("option-hint-label");

            _sectionResultsTitle = _root.Q<Label>("section-results-title");
            _resultsPlaceholderLabel = _root.Q<Label>("results-placeholder-label");
            _resultsContentContainer = _root.Q<VisualElement>("results-content-container");
            _resultsSummaryLabel = _root.Q<Label>("results-summary-label");
            _selectAllBtn = _root.Q<Button>("select-all-btn");
            _deselectAllBtn = _root.Q<Button>("deselect-all-btn");
            _resultsScroll = _root.Q<ScrollView>("results-scroll");

            _scanBtn = _root.Q<Button>("scan-btn");
            _deleteBtn = _root.Q<Button>("delete-btn");
            _statusLabel = _root.Q<Label>("status-label");
        }

        private void SetupEventHandlers()
        {
            // 言語切替
            _langBtn.clicked += () =>
            {
                FolderCleanerLoc.CurrentLanguage = FolderCleanerLoc.CurrentLanguage == Language.Japanese
                    ? Language.English
                    : Language.Japanese;
            };

            // バージョン手動リロード
            _versionReloadBtn.clicked += () =>
            {
                FolderCleanerVersion.ForceRecheck();
                LoadVersionResultFromSessionState();
            };

            // 整理するフォルダ
            _targetFolderField.objectType = typeof(DefaultAsset);
            _targetFolderField.value = _textureFolder;
            _targetFolderField.RegisterValueChangedCallback(evt =>
            {
                _textureFolder = evt.newValue as DefaultAsset;
                ValidateTargetFolder();
                AutoExcludeTextureFolderIfNeeded();
                UpdateActionButtonsState();
            });

            // フォルダリスト追加ボタン
            _addProtectedSubfolderBtn.clicked += () =>
            {
                _excludedTextureFolders.Add(null);
                RenderFolderList(_protectedSubfoldersContainer, _excludedTextureFolders);
            };

            _addSourceFolderBtn.clicked += () =>
            {
                _sourceFolders.Add(null);
                RenderFolderList(_sourceFoldersContainer, _sourceFolders);
                UpdateActionButtonsState();
            };

            _addExcludedFolderBtn.clicked += () =>
            {
                _excludedFolders.Add(null);
                RenderFolderList(_excludedFoldersContainer, _excludedFolders);
            };

            // 削除オプション切替 (青枠 DennokoTheme.uss 連携)
            _optionTrashBtn.clicked += () => SetDeleteMode(true);
            _optionDeleteBtn.clicked += () => SetDeleteMode(false);
            SetDeleteMode(_moveToTrash);

            // スキャン結果 全選択 / 全解除
            _selectAllBtn.clicked += () =>
            {
                _selectedPaths.UnionWith(_unreferenced);
                RenderResultsList();
                UpdateActionButtonsState();
            };

            _deselectAllBtn.clicked += () =>
            {
                _selectedPaths.Clear();
                RenderResultsList();
                UpdateActionButtonsState();
            };

            // メインアクションボタン
            _scanBtn.clicked += Scan;
            _deleteBtn.clicked += Delete;

            // 各フォルダリスト初期描画
            RenderFolderList(_protectedSubfoldersContainer, _excludedTextureFolders);
            RenderFolderList(_sourceFoldersContainer, _sourceFolders);
            RenderFolderList(_excludedFoldersContainer, _excludedFolders);
            RenderResultsList();
        }

        private void SetDeleteMode(bool moveToTrash)
        {
            _moveToTrash = moveToTrash;
            _optionTrashBtn.EnableInClassList("dennoko-button-active", _moveToTrash);
            _optionDeleteBtn.EnableInClassList("dennoko-button-active", !_moveToTrash);
            _optionHintLabel.text = FolderCleanerLoc.Tr(_moveToTrash ? "OptionTrashHint" : "OptionDeleteHint");
            _optionHintLabel.EnableInClassList("dennoko-warning-hint", !_moveToTrash);
            _optionHintLabel.EnableInClassList("dennoko-hint", _moveToTrash);
        }

        private void ValidateTargetFolder()
        {
            if (_textureFolder != null && GetValidFolderPath(_textureFolder) == null)
            {
                _targetFolderError.style.display = DisplayStyle.Flex;
                _targetFolderError.text = FolderCleanerLoc.Tr("ErrorNotAFolder");
            }
            else
            {
                _targetFolderError.style.display = DisplayStyle.None;
            }
        }

        private void RenderFolderList(VisualElement container, List<DefaultAsset> list)
        {
            container.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                int index = i;
                var row = new VisualElement();
                row.AddToClassList("folder-item-row");

                var field = new ObjectField
                {
                    objectType = typeof(DefaultAsset),
                    value = list[index]
                };
                field.AddToClassList("folder-field");
                field.RegisterValueChangedCallback(evt =>
                {
                    list[index] = evt.newValue as DefaultAsset;
                    AutoExcludeTextureFolderIfNeeded();
                    UpdateActionButtonsState();
                });
                row.Add(field);

                var removeBtn = new Button(() =>
                {
                    list.RemoveAt(index);
                    RenderFolderList(container, list);
                    UpdateActionButtonsState();
                })
                {
                    text = "✕"
                };
                removeBtn.AddToClassList("remove-folder-btn");
                row.Add(removeBtn);

                container.Add(row);
            }
        }

        private void RenderResultsList()
        {
            if (!_hasScanned)
            {
                _resultsPlaceholderLabel.style.display = DisplayStyle.Flex;
                _resultsPlaceholderLabel.text = FolderCleanerLoc.Tr("ResultsPlaceholder");
                _resultsContentContainer.style.display = DisplayStyle.None;
                return;
            }

            if (_unreferenced.Count == 0)
            {
                _resultsPlaceholderLabel.style.display = DisplayStyle.Flex;
                _resultsPlaceholderLabel.text = FolderCleanerLoc.Tr("ResultsNoneFound");
                _resultsContentContainer.style.display = DisplayStyle.None;
                return;
            }

            _resultsPlaceholderLabel.style.display = DisplayStyle.None;
            _resultsContentContainer.style.display = DisplayStyle.Flex;

            _resultsSummaryLabel.text = FolderCleanerLoc.Tr("ResultsSummary", _unreferenced.Count, _selectedPaths.Count);

            _resultsScroll.Clear();
            foreach (var path in _unreferenced)
            {
                string assetPath = path;
                var row = new VisualElement();
                row.AddToClassList("result-row");

                var toggle = new Toggle
                {
                    value = _selectedPaths.Contains(assetPath)
                };
                toggle.AddToClassList("result-toggle");
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) _selectedPaths.Add(assetPath);
                    else _selectedPaths.Remove(assetPath);
                    _resultsSummaryLabel.text = FolderCleanerLoc.Tr("ResultsSummary", _unreferenced.Count, _selectedPaths.Count);
                    UpdateActionButtonsState();
                });
                row.Add(toggle);

                var nameLabel = new Label(Path.GetFileName(assetPath));
                nameLabel.AddToClassList("result-name");
                nameLabel.tooltip = assetPath;
                row.Add(nameLabel);

                var pingBtn = new Button(() =>
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                })
                {
                    text = FolderCleanerLoc.Tr("PingAsset")
                };
                pingBtn.AddToClassList("result-ping-btn");
                row.Add(pingBtn);

                _resultsScroll.Add(row);
            }
        }

        private void UpdateActionButtonsState()
        {
            bool foldersValid = GetValidFolderPath(_textureFolder) != null
                             && CollectValidFolderPaths(_sourceFolders).Count > 0;
            _scanBtn.SetEnabled(foldersValid);
            _deleteBtn.SetEnabled(_hasScanned && _selectedPaths.Count > 0);
        }

        public void RefreshLocalization()
        {
            if (_root == null) return;

            titleContent = new GUIContent(FolderCleanerLoc.Tr("WindowTitle"));
            _titleLabel.text = FolderCleanerLoc.Tr("HeaderTitle");
            _langBtn.text = FolderCleanerLoc.Tr("LangButton");
            _langBtn.tooltip = FolderCleanerLoc.Tr("LangButtonTooltip");
            _versionReloadBtn.tooltip = FolderCleanerLoc.Tr("ReloadVersionTooltip");

            _sectionTargetTitle.text = FolderCleanerLoc.Tr("SectionTarget");
            _targetFolderLabel.text = FolderCleanerLoc.Tr("TargetFolderLabel");
            _targetFolderHint.text = FolderCleanerLoc.Tr("TargetFolderHint");
            _protectedSubfoldersTitle.text = FolderCleanerLoc.Tr("ProtectedSubfoldersTitle");
            _protectedSubfoldersHint.text = FolderCleanerLoc.Tr("ProtectedSubfoldersHint");
            _addProtectedSubfolderBtn.text = FolderCleanerLoc.Tr("AddProtectedSubfolder");

            _sectionReferencesTitle.text = FolderCleanerLoc.Tr("SectionReferences");
            _sourceFoldersTitle.text = FolderCleanerLoc.Tr("SourceFoldersTitle");
            _sourceFoldersHint.text = FolderCleanerLoc.Tr("SourceFoldersHint");
            _addSourceFolderBtn.text = FolderCleanerLoc.Tr("AddSourceFolder");
            _excludedFoldersTitle.text = FolderCleanerLoc.Tr("ExcludedFoldersTitle");
            _excludedFoldersHint.text = FolderCleanerLoc.Tr("ExcludedFoldersHint");
            _addExcludedFolderBtn.text = FolderCleanerLoc.Tr("AddExcludedFolder");

            _sectionOptionsTitle.text = FolderCleanerLoc.Tr("SectionOptions");
            _optionTrashBtn.text = FolderCleanerLoc.Tr("OptionTrash");
            _optionDeleteBtn.text = FolderCleanerLoc.Tr("OptionDelete");
            _optionHintLabel.text = FolderCleanerLoc.Tr(_moveToTrash ? "OptionTrashHint" : "OptionDeleteHint");

            _sectionResultsTitle.text = FolderCleanerLoc.Tr("SectionResults");
            _selectAllBtn.text = FolderCleanerLoc.Tr("SelectAll");
            _deselectAllBtn.text = FolderCleanerLoc.Tr("DeselectAll");

            _scanBtn.text = FolderCleanerLoc.Tr("ButtonScan");
            _deleteBtn.text = FolderCleanerLoc.Tr("ButtonDelete");

            ValidateTargetFolder();
            RenderResultsList();
            ApplyVersionLabel();
        }

        // ─── バージョンチェック ─────────────────────────────────────────────
        private void StartVersionCheck()
        {
            LoadVersionResultFromSessionState();
            FolderCleanerVersion.StartCheckBackgroundTask();
        }

        internal void LoadVersionResultFromSessionState()
        {
            string local  = FolderCleanerVersion.Current;
            string latest = SessionState.GetString(FolderCleanerVersion.VerCheckLatestKey, string.Empty);
            bool   done   = SessionState.GetBool(FolderCleanerVersion.VerCheckDoneKey, false);
            bool   error  = SessionState.GetBool(FolderCleanerVersion.VerCheckErrorKey, false);

            DennokoVersionChecker.State state;
            if (!done)
                state = DennokoVersionChecker.State.Checking;
            else if (error || string.IsNullOrEmpty(latest))
                state = DennokoVersionChecker.State.Error;
            else if (DennokoVersionChecker.IsUpdateAvailable(latest, local))
                state = DennokoVersionChecker.State.UpdateAvailable;
            else
                state = DennokoVersionChecker.State.UpToDate;

            _versionResult = new DennokoVersionChecker.Result
            {
                State = state,
                LocalVersion = local,
                LatestVersion = latest,
                Url = SessionState.GetString(FolderCleanerVersion.VerCheckUrlKey, string.Empty),
                Message = SessionState.GetString(FolderCleanerVersion.VerCheckMessageKey, string.Empty)
            };
            ApplyVersionLabel();
        }

        private void ApplyVersionLabel()
        {
            if (_versionLabel == null) return;

            var r = _versionResult;
            string baseText = "v" + r.LocalVersion;
            string text;
            bool update = false, error = false;
            switch (r.State)
            {
                case DennokoVersionChecker.State.UpdateAvailable:
                    text = baseText + "  " + FolderCleanerLoc.Tr("VerUpdateAvailable", r.LatestVersion);
                    update = true;
                    break;
                case DennokoVersionChecker.State.Error:
                    text = baseText + "  " + FolderCleanerLoc.Tr("VerError");
                    error = true;
                    break;
                case DennokoVersionChecker.State.Checking:
                    text = baseText + "  " + FolderCleanerLoc.Tr("VerChecking");
                    break;
                default:
                    text = baseText;
                    break;
            }
            _versionLabel.text = text;
            _versionLabel.EnableInClassList("dennoko-version-label--update", update);
            _versionLabel.EnableInClassList("dennoko-version-label--error", error);
        }

        // ─── ステータスバー ─────────────────────────────────────────────────
        private void SetStatus(string message, StatusType type, long autoResetMs = 3500)
        {
            if (_statusLabel == null) return;

            _statusLabel.text = message;
            _statusLabel.EnableInClassList("dennoko-status--success", type == StatusType.Success);
            _statusLabel.EnableInClassList("dennoko-status--error",   type == StatusType.Error);

            _statusResetSchedule?.Pause();
            if (type != StatusType.Info)
            {
                _statusResetSchedule = _statusLabel.schedule
                    .Execute(() => SetStatus(FolderCleanerLoc.Tr("StatusReady"), StatusType.Info))
                    .StartingIn(autoResetMs);
            }
        }

        // ─── ビジネスロジック ───────────────────────────────────────────────
        private static string GetValidFolderPath(DefaultAsset folder)
        {
            if (folder == null) return null;
            string path = AssetDatabase.GetAssetPath(folder);
            return AssetDatabase.IsValidFolder(path) ? path : null;
        }

        private static bool IsSubFolderOf(string child, string parent)
        {
            if (string.IsNullOrEmpty(child) || string.IsNullOrEmpty(parent)) return false;
            return child.StartsWith(parent + "/", StringComparison.Ordinal);
        }

        private void AutoExcludeTextureFolderIfNeeded()
        {
            if (_textureFolder == null) return;
            string texturePath = GetValidFolderPath(_textureFolder);
            if (texturePath == null) return;

            bool isSubFolder = false;
            foreach (var source in _sourceFolders)
            {
                string sourcePath = GetValidFolderPath(source);
                if (sourcePath != null && IsSubFolderOf(texturePath, sourcePath))
                {
                    isSubFolder = true;
                    break;
                }
            }
            if (!isSubFolder) return;

            if (_excludedFolders.Contains(_textureFolder)) return;
            _excludedFolders.Add(_textureFolder);
            RenderFolderList(_excludedFoldersContainer, _excludedFolders);
        }

        private static List<string> CollectValidFolderPaths(List<DefaultAsset> folders)
        {
            var result = new List<string>();
            foreach (var folder in folders)
            {
                string path = GetValidFolderPath(folder);
                if (path != null && !result.Contains(path))
                    result.Add(path);
            }
            return result;
        }

        private void Scan()
        {
            string texturePath = GetValidFolderPath(_textureFolder);
            var sourcePaths = CollectValidFolderPaths(_sourceFolders);
            if (texturePath == null || sourcePaths.Count == 0)
            {
                SetStatus(FolderCleanerLoc.Tr("ErrorInvalidFolders"), StatusType.Error);
                return;
            }

            var excludedPaths = CollectValidFolderPaths(_excludedFolders);
            var excludedTexturePaths = CollectValidFolderPaths(_excludedTextureFolders);
            var result = FolderTextureScanner.Scan(texturePath, sourcePaths, excludedPaths, excludedTexturePaths);

            _unreferenced.Clear();
            _unreferenced.AddRange(result.UnreferencedTexturePaths);

            _selectedPaths.Clear();
            _selectedPaths.UnionWith(result.UnreferencedTexturePaths);

            _hasScanned = true;
            RenderResultsList();
            UpdateActionButtonsState();

            SetStatus(
                FolderCleanerLoc.Tr("ScanSuccess", result.TotalTextureCount, _unreferenced.Count),
                StatusType.Success);
        }

        private void Delete()
        {
            var pathsToDelete = new List<string>();
            foreach (var path in _unreferenced)
            {
                if (_selectedPaths.Contains(path))
                    pathsToDelete.Add(path);
            }

            if (pathsToDelete.Count == 0) return;

            string dialogMsg = _moveToTrash
                ? FolderCleanerLoc.Tr("DialogDeleteMessageTrash", pathsToDelete.Count)
                : FolderCleanerLoc.Tr("DialogDeleteMessageDirect", pathsToDelete.Count);

            bool ok = EditorUtility.DisplayDialog(
                FolderCleanerLoc.Tr("DialogDeleteTitle"),
                dialogMsg,
                FolderCleanerLoc.Tr("DialogProceed"),
                FolderCleanerLoc.Tr("DialogCancel"));
            if (!ok) return;

            var result = TextureRemover.Remove(pathsToDelete, _moveToTrash);

            _unreferenced.Clear();
            _selectedPaths.Clear();
            _hasScanned = false;
            RenderResultsList();
            UpdateActionButtonsState();

            if (result.AllSucceeded)
            {
                string successMsg = _moveToTrash
                    ? FolderCleanerLoc.Tr("DeleteSuccessTrash", result.DeletedCount)
                    : FolderCleanerLoc.Tr("DeleteSuccessDirect", result.DeletedCount);
                Debug.Log($"[FolderCleaner] {successMsg}");
                SetStatus(successMsg, StatusType.Success);
            }
            else
            {
                Debug.LogWarning(
                    $"[FolderCleaner] {result.DeletedCount} deleted, {result.FailedPaths.Count} failed:\n"
                    + string.Join("\n", result.FailedPaths));
                SetStatus(FolderCleanerLoc.Tr("DeletePartialFailed", result.DeletedCount, result.FailedPaths.Count), StatusType.Error);
            }
        }
    }
}
