using System.Collections.Generic;
using System.Linq;
using U0UGames.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public class LocalizationConfigEditorWindow
    {
        private static class EditorPrefsKey
        {
            public const string ModuleNamesFoldout = "ModuleNamesFoldout";
            public const string GenerateConfigFoldout = "GenerateConfigFoldout";
            public const string LanguageCodeFoldout = "LanguageCodeFoldout";
            public const string LanguageDisplayNameFoldout = "LanguageDisplayNameFoldout";
            public const string LanguageCodeIndex = "LanguageCodeIndex";
        }
        
        private LocalizationConfig _localizationConfig;
        private bool _isDefaultModulesEditorPanelFoldout;
        private bool _isGenerateConfigPanelFoldout;
        private bool _isLanguageCodePanelFoldout;
        private bool _isLanguageDisplayNamePanelFoldout;
        private int _languageCodeIndex;
        
        public void Init()
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }
            
            _isDefaultModulesEditorPanelFoldout = EditorPrefs.GetBool(EditorPrefsKey.ModuleNamesFoldout);
            _isGenerateConfigPanelFoldout = EditorPrefs.GetBool(EditorPrefsKey.GenerateConfigFoldout);
            _isLanguageCodePanelFoldout = EditorPrefs.GetBool(EditorPrefsKey.LanguageCodeFoldout);
            _isLanguageDisplayNamePanelFoldout = EditorPrefs.GetBool(EditorPrefsKey.LanguageDisplayNameFoldout);
            _languageCodeIndex = EditorPrefs.GetInt(EditorPrefsKey.LanguageCodeIndex);
        }


        
        private void ShowChooseLanguage()
        {
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    var dataFolderAssetPath = _localizationConfig.excelDataFolderRootPath;
                    EditorGUILayout.LabelField("原始表格文件夹:",GUILayout.Width(8*12));
                    var selectRawDataFolderPath = LocalizationDataUtils.SelectFolderBtn(dataFolderAssetPath, "选择数据文件夹");
                    if (!string.IsNullOrEmpty(selectRawDataFolderPath))
                    {
                        _localizationConfig.excelDataFolderRootPath = UnityPathUtility.FullPathToRootFolderPath(selectRawDataFolderPath);
                    }
                    EditorGUILayout.EndHorizontal();

                }
                {  
                    EditorGUILayout.BeginHorizontal();
                    var translateDataFolderAssetPath = _localizationConfig.translateDataFolderRootPath;
                    EditorGUILayout.LabelField("翻译表格文件夹:",GUILayout.Width(8*12));
                    var selectTranslateFolderPath = LocalizationDataUtils.SelectFolderBtn(translateDataFolderAssetPath, "选择数据文件夹");
                    if (!string.IsNullOrEmpty(selectTranslateFolderPath))
                    {
                        _localizationConfig.translateDataFolderRootPath = UnityPathUtility.FullPathToRootFolderPath(selectTranslateFolderPath);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            
            {
                EditorGUILayout.BeginHorizontal();
                
                var enableLanguageCodeList = new List<string>(_localizationConfig.languageDisplayDataList.Count+1);
                foreach (var generateConfig in _localizationConfig.languageDisplayDataList)
                {
                    enableLanguageCodeList.Add(generateConfig.languageCode);
                }
                EditorGUILayout.LabelField("原始数据语言:",GUILayout.Width(8*12));
                var currLanguageCodeIndex = _localizationConfig.originalLanguageCodeIndex;
                currLanguageCodeIndex = EditorGUILayout.Popup(currLanguageCodeIndex, 
                    enableLanguageCodeList.ToArray(),
                    GUILayout.Width(60));
                if (currLanguageCodeIndex >= 0)
                {
                    _localizationConfig.originalLanguageCodeIndex = currLanguageCodeIndex;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }


        // private string _filter;
        // private int _selectModuleNameIndex;
        // private void SelectDefaultModules()
        // {
        //     EditorGUILayout.BeginVertical();
        //     List<string> modulesList = _localizationConfig.defaultModuleNames;
        //     
        //     
        //     _isDefaultModulesEditorPanelFoldout = EditorGUILayout.Foldout(_isDefaultModulesEditorPanelFoldout, "默认模块名");
        //     EditorPrefs.SetBool(EditorPrefsKey.ModuleNamesFoldout, _isDefaultModulesEditorPanelFoldout);
        //
        //     if (_isDefaultModulesEditorPanelFoldout)
        //     {
        //         // EditorGUI.indentLevel++;
        //         EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //         for (int i = 0; i < modulesList.Count; i++) 
        //         {
        //             EditorGUILayout.BeginHorizontal();
        //             GUI.enabled = false;
        //             modulesList[i] = EditorGUILayout.TextField(modulesList[i]);
        //             GUI.enabled = true;
        //
        //             bool stopDrawing = false;
        //             if (GUILayout.Button("-", GUILayout.Width(20))) 
        //             {
        //                 modulesList.RemoveAt(i);
        //                 stopDrawing = true;
        //             }
        //             EditorGUILayout.EndHorizontal();
        //
        //             if (stopDrawing)
        //             {
        //                 break;
        //             }
        //         }
        //         {
        //             EditorGUILayout.BeginHorizontal();
        //             {
        //                 var allModuleNames = _localizationConfig.allExistModuleNames;
        //                 GUI.enabled = false;
        //                 if (allModuleNames != null && allModuleNames.Count > 0)
        //                 {
        //                     GUI.enabled = true;
        //                 }
        //
        //                 string newModuleName = null;
        //                 if (allModuleNames != null )
        //                 {
        //                     _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
        //                     string[] filteredModuleOptions;
        //                     if (string.IsNullOrEmpty(_filter))
        //                     {
        //                         filteredModuleOptions = allModuleNames.ToArray();
        //                     }
        //                     else
        //                     {
        //                         filteredModuleOptions = allModuleNames.Where(option => option.ToLower().Contains(_filter.ToLower())).ToArray();
        //                     }
        //
        //                     if (filteredModuleOptions.Length > 0)
        //                     {
        //                         _selectModuleNameIndex = EditorGUILayout.Popup(_selectModuleNameIndex, filteredModuleOptions);
        //                         if (_selectModuleNameIndex >= 0)
        //                         {
        //                             newModuleName = filteredModuleOptions[_selectModuleNameIndex];
        //                         }
        //                     }
        //                     else
        //                     {
        //                         EditorGUILayout.Popup(-1, filteredModuleOptions);
        //                     }
        //                 }
        //                 
        //                 if (GUILayout.Button("+",GUILayout.Width(20))) 
        //                 {
        //                     if (!string.IsNullOrEmpty(newModuleName) 
        //                         && allModuleNames.Contains(newModuleName) 
        //                         && !modulesList.Contains(newModuleName))
        //                     {
        //                         modulesList.Add(newModuleName);
        //                     }
        //                 }
        //                 GUI.enabled = true;
        //             }
        //             EditorGUILayout.EndHorizontal();
        //         }
        //         EditorGUILayout.EndVertical();
        //     }
        //
        //     
        //     EditorGUILayout.EndVertical();
        // }
        
        private void ShowGenerateConfigView()
        {
            var configList = _localizationConfig._generateConfigList;
            EditorGUILayout.BeginVertical();
            
            _isGenerateConfigPanelFoldout = EditorGUILayout.Foldout(_isGenerateConfigPanelFoldout, "[!弃用!]本地化数据文件夹");
            EditorPrefs.SetBool(EditorPrefsKey.GenerateConfigFoldout, _isGenerateConfigPanelFoldout);

            if (_isGenerateConfigPanelFoldout)
            {
                // EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                for (int i = 0; i < configList.Count; i++) 
                {                
                    EditorGUILayout.BeginHorizontal();

                    configList[i].languageCode = EditorGUILayout.TextField(configList[i].languageCode,GUILayout.Width(60));

                    string openFolderPath = configList[i].dataFolderRootPath;
                    string buttonText = configList[i].dataFolderRootPath;
                    if (string.IsNullOrEmpty(configList[i].dataFolderRootPath))
                    {
                        buttonText = "选择数据文件夹";
                        openFolderPath = Application.dataPath;
                    }
                    if (GUILayout.Button(buttonText))
                    {
                        string selectFolderPath = EditorUtility.OpenFolderPanel("选择数据文件夹", openFolderPath, null);
                        if (!string.IsNullOrEmpty(selectFolderPath))
                        {
                            configList[i].dataFolderRootPath = UnityPathUtility.FullPathToRootFolderPath(selectFolderPath);
                        }
                    }
                    bool stopDrawing = false;
                    if (GUILayout.Button("-", GUILayout.Width(20))) 
                    {
                        configList.RemoveAt(i);
                        stopDrawing = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (stopDrawing)
                    {
                        break;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+",GUILayout.Width(20))) 
                    {
                        configList.Add(new LocalizationConfig.GenerateConfig());
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                // EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
        
         private void ShowLanguageCodeView()
        {
            var languageCodeList = _localizationConfig.inGameLanguageCodeList;
            var enableLanguageCodeList = new List<string>(_localizationConfig.languageDisplayDataList.Count+1);
            // enableLanguageCodeList.Add("None");
            foreach (var generateConfig in _localizationConfig.languageDisplayDataList)
            {
                enableLanguageCodeList.Add(generateConfig.languageCode);
            }
            EditorGUILayout.BeginVertical();
            _isLanguageCodePanelFoldout = EditorGUILayout.Foldout(_isLanguageCodePanelFoldout, "游戏内使用的语言");
            EditorPrefs.SetBool(EditorPrefsKey.LanguageCodeFoldout, _isLanguageCodePanelFoldout);

            if (_isLanguageCodePanelFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginVertical();
                    for (int i = 0; i < languageCodeList.Count; i++) 
                    {                
                        EditorGUILayout.BeginHorizontal();
                        GUI.enabled = false;
                        EditorGUILayout.TextField(languageCodeList[i]);
                        GUI.enabled = true;
                        bool stopDrawing = false;
                        if (GUILayout.Button("-", GUILayout.Width(20))) 
                        {
                            languageCodeList.RemoveAt(i);
                            stopDrawing = true;
                        }
                        EditorGUILayout.EndHorizontal();

                        if (stopDrawing)
                        {
                            break;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        _languageCodeIndex = EditorGUILayout.Popup(_languageCodeIndex, enableLanguageCodeList.ToArray());
                        if (_languageCodeIndex >= 0)
                        {
                            EditorPrefs.SetInt(EditorPrefsKey.LanguageCodeIndex,_languageCodeIndex);
                        }

                        string newCode = null;
                        if (_languageCodeIndex >= 0 && _languageCodeIndex < enableLanguageCodeList.Count)
                        {
                            newCode = enableLanguageCodeList[_languageCodeIndex];
                        }
                        if (GUILayout.Button("+",GUILayout.Width(20))) 
                        {
                            if (!string.IsNullOrEmpty(newCode) && !languageCodeList.Contains(newCode))
                            {
                                languageCodeList.Add(newCode);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

         private void ShowLanguageDisplayNameView()
         {
            var enableLanguageCodeList = new List<string>(_localizationConfig.languageDisplayDataList.Count+1);
            foreach (var generateConfig in _localizationConfig.languageDisplayDataList)
            {
                enableLanguageCodeList.Add(generateConfig.languageCode);
            }
            EditorGUILayout.BeginVertical();
            _isLanguageDisplayNamePanelFoldout = EditorGUILayout.Foldout(_isLanguageDisplayNamePanelFoldout, "语言显示名称");
            EditorPrefs.SetBool(EditorPrefsKey.LanguageDisplayNameFoldout, _isLanguageDisplayNamePanelFoldout);

            var displayDataList = _localizationConfig.languageDisplayDataList;
            if (displayDataList == null || displayDataList.Count != enableLanguageCodeList.Count)
            {
                _localizationConfig.languageDisplayDataList = new List<LocalizationConfig.LanguageDisplayData>(enableLanguageCodeList.Count);
                displayDataList = _localizationConfig.languageDisplayDataList;
                foreach (var code in enableLanguageCodeList)
                {
                    displayDataList.Add(new LocalizationConfig.LanguageDisplayData()
                    {
                        languageCode = code,
                        displayName = code,
                    });
                }
            }
            
            if (_isLanguageDisplayNamePanelFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginVertical();
                    for (int i = 0; i < displayDataList.Count; i++) 
                    {                
                        EditorGUILayout.BeginHorizontal();
                        GUI.enabled = false;
                        EditorGUILayout.TextField(displayDataList[i].languageCode);
                        GUI.enabled = true;
                        displayDataList[i].displayName = EditorGUILayout.TextField(displayDataList[i].displayName);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
         }
         
        public void OnGUI()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                ShowChooseLanguage();
                EditorGUILayout.EndVertical();
            }
            
            // GUILayout.Space(5);
            // SelectDefaultModules();

            GUILayout.Space(5);
            ShowLanguageDisplayNameView();
            GUILayout.Space(5);
            ShowLanguageCodeView();
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);
            
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            ShowGenerateConfigView();
            EditorGUILayout.EndVertical();
            

            if (GUI.changed)
            {
                // 标记对象为已修改
                EditorUtility.SetDirty(_localizationConfig);
                // 保存已修改的 Asset
                AssetDatabase.SaveAssetIfDirty(_localizationConfig);
            }
            
        }
    }

 
}