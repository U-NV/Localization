using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace U0UGames.Localization.Editor
{
    public class LocalizationNewFileGenerateWindow
    {
        private LocalizationConfig _localizationConfig;
        private LocalizationExcelExporter _excelExporter;
        private LocalizationJsonGenerator _jsonGenerator;
        private LocalizationProjectCollector _projectCollector;

        public void Init()
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }
            _excelExporter = new LocalizationExcelExporter(_localizationConfig);
            _jsonGenerator = new LocalizationJsonGenerator(_localizationConfig);
            _projectCollector = new LocalizationProjectCollector(_localizationConfig);
        }

        private int GetDataLanguageCodeIndex()
        {
            var languageCodeIndex = _localizationConfig.originalLanguageCodeIndex;
            if (languageCodeIndex < 0 || languageCodeIndex >= _localizationConfig.languageDisplayDataList.Count)
            {
                return -1;
            }
            return languageCodeIndex;
        }

        private void ShowConfig(string configName, string configValue)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField($"{configName}", GUILayout.Width(12 * 8));
                string showText = configValue;
                if (string.IsNullOrEmpty(configValue))
                {
                    showText = "请在配置文件中指定";
                }
                GUI.enabled = false;
                GUILayout.TextArea(showText);
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ExportLocalizationExcelFilePanel()
        {
            string info = "将原始数据表格 导出为 翻译表格";
            EditorGUILayout.LabelField(info, EditorStyles.boldLabel);

            var languageCodeIndex = GetDataLanguageCodeIndex();
            if (languageCodeIndex < 0 || languageCodeIndex >= _localizationConfig.languageDisplayDataList.Count)
            {
                EditorGUILayout.LabelField("Error: 选择了无效的语言码, 请在配置界面中重新选择", EditorStyles.helpBox);
                return;
            }
            string currLanguageCode = _localizationConfig.languageDisplayDataList[languageCodeIndex].languageCode;

            ShowConfig("原始表格路径:", _localizationConfig.excelDataFolderRootPath);
            ShowConfig("翻译表格路径:", _localizationConfig.translateDataFolderRootPath);
            ShowConfig("原文语言:", currLanguageCode);

            _localizationConfig.saveExportDiffFile = EditorGUILayout.Toggle("保存导出差异文件", _localizationConfig.saveExportDiffFile);

            {
                GUI.enabled = true;
                string buttonText = "导出翻译表格";
                if (string.IsNullOrEmpty(_localizationConfig.excelDataFolderRootPath))
                {
                    buttonText = "当前excel文件夹路径为空，指定剧情excel文件夹后才能导出";
                    GUI.enabled = false;
                }

                if (GUILayout.Button(buttonText))
                {
                    _excelExporter.CheckRepeatKeyword(currLanguageCode, out bool haveRepeatKeyword);
                    if (!haveRepeatKeyword)
                    {
                        _excelExporter.ExportTranslateFile(currLanguageCode);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "无法导出翻译表格，原始数据中存在重复的关键词，详见更新日志", "确认");
                    }
                }
                GUI.enabled = true;
            }
        }

        private Vector2 _generateConfigScrollPos = Vector2.zero;
        private void ShowLocalizeDataFolder()
        {
            {
                EditorGUILayout.LabelField("当前的配置：");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _generateConfigScrollPos = EditorGUILayout.BeginScrollView(_generateConfigScrollPos, GUILayout.MinHeight(130));
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("语言码", GUILayout.Width(40));
                    EditorGUILayout.LabelField("|", GUILayout.Width(5));
                    EditorGUILayout.LabelField("文件生成路径");
                    EditorGUILayout.EndHorizontal();
                }
                var languageCodeList = _localizationConfig.GetLanguageCodeList();
                foreach (var languageCode in languageCodeList)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(languageCode, GUILayout.Width(40));
                    EditorGUILayout.LabelField("|", GUILayout.Width(5));
                    var resultPath = LocalizationManager.GetJsonFolderFullPath(languageCode);
                    resultPath = UnityPathUtility.FullPathToAssetPath(resultPath);
                    if (GUILayout.Button(resultPath, GUILayout.MinWidth(300)))
                    {
                        Object obj = AssetDatabase.LoadMainAssetAtPath(resultPath);
                        if (obj != null)
                        {
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }

        private void GenerateFromLocalizeDataFolder()
        {
            ShowLocalizeDataFolder();
            if (!GUILayout.Button("从翻译表格中生成Json数据模块"))
            {
                return;
            }
            var languageCodeList = _localizationConfig.GetLanguageCodeList();
            if (languageCodeList == null || languageCodeList.Count == 0)
            {
                Debug.LogError("没有配置任何语言");
                return;
            }

            _jsonGenerator.GenerateJsonFiles();
            AssetDatabase.Refresh();
        }

        private void ShowCollectFromSceneAndPrefabs()
        {
            EditorGUILayout.LabelField("从场景和预制件中收集本地化数据", EditorStyles.boldLabel);
            if (GUILayout.Button("收集并生成本地化Json文件"))
            {
                _projectCollector.CollectLocalizeDataFromProject();
            }
        }

        public void OnGUI()
        {
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                ShowCollectFromSceneAndPrefabs();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                ExportLocalizationExcelFilePanel();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GenerateFromLocalizeDataFolder();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
