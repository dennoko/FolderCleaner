using System.Collections.Generic;
using UnityEditor;

namespace dennokoworks.FolderCleaner
{
    /// <summary>削除結果。成功件数と失敗したパス一覧を持つ。</summary>
    internal struct RemoveResult
    {
        public int DeletedCount;
        public List<string> FailedPaths;
        public bool AllSucceeded => FailedPaths == null || FailedPaths.Count == 0;
    }

    /// <summary>テクスチャアセットの削除（ゴミ箱移動 / 完全削除）を担う純粋なロジック（UI 非依存）。</summary>
    internal static class TextureRemover
    {
        /// <param name="moveToTrash">true=OS のゴミ箱へ移動 / false=完全削除。</param>
        public static RemoveResult Remove(IReadOnlyList<string> paths, bool moveToTrash)
        {
            int deleted = 0;
            var failed = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                {
                    bool success = moveToTrash
                        ? AssetDatabase.MoveAssetToTrash(path)
                        : AssetDatabase.DeleteAsset(path);
                    if (success) deleted++;
                    else failed.Add(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            return new RemoveResult { DeletedCount = deleted, FailedPaths = failed };
        }
    }
}
