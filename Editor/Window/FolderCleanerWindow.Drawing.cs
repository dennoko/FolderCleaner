using UnityEngine;
using UnityEditor;

namespace dennokoworks.FolderCleaner
{
    /// <summary>FolderCleanerWindow の IMGUI 描画処理（フローティングデザイン準拠）。</summary>
    public partial class FolderCleanerWindow
    {
        // ─── ヘッダー ──────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Folder Cleaner", FolderCleanerTheme.TitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DrawSeparator();
        }

        // ─── 設定エリア ────────────────────────────────────────────────────
        private void DrawSettingsArea()
        {
            GUILayout.BeginVertical();

            DrawSection("フォルダ設定", FolderSettingsHelp, DrawFolderSettings);
            DrawSection("オプション", OptionsHelp, DrawOptions);
            DrawSection("結果", ResultsHelp, DrawResults);

            GUILayout.EndVertical();
        }

        // ─── セクションのヘルプ文（右上「？」のツールチップ） ──────────────────
        private const string FolderSettingsHelp =
            "参照元フォルダ配下の全アセット（マテリアルに限らず）からの参照を調べ、" +
            "テクスチャ検索フォルダ内でどこからも参照されていないテクスチャを検出します。検索はサブフォルダを含みます。\n\n" +
            "除外サブフォルダ配下のアセットは参照の起点としてカウントしません" +
            "（例: 参照元に A/ を指定し、A/Texture/ を除外すると、A/Texture/ 内のテクスチャは A/Texture/ 以外からの参照のみで判定されます）。";

        private const string OptionsHelp =
            "削除方法を選びます。\n" +
            "・ゴミ箱へ移動: 削除したテクスチャは OS のゴミ箱へ移動します（復元可能）。\n" +
            "・完全に削除: 削除したテクスチャは完全に削除されます（復元不可）。\n\n" +
            "注意: 参照元フォルダ外のアセットが参照しているテクスチャも、ここでは未参照とみなされ削除対象になります。";

        private const string ResultsHelp =
            "「スキャン」を実行すると、未参照と判定されたテクスチャの一覧がここに表示されます。" +
            "各行の「選択」でプロジェクトウィンドウ上の該当アセットをハイライトできます。";

        private void DrawFolderSettings()
        {
            // テクスチャ検索フォルダ・参照元フォルダの変更を検知し、
            // テクスチャ検索フォルダが参照元のサブフォルダなら自動で除外へ追加する。
            EditorGUI.BeginChangeCheck();

            _textureFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "テクスチャ検索フォルダ", _textureFolder, typeof(DefaultAsset), false);
            if (_textureFolder != null && GetValidFolderPath(_textureFolder) == null)
                GUILayout.Label("※ テクスチャ検索フォルダにはフォルダを指定してください。", FolderCleanerTheme.SecondaryTextStyle);

            EditorGUILayout.Space(6);
            GUILayout.Label("参照元フォルダ（複数指定可）", FolderCleanerTheme.CaptionStyle);
            DrawFolderList(_sourceFolders, "＋ 参照元フォルダを追加");

            if (EditorGUI.EndChangeCheck())
                AutoExcludeTextureFolderIfNeeded();

            EditorGUILayout.Space(6);
            GUILayout.Label("除外サブフォルダ（参照元としてカウントしない）", FolderCleanerTheme.CaptionStyle);
            DrawFolderList(_excludedFolders, "＋ 除外サブフォルダを追加");
        }

        /// <summary>DefaultAsset フォルダのリストを編集する UI（行ごとに削除ボタン、末尾に追加ボタン）。</summary>
        private void DrawFolderList(System.Collections.Generic.List<DefaultAsset> list, string addLabel)
        {
            int removeIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                GUILayout.BeginHorizontal();
                list[i] = (DefaultAsset)EditorGUILayout.ObjectField(list[i], typeof(DefaultAsset), false);
                if (GUILayout.Button("✕", FolderCleanerTheme.MiniButtonStyle, GUILayout.Width(24)))
                    removeIndex = i;
                GUILayout.EndHorizontal();

                if (list[i] != null && GetValidFolderPath(list[i]) == null)
                    GUILayout.Label("※ フォルダを指定してください。", FolderCleanerTheme.SecondaryTextStyle);
            }
            if (removeIndex >= 0)
                list.RemoveAt(removeIndex);

            if (GUILayout.Button(addLabel, FolderCleanerTheme.MiniButtonStyle))
                list.Add(null);
        }

        private void DrawOptions()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("削除方法", FolderCleanerTheme.SecondaryTextStyle, GUILayout.Width(70));
            if (GUILayout.Toggle(_moveToTrash, "ゴミ箱へ移動", FolderCleanerTheme.MiniButtonLeftStyle))
                _moveToTrash = true;
            if (GUILayout.Toggle(!_moveToTrash, "完全に削除", FolderCleanerTheme.MiniButtonRightStyle))
                _moveToTrash = false;
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            GUILayout.Label(
                _moveToTrash
                    ? "削除したテクスチャは OS のゴミ箱へ移動します（復元可能）。"
                    : "削除したテクスチャは完全に削除されます（復元不可）。",
                FolderCleanerTheme.SecondaryTextStyle);
        }

        private void DrawResults()
        {
            if (!_hasScanned)
            {
                GUILayout.Label("「スキャン」を実行すると未参照テクスチャの一覧が表示されます。",
                    FolderCleanerTheme.SecondaryTextStyle);
                return;
            }

            GUILayout.Label($"未参照テクスチャ: {_unreferenced.Count} 件", FolderCleanerTheme.CaptionStyle);

            if (_unreferenced.Count == 0)
            {
                GUILayout.Label("削除対象のテクスチャはありませんでした。", FolderCleanerTheme.SecondaryTextStyle);
                return;
            }

            _resultScrollPosition = EditorGUILayout.BeginScrollView(
                _resultScrollPosition, GUILayout.Height(160));
            foreach (var path in _unreferenced)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(System.IO.Path.GetFileName(path), FolderCleanerTheme.SecondaryTextStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("選択", FolderCleanerTheme.MiniButtonStyle, GUILayout.Width(44)))
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(path);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ─── フッター ──────────────────────────────────────────────────────
        private void DrawFooter()
        {
            GUILayout.BeginVertical(FolderCleanerTheme.CardStyle);

            bool foldersValid = GetValidFolderPath(_textureFolder) != null
                             && CollectValidFolderPaths(_sourceFolders).Count > 0;

            using (new EditorGUI.DisabledGroupScope(!foldersValid))
            {
                if (GUILayout.Button("スキャン", FolderCleanerTheme.ActionButtonStyle))
                    Scan();
            }

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledGroupScope(!_hasScanned || _unreferenced.Count == 0))
            {
                if (GUILayout.Button("削除", FolderCleanerTheme.SecondaryButtonStyle))
                    Delete();
            }

            GUILayout.EndVertical();
        }

        // ─── ステータスバー ────────────────────────────────────────────────
        private void DrawStatusBar()
        {
            GUILayout.Box(_statusMessage, GetStatusStyle(_statusType), GUILayout.ExpandWidth(true));
        }

        private GUIStyle GetStatusStyle(StatusType type)
        {
            return type switch
            {
                StatusType.Success => FolderCleanerTheme.StatusSuccessStyle,
                StatusType.Error   => FolderCleanerTheme.StatusErrorStyle,
                _                  => FolderCleanerTheme.StatusInfoStyle,
            };
        }

        // ─── 共通ヘルパー ──────────────────────────────────────────────────
        private void DrawSection(string title, string helpTooltip, System.Action content)
        {
            GUILayout.BeginVertical(FolderCleanerTheme.CardStyle);

            // 見出し行: 左にタイトル、右上に「？」（マウスオーバーで説明をツールチップ表示）
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, FolderCleanerTheme.SectionHeaderStyle);
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(helpTooltip))
                GUILayout.Label(new GUIContent("？", helpTooltip),
                    FolderCleanerTheme.HelpIconStyle, GUILayout.Width(18), GUILayout.Height(16));
            GUILayout.EndHorizontal();

            DrawSeparator();
            content?.Invoke();
            GUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, FolderCleanerTheme.Outline);
            EditorGUILayout.Space(4);
        }
    }
}
