using System;
using System.IO;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public static class UnityPathUtility
    {
        // Unity Application.dataPath 固定以 "/Assets" 结尾，取父目录更可靠
        // 避免 Replace("Assets","") 把路径中其他 "Assets" 单词也替换掉
        private static string _rootFolderPath;
        public static string RootFolderPath
        {
            get
            {
                if (_rootFolderPath != null) return _rootFolderPath;
                string dataPath = Application.dataPath.Replace('\\', '/');
                // dataPath = "C:/project/Assets" → 取掉末尾 "/Assets"
                _rootFolderPath = dataPath.EndsWith("/Assets")
                    ? dataPath[..^"Assets".Length]   // "C:/project/"
                    : dataPath + "/../";
                return _rootFolderPath;
            }
        }

        public static string FullPathToAssetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            string normalizedFull = fullPath.Replace('\\', '/');
            string normalizedData = Application.dataPath.Replace('\\', '/');
            string result = normalizedFull.Replace(normalizedData, "Assets");
            Debug.Log($"[PathUtil] FullPathToAssetPath: '{fullPath}' → '{result}'  (dataPath='{normalizedData}')");
            return result;
        }

        public static string AssetPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            // 只替换开头的 "Assets"，防止路径中其他位置的 "Assets" 被误替换
            string normalized = assetPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets"))
                return Application.dataPath.Replace('\\', '/') + normalized["Assets".Length..];
            return normalized;
        }

        public static string FullPathToRootFolderPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            string normalizedFull = fullPath.Replace('\\', '/');
            string normalizedRoot = RootFolderPath.Replace('\\', '/');
            string result = normalizedFull.StartsWith(normalizedRoot)
                ? normalizedFull[normalizedRoot.Length..]
                : normalizedFull;
            Debug.Log($"[PathUtil] FullPathToRootFolderPath: '{fullPath}' → '{result}'  (RootFolderPath='{normalizedRoot}')");
            return result;
        }

        public static string RootFolderPathToFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            string normalizedRoot = RootFolderPath.Replace('\\', '/');
            string normalizedRel  = relativePath.Replace('\\', '/');
            // 如果传进来的已经是绝对路径（历史数据），直接原样返回
            string result = (normalizedRel.Length >= 2 && normalizedRel[1] == ':') || normalizedRel.StartsWith("/")
                ? normalizedRel
                : normalizedRoot.TrimEnd('/') + '/' + normalizedRel.TrimStart('/');
            Debug.Log($"[PathUtil] RootFolderPathToFullPath: '{relativePath}' → '{result}'  (RootFolderPath='{normalizedRoot}')");
            return result;
        }

        public static void DeleteAllFile(string folderPath, bool skipMetaFile)
        {
            if (!Directory.Exists(folderPath))return;
            
            string[] files = Directory.GetFiles(folderPath);
            foreach (var filePath in files)
            {
                if (skipMetaFile)
                {
                    var extension = Path.GetExtension(filePath);
                    if (extension == ".meta")
                    {
                        continue;
                    }
                }
                
                try
                {
                    File.Delete(filePath);
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
        }
    }
}
