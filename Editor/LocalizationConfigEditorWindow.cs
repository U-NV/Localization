using System.Collections.Generic;
using System.Linq;
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
                if (!_localizationConfig.IsValid())
                {
                    EditorGUILayout.LabelField("Error: 没有配置任何语言，请先添加语言配置", EditorStyles.helpBox);
                    return;
                }
                
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

        private Vector2 _generateConfigScrollPos;
        private void ShowGenerateConfigView()
        {
            var configList = _localizationConfig.languageDisplayDataList;

            EditorGUILayout.BeginVertical();

            _isGenerateConfigPanelFoldout = EditorGUILayout.Foldout(_isGenerateConfigPanelFoldout, "语言配置");
            EditorPrefs.SetBool(EditorPrefsKey.GenerateConfigFoldout, _isGenerateConfigPanelFoldout);

            if (_isGenerateConfigPanelFoldout)
            {
                // EditorGUI.indentLevel++;
                _generateConfigScrollPos = EditorGUILayout.BeginScrollView(_generateConfigScrollPos, EditorStyles.helpBox);

                for (int i = 0; i < configList.Count; i++) 
                {                
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.BeginVertical();
                    configList[i].languageCode = EditorGUILayout.TextField("语言码:",configList[i].languageCode);
                    configList[i].displayName = EditorGUILayout.TextField("显示名称:",configList[i].displayName);
                    EditorGUILayout.EndVertical();

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
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("+")) 
                    {
                        configList.Add(new LocalizationConfig.LanguageConfig());
                    }
                }
                EditorGUILayout.EndHorizontal();
                
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
                    if(languageCodeList == null || languageCodeList.Count == 0)
                    {
                        EditorGUILayout.LabelField("暂无语言");
                    }
                    else
                    {
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
         
        public void OnGUI()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);


            {

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                LocalizationGlossary tempGlossary = (LocalizationGlossary)EditorGUILayout.ObjectField("名词术语表", _localizationConfig.Glossary, typeof(LocalizationGlossary), false);
                if(tempGlossary != null && tempGlossary != _localizationConfig.Glossary){
                    _localizationConfig.SetGlossary(tempGlossary);
                    EditorUtility.SetDirty(_localizationConfig);
                    AssetDatabase.SaveAssetIfDirty(_localizationConfig);
                }

                EditorGUILayout.EndVertical();
            }
            
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                ShowChooseLanguage();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(5);
            ShowLanguageCodeView();

            GUILayout.Space(5);
            
            
            EditorGUILayout.BeginVertical();
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