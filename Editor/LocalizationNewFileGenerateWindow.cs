using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using U0UGames.Localization.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace U0UGames.Localization.Editor
{
    public class LocalizationNewFileGenerateWindow
    {
        private LocalizationConfig _localizationConfig;
        public void Init()
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }
        }
        private int GetDataLanguageCodeIndex()
        {
            var languageCodeIndex = _localizationConfig.originalLanguageCodeIndex;
            if (languageCodeIndex < 0 || languageCodeIndex >= _localizationConfig.languageDisplayDataList.Count)
            {
                return -1; // 返回-1表示无效索引，由调用方处理错误显示
            }
            return languageCodeIndex;
        }
        private void ClearFolder(string dataFolderFullPath)
        {
            var files = Directory.GetFiles(dataFolderFullPath);
            foreach (var filePath in files)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LocalizeManager TryClearFolder:{e}");
                }
            }
        }
        private void ShowConfig(string configName, string configValue)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField($"{configName}",GUILayout.Width(12*8));
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

        private string GetOriginTextChangeTips(string oldText, string newText)
        {
            var currTime = DateTime.Now.ToString("d");
            return $"修改:[{oldText}]->[{newText}] {currTime}";
        }
        private string GetNewTextTips()
        {
            var currTime = DateTime.Now.ToString("d");
            return $"新增 {currTime}";
        }
        private string GetKeyChangeTips()
        {
            var currTime = DateTime.Now.ToString("d");
            return $"关键词改变 {currTime}";
        }
        private string GetSameKeyErrorString()
        {
            return $"Error: 关键词重复！！\n";
        }
        private void FillSameKeyLineData(string currLanguageCode,
            string fileName, int rawDataLineIndex,
            LocalizationDataUtils.LocalizeLineData rawDataLine,
            LocalizationDataUtils.LocalizeLineData targetTranslateData)
        {
            // 遍历所有语言的数据，将翻译数据填入原始文件
            foreach (var languageData in _localizationConfig.languageDisplayDataList)
            {
                var languageCode = languageData.languageCode;
                // 如果是原文语言，检查原文是否一致, 并跳过同步
                if (currLanguageCode == languageCode)
                {
                    var rawText = rawDataLine.translatedValues[languageCode];
                    var translateText = targetTranslateData.translatedValues[languageCode];
                    // 如果两者不一致，记录变更
                    if (rawText != translateText)
                    {
                        var changeTips = GetOriginTextChangeTips(translateText, rawText);
                        var tips2 = rawDataLine.tips2;
                        tips2 ??= "";
                        if (!tips2.EndsWith(changeTips))
                        {
                            rawDataLine.tips2 += string.IsNullOrEmpty(rawDataLine.tips2)?changeTips:"\n"+changeTips;;
                        }
                        AddChangeInfo(fileName, new ExcelLineChangeInfo(rawDataLineIndex, rawDataLine.key)
                        {
                            originalValue = translateText,
                            newValue = rawText,
                            isChanged = true
                        });
                    }
                    continue;
                }
                
                // 将原始数据的其他语言部分变为翻译数据
                rawDataLine.translatedValues[languageCode] =
                    targetTranslateData.translatedValues[languageCode];
            }
        }
        
        private bool TryFillDataWithSameKey(string currLanguageCode,
            LocalizationDataUtils.LocalizationFileData translateFileData,
            LocalizationDataUtils.LocalizeLineData rawDataLine, int rawDataLineIndex)
        {
            if (rawDataLine == null) return false;
            var lineDataKey = rawDataLine.key;
            if (string.IsNullOrEmpty(lineDataKey)) return false;
            // 如果翻译中找不到相同的文本关键词，返回
            var sameKeyTranslateDataList = translateFileData.GetDataListByKey(lineDataKey);
            if (sameKeyTranslateDataList == null || sameKeyTranslateDataList.Count == 0)
            {
                return false;
            }
            
            if(sameKeyTranslateDataList.Count>1)
            {
                AddChangeInfo(translateFileData.fileName, new ExcelLineChangeInfo(rawDataLineIndex, rawDataLine.key)
                {
                    originalValue = GetSameKeyErrorString(),
                    newValue = $"翻译文件{translateFileData.fileName}中有相同的关键词",
                    isError = true
                });
            }

            foreach (var sameKeyTranslateData in sameKeyTranslateDataList)
            {
                rawDataLine.translateDataAllValues = sameKeyTranslateData.translateDataAllValues;
                FillSameKeyLineData(currLanguageCode, 
                    translateFileData.fileName, rawDataLineIndex, rawDataLine, 
                    sameKeyTranslateData);
            }
            return true;
        }

        private bool TryFillDataWithSameOriginText(string currLanguageCode,
            LocalizationDataUtils.LocalizationFileData translateFileData,
            LocalizationDataUtils.LocalizeLineData rawDataLine, int rawDataLineIndex)
        {
            if (rawDataLine == null) return false;
            if (string.IsNullOrEmpty(rawDataLine.key)) return false;
            var originText = rawDataLine.translatedValues[currLanguageCode];
            if (string.IsNullOrEmpty(originText)) return false;
            if (!translateFileData.TryGetDataByOriginText(currLanguageCode, originText, out var targetTranslateDataLine))
            {
                return false;
            }
            
            // 二次确认
            var findText = targetTranslateDataLine.translatedValues[currLanguageCode];
            if (findText != originText) return false;
            
            rawDataLine.translateDataAllValues = targetTranslateDataLine.translateDataAllValues;

            var newTextTips = GetNewTextTips();
            rawDataLine.tips2 ??= "";
            if (rawDataLine.tips2.EndsWith(newTextTips))
            {
                rawDataLine.tips2 = rawDataLine.tips2.Replace(newTextTips, GetKeyChangeTips());
            }
            AddChangeInfo(translateFileData.fileName, new ExcelLineChangeInfo(rawDataLineIndex, rawDataLine.key)
            {
                originalValue = findText,
                newValue = findText,
                isKeyChanged = true
            });
            // 找到了原文相同的文本，将翻译文件填入原始文件
            foreach (var languageData in _localizationConfig.languageDisplayDataList)
            {
                var languageCode = languageData.languageCode;
                // 不同步原始文本，只同步其他语言
                if (currLanguageCode == languageCode) continue;
                rawDataLine.translatedValues[languageCode] = targetTranslateDataLine.translatedValues[languageCode];
            }
            return true;
        }


        
        private void FillRawDataWithTranslateData(string currLanguageCode,
            LocalizationDataUtils.LocalizationFileData rawExcelFileData,
            LocalizationDataUtils.LocalizationFileData sameNameTranslateData)
        {
            // 遍历翻译文件中的所有数据
            for (var index = 0; index < rawExcelFileData.dataList.Count; index++)
            {
                LocalizationDataUtils.LocalizeLineData rawDataLine = rawExcelFileData.dataList[index];
                // 如果找到了匹配的关键词，则不进行接下来的步骤
                if (TryFillDataWithSameKey(currLanguageCode, sameNameTranslateData, rawDataLine,index))
                {
                    continue;
                }
    
                // 否则尝试通过原文查找对应关系。
                if (TryFillDataWithSameOriginText(currLanguageCode, sameNameTranslateData, rawDataLine, index))
                {
                    continue;
                }
                
                // 如果关键词为空，而且没有找到匹配的数据，视为无效数据，返回
                if (string.IsNullOrEmpty(rawDataLine.key))
                {
                    rawDataLine.translateDataAllValues = rawDataLine.translatedValues;
                    continue;
                }
                // 都不满足，且关键词不为空则为新增文本
                rawDataLine.translateDataAllValues.Clear();
                rawDataLine.tips2 ??= "";
                var newTextTips = GetNewTextTips();
                if (!rawDataLine.tips2.EndsWith(newTextTips))
                {
                    rawDataLine.tips2 += string.IsNullOrEmpty(rawDataLine.tips2)?newTextTips:"\n"+newTextTips;
                }
                AddChangeInfo(rawExcelFileData.fileName, new ExcelLineChangeInfo(index, rawDataLine.key)
                {
                    originalValue = "",
                    newValue = rawDataLine.translatedValues[currLanguageCode],
                    isNew = true
                });
            }
        }


        private Dictionary<string, List<ExcelLineChangeInfo>> _excelChangeInfoLookup = new();
        private Dictionary<string, string> _rawDataKeyVisitMark = new();
        private void AddChangeInfo(string fileName,ExcelLineChangeInfo info)
        {
            if (!_excelChangeInfoLookup.TryGetValue(fileName, out var list))
            {
                list = new List<ExcelLineChangeInfo>();
                _excelChangeInfoLookup[fileName] = list;
            }
            list.Add(info);
        }

        private void SaveExcelChangeInfoList(string folderName)
        {
            if(!_localizationConfig.saveExportDiffFile){
                return;
            }
            if (!Directory.Exists(folderName))
            {
                return;
            }
            
            var dateTime = DateTime.Now;
            var timeString =
                $"{dateTime.Year:0000}{dateTime.Month:00}{dateTime.Day:00}-{dateTime.Hour:00}{dateTime.Minute:00}{dateTime.Second:00}";
            var fileName = $"更新日志 {timeString}";
            LocalizationDataUtils.ConvertChangeInfoToExcelFile(_excelChangeInfoLookup, fileName, folderName);
        }

        private void RemoveNotUseValue(LocalizationDataUtils.LocalizationFileData rawExcelFileData)
        { 
            foreach (var rawDataLine in rawExcelFileData.dataList)
            {
                rawDataLine.translateDataAllValues = rawDataLine.translatedValues;
            }
        }
        
        private void ExportTranslateFile(string currLanguageCode)
        {
            string info = "将原始数据表格 导出为 翻译表格";

            EditorUtility.DisplayProgressBar("导出",info,0);
            var translateDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);

            if (!Directory.Exists(translateDataFolderFullPath))
            {
                Directory.CreateDirectory(translateDataFolderFullPath);
            }
            
            var rawDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.excelDataFolderRootPath);
            var translateDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.translateDataFolderRootPath);
            
            if (rawDataList == null || rawDataList.Count == 0)
            {
                Debug.LogError("找不到任何可以导出的文件");
                return;
            }
            // 删除旧的数据
            // ClearFolder(_localizationConfig.translateDataFolderRootPath);
            
            _excelChangeInfoLookup??= new Dictionary<string, List<ExcelLineChangeInfo>>();
            _excelChangeInfoLookup.Clear();
            _rawDataKeyVisitMark ??= new ();
            _rawDataKeyVisitMark.Clear();
            // 同步翻译数据
            for (var index = 0; index < rawDataList.Count; index++)
            {
                var rawExcelFileData = rawDataList[index];
                if (!rawExcelFileData.IsValid())
                {
                    string errorInfo = $"文件[{rawExcelFileData.fileName}]中找不到任何有效的数据，跳过此文件的导出工作。";
                    // EditorUtility.DisplayDialog("错误", errorInfo, "确认");
                    Debug.LogError(errorInfo);
                    continue;
                }
                
                // 查找有无同名翻译文件
                LocalizationDataUtils.LocalizationFileData sameNameTranslateData = null;
                foreach (var translateData in translateDataList)
                {
                    if (translateData.fileName == rawExcelFileData.fileName)
                    {
                        sameNameTranslateData = translateData;
                        break;
                    }
                }

                // 存在同名翻译文件
                if (sameNameTranslateData != null)
                {
                    // 存在同名文件则将翻译数据填入原始文件中
                    FillRawDataWithTranslateData(currLanguageCode, rawExcelFileData, sameNameTranslateData);
                }
                else
                {
                    // 只保留原始文件中必要的数据
                    RemoveNotUseValue(rawExcelFileData);
                }
                
                // 将填入了翻译数据的原始文件保存到对应路径
                LocalizationDataUtils.ConvertToTranslateExcelFile(rawExcelFileData, translateDataFolderFullPath);
                float progress = (float)index / (float)rawDataList.Count;
                EditorUtility.DisplayProgressBar("导出翻译表格", info, progress);
            }

            var folderName = Path.GetDirectoryName(translateDataFolderFullPath);
            SaveExcelChangeInfoList(folderName);
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        private void CheckRepeatKeyword(string currLanguageCode, out bool haveRepeatKeyword)
        {
            _excelChangeInfoLookup??= new Dictionary<string, List<ExcelLineChangeInfo>>();
            _excelChangeInfoLookup.Clear();
            _rawDataKeyVisitMark ??= new ();
            _rawDataKeyVisitMark.Clear();
            var rawDataFileList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.excelDataFolderRootPath);
            foreach (var rawDataFile in rawDataFileList)
            {
                for (var index = 0; index < rawDataFile.dataList.Count; index++)
                {
                    var rawDataLine = rawDataFile.dataList[index];
                    if (string.IsNullOrEmpty(rawDataLine.key)) continue;
                    if (_rawDataKeyVisitMark.TryGetValue(rawDataLine.key, out var fileName))
                    {
                        AddChangeInfo(rawDataFile.fileName, new ExcelLineChangeInfo(index, rawDataLine.key)
                        {
                            originalValue = GetSameKeyErrorString(),
                            newValue = $"与原始文件{fileName}中关键词相同",
                            isError = true
                        });
                    }
                    else
                    {
                        _rawDataKeyVisitMark[rawDataLine.key] = rawDataFile.fileName;
                    }
                }
            }
            haveRepeatKeyword = _excelChangeInfoLookup.Count>0;
            if (haveRepeatKeyword)
            {
                var translateDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);
                if (!Directory.Exists(translateDataFolderFullPath))
                {
                    Directory.CreateDirectory(translateDataFolderFullPath);
                }
                var folderName = Path.GetDirectoryName(translateDataFolderFullPath);
                SaveExcelChangeInfoList(folderName);
            }
            return;
        }
        
        private void ExportLocalizationExcelFilePanel()
        {
            string info = "将原始数据表格 导出为 翻译表格";
            EditorGUILayout.LabelField(info,EditorStyles.boldLabel);
            
            var languageCodeIndex = GetDataLanguageCodeIndex();
            if (languageCodeIndex < 0 || languageCodeIndex >= _localizationConfig.languageDisplayDataList.Count)
            {
                EditorGUILayout.LabelField("Error: 选择了无效的语言码, 请在配置界面中重新选择", EditorStyles.helpBox);
                return;
            }
            string currLanguageCode = _localizationConfig.languageDisplayDataList[languageCodeIndex].languageCode;
            
            ShowConfig("原始表格路径:",_localizationConfig.excelDataFolderRootPath);
            ShowConfig("翻译表格路径:",_localizationConfig.translateDataFolderRootPath);
            ShowConfig("原文语言:",currLanguageCode);
            
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
                    CheckRepeatKeyword(currLanguageCode, out bool haveRepeatKeyword);
                    if (!haveRepeatKeyword)
                    {
                        ExportTranslateFile(currLanguageCode);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "无法导出翻译表格，原始数据中存在重复的关键词，详见更新日志", "确认");
                    }
                }
                GUI.enabled = true;
            }
        }

        private class JsonDataModule
        {
            public string moduleName;
            private Dictionary<string,Dictionary<string, string>> languageKvpLookup = new Dictionary<string, Dictionary<string, string>>();

            public JsonDataModule(string name)
            {
                this.moduleName = name;
                languageKvpLookup.Clear();
            }

            public void AddData(string languageCode, string key, string value)
            {
                if (!languageKvpLookup.TryGetValue(languageCode, out var translateLookup))
                {
                    translateLookup = new Dictionary<string, string>();
                    languageKvpLookup[languageCode] = translateLookup;
                }
                translateLookup[key] = value;
            }
            private void SaveFile(string path, string info)
            {
                string folderPath = Path.GetDirectoryName(path);
                if (folderPath!=null && !Directory.Exists(folderPath))
                {
                    // 如果出现了重名的文件，先删除这个文件，否则无法创建同名文件夹
                    if (File.Exists(folderPath))
                    {
                        File.Delete(folderPath);
                    }
                    Debug.Log($"CreateFolder:{folderPath}");
                    Directory.CreateDirectory(folderPath);
                }
                File.WriteAllText(path, info);
            }
            public void SaveJsonFile()
            {
                foreach (var kvp in languageKvpLookup)
                {
                    var languageCode = kvp.Key;
                    string fullPath = LocalizationManager.GetJsonDataFullPath(languageCode, moduleName);
                    var jsonFile = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);


                    SaveFile(fullPath, jsonFile);



                    LocalizationAddressableHelper.SetJsonAddressable(fullPath, languageCode, moduleName);
                }
                AssetDatabase.SaveAssets();
            }
        }

        private void AddToJsonDataModule(LocalizationDataUtils.LocalizeLineData lineData)
        {
            var languageKey = lineData.key;
            if(string.IsNullOrEmpty(languageKey))return;
            string moduleName = LocalizationManager.GetModuleName(lineData.key);
            if(string.IsNullOrEmpty(moduleName))return;
            if (!_jsonDataModuleLookup.TryGetValue(moduleName, out var dataModule))
            {
                dataModule = new JsonDataModule(moduleName);
                _jsonDataModuleLookup[moduleName] = dataModule;
            }
            foreach (var kvp in lineData.translatedValues)
            {
                var languageCode = kvp.Key;
                var languageValue = kvp.Value;
                dataModule.AddData(languageCode,languageKey,languageValue);
            }
            
        }
        
        private Dictionary<string, JsonDataModule> _jsonDataModuleLookup = new Dictionary<string, JsonDataModule>();
        private Vector2 _generateConfigScrollPos = Vector2.zero;
        private void ShowLocalizeDataFolder()
        {
            {
                // 展示配置
                EditorGUILayout.LabelField("当前的配置：");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                _generateConfigScrollPos = EditorGUILayout.BeginScrollView(_generateConfigScrollPos,GUILayout.MinHeight(130));
                {
                    // 表头
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("语言码",GUILayout.Width(40));
                    EditorGUILayout.LabelField("|",GUILayout.Width(5));
                    EditorGUILayout.LabelField("文件生成路径");
                    EditorGUILayout.EndHorizontal();
                }
                var languageCodeList = _localizationConfig.GetLanguageCodeList();
                foreach (var languageCode in languageCodeList)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(languageCode,GUILayout.Width(40));
                    EditorGUILayout.LabelField("|",GUILayout.Width(5));
                    var resultPath = LocalizationManager.GetJsonFolderFullPath(languageCode);
                    if (GUILayout.Button(resultPath,GUILayout.MinWidth(300)))
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

        public static void BuildAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null){
                Debug.LogError("AddressableAssetSettings is null");
                return;
            } 

            // 1. 找到"打包模式"的构建器 (BuildScriptPackedMode)
            // 只有这个模式会生成真正的 .bundle 文件
            var packedModeBuilder = settings.DataBuilders
                .FirstOrDefault(b => b.GetType().Name.Contains("BuildScriptPackedMode"));

            if (packedModeBuilder != null)
            {
                int buildIndex = settings.DataBuilders.IndexOf(packedModeBuilder);
                settings.ActivePlayerDataBuilderIndex = buildIndex;
                
                Debug.Log($"[Addressables] 已设置构建器索引: Player={buildIndex}");
                
                // 2. 清理旧缓存并执行构建
                AddressableAssetSettings.CleanPlayerContent();
                AddressableAssetSettings.BuildPlayerContent();
                
                Debug.Log("[Addressables] 物理 Bundle 构建完成！");
            }
            else
            {
                Debug.LogError("[Addressables] 未找到 BuildScriptPackedMode 构建器！请检查 Addressables 配置。");
            }
        }
        private void GenerateJsonFiles()
        {
            EditorUtility.DisplayProgressBar("导出Json文件", "导出Json文件", 0);

            var languageCodeList = _localizationConfig.GetLanguageCodeList();
            var currLanguageCode = _localizationConfig.OriginalLanguageCode;
            
            // 重置所有数据
            _jsonDataModuleLookup.Clear();
            foreach (var languageCode in languageCodeList)
            {
                string folderPath = LocalizationManager.GetJsonFolderFullPath(languageCode);
                UnityPathUtility.DeleteAllFile(folderPath,false);
            }
            // 统计json模块数据
            var translateDataFileList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.translateDataFolderRootPath);
            foreach (var dataFile in translateDataFileList)
            {
                foreach (var dataline in dataFile.dataList)
                {
                    AddToJsonDataModule(dataline);
                }
            }
            // 保存json模块数据
            var moduleList = _jsonDataModuleLookup.Values.ToArray();
            var moduleCount = moduleList.Length;
            for(int i=0;i<moduleCount;i++)
            {
                var jsonModule = moduleList[i];
                jsonModule.SaveJsonFile();
                EditorUtility.DisplayProgressBar("导出Json文件", $"导出Json文件:{jsonModule.moduleName}", i/(float)moduleCount);
            }
            AssetDatabase.SaveAssets();
            BuildAddressables();
            EditorUtility.ClearProgressBar();
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

            GenerateJsonFiles();
            AssetDatabase.Refresh();
        }
        
        private void ShowCollectFromSceneAndPrefabs()
        {
            EditorGUILayout.LabelField("从场景和预制件中收集本地化数据", EditorStyles.boldLabel);
            if (GUILayout.Button("收集并生成本地化Json文件"))
            {
                CollectLocalizeDataFromProject();
            }
        }

        private static readonly string CollectedJsonFileName = "CollectedLocalizeData.json";

        private void CollectLocalizeDataFromProject()
        {
            Dictionary<string, string> collectedData = new Dictionary<string, string>();
            int warningCount = 0;

            EditorUtility.DisplayProgressBar("收集本地化数据", "正在搜索预制件...", 0f);

            // 1. 搜集项目中的预制件
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                LocalizeText[] components = prefab.GetComponentsInChildren<LocalizeText>(true);
                foreach (var comp in components)
                {
                    string objPath = $"预制件: {path} -> {GetGameObjectPath(comp.gameObject)}";
                    if (!ProcessLocalizeComponent(comp, objPath, collectedData))
                    {
                        warningCount++;
                    }
                }

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("收集本地化数据",
                        $"正在搜索预制件 ({i}/{prefabGuids.Length})...",
                        (float)i / prefabGuids.Length * 0.8f);
                }
            }

            // 2. 搜集项目中所有场景（包括未打开的）
            var currentSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" });
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);

                    EditorUtility.DisplayProgressBar("收集本地化数据",
                        $"正在搜索场景 ({i + 1}/{sceneGuids.Length}): {Path.GetFileNameWithoutExtension(scenePath)}",
                        0.8f + (float)i / sceneGuids.Length * 0.2f);

                    try
                    {
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[本地化收集] 无法打开场景: {scenePath}，已跳过。\n{e.Message}");
                        continue;
                    }

                    LocalizeText[] sceneComponents = Object.FindObjectsOfType<LocalizeText>(true);
                    foreach (var comp in sceneComponents)
                    {
                        string objPath = $"场景: {scenePath} -> {GetGameObjectPath(comp.gameObject)}";
                        if (!ProcessLocalizeComponent(comp, objPath, collectedData))
                        {
                            warningCount++;
                        }
                    }
                }

                // 恢复原先打开的场景
                if (currentSceneSetup != null && currentSceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(currentSceneSetup);
                }
            }

            EditorUtility.ClearProgressBar();

            // 3. 保存为 Json
            if (collectedData.Count > 0)
            {
                string json = JsonConvert.SerializeObject(collectedData, Formatting.Indented);
                
                var folderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.excelDataFolderRootPath);
                if (!Directory.Exists(folderFullPath))
                {
                    Directory.CreateDirectory(folderFullPath);
                }
                
                string savePath = Path.Combine(folderFullPath, CollectedJsonFileName);
                File.WriteAllText(savePath, json);
                Debug.Log($"成功收集 {collectedData.Count} 条数据（{warningCount} 条警告），已保存至: {savePath}");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("未收集到任何有效的本地化数据。");
            }
        }

        /// <returns>true 表示正常处理，false 表示遇到警告</returns>
        private bool ProcessLocalizeComponent(LocalizeText comp, string objectPath, Dictionary<string, string> collectedData)
        {
            if (comp == null) return true;

            string key;
            string value;
            try
            {
                (key, value) = comp.GetLocalizeData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[本地化收集] 获取 {objectPath} 上的 LocalizeData 失败：{e.Message}", comp.gameObject);
                return false;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[本地化收集] 关键词为空！对象路径: {objectPath}", comp.gameObject);
                return false;
            }

            if (collectedData.TryGetValue(key, out var existingValue))
            {
                if (existingValue != value)
                {
                    Debug.LogWarning($"[本地化收集] 关键词重复且值不同！Key: {key}, 当前值: \"{value}\", 已存在值: \"{existingValue}\"。\n对象路径: {objectPath}", comp.gameObject);
                }
                return false;
            }

            collectedData.Add(key, value);
            return true;
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
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