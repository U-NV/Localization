using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace U0UGames.Localization.Editor
{

    
    public class LocalizationDataProcessWindow
    {
        public class EditorPrefsKey
        {
            // public const string DataProcessBarModeIndex = 
            //     "LocalizationDataProcessWindow.DataProcessBarModeIndex";
            public const string NeedClearAllFilesBeforeGenerate = 
                "LocalizationDataProcessWindow.NeedClearAllFilesBeforeGenerate";
            public const string NeedClearAllFilesBeforeSync = 
                "LocalizationDataProcessWindow.NeedClearAllFilesBeforeSync";
            public const string AdditionalTextFilePath = 
                "LocalizationDataProcessWindow.AdditionalTextFilePath";
            public const string SyncDataFolderPath =
                "LocalizationDataProcessWindow.SyncDataFolderPath";
        }

        // private int _languageCodeIndex;
        private LocalizationConfig _localizationConfig;
        private LocalizationExcelExporter _excelExporter;
        // private bool _needClearAllFilesBeforeGenerate = false;
        private bool _needClearAllFilesBeforeSync = false;

        private string _additionalTextFileAssetPath = "";
        // private int _dataProcessBarModeIndex = 0;
        public void Init()
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }
            _excelExporter = new LocalizationExcelExporter(_localizationConfig);
            // _needClearAllFilesBeforeGenerate = 
            //     EditorPrefs.GetBool(EditorPrefsKey.NeedClearAllFilesBeforeGenerate);
            
            _needClearAllFilesBeforeSync = 
                EditorPrefs.GetBool(EditorPrefsKey.NeedClearAllFilesBeforeSync);
            
            _additionalTextFileAssetPath = EditorPrefs.GetString(EditorPrefsKey.AdditionalTextFilePath);
            _translateSyncDataFolderPath = EditorPrefs.GetString(EditorPrefsKey.SyncDataFolderPath, "");
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
   

        
        

        private string _statisticsResult = "";
        private int CountChineseCharacters(string input)
        {
            // 汉字的Unicode范围
            var range = new[] { '\u4e00', '\u9fff' };
            return input.Count(c => c >= range[0] && c <= range[1]);
        }
        
        private void StatisticsOriginData()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("统计", EditorStyles.boldLabel);

            var languageCode = _localizationConfig.OriginalLanguageCode;
            if (string.IsNullOrEmpty(languageCode))
            {
                EditorGUILayout.LabelField("未选择语言");
                EditorGUILayout.EndVertical();
                return;
            }

            var infoText = string.IsNullOrEmpty(_statisticsResult)?"点击下方按钮开始统计":_statisticsResult;
            EditorGUILayout.TextArea(infoText,EditorStyles.wordWrappedLabel);
            
            string info = $"统计{languageCode}的文本数量";
            if (GUILayout.Button(info))
            {
                EditorUtility.DisplayProgressBar("统计字符数",info,0);
                {
                    int charNum = 0;
                    int chineseCharNum = 0;
                    
                    int inUseCharNum = 0;
                    int inUseChineseCharNum = 0;
                    
                    var dataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(languageCode, _localizationConfig.excelDataFolderRootPath);
                    if (dataList != null)
                    {
                        for (var index = 0; index < dataList.Count; index++)
                        {
                            var fileData = dataList[index];
                            float progress = (float)index / dataList.Count;
                            EditorUtility.DisplayProgressBar("统计字符数", $"统计{languageCode}:{fileData.fileName}文本数量", progress);

                            if (fileData.dataList == null || fileData.dataList.Count == 0) continue;
                            foreach (var lineData in fileData.dataList)
                            {
                                if (lineData == null) continue;
                                var text = lineData.translatedValues[languageCode];

                                if (string.IsNullOrEmpty(text)) continue;

                                var length = text.Length;
                                var lengthWithoutRichText = CountChineseCharacters(text);
                                charNum += length;
                                chineseCharNum += lengthWithoutRichText;

                                if (!string.IsNullOrEmpty(lineData.key))
                                {
                                    inUseCharNum += length;
                                    inUseChineseCharNum += lengthWithoutRichText;
                                }
                            }
                        }
                    }
                    _statisticsResult = $"所有文本\n" +
                                        $"字数统计: {charNum:N0}\t 字数统计(纯中文): {chineseCharNum:N0}\n\n" +
                                        $"游戏内使用的文本\n" +
                                        $"字数统计: {inUseCharNum:N0}\t 字数统计(纯中文): {inUseChineseCharNum:N0}";
                    
                }
                EditorUtility.ClearProgressBar();
            }
            EditorGUILayout.EndVertical();
        }

        private const string AllCharFileName = "AllLocalizedText.txt";
        private void GetAllChar()
        {
            var languageCode = _localizationConfig.OriginalLanguageCode;
            if (string.IsNullOrEmpty(languageCode))
            {
                EditorGUILayout.LabelField("未选择语言");
                return;
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("获得所有文字", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("额外文本路径:",GUILayout.Width(12*8));
            var btnInfo = string.IsNullOrEmpty(_additionalTextFileAssetPath)?"空":_additionalTextFileAssetPath;
            if (GUILayout.Button(btnInfo))
            {
                var defaultFolderPath = string.IsNullOrEmpty(_additionalTextFileAssetPath) ? 
                    Application.dataPath : Path.GetDirectoryName(UnityPathUtility.AssetPathToFullPath(_additionalTextFileAssetPath));
            
                var newFilePath = EditorUtility.OpenFilePanel("选择文件", defaultFolderPath, "txt");
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    _additionalTextFileAssetPath = UnityPathUtility.FullPathToAssetPath(newFilePath);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            

            
            var allCharFileSaveFolderFullPath = Path.Join(Application.dataPath, LocalizationManager.LocalizationResourcesFolder);
            var folderAssetPath = UnityPathUtility.FullPathToAssetPath(allCharFileSaveFolderFullPath);
            var assetPath = Path.Join(folderAssetPath, AllCharFileName);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("生成文件路径:", GUILayout.Width(12*8));
                if (GUILayout.Button(assetPath))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("生成文件"))
            {
                if (!Directory.Exists(allCharFileSaveFolderFullPath))
                {
                    Directory.CreateDirectory(allCharFileSaveFolderFullPath);
                }

                var charHashSet = new HashSet<char>(10000);
                var allTextString = new StringBuilder();
                EditorUtility.DisplayProgressBar("统计字符","统计字符",0);
                {
                    var dataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(languageCode, _localizationConfig.translateDataFolderRootPath);
                    if (dataList != null)
                    {
                        for (var index = 0; index < dataList.Count; index++)
                        {
                            var fileData = dataList[index];
                            float progress = (float)index / dataList.Count;
                            EditorUtility.DisplayProgressBar("统计字符", $"统计{languageCode}:{fileData.fileName}文本", progress);

                            if (fileData.dataList == null || fileData.dataList.Count == 0) continue;
                            foreach (var lineData in fileData.dataList)
                            {
                                if (lineData == null) continue;
                                foreach (var localizedTextKvp in lineData.translatedValues)
                                {
                                    var text = localizedTextKvp.Value;
                                    if (string.IsNullOrEmpty(text)) continue;
                                    foreach (var c in text)
                                    {
                                        if (charHashSet.Add(c))
                                        {
                                            allTextString.Append(c);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (_additionalTextFileAssetPath != null)
                    {
                        var fullPath = UnityPathUtility.AssetPathToFullPath(_additionalTextFileAssetPath);
                        if (File.Exists(fullPath))
                        {
                            var additionalText = File.ReadAllText(fullPath);
                            foreach (var c in additionalText)
                            {
                                if (charHashSet.Add(c))
                                {
                                    allTextString.Append(c);
                                }
                            }
                        }
                    }
                    
                }
                EditorUtility.ClearProgressBar();
                
                var assetFullPath = UnityPathUtility.AssetPathToFullPath(assetPath);
                File.WriteAllText(assetFullPath, allTextString.ToString());
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndVertical();
        }

        private void SyncToTranslateData(
            string currLanguageCode,
            HashSet<string> translationLanguageCodes,
            LocalizationDataUtils.LocalizeLineData syncDataLine,
            LocalizationDataUtils.LocalizeLineData translateDataLine)
        {
            foreach (var newData in syncDataLine.translateDataAllValues)
            {
                var key = newData.Key;

                // 原文列：以本地为准，跳过
                if (key == currLanguageCode) continue;

                // 译文列：以回收表格为准，直接覆盖（保留翻译人员的全部工作成果）
                if (translationLanguageCodes.Contains(key))
                {
                    translateDataLine.translateDataAllValues[key] = newData.Value;
                    continue;
                }

                // 系统列（lTips1、lTips2 等）：仅补空，不覆盖本地内容
                if (!translateDataLine.translateDataAllValues.TryGetValue(key, out var currValue)
                    || string.IsNullOrEmpty(currValue))
                {
                    translateDataLine.translateDataAllValues[key] = newData.Value;
                }
            }
        }
        
        private void SyncTranslateData(string currLanguageCode, string syncDataFolder)
        {
            var syncDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, syncDataFolder);
            var translateDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.translateDataFolderRootPath);
            if (syncDataList == null || syncDataList.Count == 0)
            {
                Debug.LogError("找不到任何可以同步的文件");
                return;
            }
            var translateDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);
            if (!Directory.Exists(translateDataFolderFullPath))
            {
                Directory.CreateDirectory(translateDataFolderFullPath);
            }

            // 构建译文语言码集合（排除原文语言，用于判断哪些列以回收表格为准）
            var translationLanguageCodes = new HashSet<string>();
            foreach (var langConfig in _localizationConfig.languageDisplayDataList)
            {
                if (langConfig.languageCode != currLanguageCode)
                    translationLanguageCodes.Add(langConfig.languageCode);
            }

            for (var index = 0; index < syncDataList.Count; index++)
            {
                var syncDataFile = syncDataList[index];
                string info = $"将{syncDataFile.fileName}中的数据同步到翻译表格";

                // 查找有无同名翻译文件
                LocalizationDataUtils.LocalizationFileData sameNameTranslateData = null;
                foreach (var translateData in translateDataList)
                {
                    if (translateData.fileName == syncDataFile.fileName)
                    {
                        sameNameTranslateData = translateData;
                        break;
                    }
                }

                // 不存在同名翻译文件
                if (sameNameTranslateData == null)
                {
                    Debug.LogWarning($"找不到{syncDataFile.fileName}对应的翻译文件");
                    continue;
                }

                // 遍历同步数据的每一行，查找翻译文件中有无对应的关键词
                foreach (var syncDataLine in syncDataFile.dataList)
                {
                    var dataKey = syncDataLine.key;
                    // 没有设置关键词则跳过
                    if (string.IsNullOrEmpty(dataKey)) continue;
                    var translateDataLineList = sameNameTranslateData.GetDataListByKey(dataKey);

                    // 翻译文件中找不到对应的关键词，则尝试查找原文
                    if (translateDataLineList == null || translateDataLineList.Count == 0)
                    {
                        if (syncDataLine.translatedValues.TryGetValue(currLanguageCode, out var originText))
                        {
                            if (sameNameTranslateData.TryGetDataByOriginText(currLanguageCode, originText,
                                    out var translateDataLine))
                            {
                                SyncToTranslateData(currLanguageCode, translationLanguageCodes, syncDataLine, translateDataLine);
                            }
                        }
                        continue;
                    }

                    // 找到对应的关键词则直接同步
                    foreach (var translateDataLine in translateDataLineList)
                    {
                        SyncToTranslateData(currLanguageCode, translationLanguageCodes, syncDataLine, translateDataLine);
                    }
                }
                // 将填入了翻译数据的原始文件保存到对应路径
                LocalizationDataUtils.ConvertToTranslateExcelFile(sameNameTranslateData, translateDataFolderFullPath);
                float progress = (float)index / (float)syncDataList.Count;
                EditorUtility.DisplayProgressBar("同步翻译表格", info, progress);
            }
        }


        private string _translateSyncDataFolderPath = "";
        private void SelectSyncDataFolder()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                string titleName = "下载文件夹:";
                EditorGUILayout.LabelField(titleName,GUILayout.Width(12*titleName.Length));
                string buttonText = string.IsNullOrEmpty(_translateSyncDataFolderPath)? Application.dataPath : _translateSyncDataFolderPath;
                if (GUILayout.Button(buttonText))
                {
                    var newPath = EditorUtility.OpenFolderPanel(titleName, buttonText,null);
                if (!string.IsNullOrEmpty(newPath))
                {
                    _translateSyncDataFolderPath = newPath;
                    EditorPrefs.SetString(EditorPrefsKey.SyncDataFolderPath, newPath);
                }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SyncTranslateFileData()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("收回并更新翻译表格", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "用途：翻译人员将部分内容翻译完毕后，将最新的翻译表格下载到本地，\n" +
                "选择下载文件夹后点击按钮，将自动完成：\n" +
                "  1. 把下载表格中的译文填入本地翻译表格\n" +
                "  2. 将游戏内文本的最新变动（增/删/改）合并进来\n" +
                "  3. 重新导出带颜色标注的翻译表格，供翻译人员继续工作",
                EditorStyles.helpBox);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("下载文件夹（翻译人员提供的最新表格所在目录）：", EditorStyles.miniLabel);
            SelectSyncDataFolder();
            
            var languageCode = _localizationConfig.OriginalLanguageCode;
            if (string.IsNullOrEmpty(languageCode))
            {
                EditorGUILayout.LabelField("Error: 未选择原文语言，请先在配置界面中设置", EditorStyles.helpBox);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("收回译文到翻译表格"))
            {
                EditorUtility.DisplayProgressBar("同步译文", "正在将下载表格中的译文填入本地...", 0);
                
                var rootSyncFolderPath = UnityPathUtility.FullPathToRootFolderPath(_translateSyncDataFolderPath);
                SyncTranslateData(languageCode, rootSyncFolderPath);

                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndVertical();
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Separator();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                StatisticsOriginData();
                EditorGUILayout.EndVertical();
            }  

            EditorGUILayout.Separator();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                SyncTranslateFileData();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Separator();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GetAllChar();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();

        }


    }
}