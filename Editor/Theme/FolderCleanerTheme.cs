using UnityEngine;
using UnityEditor;

namespace dennokoworks.FolderCleaner
{
    /// <summary>
    /// dennoko.dev カラースキーマに基づくテーマ定義（フローティングデザイン）。
    /// OnGUI の先頭で Initialize() を呼び出すことで、スタイルを遅延初期化する。
    ///
    /// このクラスは partial で 3 ファイルに分割されている:
    ///   - FolderCleanerTheme.cs               … カラー定義・ライフサイクル・テクスチャ/色ユーティリティ（本ファイル）
    ///   - FolderCleanerTheme.Styles.cs        … GUIStyle 定義と BuildStyles
    ///   - FolderCleanerTheme.EditorOverride.cs … ライト/ダークモード対応の EditorStyles 一時上書き
    /// </summary>
    internal static partial class FolderCleanerTheme
    {
        // ─── Colors ──────────────────────────────────────────────────────────

        // theme.surface (Neutral Layer)
        public static readonly Color Surface0 = Hex(0x121212); // app background
        public static readonly Color Surface1 = Hex(0x1e1e1e); // cards, inputs
        public static readonly Color Surface2 = Hex(0x2c2c2c); // hover, toolbar

        // theme.outline
        public static readonly Color Outline = Hex(0x3a3a3a);

        // theme.typography
        public static readonly Color TextPrimary   = Hex(0xffffff);
        public static readonly Color TextSecondary = Hex(0xcccccc);
        public static readonly Color TextTertiary  = Hex(0xaaaaaa);
        public static readonly Color TextDisabled  = Hex(0x555555);

        // theme.semantic
        public static readonly Color SemanticError   = Hex(0x9b1b30);
        public static readonly Color SemanticWarning = Hex(0xffb74d);
        public static readonly Color SemanticSuccess = Hex(0x4caf50);
        public static readonly Color SemanticInfo    = Hex(0x64b5f6);

        // theme.interaction
        public static readonly Color Accent       = Color.white;
        public static readonly Color HoverOverlay = new Color(1f, 1f, 1f, 0.05f);

        // ─── Cached Textures ─────────────────────────────────────────────────

        private static Texture2D _texSurface0;
        private static Texture2D _texSurface1;
        private static Texture2D _texSurface2;
        private static Texture2D _texCard;        // Surface1 fill + Outline border (3x3)
        private static Texture2D _texAccentCard;  // Surface2 fill + Outline border (3x3)
        private static Texture2D _texSearchField; // Input fields background (3x3 bordered)

        // ─── Lifecycle State ─────────────────────────────────────────────────

        private static bool _initialized;
        private static bool _lastIsProSkin;

        /// <summary>OnGUI の先頭で呼び出す。初回のみスタイルを構築する。</summary>
        public static void Initialize()
        {
            bool currentProSkin = EditorGUIUtility.isProSkin;
            if (_initialized && _lastIsProSkin != currentProSkin)
            {
                DisposeTextures();
            }
            _lastIsProSkin = currentProSkin;

            if (_initialized) return;
            _initialized = true;
            EnsureTextures();
            BuildStyles();
        }

        private static void EnsureTextures()
        {
            if (!_texSurface0)   _texSurface0   = MakeTex(Surface0);
            if (!_texSurface1)   _texSurface1   = MakeTex(Surface1);
            if (!_texSurface2)   _texSurface2   = MakeTex(Surface2);
            if (!_texCard)       _texCard       = MakeBorderedTex(Surface1, Outline);
            if (!_texAccentCard) _texAccentCard = MakeBorderedTex(Surface2, Outline);
            if (!_texSearchField) _texSearchField = MakeBorderedTex(Surface2, Hex(0x5a5a5a));
        }

        /// <summary>ステータスレベル（0=info / 1=success / 2=error）に対応するスタイルを返す。</summary>
        public static GUIStyle GetStatusStyle(int statusLevel)
        {
            return statusLevel switch
            {
                1 => StatusSuccessStyle, // success
                2 => StatusErrorStyle,   // error
                _ => StatusInfoStyle,    // info / default
            };
        }

        /// <summary>テクスチャと状態を明示破棄する（テーマ切り替えやドメインリロード時に安全にクリーンアップするため）。</summary>
        internal static void DisposeTextures()
        {
            PopEditorTheme();

            if (_texSurface0) Object.DestroyImmediate(_texSurface0);
            if (_texSurface1) Object.DestroyImmediate(_texSurface1);
            if (_texSurface2) Object.DestroyImmediate(_texSurface2);
            if (_texCard)     Object.DestroyImmediate(_texCard);
            if (_texAccentCard) Object.DestroyImmediate(_texAccentCard);
            if (_texSearchField) Object.DestroyImmediate(_texSearchField);

            _texSurface0   = null;
            _texSurface1   = null;
            _texSurface2   = null;
            _texCard       = null;
            _texAccentCard = null;
            _texSearchField = null;
            _initialized   = false;
            _backups       = null;
        }

        // ─── Style Utilities ─────────────────────────────────────────────────

        /// <summary>
        /// GUIStyle の全 state の textColor を同一色に固定する。
        /// EditorStyles.* を継承したスタイルはライトモードの色を引き継ぐため、
        /// hover/active/focused/on* を含む全 state を明示設定して上書きする。
        /// </summary>
        private static void FixAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor    = color;
            style.hover.textColor     = color;
            style.active.textColor    = color;
            style.focused.textColor   = color;
            style.onNormal.textColor  = color;
            style.onHover.textColor   = color;
            style.onActive.textColor  = color;
            style.onFocused.textColor = color;
        }

        private static void FixAllStateBackgrounds(GUIStyle style, Texture2D tex)
        {
            style.normal.background    = tex;
            style.hover.background     = tex;
            style.active.background    = tex;
            style.focused.background   = tex;
            style.onNormal.background  = tex;
            style.onHover.background   = tex;
            style.onActive.background  = tex;
            style.onFocused.background = tex;
        }

        // ─── Texture Utilities ───────────────────────────────────────────────

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Texture2D MakeBorderedTex(Color fillColor, Color borderColor)
        {
            const int size = 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y,
                        (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                            ? borderColor
                            : fillColor);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.hideFlags  = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >>  8) & 0xFF) / 255f,
            ( rgb        & 0xFF) / 255f);
    }
}
