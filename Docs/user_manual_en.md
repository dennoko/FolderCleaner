# Folder Cleaner User Guide

**Folder Cleaner** is a Unity Editor extension that identifies and deletes unreferenced texture assets within a specified target folder that are not used by any asset under designated reference source folders.

---

## 1. How to Launch

From the Unity Editor top menu bar, select:
- **dennokoworks** > **Folder Cleaner**

Upon launching, the dedicated editor window following the UI Toolkit (dennokoworks floating design) will open.

---

## 2. Header Features

- **Version Display & Update Check**:
  - The current version (e.g., `v1.0.0`) is displayed next to the window title. An automatic check for updates is performed on startup. When an update is available, the latest version number is highlighted in green; if the latest version cannot be fetched, "Cannot fetch latest version" is shown in orange.
  - Click the `↻` button on the right to manually recheck for updates at any time.
- **Language Switcher ([EN] / [JA])**:
  - Click the language button in the upper-right corner to instantly toggle the UI between Japanese and English (the setting is automatically saved).

---

## 3. Settings & Options

The window is organized into four sections: "Textures to Clean", "Reference Sources", "Delete Options", and "Scan Results".

### Textures to Clean (Deletion Target)
| Setting | Description |
| :--- | :--- |
| **Target Folder** | Select the folder where you want to detect and clean unused textures (textures in subfolders are also scanned). |
| **Protected Subfolders** | Specify subfolders within the Target Folder that should be protected from deletion. Click "+ Add Protected Subfolder" to add multiple folders. |

### Reference Sources (Keep Criteria)
| Setting | Description |
| :--- | :--- |
| **Reference Sources (Materials, Prefabs, etc.)** | Specify folders containing assets (e.g., `Assets/Materials`, `Assets/Prefabs`) that may use the textures. Textures referenced by assets in these folders will be preserved (kept). Click "+ Add Reference Source" to add multiple folders. |
| **Excluded Subfolders** | Specify subfolders to exclude from the reference scan (e.g., to prevent self-reference from falsely marking a texture as in-use). Click "+ Add Excluded Subfolder" to add multiple folders. |

> [!NOTE] **Automatic Exclusion Feature**
> If the "Target Folder" is located inside any of the "Reference Sources" folders, it is automatically added to "Excluded Subfolders" to prevent assets inside the texture folder itself from self-referencing and falsely counting as in-use.

### Delete Options
The currently selected mode is clearly indicated by a **blue border highlight** on the active button, as well as the explanation text directly below it.

| Mode | Description |
| :--- | :--- |
| **Move to Trash (Recommended)** | Deleted textures are moved to your operating system's trash/recycle bin (can be recovered later). |
| **Delete Permanently** | Deleted textures are permanently removed from the project and disk (cannot be recovered). |

---

## 4. Step-by-Step Instructions

### Step 1: Specify Folders
1. Drag and drop or pick the target folder into **"Target Folder"**.
2. Specify the folders containing assets that use textures into **"Reference Sources"**.
3. If necessary, configure **"Protected Subfolders"** or **"Excluded Subfolders"**.

### Step 2: Run Scan
1. Click the **"Scan"** button at the bottom of the window.
2. Once the scan is complete, the list and count of unreferenced textures will appear in the **"Scan Results"** section.
3. Click the **"Ping"** button on the right side of each item to highlight and inspect the asset in the Project window.
4. If there are textures you wish to keep, uncheck their checkboxes on the left (you can also use "Select All" and "Deselect All").

### Step 3: Execute Deletion
1. After confirming the selected textures for deletion, click the **"Delete Selected"** button at the bottom of the window.
2. A confirmation dialog will appear. Click **"Proceed"** to delete the selected files (or move them to the trash).

---

## 5. Important Notes

> [!WARNING] **Scope of Reference Checking**
> This tool only checks for references from assets located under the folders specified in **"Reference Sources"**.
> If an asset outside of the specified reference source folders (e.g., in another package or an unselected folder) references a texture, the tool will still consider it "unreferenced" and mark it for deletion.
> Before executing deletion, please make sure that your reference source paths cover all relevant project folders.
