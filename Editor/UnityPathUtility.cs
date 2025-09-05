using System;
using System.IO;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public static class UnityPathUtility
    {
        public static string FullPathToAssetPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            return fullPath.Replace(Application.dataPath,"Assets");
        }
        public static string AssetPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return assetPath.Replace("Assets", Application.dataPath);
        }

        public static string RootFolderPath = Application.dataPath.Replace("Assets","");
        public static string FullPathToRootFolderPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            return fullPath.Replace(RootFolderPath,"");
        }
        public static string RootFolderPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return Path.Join(RootFolderPath, assetPath);
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
