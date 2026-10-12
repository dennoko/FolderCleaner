using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dennokoworks.FolderCleaner
{
    public enum Language
    {
        Japanese,
        English
    }

    public static class FolderCleanerLoc
    {
        private const string PrefsKey = "FolderCleaner_Language";
        private static Language? _currentLanguage;

        public static event Action OnLanguageChanged;

        public static Language CurrentLanguage
        {
            get
            {
                if (!_currentLanguage.HasValue)
                {
                    string saved = EditorPrefs.GetString(PrefsKey, string.Empty);
                    if (saved == "en")
                        _currentLanguage = Language.English;
                    else if (saved == "ja")
                        _currentLanguage = Language.Japanese;
                    else
                        _currentLanguage = Application.systemLanguage == SystemLanguage.Japanese
                            ? Language.Japanese
                            : Language.English;
                }
                return _currentLanguage.Value;
            }
            set
            {
                if (_currentLanguage == value) return;
                _currentLanguage = value;
                EditorPrefs.SetString(PrefsKey, value == Language.English ? "en" : "ja");
                OnLanguageChanged?.Invoke();
            }
        }

        public static string Tr(string key, params object[] args)
        {
            var dict = CurrentLanguage == Language.English ? StringsEn : StringsJa;
            if (!dict.TryGetValue(key, out string text))
            {
                if (!StringsJa.TryGetValue(key, out text))
                    text = key;
            }

            if (args != null && args.Length > 0)
            {
                try { return string.Format(text, args); }
                catch { return text; }
            }
            return text;
        }

        private static readonly Dictionary<string, string> StringsJa = new Dictionary<string, string>
        {
            // ヘッダー
            { "WindowTitle", "Folder Cleaner" },
            { "HeaderTitle", "Folder Cleaner" },
            { "LangButton", "EN" },
            { "LangButtonTooltip", "Switch to English" },
            { "ReloadVersionTooltip", "アップデートを再確認" },
            { "VerChecking", "確認中..." },
            { "VerUpdateAvailable", "更新あり {0}" },
            { "VerError", "最新版を取得できません" },

            // セクション1: 整理対象テクスチャ
            { "SectionTarget", "整理するテクスチャ（削除対象）" },
            { "TargetFolderLabel", "整理するフォルダ" },
            { "TargetFolderHint", "不要なテクスチャを検出・削除したいフォルダを指定します。" },
            { "ProtectedSubfoldersTitle", "保護するサブフォルダ（削除候補から除外）" },
            { "ProtectedSubfoldersHint", "このサブフォルダ内のテクスチャは削除候補に含めません。" },
            { "AddProtectedSubfolder", "＋ 保護するサブフォルダを追加" },

            // セクション2: 使用判定・保護基準
            { "SectionReferences", "使用判定（保護基準）" },
            { "SourceFoldersTitle", "使用元フォルダ (マテリアル・プレハブ等)" },
            { "SourceFoldersHint", "ここにあるアセットが使用しているテクスチャは保護（保持）されます。" },
            { "AddSourceFolder", "＋ 使用元フォルダを追加" },
            { "ExcludedFoldersTitle", "判定から除外するサブフォルダ" },
            { "ExcludedFoldersHint", "このフォルダからの参照はカウントしません（自己参照などの誤判定防止）。" },
            { "AddExcludedFolder", "＋ 判定から除外するサブフォルダを追加" },

            // セクション3: 削除オプション
            { "SectionOptions", "削除オプション" },
            { "OptionTrash", "ゴミ箱へ移動（推奨）" },
            { "OptionDelete", "完全に削除" },
            { "OptionTrashHint", "削除したテクスチャは OS のゴミ箱へ移動します（後から復元可能）。" },
            { "OptionDeleteHint", "プロジェクトから完全に削除されます（復元できません）。" },

            // セクション4: スキャン結果
            { "SectionResults", "スキャン結果" },
            { "ResultsPlaceholder", "「スキャン」を実行すると未参照テクスチャの一覧が表示されます。" },
            { "ResultsNoneFound", "削除対象の未参照テクスチャはありませんでした。" },
            { "ResultsSummary", "未参照: {0} 件 (削除選択: {1} 件)" },
            { "SelectAll", "すべて選択" },
            { "DeselectAll", "すべて解除" },
            { "PingAsset", "選択" },

            // セクションヘルプ（見出し右の「？」のツールチップ）
            { "HelpTarget", "このフォルダ内（サブフォルダ含む）のテクスチャのうち、使用元フォルダのどのアセットからも参照されていないものを削除候補として検出します。

保護するサブフォルダ内のテクスチャは削除候補から除外されます。" },
            { "HelpReferences", "使用元フォルダ配下の全アセット（マテリアルに限らず、サブフォルダ含む）からの参照を調べます。

判定から除外するサブフォルダ配下のアセットは、参照の起点としてカウントしません。整理するフォルダが使用元フォルダのサブフォルダの場合は自動で追加されます。

注意: 使用元フォルダ外のアセットが参照しているテクスチャも未参照とみなされ、削除対象になります。" },
            { "HelpOptions", "削除方法を選びます。
・ゴミ箱へ移動: 削除したテクスチャは OS のゴミ箱へ移動します（復元可能）。
・完全に削除: 削除したテクスチャは完全に削除されます（復元不可）。" },
            { "HelpResults", "「スキャン」を実行すると、未参照と判定されたテクスチャの一覧がここに表示されます。チェックを外したテクスチャは削除されません。

各行の「選択」で Project ウィンドウ上の該当アセットをハイライトできます。" },

            // フッター
            { "ButtonScan", "スキャン" },
            { "ButtonDelete", "削除を実行" },

            // ステータス & エラー
            { "StatusReady", "Ready" },
            { "ErrorInvalidFolders", "整理するフォルダと、1つ以上の使用元フォルダを指定してください。" },
            { "ErrorNotAFolder", "※ フォルダアセットを指定してください。" },
            { "ScanSuccess", "スキャン完了: 全テクスチャ {0} 件中、未参照 {1} 件" },
            { "DeleteSuccessTrash", "{0} 件のテクスチャをゴミ箱へ移動しました。" },
            { "DeleteSuccessDirect", "{0} 件のテクスチャを完全に削除しました。" },
            { "DeletePartialFailed", "{0} 件削除、{1} 件失敗（コンソール参照）。" },

            // 確認ダイアログ
            { "DialogDeleteTitle", "未参照テクスチャの削除" },
            { "DialogDeleteMessageTrash", "{0} 件のテクスチャをゴミ箱へ移動します。よろしいですか？" },
            { "DialogDeleteMessageDirect", "{0} 件のテクスチャを完全に削除します。よろしいですか？（※復元できません）" },
            { "DialogProceed", "実行" },
            { "DialogCancel", "キャンセル" },
        };

        private static readonly Dictionary<string, string> StringsEn = new Dictionary<string, string>
        {
            // Header
            { "WindowTitle", "Folder Cleaner" },
            { "HeaderTitle", "Folder Cleaner" },
            { "LangButton", "JA" },
            { "LangButtonTooltip", "日本語に切り替え" },
            { "ReloadVersionTooltip", "Recheck for updates" },
            { "VerChecking", "Checking..." },
            { "VerUpdateAvailable", "Update {0}" },
            { "VerError", "Cannot fetch latest version" },

            // Section 1: Target Textures
            { "SectionTarget", "Textures to Clean (Deletion Target)" },
            { "TargetFolderLabel", "Target Folder" },
            { "TargetFolderHint", "Select the folder where you want to detect and clean unused textures." },
            { "ProtectedSubfoldersTitle", "Protected Subfolders (Exclude from deletion)" },
            { "ProtectedSubfoldersHint", "Textures in these subfolders will not be targeted for deletion." },
            { "AddProtectedSubfolder", "+ Add Protected Subfolder" },

            // Section 2: Reference Sources
            { "SectionReferences", "Reference Sources (Keep Criteria)" },
            { "SourceFoldersTitle", "Reference Sources (Materials, Prefabs, etc.)" },
            { "SourceFoldersHint", "Textures referenced by assets in these folders will be kept." },
            { "AddSourceFolder", "+ Add Reference Source" },
            { "ExcludedFoldersTitle", "Excluded Subfolders (Ignored in check)" },
            { "ExcludedFoldersHint", "References from assets in these folders are ignored (prevents self-reference)." },
            { "AddExcludedFolder", "+ Add Excluded Subfolder" },

            // Section 3: Delete Options
            { "SectionOptions", "Delete Options" },
            { "OptionTrash", "Move to Trash (Recommended)" },
            { "OptionDelete", "Delete Permanently" },
            { "OptionTrashHint", "Deleted textures will be moved to OS trash (recoverable)." },
            { "OptionDeleteHint", "Textures will be permanently removed from disk (cannot recover)." },

            // Section 4: Results
            { "SectionResults", "Scan Results" },
            { "ResultsPlaceholder", "Click 'Scan' to find unreferenced textures." },
            { "ResultsNoneFound", "No unreferenced textures found." },
            { "ResultsSummary", "Unreferenced: {0} ({1} selected)" },
            { "SelectAll", "Select All" },
            { "DeselectAll", "Deselect All" },
            { "PingAsset", "Ping" },

            // Section help (tooltip of the "?" next to each section title)
            { "HelpTarget", "Textures in this folder (including subfolders) that are not referenced by any asset in the reference sources are detected as deletion candidates.

Textures in protected subfolders are excluded from the candidates." },
            { "HelpReferences", "References from every asset under the reference sources (not only materials, including subfolders) are checked.

Assets under excluded subfolders are not used as reference origins. If the target folder is a subfolder of a reference source, it is added here automatically.

Note: textures referenced only by assets outside the reference sources are also treated as unreferenced and become deletion targets." },
            { "HelpOptions", "Choose how textures are deleted.
- Move to Trash: moved to the OS trash (recoverable).
- Delete Permanently: removed permanently (cannot be recovered)." },
            { "HelpResults", "After running 'Scan', textures judged as unreferenced are listed here. Unchecked textures will not be deleted.

Use 'Ping' on each row to highlight the asset in the Project window." },

            // Footer
            { "ButtonScan", "Scan" },
            { "ButtonDelete", "Delete Selected" },

            // Status & Errors
            { "StatusReady", "Ready" },
            { "ErrorInvalidFolders", "Please specify a Target Folder and at least one Reference Source." },
            { "ErrorNotAFolder", "※ Please select a folder asset." },
            { "ScanSuccess", "Scan complete: {1} unreferenced of {0} textures" },
            { "DeleteSuccessTrash", "{0} texture(s) moved to trash." },
            { "DeleteSuccessDirect", "{0} texture(s) permanently deleted." },
            { "DeletePartialFailed", "{0} deleted, {1} failed (see console)." },

            // Confirm Dialog
            { "DialogDeleteTitle", "Delete Unreferenced Textures" },
            { "DialogDeleteMessageTrash", "{0} texture(s) will be moved to trash. Proceed?" },
            { "DialogDeleteMessageDirect", "{0} texture(s) will be permanently deleted. Proceed? (Cannot recover)" },
            { "DialogProceed", "Proceed" },
            { "DialogCancel", "Cancel" },
        };
    }
}
