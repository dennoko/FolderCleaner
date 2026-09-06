# FolderCleaner — Unity 6 移行調査

- 調査日: 2026-09-06
- 現行: Unity 2022.3.22f1 / Built-in RP
- 目標: Unity 6 (6000.0 LTS) / **BiRP 維持**
- 共通調査: [`unity6-migration-overview.md`](unity6-migration-overview.md)

## 判定

✅ **対応済** — Unity 6 非対応の API は **0 件**。修正すべきコードはない。
外部依存も **なし**。IMGUI テーマの見た目確認のみで移行完了する。

## 構成

| 項目 | 内容 |
|---|---|
| 規模 | C# 7 ファイル / 約 1,162 行 |
| asmdef | なし（プロジェクト既定アセンブリ） |
| エントリ | `MenuItem("dennokoworks/Folder Cleaner")` |
| UI | IMGUI（`FolderCleanerTheme`） |
| 外部依存 | **なし**（VRChat SDK / NDMF / lilToon すべて非依存） |

外部依存がなく、レンダリング API も使わないため、**Unity 6 移行リスクが最も低いツールの一つ**。

## 検出事項

### 1. IMGUI テーマ: 共有 `EditorStyles` の書き換え（🔍 視覚検証のみ）

`Editor/Theme/FolderCleanerTheme.cs:151,161`

```csharp
var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
```

共通調査 3.3 節と同じパターン。`PushEditorTheme()` / `PopEditorTheme()` で
`EditorStyles` と `GUI.skin` をグローバルに書き換え、対称に復元している。

- API（`EditorStyles`, `GUIStyle`, `GUIStyleState`, `RectOffset`）は Unity 6 でも不変。
  **コンパイルは通る。**
- Unity 6 は Editor スキンが刷新されているため、`border` / `padding` の前提が崩れ、
  余白・枠線がずれる可能性がある。
- 生成テクスチャは `mipChain: false` のため、Unity 6 の mipmap limit 変更の影響を受けない。

**対応**: Unity 6 上でウィンドウを開き、目視確認。崩れていれば `RectOffset` の数値のみ調整する。
構造変更は不要。

### 2. `AssetDatabase` によるフォルダ操作（✅ 影響なし）

未使用アセットの検出・削除に `AssetDatabase` API を使用。
`AssetDatabase.GetSubFolders` / `FindAssets` / `DeleteAsset` / `Refresh` などは
Unity 6 で変更なし。**修正不要。**

> **参考**: Unity 6 では Package Manager のグローバルキャッシュ構成が変わり、
> `UPM_CACHE_PATH` / `UPM_NPM_CACHE_PATH` 環境変数が廃止された（→ `UPM_CACHE_ROOT`）。
> ただしこれは `Packages/` 配下の話であり、
> 本ツールが対象とする `Assets/` 配下のフォルダ操作には影響しない。

### 3. ファイル I/O（✅ 影響なし）

`System.IO` による物理ファイル操作。C# 言語バージョンは
2022.3 / Unity 6 ともに **C# 9 / .NET Standard 2.1** で変更なし。**修正不要。**

## 非該当の確認

| 確認項目 | 結果 |
|---|---|
| `Object.FindObjectsOfType` / `FindObjectOfType` | **なし** |
| UnityEditor 内部 API へのリフレクション | **なし** |
| `GraphicsFormat.DepthAuto` / `ShadowAuto` / `VideoAuto` | **なし** |
| UI Toolkit（`ExecuteDefaultAction` / `UxmlFactory` 等） | **なし**（IMGUI のみ） |
| `RenderTexture` / `Graphics.Blit` / Compute シェーダ | **なし** |
| カスタムシェーダ | **なし** |
| `Lightmapping` / 物理 API | **なし** |
| VRChat SDK / NDMF / lilToon 依存 | **なし** |
| バージョン定義 `#if UNITY_*` | **なし** |
| TextCore フォント生成（`DennokoUIFont`） | **なし** |

**BiRP → Unity 6 の描画系変更の影響は完全にゼロ**（レンダリング API を一切使っていない）。

## 移行手順

### フェーズ 1（Unity 2022.3.22f1 のまま実施可）

- なし。修正すべき箇所がない。

### フェーズ 2（Unity 6 検証プロジェクト・先行実施可）

外部依存がゼロのため、Unity 6 の空プロジェクトに `FolderCleaner/` を
コピーするだけで完全な検証ができる。**共通調査フェーズ 2 の先行検証対象。**

- [ ] コンパイルエラー・警告が 0 件
- [ ] 下記チェックリストで動作確認

**これで本ツールの移行作業は完了**。フェーズ 3 以降で追加作業は発生しない。

## 検証チェックリスト（Unity 6）

- [ ] コンパイルエラー・警告が 0 件
- [ ] `dennokoworks/Folder Cleaner` からウィンドウが開く
- [ ] 対象フォルダの走査が完了し、未使用アセットが正しく列挙される
- [ ] 参照ありのアセットが誤って未使用と判定されていない（`AssetDatabase` 依存の確認）
- [ ] 削除プレビューが正しく表示される
- [ ] 実際の削除が成功し、`.meta` ファイルも一緒に削除される
- [ ] 削除後に `AssetDatabase.Refresh` が走り、Project ウィンドウに反映される
- [ ] IMGUI テーマ: テキストフィールド / Popup / ObjectField の枠線・余白・文字色が崩れていない
- [ ] ウィンドウを閉じた後、他の Editor ウィンドウのスタイルが元に戻っている（Push/Pop の対称性）
- [ ] 大量のアセットを含むフォルダでパフォーマンスが劣化していない
