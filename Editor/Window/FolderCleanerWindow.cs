using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace dennokoworks.FolderCleaner
{
    /// <summary>
    /// 指定したテクスチャフォルダ内のテクスチャのうち、参照元フォルダ配下の
    /// 全アセットから（シェーダー種別を問わず）参照されていないものを検出・削除する。
    /// UI は dennokoworks フローティングデザイン（FolderCleanerTheme）に準拠する。
    ///
    /// 描画処理は partial の FolderCleanerWindow.Drawing.cs に分離している。
    /// スキャン/削除のロジックは Core/FolderTextureScanner・Core/TextureRemover に委譲する。
    /// </summary>
    public partial class FolderCleanerWindow : EditorWindow
    {
        // ─── Status ──────────────────────────────────────────────────────────
        public enum StatusType { Info, Success, Error }
        private string     _statusMessage   = "Ready";
        private StatusType _statusType      = StatusType.Info;
        private double     _statusResetTime = -1.0;

        // ─── 設定 ────────────────────────────────────────────────────────────
        private DefaultAsset _textureFolder; // 削除候補テクスチャの検索フォルダ
        private readonly List<DefaultAsset> _sourceFolders           = new List<DefaultAsset> { null }; // 参照元アセットの検索フォルダ（複数）
        private readonly List<DefaultAsset> _excludedFolders         = new List<DefaultAsset>();         // 参照元スキャンから除外するサブフォルダ（複数）
        private readonly List<DefaultAsset> _excludedTextureFolders  = new List<DefaultAsset>();         // テクスチャ検索から除外するサブフォルダ（複数）
        private bool _moveToTrash = true;     // true=ゴミ箱へ移動 / false=完全削除

        // ─── スキャン結果 ────────────────────────────────────────────────────
        private readonly List<string> _unreferenced = new List<string>(); // 未参照テクスチャのパス
        private readonly HashSet<string> _selectedPaths = new HashSet<string>(); // 削除対象としてチェックされたパス
        private bool _hasScanned;

        // ─── スクロール ──────────────────────────────────────────────────────
        private Vector2 _scrollPosition;
        private Vector2 _resultScrollPosition;

        // ─── Window Registration ─────────────────────────────────────────────
        [MenuItem("dennokoworks/Folder Cleaner")]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderCleanerWindow>("Folder Cleaner");
            window.minSize = new Vector2(400, 600);
        }

        // ─── OnGUI Entry Point ───────────────────────────────────────────────
        private void OnGUI()
        {
            // ステータスの自動リセット（Info 以外を一定時間後に戻す）
            if (_statusResetTime > 0 && EditorApplication.timeSinceStartup > _statusResetTime)
            {
                _statusMessage   = "Ready";
                _statusType      = StatusType.Info;
                _statusResetTime = -1.0;
            }

            FolderCleanerTheme.Initialize();
            FolderCleanerTheme.PushEditorTheme();

            try
            {
                EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), FolderCleanerTheme.Surface0);

                DrawHeader();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawSettingsArea();
                EditorGUILayout.EndScrollView();

                DrawFooter();
                DrawStatusBar();
            }
            finally
            {
                FolderCleanerTheme.PopEditorTheme();
            }
        }

        // ─── ビジネスロジック ───────────────────────────────────────────────

        /// <summary>DefaultAsset が有効なフォルダであればそのアセットパスを返し、そうでなければ null。</summary>
        private static string GetValidFolderPath(DefaultAsset folder)
        {
            if (folder == null) return null;
            string path = AssetDatabase.GetAssetPath(folder);
            return AssetDatabase.IsValidFolder(path) ? path : null;
        }

        /// <summary>child が parent のサブフォルダ（同一フォルダは含まない）であれば true。</summary>
        private static bool IsSubFolderOf(string child, string parent)
        {
            if (string.IsNullOrEmpty(child) || string.IsNullOrEmpty(parent)) return false;
            return child.StartsWith(parent + "/", System.StringComparison.Ordinal);
        }

        /// <summary>
        /// テクスチャ検索フォルダがいずれかの参照元フォルダのサブフォルダであれば、
        /// 自動で除外サブフォルダへ追加する（既に含まれている場合は何もしない）。
        /// </summary>
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
        }

        /// <summary>リスト内の有効なフォルダパスを重複排除して返す。</summary>
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

        /// <summary>未参照テクスチャを算出する。</summary>
        private void Scan()
        {
            string texturePath = GetValidFolderPath(_textureFolder);
            var sourcePaths = CollectValidFolderPaths(_sourceFolders);
            if (texturePath == null || sourcePaths.Count == 0)
            {
                SetStatus("テクスチャ検索フォルダと、1 つ以上の参照元フォルダを正しく指定してください。", StatusType.Error);
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
            _resultScrollPosition = Vector2.zero;
            SetStatus(
                $"スキャン完了: テクスチャ {result.TotalTextureCount} 件中、未参照 {_unreferenced.Count} 件",
                StatusType.Success);
        }

        /// <summary>未参照テクスチャを削除する。</summary>
        private void Delete()
        {
            var pathsToDelete = new List<string>();
            foreach (var path in _unreferenced)
            {
                if (_selectedPaths.Contains(path))
                    pathsToDelete.Add(path);
            }

            if (pathsToDelete.Count == 0) return;

            string mode = _moveToTrash ? "ゴミ箱へ移動" : "完全に削除";
            bool ok = EditorUtility.DisplayDialog(
                "未参照テクスチャの削除",
                $"{pathsToDelete.Count} 件のテクスチャを{mode}します。よろしいですか？",
                "実行", "キャンセル");
            if (!ok) return;

            var result = TextureRemover.Remove(pathsToDelete, _moveToTrash);

            _unreferenced.Clear();
            _selectedPaths.Clear();
            _hasScanned = false;

            if (result.AllSucceeded)
            {
                Debug.Log($"[FolderCleaner] {result.DeletedCount} 件のテクスチャを{mode}しました。");
                SetStatus($"{result.DeletedCount} 件のテクスチャを{mode}しました。", StatusType.Success);
            }
            else
            {
                Debug.LogWarning(
                    $"[FolderCleaner] {result.DeletedCount} 件削除、{result.FailedPaths.Count} 件失敗:\n"
                    + string.Join("\n", result.FailedPaths));
                SetStatus($"{result.DeletedCount} 件削除、{result.FailedPaths.Count} 件失敗（コンソール参照）。", StatusType.Error);
            }
        }

        /// <summary>ステータスバーにメッセージを表示し、一定時間後に "Ready" へ戻す。</summary>
        private void SetStatus(string message, StatusType type, double autoResetSeconds = 4.0)
        {
            _statusMessage   = message;
            _statusType      = type;
            _statusResetTime = type == StatusType.Info
                ? -1.0
                : EditorApplication.timeSinceStartup + autoResetSeconds;
            Repaint();
        }
    }
}
