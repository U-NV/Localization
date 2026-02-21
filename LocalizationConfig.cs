using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using System;



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
                // 确保Resources目录存在
                EnsureDirectoryExists("Assets/Resources");
                
                config = ScriptableObject.CreateInstance<LocalizationConfig>();
                
                // 初始化默认值，避免索引越界
                InitializeDefaultValues(config);
                
                AssetDatabase.CreateAsset(config, LocalizationConfigAssetPath);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            return config;
        }
        
        /// <summary>
        /// 初始化配置的默认值
        /// </summary>
        private static void InitializeDefaultValues(LocalizationConfig config)
        {
            // 初始化语言列表
            if (config.languageDisplayDataList == null || config.languageDisplayDataList.Count == 0)
            {
                config.languageDisplayDataList = new List<LanguageConfig>
                {
                    new LanguageConfig { languageCode = "zh-cn", displayName = "简体中文" },
                    new LanguageConfig { languageCode = "en", displayName = "English" }
                };
            }
            
            // 确保索引有效
            if (config.originalLanguageCodeIndex < 0 || config.originalLanguageCodeIndex >= config.languageDisplayDataList.Count)
            {
                config.originalLanguageCodeIndex = 0; // 默认选择第一个语言
            }
            
            // 初始化其他默认值
            if (config.inGameLanguageCodeList == null)
            {
                config.inGameLanguageCodeList = new List<string>();
            }
        }
        
        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (AssetDatabase.IsValidFolder(directoryPath))
                return;
                
            // 分割路径并逐级创建目录
            string[] pathParts = directoryPath.Split('/');
            string currentPath = pathParts[0]; // "Assets"
            
            for (int i = 1; i < pathParts.Length; i++)
            {
                string nextPath = currentPath + "/" + pathParts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                }
                currentPath = nextPath;
            }
        }
#endif
        
        [System.Serializable]
        public class LanguageConfig
        {
            public string languageCode;
            public string displayName;
        }

        [SerializeField] private LocalizationGlossary _glossary;
        public LocalizationGlossary Glossary =>_glossary;

        [FormerlySerializedAs("languageCodeIndexOfExcelDataFile")] 
        public int originalLanguageCodeIndex;
        public string OriginalLanguageCode
        {
            get
            {
                if (originalLanguageCodeIndex < 0 || originalLanguageCodeIndex >= languageDisplayDataList.Count)
                {
                    return null;
                }
                return languageDisplayDataList[originalLanguageCodeIndex].languageCode;
            }
        }
        public string excelDataFolderRootPath;
        public string translateDataFolderRootPath;
        public List<LanguageConfig> languageDisplayDataList = new List<LanguageConfig>();
        [FormerlySerializedAs("languageCodeList")] 
        public List<string> inGameLanguageCodeList = new List<string>();

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
        
        /// <summary>
        /// 检查配置是否有效
        /// </summary>
        public bool IsValid()
        {
            return languageDisplayDataList != null && languageDisplayDataList.Count > 0;
        }
        
        /// <summary>
        /// 获取有效的语言代码列表，如果配置无效则返回空列表
        /// </summary>
        public List<string> GetValidLanguageCodeList()
        {
            if (!IsValid())
            {
                return new List<string>();
            }
            return GetLanguageCodeList();
        }
        
        // public List<string> allExistModuleNames = new List<string>();
        public LanguageConfig _GetGenerateConfig(string languageCode)
        {
            foreach (var generateConfig in languageDisplayDataList)
            {
                if (generateConfig.languageCode == languageCode)
                {
                    return generateConfig;
                }
            }
            return null;
        }

        public void SetGlossary(LocalizationGlossary tempGlossary)
        {
            _glossary = tempGlossary;
        }


        public string translateApiKey;
        public string translateApiUrl;
        public string translateAIPrompt;
        public bool saveExportDiffFile;

    }
}