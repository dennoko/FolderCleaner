using UnityEngine;

namespace dennokoworks.FolderCleaner
{
    /// <summary>FolderCleanerTheme の GUIStyle 定義と構築（BuildStyles）。</summary>
    internal static partial class FolderCleanerTheme
    {
        // Layout / Container
        public static GUIStyle CardStyle      { get; private set; } // sections (padding あり)
        public static GUIStyle CardOuterStyle { get; private set; } // ツールバー付き外枠 (padding なし)
        public static GUIStyle ToolbarStyle   { get; private set; } // ツールバー行

        // Typography
        public static GUIStyle TitleStyle            { get; private set; } // ウィンドウタイトル
        public static GUIStyle SectionHeaderStyle    { get; private set; } // 非トグルセクション見出し
        public static GUIStyle ToggleSectionOnStyle  { get; private set; } // トグル ON 時の見出し
        public static GUIStyle ToggleSectionOffStyle { get; private set; } // トグル OFF 時の見出し
        public static GUIStyle SecondaryTextStyle    { get; private set; } // 説明文
        public static GUIStyle CaptionStyle          { get; private set; } // 補足・メタデータ

        // Buttons
        public static GUIStyle ActionButtonStyle     { get; private set; } // Primary Action
        public static GUIStyle SecondaryButtonStyle  { get; private set; } // Secondary Action
        public static GUIStyle MiniButtonStyle       { get; private set; }
        public static GUIStyle MiniButtonLeftStyle   { get; private set; }
        public static GUIStyle MiniButtonRightStyle  { get; private set; }

        // Inspector / Toolbar
        public static GUIStyle InspectorRootStyle    { get; private set; }
        public static GUIStyle ToolbarButtonStyle    { get; private set; }

        // Status bar
        public static GUIStyle StatusInfoStyle    { get; private set; }
        public static GUIStyle StatusSuccessStyle { get; private set; }
        public static GUIStyle StatusErrorStyle   { get; private set; }

        private static void BuildStyles()
        {
            // ── Container ────────────────────────────────────────────────────

            CardStyle = new GUIStyle();
            CardStyle.normal.background = _texCard;
            CardStyle.border  = new RectOffset(1, 1, 1, 1);
            CardStyle.padding = new RectOffset(10, 10, 8, 8);
            CardStyle.margin  = new RectOffset(8, 8, 8, 8);

            CardOuterStyle = new GUIStyle();
            CardOuterStyle.normal.background = _texCard;
            CardOuterStyle.border  = new RectOffset(1, 1, 1, 1);
            CardOuterStyle.padding = new RectOffset(0, 0, 0, 0);
            CardOuterStyle.margin  = new RectOffset(8, 8, 8, 8);

            ToolbarStyle = new GUIStyle();
            ToolbarStyle.normal.background = _texSurface2;
            ToolbarStyle.padding = new RectOffset(6, 6, 4, 4);
            ToolbarStyle.margin  = new RectOffset(0, 0, 0, 0);

            // ── Typography ───────────────────────────────────────────────────
            // new GUIStyle() から構築してテーマ非依存とする。
            // EditorStyles.* を継承すると未設定の state にライトモード色が混入するため使用しない。

            TitleStyle = new GUIStyle();
            TitleStyle.fontStyle = FontStyle.Bold;
            TitleStyle.fontSize  = 14;
            TitleStyle.alignment = TextAnchor.MiddleLeft;
            FixAllTextColors(TitleStyle, TextPrimary);

            SectionHeaderStyle = new GUIStyle();
            SectionHeaderStyle.fontStyle = FontStyle.Bold;
            SectionHeaderStyle.fontSize  = 10;
            SectionHeaderStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(SectionHeaderStyle, TextTertiary);

            ToggleSectionOnStyle = new GUIStyle();
            ToggleSectionOnStyle.fontStyle = FontStyle.Bold;
            ToggleSectionOnStyle.fontSize  = 10;
            ToggleSectionOnStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(ToggleSectionOnStyle, TextPrimary);

            ToggleSectionOffStyle = new GUIStyle();
            ToggleSectionOffStyle.fontStyle = FontStyle.Bold;
            ToggleSectionOffStyle.fontSize  = 10;
            ToggleSectionOffStyle.margin    = new RectOffset(0, 0, 0, 2);
            FixAllTextColors(ToggleSectionOffStyle, TextTertiary);

            SecondaryTextStyle = new GUIStyle();
            SecondaryTextStyle.wordWrap = true;
            FixAllTextColors(SecondaryTextStyle, TextSecondary);

            CaptionStyle = new GUIStyle();
            CaptionStyle.fontSize = 9;
            FixAllTextColors(CaptionStyle, TextTertiary);

            // ── Toolbar Button ────────────────────────────────────────────────

            ToolbarButtonStyle = new GUIStyle();
            ToolbarButtonStyle.normal.background   = null;
            ToolbarButtonStyle.hover.background    = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            ToolbarButtonStyle.active.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            ToolbarButtonStyle.border    = new RectOffset(0, 0, 0, 0);
            ToolbarButtonStyle.margin    = new RectOffset(1, 1, 1, 1);
            ToolbarButtonStyle.padding   = new RectOffset(6, 6, 2, 2);
            ToolbarButtonStyle.fontSize  = 10;
            ToolbarButtonStyle.alignment = TextAnchor.MiddleCenter;
            ToolbarButtonStyle.normal.textColor    = TextTertiary;
            ToolbarButtonStyle.hover.textColor     = TextSecondary;
            ToolbarButtonStyle.active.textColor    = TextPrimary;
            ToolbarButtonStyle.focused.textColor   = TextTertiary;
            ToolbarButtonStyle.onNormal.textColor  = TextPrimary;
            ToolbarButtonStyle.onHover.textColor   = TextPrimary;
            ToolbarButtonStyle.onActive.textColor  = TextPrimary;
            ToolbarButtonStyle.onFocused.textColor = TextPrimary;

            // ── Inspector Root ────────────────────────────────────────────────

            InspectorRootStyle = new GUIStyle();
            InspectorRootStyle.normal.background = _texSurface0;
            InspectorRootStyle.margin   = new RectOffset(0, 0, 0, 0);
            InspectorRootStyle.padding  = new RectOffset(10, 10, 8, 8);
            InspectorRootStyle.overflow = new RectOffset(20, 20, 0, 0);

            // ── Buttons ──────────────────────────────────────────────────────
            // GUI.skin.button / EditorStyles.miniButton* を継承すると Unity の角丸・グラデーション・
            // scaledBackgrounds が引き継がれてフラットなテクスチャと混ざる。
            // そのため new GUIStyle() から全プロパティを明示的に構築する。

            ActionButtonStyle = new GUIStyle();
            ActionButtonStyle.normal.background  = _texAccentCard;
            ActionButtonStyle.hover.background   = MakeTex(Color.Lerp(Surface2, Color.white, 0.07f));
            ActionButtonStyle.active.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.15f));
            ActionButtonStyle.border       = new RectOffset(1, 1, 1, 1);
            ActionButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
            ActionButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
            ActionButtonStyle.fontSize     = 13;
            ActionButtonStyle.fontStyle    = FontStyle.Bold;
            ActionButtonStyle.fixedHeight  = 34;
            ActionButtonStyle.alignment    = TextAnchor.MiddleCenter;
            ActionButtonStyle.stretchWidth = true;
            FixAllTextColors(ActionButtonStyle, TextPrimary);

            SecondaryButtonStyle = new GUIStyle();
            SecondaryButtonStyle.normal.background = MakeBorderedTex(Surface1, Outline);
            SecondaryButtonStyle.hover.background  = _texAccentCard;
            SecondaryButtonStyle.active.background = MakeTex(Color.Lerp(Surface1, Color.white, 0.10f));
            SecondaryButtonStyle.border       = new RectOffset(1, 1, 1, 1);
            SecondaryButtonStyle.margin       = new RectOffset(4, 4, 2, 2);
            SecondaryButtonStyle.padding      = new RectOffset(6, 6, 3, 3);
            SecondaryButtonStyle.fontSize     = 11;
            SecondaryButtonStyle.fixedHeight  = 26;
            SecondaryButtonStyle.alignment    = TextAnchor.MiddleCenter;
            SecondaryButtonStyle.stretchWidth = true;
            SecondaryButtonStyle.normal.textColor   = TextSecondary;
            SecondaryButtonStyle.hover.textColor    = TextPrimary;
            SecondaryButtonStyle.active.textColor   = TextPrimary;
            SecondaryButtonStyle.focused.textColor  = TextSecondary;
            SecondaryButtonStyle.onNormal.textColor  = TextSecondary;
            SecondaryButtonStyle.onHover.textColor   = TextPrimary;
            SecondaryButtonStyle.onActive.textColor  = TextPrimary;
            SecondaryButtonStyle.onFocused.textColor = TextSecondary;

            MiniButtonStyle = BuildMiniButtonStyle();
            MiniButtonLeftStyle = BuildMiniButtonStyle();
            MiniButtonRightStyle = BuildMiniButtonStyle();

            // ── Status Bar ───────────────────────────────────────────────────

            var statusBase = new GUIStyle();
            statusBase.border    = new RectOffset(1, 1, 1, 1);
            statusBase.padding   = new RectOffset(8, 8, 5, 5);
            statusBase.margin    = new RectOffset(4, 4, 2, 2);
            statusBase.fontSize  = 11;
            statusBase.wordWrap  = true;
            statusBase.alignment = TextAnchor.MiddleLeft;

            StatusInfoStyle = new GUIStyle(statusBase);
            StatusInfoStyle.normal.background = _texSurface1;
            FixAllTextColors(StatusInfoStyle, TextSecondary);

            StatusSuccessStyle = new GUIStyle(statusBase);
            StatusSuccessStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticSuccess, 0.3f));
            FixAllTextColors(StatusSuccessStyle, SemanticSuccess);

            StatusErrorStyle = new GUIStyle(statusBase);
            StatusErrorStyle.normal.background = MakeTex(Color.Lerp(Surface1, SemanticError, 0.5f));
            FixAllTextColors(StatusErrorStyle, new Color(1f, 0.65f, 0.65f));
        }

        /// <summary>ミニボタン用スタイルを構築する（Left/Right セグメントも同一の外観で構成）。</summary>
        private static GUIStyle BuildMiniButtonStyle()
        {
            var style = new GUIStyle();
            style.normal.background = _texAccentCard;
            style.normal.textColor  = TextTertiary;
            style.hover.background  = MakeTex(Color.Lerp(Surface2, Color.white, 0.10f));
            style.hover.textColor   = TextSecondary;
            style.active.background = MakeTex(Color.Lerp(Surface2, Color.white, 0.18f));
            style.active.textColor  = TextPrimary;
            style.border      = new RectOffset(1, 1, 1, 1);
            style.margin      = new RectOffset(2, 2, 1, 1);
            style.padding     = new RectOffset(4, 4, 1, 2);
            style.fontSize    = 10;
            style.fixedHeight = 16;
            style.alignment   = TextAnchor.MiddleCenter;
            style.focused.textColor = TextTertiary;
            style.onNormal.textColor  = TextPrimary;
            style.onHover.textColor   = TextPrimary;
            style.onActive.textColor  = TextPrimary;
            style.onFocused.textColor = TextPrimary;
            return style;
        }
    }
}
