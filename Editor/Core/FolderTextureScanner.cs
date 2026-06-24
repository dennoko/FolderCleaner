using System.Collections.Generic;
using UnityEditor;

namespace dennokoworks.FolderCleaner
{
    /// <summary>スキャン結果。未参照テクスチャのパス一覧と、走査したテクスチャ総数を持つ。</summary>
    internal struct ScanResult
    {
        public List<string> UnreferencedTexturePaths;
        public int TotalTextureCount;
    }

    /// <summary>
    /// テクスチャフォルダ内のテクスチャのうち、参照元フォルダ配下の全アセットから
    /// 参照されていないものを算出する純粋なロジック（UI 非依存）。
    /// </summary>
    internal static class FolderTextureScanner
    {
        /// <param name="textureFolderPath">削除候補テクスチャの検索フォルダ（Assets 相対パス、サブフォルダ含む）。</param>
        /// <param name="sourceFolderPaths">参照元アセットの検索フォルダ一覧（Assets 相対パス、サブフォルダ含む）。</param>
        /// <param name="excludedFolderPaths">参照元スキャンの起点から除外するサブフォルダ一覧（Assets 相対パス、サブフォルダ含む）。null 可。</param>
        /// <param name="excludedTextureFolderPaths">削除候補テクスチャの検索から除外するサブフォルダ一覧（Assets 相対パス、サブフォルダ含む）。null 可。</param>
        public static ScanResult Scan(
            string textureFolderPath,
            IReadOnlyList<string> sourceFolderPaths,
            IReadOnlyList<string> excludedFolderPaths,
            IReadOnlyList<string> excludedTextureFolderPaths)
        {
            // 削除候補テクスチャ（サブフォルダ含む、ただし除外指定されたフォルダを除く）
            var texturePaths = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { textureFolderPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (IsUnderAny(path, excludedTextureFolderPaths)) continue;
                texturePaths.Add(path);
            }

            // 参照元フォルダ配下の全アセット（型・シェーダー非依存）が参照するアセット集合を構築。
            // 複数フォルダが重複・入れ子でも HashSet と GUID 走査で自然に重複排除される。
            var referenced = new HashSet<string>();
            var sourceArray = new string[sourceFolderPaths.Count];
            for (int i = 0; i < sourceFolderPaths.Count; i++) sourceArray[i] = sourceFolderPaths[i];

            foreach (var guid in AssetDatabase.FindAssets("t:Object", sourceArray))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;

                // 除外サブフォルダ配下のアセットは「参照の起点」にしない（推移参照の終端としては依然カウントされうる）。
                if (IsUnderAny(assetPath, excludedFolderPaths))
                    continue;

                // recursive=true で推移的な参照（prefab→material→texture 等）も辿る。
                // GetDependencies は対象アセット自身も含むため、自己参照を除外する。
                foreach (var dep in AssetDatabase.GetDependencies(assetPath, true))
                    if (dep != assetPath)
                        referenced.Add(dep);
            }

            var unreferenced = new List<string>();
            foreach (var path in texturePaths)
                if (!referenced.Contains(path))
                    unreferenced.Add(path);
            unreferenced.Sort();

            return new ScanResult
            {
                UnreferencedTexturePaths = unreferenced,
                TotalTextureCount = texturePaths.Count,
            };
        }

        /// <summary>assetPath が folders のいずれかのフォルダ自身、またはその配下にあれば true。</summary>
        private static bool IsUnderAny(string assetPath, IReadOnlyList<string> folders)
        {
            if (folders == null) return false;
            foreach (var folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                if (assetPath == folder || assetPath.StartsWith(folder + "/"))
                    return true;
            }
            return false;
        }
    }
}
