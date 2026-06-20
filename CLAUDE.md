# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

FolderCleaner is a Unity Editor extension (targets Unity 2022.3+) that deletes textures in a
designated folder that are not referenced by any asset under a designated source folder. Reference
detection is shader-agnostic — it works for textures referenced through materials of any shader.

This package lives under `Assets/dennokoworks/FolderCleaner` inside a host VRChat/Unity project.
There is no standalone build/lint/test tooling: code is compiled by the Unity Editor on import, and
the tool is exercised from the menu **dennokoworks > Folder Cleaner**. The requirements spec is in
[Docs/Impl/requirements.md](Docs/Impl/requirements.md) (Japanese).

## Architecture

All code is editor-only and sits in `namespace dennokoworks.FolderCleaner` under [Editor/](Editor/).
The design deliberately keeps scan/delete logic UI-independent so it can be reasoned about and
tested in isolation from IMGUI.

- **Core (pure logic, no UI):**
  - [Editor/Core/FolderTextureScanner.cs](Editor/Core/FolderTextureScanner.cs) — `Scan(textureFolderPath, sourceFolderPaths, excludedFolderPaths)` returns a `ScanResult`. It collects all `t:Texture` under the texture folder, then walks `AssetDatabase.GetDependencies(path, recursive: true)` for every asset under the (multiple) source folders to build the referenced set. Recursion is what makes it shader-agnostic (prefab → material → texture is followed transitively). `GetDependencies` includes the asset itself, so self-references are excluded. Assets under `excludedFolderPaths` are skipped as scan *origins* (but can still be marked referenced transitively from a non-excluded asset) — this lets you point the source at `A/` while excluding `A/Texture/`.
  - [Editor/Core/TextureRemover.cs](Editor/Core/TextureRemover.cs) — `Remove(paths, moveToTrash)` returns a `RemoveResult`. Wraps deletion in `StartAssetEditing`/`StopAssetEditing` + `Refresh`, choosing `MoveAssetToTrash` vs `DeleteAsset`.
- **Window (UI, partial class `FolderCleanerWindow : EditorWindow`):**
  - [Editor/Window/FolderCleanerWindow.cs](Editor/Window/FolderCleanerWindow.cs) — state, `[MenuItem]` registration, `OnGUI` entry point, and the `Scan`/`Delete`/`SetStatus` business logic that bridges UI to Core.
  - [Editor/Window/FolderCleanerWindow.Drawing.cs](Editor/Window/FolderCleanerWindow.Drawing.cs) — all IMGUI drawing (`DrawHeader`/`DrawSettingsArea`/`DrawFooter`/`DrawStatusBar`).
- **Theme (partial class `FolderCleanerTheme`):** [Editor/Theme/](Editor/Theme/) — `FolderCleanerTheme.cs` (colors, texture/style lifecycle), `.Styles.cs` (GUIStyle definitions), `.EditorOverride.cs` (Push/Pop overrides of built-in `EditorStyles` for light/dark parity).

## UI / theme conventions (dennokoworks floating design)

The window UI follows the dennokoworks design system. When implementing or modifying any
EditorWindow/CustomEditor UI, **use the `dennokoworks_color_schema` skill** — it carries the color
palette, templates, and IMGUI techniques. The hard rules that this codebase already follows:

1. `OnGUI` must call `FolderCleanerTheme.Initialize()` then `PushEditorTheme()`, and `PopEditorTheme()` in a `finally`. Fill the background with `EditorGUI.DrawRect(..., Surface0)` first.
2. Build every `GUIStyle` from `new GUIStyle()` — never inherit from `EditorStyles.*` or `GUI.skin.*`. Inherited styles leak light-mode colors into unset states and break on theme switch. (`FixAllTextColors`/`FixAllStateBackgrounds` in `FolderCleanerTheme.cs` exist to force all 8 states explicitly.)
3. Guard cached textures with the Unity null check `if (!_tex) _tex = MakeTex(...)`; `_initialized` resets on domain reload so textures rebuild automatically. `DisposeTextures()` also rebuilds when the pro-skin flag flips.

## Conventions

- Code comments and user-facing strings are in Japanese; keep that consistent when editing.
- `.gitignore` ignores `*.meta`. Unity regenerates `.meta` files on import.
