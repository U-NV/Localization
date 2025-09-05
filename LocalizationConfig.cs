using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
# endif

namespace U0UGames.Localization
{
    [System.Serializable]
    public class LocalizationConfig:ScriptableObject
    {
#if UNITY_EDITOR
        private static string LocalizationConfigAssetPath
        {
            get
            {
                var path = $@"Assets\Resources\{LocalizationManager.LocalizationConfigFileName}.asset";
                return path;
            }
        }
        public static LocalizationConfig GetOrCreateLocalizationConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(LocalizationConfigAssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LocalizationConfig>();
                AssetDatabase.CreateAsset(config, LocalizationConfigAssetPath);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            return config;
        }
#endif

        [System.Serializable]
        public class GenerateConfig
        {
            public string languageCode;
            [FormerlySerializedAs("dataFolderPath")] [FormerlySerializedAs("dataFolderAssetPath")] public string dataFolderRootPath;
        }
        
        [System.Serializable]
        public class LanguageDisplayData
        {
            public string languageCode;
            public string displayName;
        }

        [SerializeField] private int _maxDynamicModuleCount;
        public int MaxDynamicModuleCount=>_maxDynamicModuleCount;
        
        
        [FormerlySerializedAs("languageCodeIndexOfExcelDataFile")] 
        public int originalLanguageCodeIndex;
        public string OriginalLanguageCode
        {
            get
            {
                try
                {
                    return languageDisplayDataList[originalLanguageCodeIndex].languageCode;
                }
                catch
                {
                    return null;
                }
            }
        }
        public string excelDataFolderRootPath;
        public string translateDataFolderRootPath;
        public List<LanguageDisplayData> languageDisplayDataList = new List<LanguageDisplayData>();
        public List<string> defaultModuleNames = new List<string>();

        [FormerlySerializedAs("languageCodeList")] 
        public List<string> inGameLanguageCodeList = new List<string>();
        
        
        public List<GenerateConfig> _generateConfigList = new List<GenerateConfig>();

        private List<string> _languageCodeList = new List<string>();
        public List<string> GetLanguageCodeList()
        {
            if(_languageCodeList.Count == languageDisplayDataList.Count)return _languageCodeList;
            _languageCodeList.Clear();
            foreach (var data in languageDisplayDataList)
            {
                _languageCodeList.Add(data.languageCode);
            }
            return _languageCodeList;
        }
        
        // public List<string> allExistModuleNames = new List<string>();
        public GenerateConfig _GetGenerateConfig(string languageCode)
        {
            foreach (var generateConfig in _generateConfigList)
            {
                if (generateConfig.languageCode == languageCode)
                {
                    return generateConfig;
                }
            }
            return null;
        }

        public static string GetJsonFileFolderFullPath(string languageCode)
        {
            return Path.Combine(Application.dataPath,
                LocalizationManager.LocalizationResourcesFolder,
                languageCode);
        }
        public static string GetJsonDataFullPath(string languageCode, string moduleName)
        {
            return Path.Combine(
                GetJsonFileFolderFullPath(languageCode),
                $"{moduleName}.json");
        }
    }
}