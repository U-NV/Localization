using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;
using U0UGames.Framework.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace U0UGames.Localization.Editor
{
    public class LocalizationFileGenerateWindow
    {
        private static class EditorPrefsKey
        {
            public const string NeedDeleteOldFile = "needDeleteOldFile";
            public const string RawDataFolderFoldOut = "RawDataFolderFoldOut";
            public const string LocalizeDataFolderFoldOut = "LocalizeDataFolderFoldOut";
        }

        private bool _needDeleteOldFile;
        private bool _isRawDataFolderFoldOut;
        private bool _isLocalizeDataFolderFoldOut;
        public void Init()
        {
            _needDeleteOldFile = EditorPrefs.GetBool(EditorPrefsKey.NeedDeleteOldFile);
            _isRawDataFolderFoldOut = EditorPrefs.GetBool(EditorPrefsKey.RawDataFolderFoldOut);
            _isLocalizeDataFolderFoldOut = EditorPrefs.GetBool(EditorPrefsKey.LocalizeDataFolderFoldOut);
        }
        private string GetAssetPath(string fullPath)
        {
            string tempPath = Path.GetRelativePath(Application.dataPath, fullPath);
            return Path.Combine("Assets", tempPath);
        }
        
        private void SaveFile(string path, string info,ImportAssetOptions options = ImportAssetOptions.Default )
        {
            string folderPath = Path.GetDirectoryName(path);
            if (folderPath!=null && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            File.WriteAllText(path, info);
            AssetDatabase.ImportAsset(GetAssetPath(folderPath), options);
        }

        private  Dictionary<string, Dictionary<string, string>> ProcessLocalizeData(List<LocalizationDataUtils.LocalizeLineData> lineDataList)
        {
            // 分析本地化文件
            Dictionary<string, Dictionary<string, string>> resultDictionary =
                new Dictionary<string, Dictionary<string, string>>();

            foreach (var lineData in lineDataList)
            {
                if(string.IsNullOrEmpty(lineData.key))continue;
                string moduleName = LocalizationManager.GetModuleName(lineData.key);
                if(string.IsNullOrEmpty(moduleName))continue;
                // LocalizationManager.ProcessLocalizeKey(kvp.Key, out string prefix, out string keyword);
                if (!resultDictionary.TryGetValue(moduleName, out var moduleTextLookup))
                {
                    moduleTextLookup = new Dictionary<string, string>();
                    resultDictionary[moduleName] = moduleTextLookup;
                }

                // moduleTextLookup[lineData.key] = lineData.value;
            }
            return resultDictionary;
        }


        
        // private string[] _exitModuleNames;
        // private void WriteLocalizeFileToJson(string languageCode, 
        //     List<LocalizationDataUtils.LocalizeLineData> lineDataList,
        //     bool deleteOldFile = true)
        // {
        //     // 分析本地化文件
        //     Dictionary<string, Dictionary<string, string>> resultDictionary = ProcessLocalizeData(lineDataList);
        //     
        //     string folderPath = LocalizationConfig.GetJsonFileFolderFullPath(languageCode);
        //     
        //     // 清空文件夹中的旧文件
        //     if (deleteOldFile)
        //     {
        //         UnityPathUtility.DeleteAllFile(folderPath);
        //     }
        //     
        //     // _exitModuleNames = resultDictionary.Keys.ToArray();
        //     // 保存本地化文件
        //     foreach (var kvp in resultDictionary)
        //     {
        //         string moduleName = kvp.Key;
        //         // _exitModuleNames.Add(moduleName);
        //         
        //         var jsonFile = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
        //         if(string.IsNullOrEmpty(jsonFile))continue;
        //
        //         string fullPath = LocalizationConfig.GetJsonDataFullPath(languageCode, moduleName);
        //         SaveFile(fullPath, jsonFile,ImportAssetOptions.ForceUpdate);
        //     }
        // }
        
        private string SelectFolder(string title, string oldPath)
        {
            string newPath = oldPath;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(title);
            GUILayout.Space(1);
            if (GUILayout.Button(oldPath))
            {
                string selectFolderPath = EditorUtility.OpenFolderPanel(title, Application.dataPath, null);
                newPath = selectFolderPath;
            }
            GUILayout.EndHorizontal();
            
            return newPath;
        }
        
        // private void GenerateLocalizationFile(string code, string dataFolderAssetPath)
        // {
        //     // 获得所有数据
        //     var fileDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(dataFolderAssetPath);
        //     
        //     List<LocalizationDataUtils.LocalizeLineData> allDataList = 
        //         new List<LocalizationDataUtils.LocalizeLineData>(fileDataList.Count);
        //     foreach (var fileData in fileDataList)
        //     {
        //         allDataList.AddRange(fileData.dataList);
        //     }
        //     
        //     // 保存本地化数据
        //     WriteLocalizeFileToJson(code, allDataList,_needDeleteOldFile);
        // }

        // private List<LocalizationConfig.GenerateConfig> GetUsedGenerateConfig()
        // {
        //     var localizeConfig = LocalizationManager.Config;
        //     var generateConfigList = new List<LocalizationConfig.GenerateConfig>();
        //     foreach (var config in localizeConfig.generateConfigList)
        //     {
        //         if (localizeConfig.inGameLanguageCodeList.Contains(config.languageCode))
        //         {
        //             generateConfigList.Add(config);
        //         }
        //     }
        //     // foreach (var usedCode in localizeConfig.inGameLanguageCodeList)
        //     // {
        //     //     var usedConfig = localizeConfig.generateConfigList.Find(x => x.languageCode == usedCode);
        //     //     generateConfigList.Add(usedConfig);
        //     // }
        //     return generateConfigList;
        // }

        // private void ShowLocalizeDataFolder()
        // {
        //     var generateConfigList = GetUsedGenerateConfig();
        //     {
        //         // 展示配置
        //         EditorGUILayout.LabelField("当前的配置：");
        //         EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //
        //         _generateConfigScrollPos = EditorGUILayout.BeginScrollView(_generateConfigScrollPos,GUILayout.MinHeight(130));
        //         {
        //             // 表头
        //             EditorGUILayout.BeginHorizontal();
        //             EditorGUILayout.LabelField("语言码",GUILayout.Width(40));
        //             EditorGUILayout.LabelField("|",GUILayout.Width(5));
        //             EditorGUILayout.LabelField("本地化数据路径",GUILayout.MinWidth(300));
        //             EditorGUILayout.LabelField("|",GUILayout.Width(5));
        //             EditorGUILayout.LabelField("文件生成路径",GUILayout.MinWidth(300));
        //             EditorGUILayout.EndHorizontal();
        //         }
        //         foreach (var generateConfig in generateConfigList)
        //         {
        //             EditorGUILayout.BeginHorizontal();
        //             EditorGUILayout.LabelField(generateConfig.languageCode,GUILayout.Width(40));
        //             EditorGUILayout.LabelField("|",GUILayout.Width(5));
        //             if (GUILayout.Button(generateConfig.dataFolderRootPath,GUILayout.MinWidth(300)))
        //             {
        //                 Object obj = AssetDatabase.LoadMainAssetAtPath(generateConfig.dataFolderRootPath);
        //                 if (obj != null)
        //                 {
        //                     EditorGUIUtility.PingObject(obj);
        //                 }
        //             }
        //             // EditorGUILayout.LabelField(generateConfig.dataFolderAssetPath,GUILayout.MaxWidth(300));
        //             EditorGUILayout.LabelField("|",GUILayout.Width(5));
        //             var resultPath =
        //                 $@"Assets\Resources\{LocalizationManager.LocalizationResourcesFolder}\{generateConfig.languageCode}";
        //             if (GUILayout.Button(resultPath,GUILayout.MinWidth(300)))
        //             {
        //                 Object obj = AssetDatabase.LoadMainAssetAtPath(resultPath);
        //                 if (obj != null)
        //                 {
        //                     EditorGUIUtility.PingObject(obj);
        //                 }
        //             }
        //             // EditorGUILayout.LabelField();
        //
        //             EditorGUILayout.EndHorizontal();
        //         }
        //         EditorGUILayout.EndScrollView();
        //         EditorGUILayout.EndVertical();
        //     }
        // }
        
        // private void GenerateFromLocalizeDataFolder()
        // {
        //     _isLocalizeDataFolderFoldOut = EditorGUILayout.Foldout(_isLocalizeDataFolderFoldOut, "从导出的本地化数据中生成模块");
        //     EditorPrefs.SetBool(EditorPrefsKey.LocalizeDataFolderFoldOut,_isLocalizeDataFolderFoldOut);
        //     if(!_isLocalizeDataFolderFoldOut)return;
        //     
        //     // ShowLocalizeDataFolder();
        //     if (GUILayout.Button("从本地化数据文件夹中生成数据模块"))
        //     {
        //         // var generateConfigList = GetUsedGenerateConfig();
        //         float index = 0;
        //         float num = generateConfigList.Count;
        //         foreach (var generateConfig in generateConfigList)
        //         {
        //             GenerateLocalizationFile（）
        //         }
        //         // LocalizationManager.Config.allExistModuleNames = _exitModuleNames.ToList();
        //         EditorUtility.SetDirty(LocalizationManager.Config);
        //         AssetDatabase.SaveAssets();
        //         
        //         AssetDatabase.Refresh();
        //         EditorUtility.ClearProgressBar();
        //     }
        //     
        //     // EditorGUILayout.EndVertical();
        // }

        // private void GenerateFormRawFolder()
        // {
        //     
        //     _isRawDataFolderFoldOut = EditorGUILayout.Foldout(_isRawDataFolderFoldOut, "从原始数据中生成模块");
        //     EditorPrefs.SetBool(EditorPrefsKey.RawDataFolderFoldOut,_isRawDataFolderFoldOut);
        //     if(!_isRawDataFolderFoldOut)return;
        //     
        //     var localizeConfig = LocalizationManager.Config;
        //     var languageCodeIndex = localizeConfig.languageCodeIndexOfExcelDataFile;
        //     var rawDataFolderAssetPath = localizeConfig.excelDataFolderAssetPath;
        //
        //     if (string.IsNullOrEmpty(rawDataFolderAssetPath))
        //     {
        //         EditorGUILayout.LabelField("错误:excel数据文件路径为空，请配置界面选择excel数据文件夹");
        //         return;
        //     }
        //
        //     if (languageCodeIndex < 0 || languageCodeIndex >= localizeConfig.generateConfigList.Count)
        //     {
        //         EditorGUILayout.LabelField("错误:选择了无效的语言码, 请在配置界面中重新选择");
        //         return;
        //     }
        //     
        //     var languageCode = localizeConfig.generateConfigList[languageCodeIndex].languageCode;
        //     
        //     EditorGUILayout.LabelField($"导出语言：{languageCode}");
        //
        //     EditorGUILayout.BeginHorizontal();
        //     EditorGUILayout.LabelField($"原始数据文件夹路径：",GUILayout.MaxWidth(120));
        //     if (GUILayout.Button(rawDataFolderAssetPath))
        //     {
        //         Object obj = AssetDatabase.LoadMainAssetAtPath(rawDataFolderAssetPath);
        //         if (obj != null)
        //         {
        //             EditorGUIUtility.PingObject(obj);
        //         }
        //     }
        //     EditorGUILayout.EndHorizontal();
        //
        //     if (GUILayout.Button("从原始数据文件夹中生成数据模块"))
        //     {
        //         EditorUtility.DisplayProgressBar($"本地化{languageCode}" ,"生成Json文件中...",0);
        //         GenerateLocalizationFile(languageCode, rawDataFolderAssetPath);
        //         EditorUtility.ClearProgressBar();
        //         
        //         LocalizationManager.Config.allExistModuleNames = _exitModuleNames.ToList();
        //         EditorUtility.SetDirty(LocalizationManager.Config);
        //         AssetDatabase.SaveAssets();
        //         AssetDatabase.Refresh();
        //     }
        // }
        //
        private Vector2 _existModuleNamesScrollPos = Vector2.zero;
        // public void ShowExitModules()
        // {
                        // GUILayout.Space(5);
        //
        //     {
        //         EditorGUILayout.LabelField("生成的模块：");
        //         _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
        //         string[] filteredModuleOptions = null;
        //         // var allModuleNames = LocalizationManager.Config.allExistModuleNames;
        //         if (string.IsNullOrEmpty(_filter))
        //         {
        //             filteredModuleOptions = allModuleNames.ToArray();
        //         }
        //         else
        //         {
        //             filteredModuleOptions = allModuleNames.Where(option => option.ToLower().Contains(_filter.ToLower())).ToArray();
        //         }
        //         _existModuleNamesScrollPos = EditorGUILayout.BeginScrollView(_existModuleNamesScrollPos);
        //         var localizeConfig = LocalizationManager.Config;
        //         var languageCodeIndex = localizeConfig.originalLanguageCodeIndex;
        //         string languageCode = null;
        //         if (languageCodeIndex >= 0 && languageCodeIndex < localizeConfig.generateConfigList.Count)
        //         {
        //             languageCode = localizeConfig.generateConfigList[languageCodeIndex].languageCode;
        //         }
        //         
        //         foreach (var name in filteredModuleOptions)
        //         {
        //             if (string.IsNullOrEmpty(languageCode))
        //             {
        //                 EditorGUILayout.TextField(name);
        //             }
        //             else
        //             {
        //                 if (GUILayout.Button(name))
        //                 {
        //                     var fileFullPath = LocalizationConfig.GetJsonDataFullPath(languageCode, name);
        //                     var fileAssetPath = UnityPathUtility.FullPathToAssetPath(fileFullPath);
        //                     Object obj = AssetDatabase.LoadMainAssetAtPath(fileAssetPath);
        //                     if (obj != null)
        //                     {
        //                         EditorGUIUtility.PingObject(obj);
        //                     }
        //                 }
        //             }
        //         }
        //         EditorGUILayout.EndScrollView();
        //     }
        //     
        //     
        // }
        
        private Vector2 _generateConfigScrollPos = Vector2.zero;
        private string _filter;
        public void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            {
                _needDeleteOldFile = EditorGUILayout.Toggle("先清空文件夹再写入数据",_needDeleteOldFile);
                EditorPrefs.SetBool(EditorPrefsKey.NeedDeleteOldFile, _needDeleteOldFile);
            }
            EditorGUILayout.Space(5);
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                // GenerateFromLocalizeDataFolder();
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            // EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            // GenerateFormRawFolder();
            // EditorGUILayout.EndVertical();

            // ShowExitModules();
        }
    }
    
}