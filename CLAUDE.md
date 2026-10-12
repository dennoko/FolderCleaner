# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

FolderCleaner is a Unity Editor extension (targets Unity 2022.3+) that deletes textures in a
designated folder that are not referenced by any asset under designated source folders. Reference
detection is shader-agnostic — it works for textures referenced through materials of any shader.

This package lives under `Assets/dennokoworks/FolderCleaner` inside a host VRChat/Unity project.
There is no standalone build/lint/test tooling: code is compiled by the Unity Editor on import, and
the tool is exercised from the menu **dennokoworks > Folder Cleaner**.

## Architecture

All code is editor-only and sits in `namespace dennokoworks.FolderCleaner` under [Editor/](Editor/).

- **Core (pure logic, no UI):**
  - [Editor/Core/FolderTextureScanner.cs](Editor/Core/FolderTextureScanner.cs) — `Scan(textureFolderPath, sourceFolderPaths, excludedFolderPaths, excludedTexturePaths)` returns a `ScanResult`. It collects all `t:Texture` under the texture folder, then walks `AssetDatabase.GetDependencies(path, recursive: true)` for every asset under the (multiple) source folders to build the referenced set. Recursion is what makes it shader-agnostic (prefab → material → texture is followed transitively). Assets under `excludedFolderPaths` are skipped as scan *origins*, and textures under `excludedTexturePaths` are excluded from deletion candidates.
  - [Editor/Core/TextureRemover.cs](Editor/Core/TextureRemover.cs) — `Remove(paths, moveToTrash)` returns a `RemoveResult`. Wraps deletion in `StartAssetEditing`/`StopAssetEditing` + `Refresh`, choosing `MoveAssetToTrash` vs `DeleteAsset`.
- **UI (UI Toolkit, dennokoworks floating design):**
  - [Editor/UI/FolderCleanerWindow.uxml](Editor/UI/FolderCleanerWindow.uxml) — UXML layout tree (Header, ScrollView cards, Footer, Status bar).
  - [Editor/UI/DennokoTheme.uss](Editor/UI/DennokoTheme.uss) — Base theme USS stylesheet.
  - [Editor/UI/FolderCleanerWindow.uss](Editor/UI/FolderCleanerWindow.uss) — Tool-specific custom stylesheet.
  - [Editor/UI/DennokoUIFont.cs](Editor/UI/DennokoUIFont.cs) — Meiryo SDF font management with full Japanese text warmup.
- **Window:**
  - [Editor/Window/FolderCleanerWindow.cs](Editor/Window/FolderCleanerWindow.cs) — `CreateGUI()` entry point, element binding, dynamic list management, and bridging UI events to Core logic.
- **Version Check:**
  - [Editor/VersionCheck/DennokoVersionChecker.cs](Editor/VersionCheck/DennokoVersionChecker.cs) — GitHub version checking utility.
  - [Editor/VersionCheck/FolderCleanerVersion.cs](Editor/VersionCheck/FolderCleanerVersion.cs) — Tool version cache and background updater.
  - [version.json](version.json) — Current release version metadata.
- **Localization:**
  - [Editor/Localization/FolderCleanerLoc.cs](Editor/Localization/FolderCleanerLoc.cs) — Japanese and English string dictionary and language switcher.

## UI / theme conventions (dennokoworks floating design)

The window UI follows the dennokoworks design system (UI Toolkit / UXML / USS):

1. Root element must have `.dennoko-root` class and call `DennokoUIFont.Apply(root)`.
2. All custom styles must be prefixed with `.dennoko-root` (specificity 0,2,0) and use `--dennoko-*` color variables.
3. Active/selected buttons must use `.dennoko-button-active` (blue border).
4. Textures/labels support bilingual switching (JA/EN) via `FolderCleanerLoc`.

## Conventions

- Code comments and default user-facing strings are in Japanese, with English localization support.
