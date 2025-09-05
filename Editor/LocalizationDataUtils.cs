using System;
using System.Collections.Generic;
using U0UGames.Framework;
using U0UGames.Framework.Utils;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using OfficeOpenXml;
using U0UGames.ExcelDataParser;
using U0UGames.Localization.Editor.AutoTranslate;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public class ExcelLineChangeInfo
    {
        public int lineIndex;
        public string key;
        public string originalValue;
        public string newValue;
        public string tips;
        
        public bool isNew;
        public bool isChanged;
        public bool isKeyChanged;
        public bool isError;

        public ExcelLineChangeInfo(int index, string keyword)
        {
            this.lineIndex = index;
            key = keyword;
        }
        
        public string GetChangeTypeString()
        {
            if (isKeyChanged) return "关键词改变";
            if (isNew) return "新增";
            if(isChanged)return "修改";
            if(isError)return "错误";
            return "";
        }
    }
    
    
    public static class LocalizationDataUtils
    {
        public const string LocalizeDataClassName = "LocalizeData";
        public const string LocalizeDataKeyName = "key";
        public const string LocalizeDataValueName = "value";

        public const string LocalizeDataTips1 = "tips";

        public const string LocalizationKeyName = "localizationKey";
        public const string LocalizationValueName = "localizationValue";
        public const string OriginalTextName = "originalText";
        public const string AuthorsNote = "Author's note";
        public const string TranslatorsNote = "Translator's note";
        public const string Proofread = "proofread";
        public const string LocalizationTips1 = "localizationTips1";

        public class LocalizeLineData
        {
            public string key;
            public Dictionary<string, string> translatedValues = new Dictionary<string, string>();
            public Dictionary<string, string> translateDataAllValues = new Dictionary<string, string>();

            public string tips;

            // 用于同步旧数据
            public string originalText;


            public bool IsEmpty()
            {
                var emptyKey = string.IsNullOrEmpty(key);
                var emptyTips = string.IsNullOrEmpty(tips);
                var emptyTranslateValue = true;
                if (translatedValues != null && translatedValues.Count > 0)
                {
                    foreach (var valueKvp in translatedValues)
                    {
                        if (!string.IsNullOrEmpty(valueKvp.Value))
                        {
                            emptyTranslateValue = false;
                            break;
                        }
                    }
                }

                return emptyKey && emptyTips && emptyTranslateValue;
            }

            public LocalizeLineData Clone()
            {
                var valuesClone = new Dictionary<string, string>(translatedValues.Count);
                foreach (var pair in translatedValues)
                {
                    valuesClone[pair.Key] = pair.Value;
                }

                return new LocalizeLineData()
                {
                    key = key,
                    tips = tips,
                    translatedValues = valuesClone,
                };
            }
        }

        public class LocalizationFileData
        {
            public string fileName;
            public string filePath;
            public List<LocalizeLineData> dataList = new();

            public class FileHeaderInfo
            {
                public string valueName;
                public string valueType;
                public int width;
            }

            public bool IsValid()
            {
                var haveAnyValidKey = false;
                var haveAnyValidText = false;
                foreach (var line in dataList)
                {
                    if (!string.IsNullOrEmpty(line.key))
                    {
                        haveAnyValidKey = true;
                    }
                    
                    if (!string.IsNullOrEmpty(line.originalText))
                    {
                        haveAnyValidText = true;
                    }

                    if (haveAnyValidText && haveAnyValidKey)
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool GetHeaderList(string currLanguageCode,out List<FileHeaderInfo> headerInfos)
            {
                headerInfos = new List<FileHeaderInfo>();
                
                LocalizeLineData valueNumLongestDataLine = null;
                int valueNumCount = -1;
                foreach (LocalizeLineData line in dataList)
                {
                    if (line.translateDataAllValues.Count > valueNumCount)
                    {
                        valueNumCount = line.translateDataAllValues.Count;
                        valueNumLongestDataLine = line;
                    }
                }
                if(valueNumLongestDataLine == null)return false;
                
                HashSet<string> headerList = new();
                string valueName = "#var";
                headerInfos.Add(new FileHeaderInfo()
                {
                    valueName = valueName,
                    valueType = "#type",
                    width = 10
                });
                headerList.Add(valueName);
                
                headerInfos.Add(new FileHeaderInfo()
                {
                    valueName = LocalizationKeyName,
                    valueType = "string",
                    width = 20
                });
                headerList.Add(LocalizationKeyName);
                
                
                headerInfos.Add(new FileHeaderInfo()
                {
                    valueName = LocalizeDataTips1,
                    valueType = "string",
                    width = 20
                });
                headerList.Add(LocalizeDataTips1);
                
                headerInfos.Add(new FileHeaderInfo()
                {
                    valueName = currLanguageCode,
                    valueType = "string",
                    width = 50
                });
                headerList.Add(currLanguageCode);
                
                foreach (var line in valueNumLongestDataLine.translateDataAllValues)
                {
                    var key = line.Key;
                    bool isExist = !headerList.Add(key);
                    if(isExist)continue;
                    
                    headerInfos.Add(new FileHeaderInfo()
                    {
                        valueName = key,
                        valueType = "string",
                        width = 50
                    });
                }
                return true;
            }
            
            public List<LocalizeLineData> GetDataListByKey(string key)
            {
                var sameKeyDataList = new List<LocalizeLineData>();
                foreach (var item in dataList)
                {
                    if (item.key == key)
                    {
                        sameKeyDataList.Add(item);
                    }
                }
                return sameKeyDataList;
            }
            
            public bool TryGetFirstMatchDataByKey(string key, out LocalizeLineData data)
            {
                foreach (var item in dataList)
                {
                    if (item.key == key)
                    {
                        data = item;
                        return true;
                    }
                }
                data = null;
                return false;
            }

            public bool TryGetDataByOriginText(string languageCode,string originText, out LocalizeLineData data)
            {
                data = null;
                foreach (var item in dataList)
                {
                    if (item.translatedValues[languageCode] == originText)
                    {
                        data = item;
                        return true;
                    }
                }
                return false;
            }
            // public Dictionary<string, LocalizeLineData> dataLookup = new();
            // public Dictionary<string, LocalizeLineData> originTextLookup = new();
        }

        private static bool TryGetStringValue(Dictionary<string, object> rawData, string keyName, out string value)
        {
            if (rawData.TryGetValue(keyName, out object keyObj) && keyObj is string target &&
                !string.IsNullOrEmpty(target))
            {
                value = target;
                return true;
            }

            value = null;
            return false;
        }

        public static string SelectFolderBtn(string currFolderPath, string selectTips)
        {
            var buttonText = currFolderPath;
            var defaultFolderPath = currFolderPath;
            if (string.IsNullOrEmpty(currFolderPath))
            {
                buttonText = "选择数据文件夹";
                defaultFolderPath = Application.dataPath;
            }

            if (GUILayout.Button(buttonText))
            {
                string selectFolderPath = EditorUtility.OpenFolderPanel(selectTips, defaultFolderPath, null);
                if (!string.IsNullOrEmpty(selectFolderPath))
                {
                    return selectFolderPath;
                }
            }

            return currFolderPath;
        }

        public static string SelectFileBtn(string currFilePath, string selectTips)
        {
            var buttonText = currFilePath;
            var defaultFolderPath = string.IsNullOrEmpty(currFilePath) ? null : Path.GetDirectoryName(currFilePath);
            if (string.IsNullOrEmpty(currFilePath))
            {
                buttonText = "选择数据文件";
                defaultFolderPath = Application.dataPath;
            }

            if (GUILayout.Button(buttonText))
            {
                string selectFolderPath = EditorUtility.OpenFilePanel(selectTips, defaultFolderPath, null);
                if (!string.IsNullOrEmpty(selectFolderPath))
                {
                    return selectFolderPath;
                }
            }

            return currFilePath;
        }

        private static LocalizationConfig _localizationConfig;

        private static LocalizeLineData GetLocalizeData(
            Dictionary<string, object> rawData,
            string valueLanguageCode,
            string keyName = LocalizeDataKeyName,
            string valueName = LocalizeDataValueName)
        {
            if (rawData == null) return null;

            if (_localizationConfig == null)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            if (!_localizationConfig)
            {
                ULog.LogError("Localization config is not set");
                return null;
            }

            var currLanguageCode = _localizationConfig.OriginalLanguageCode;
            var kvpData = new LocalizeLineData();
            TryGetStringValue(rawData, keyName, out kvpData.key);




            // 获得其他语种
            foreach (var languageData in _localizationConfig.languageDisplayDataList)
            {
                var languageCode = languageData.languageCode;
                TryGetStringValue(rawData, languageCode, out string translatedValue);
                kvpData.translatedValues[languageCode] = translatedValue;
            }

            // 如果指定了语言
            TryGetStringValue(rawData, valueName, out string originalTextByValue);
            if (originalTextByValue != null)
            {
                kvpData.translatedValues[valueLanguageCode] = originalTextByValue;
            }

            TryGetStringValue(rawData, OriginalTextName, out kvpData.originalText);
            if (kvpData.originalText == null)
            {
                kvpData.originalText = kvpData.translatedValues[currLanguageCode];
            }

            {
                string result = "";
                if (TryGetStringValue(rawData, LocalizeDataTips1, out var defaultTips1))
                {
                    result += defaultTips1;
                }

                if (string.IsNullOrEmpty(defaultTips1) && TryGetStringValue(rawData, LocalizationTips1, out var tips1))
                {
                    result += tips1;
                }

                if (!string.IsNullOrEmpty(result))
                {
                    kvpData.tips = result;
                }
                else
                {
                    TryGetStringValue(rawData, AuthorsNote, out kvpData.tips);
                }
            }

            foreach (KeyValuePair<string, object> kvp in rawData)
            {
                if (kvp.Value is string stringValue)
                {
                    kvpData.translateDataAllValues[kvp.Key] = stringValue;
                }
                else
                {
                    kvpData.translateDataAllValues[kvp.Key] = null;
                }
            }
            
            return kvpData;
        }

        private static List<LocalizeLineData> ProcessLocalizeData(string valueLanguageCode, object rawData)
        {
            List<LocalizeLineData> dataList = new List<LocalizeLineData>();
            switch (rawData)
            {
                case Dictionary<string, object> localizeClass:
                    var localizeClassKvpData = GetLocalizeData(localizeClass, valueLanguageCode);
                    if (localizeClassKvpData != null && !localizeClassKvpData.IsEmpty())
                    {
                        dataList.Add(localizeClassKvpData);
                    }

                    break;
                case List<object> localizeClassList:
                    foreach (var childClassData in localizeClassList)
                    {
                        if (childClassData is Dictionary<string, object> classInfo)
                        {
                            var kvpData = GetLocalizeData(classInfo, valueLanguageCode);
                            if (kvpData != null && !kvpData.IsEmpty())
                            {
                                dataList.Add(kvpData);
                            }
                        }
                    }

                    break;
            }

            return dataList;
        }



        private static List<LocalizeLineData> GetAllLocalizeData(string valueLanguageCode,
            Dictionary<string, object> lineRawData, Dictionary<string, string> typeLookUp)
        {
            List<string> localizeDataPathList = new List<string>(lineRawData.Count);
            // 找到包含LocalizeData的路径
            foreach (var pathKvp in typeLookUp)
            {
                if (pathKvp.Value.Contains(LocalizeDataClassName))
                {
                    localizeDataPathList.Add(pathKvp.Key);
                }
            }

            List<LocalizeLineData> dataList = new List<LocalizeLineData>();
            // 解析路径对应的数据
            foreach (var namePath in localizeDataPathList)
            {
                var namesList = namePath.Split(".").ToList();
                var localizeRawDataList = ExcelReader.GetRawDataAtPath(namesList, lineRawData);
                if (localizeRawDataList == null || localizeDataPathList.Count == 0)
                {
                    continue;
                }

                foreach (var localizeRawData in localizeRawDataList)
                {
                    dataList.AddRange(ProcessLocalizeData(valueLanguageCode, localizeRawData));
                }
            }

            return dataList;
        }


        private static List<LocalizeLineData> GetLineData(string valueLanguageCode,
            Dictionary<string, object> rawDataLine, Dictionary<string, string> typeLookUp)
        {
            List<LocalizeLineData> dataList = new List<LocalizeLineData>();
            // 先添加行中符合关键词的内容
            {
                LocalizeLineData rootKvpData = GetLocalizeData(rawDataLine, valueLanguageCode,
                    LocalizationKeyName, LocalizationValueName);
                if (rootKvpData != null && !rootKvpData.IsEmpty())
                {
                    dataList.Add(rootKvpData);
                }
            }

            // 递归添加行中所有本地化数据
            var localizeData = GetAllLocalizeData(valueLanguageCode, rawDataLine, typeLookUp);
            if (localizeData != null && localizeData.Count > 0)
            {
                foreach (var data in localizeData)
                {
                    if (data != null && !data.IsEmpty())
                    {
                        dataList.Add(data);
                    }
                }
            }

            return dataList;
        }

        private static List<LocalizeLineData> ProcessExcelSheet(string valueLanguageCode,
            Dictionary<string, object> dataSheet)
        {
            // 数据表为空 return
            if (dataSheet == null || dataSheet.Count == 0) return null;
            // 表中没有数据 return
            if (!dataSheet.TryGetValue(ExcelReader.RawDataKey.dataList, out var rawDataList))
            {
                return null;
            }

            if (!dataSheet.TryGetValue(ExcelReader.RawDataKey.typeLookup, out var dataTypeRawList))
            {
                return null;
            }

            // 表中数据类型错误 return
            if (rawDataList is not List<Dictionary<string, object>> dataList)
            {
                return null;
            }

            if (dataTypeRawList is not Dictionary<string, string> typeList)
            {
                return null;
            }

            List<LocalizeLineData> lineDataList = new List<LocalizeLineData>();
            foreach (var dataLine in dataList)
            {
                var kvpList = GetLineData(valueLanguageCode, dataLine, typeList);
                if (kvpList == null) continue;
                lineDataList.AddRange(kvpList);
            }

            if (lineDataList.Count == 0) return null;
            return lineDataList;
        }

        public static List<LocalizeLineData> ProcessExcelFile(string valueLanguageCode, string excelFilePath)
        {
            List<Dictionary<string, object>> rawData = ExcelReader.GetRawData(excelFilePath);
            if (rawData == null) return null;
            List<LocalizeLineData> fileDataList = new List<LocalizeLineData>();
            foreach (var dataSheet in rawData)
            {
                List<LocalizeLineData> sheetDataList = ProcessExcelSheet(valueLanguageCode, dataSheet);
                if (sheetDataList != null)
                {
                    fileDataList.AddRange(sheetDataList);
                }
            }

            return fileDataList;
        }

        private static LocalizationFileData GetFileData(string currLanguage,string valueLanguageCode, string filePath)
        {
            // 跳过临时文件
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if(fileName.StartsWith("~$"))return null;
                
            LocalizationFileData fileData = new LocalizationFileData();
            fileData.fileName = fileName;
            fileData.filePath = filePath;
            fileData.dataList = ProcessExcelFile(valueLanguageCode, filePath);
            
            // foreach (var dataLine in fileData.dataList)
            // {
                // if (dataLine.key != null)
                // {
                //     fileData.dataLookup[dataLine.key] = dataLine;
                // }

                // var originText = dataLine.originalText;
                // if (string.IsNullOrEmpty(originText))
                // {
                //     originText = dataLine.translatedValues[currLanguage];
                // }
                // if (originText != null)
                // {
                //     fileData.originTextLookup[originText] = dataLine;
                // }
            // }
            return fileData;
        }

        private static List<LocalizationFileData> GetFileDataList(string valueLanguageCode, string[] filePath)
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            var currLanguage = _localizationConfig.OriginalLanguageCode;
            List<LocalizationFileData> fileDataList = new List<LocalizationFileData>(filePath.Length);
            foreach (var excelFile in filePath)
            {
                var fileData = GetFileData(valueLanguageCode, currLanguage, excelFile);
                if(fileData == null)continue;
                fileDataList.Add(fileData);
            }
            return fileDataList;
        }
 
        public static List<LocalizationFileData> GetAllLocalizationDataFromDataFolder(string valueLanguageCode, string rawExcelRootFolderPath)
        {
            if (string.IsNullOrEmpty(rawExcelRootFolderPath)) return null;
            string fullPath = UnityPathUtility.RootFolderPathToFullPath(rawExcelRootFolderPath);
            if (!Directory.Exists(fullPath)) return null;
            
            var filePath = Directory.GetFiles(fullPath, "*.xlsx");
            return GetFileDataList(valueLanguageCode, filePath);
        }

        public static LocalizationFileData GetLocalizationData(string valueLanguageCode, string rawExcelFileRootPath)
        {
            if (string.IsNullOrEmpty(rawExcelFileRootPath)) return null;
            string fullPath = UnityPathUtility.RootFolderPathToFullPath(rawExcelFileRootPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError(string.Format("Can't find local file {0}", fullPath));
                return null;
            }
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }
            var currLanguage = _localizationConfig.OriginalLanguageCode;
            return GetFileData(currLanguage, valueLanguageCode, fullPath);
        }

        private static string[] Translate(string from, string to, string[] textList)
        {
            return BatchV3DemoInternalTest.Translate(from, to, textList);
        }

        private static bool TranslateInter(string srcLanguageCode, string targetLanguageCode, List<string> needTranslateTextList,List<LocalizeLineData> needTranslateDataList)
        {
            var translateResult = Translate(srcLanguageCode, targetLanguageCode,
                needTranslateTextList.ToArray());
            if (translateResult != null)
            {
                for (int i = 0; i < needTranslateDataList.Count; i++)
                {
                    needTranslateDataList[i].translatedValues[targetLanguageCode] = translateResult[i];
                    var oldTips = needTranslateDataList[i].tips;
                    if(string.IsNullOrEmpty(oldTips) || !oldTips.Contains("YDT"))
                        needTranslateDataList[i].tips = oldTips + "YDT";
                }

                return true;
            }

            return false;
        }
        
        public const int MaxTextSize = 4500;
        public static void Translate(string srcLanguageCode, string targetLanguageCode)
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            if (_localizationConfig == null)
            {
                Debug.LogWarning("Localization config not initialized.");
                return;
            }
            
            var currLocalizationFileDataList = 
                LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(srcLanguageCode,_localizationConfig.translateDataFolderRootPath);
            var translateDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);
            if (!Directory.Exists(translateDataFolderFullPath))
            {
                Directory.CreateDirectory(translateDataFolderFullPath);
            }

            if(currLocalizationFileDataList == null || currLocalizationFileDataList.Count == 0)return;
            foreach (var fileData in currLocalizationFileDataList)
            {
                var fileName = fileData.fileName;

                int totalIndex = 0;
                while (totalIndex < fileData.dataList.Count)
                {
                    EditorUtility.DisplayProgressBar($"将{srcLanguageCode}翻译至{targetLanguageCode}",$"正在翻译{fileName}",totalIndex/(float)fileData.dataList.Count);

                    int textSize = 0;
                    List<LocalizeLineData> needTranslateDataList = new List<LocalizeLineData>();
                    List<string> needTranslateTextList = new List<string>();
                    
                    // 收集翻译数据包
                    for (var startIndex = totalIndex; startIndex < fileData.dataList.Count; startIndex++)
                    {
                        var kvpData = fileData.dataList[startIndex];
                        // 只翻译没有翻译过且游戏内正在使用的文本，即翻译文本为空，原文不为空，关键词不为空
                        var key = kvpData.key;
                        var originalText = kvpData.translatedValues[srcLanguageCode];
                        var targetText = kvpData.translatedValues[targetLanguageCode];
                        if (!string.IsNullOrEmpty(key) && string.IsNullOrEmpty(targetText) && !string.IsNullOrEmpty(originalText))
                        {
                            var newTextLength = originalText.Length;
                            var newDataSize = textSize + newTextLength;
                            if (newDataSize > MaxTextSize)
                            {
                                break;
                            }
                            textSize += newTextLength;
                            needTranslateDataList.Add(kvpData);
                            needTranslateTextList.Add(originalText);
                        }
                        totalIndex++;
                    }

                    // 将收集到的数据提交翻译
                    if (textSize <= MaxTextSize && textSize>=0 && needTranslateTextList.Count > 0)
                    {
                        bool success = TranslateInter(srcLanguageCode, targetLanguageCode,
                            needTranslateTextList, needTranslateDataList);
                        if (!success)
                        {
                            ULog.LogError($"翻译失败:{fileName} ({srcLanguageCode}翻译至{targetLanguageCode})");
                        }
                    }
                }

                LocalizationDataUtils.ConvertToExcelFile(
                    fileData,
                    translateDataFolderFullPath
                );

            }
            
            EditorUtility.ClearProgressBar();
        }
        
        // public static string GetLocalizationDataFileName(string languageCode, string rawDataFileName)
        // {
        //     return $"{rawDataFileName}_{languageCode}";
        // }

        // public static string LocalizationFileNameRecover(string languageCode, string LocalizationFileName)
        // {
        //     string additionalData = $"_{languageCode}";
        //     if (LocalizationFileName.Contains(additionalData))
        //     {
        //         return LocalizationFileName.Replace(additionalData, "");
        //     }
        //     return LocalizationFileName;
        // }
        private static readonly int ExcelDataLineStartIndex = 3;
        public static void ConvertChangeInfoToExcelFile(Dictionary<string,List<ExcelLineChangeInfo>> changeInfoList, string fileName, string folderPath)
        {
            // 创建ExcelPackage对象
            using (ExcelPackage package = new ExcelPackage())
            {
                // 添加一个工作表
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");
                
                // 设置单元格的值
                worksheet.Cells[1, 1].Value = "文件名称";
                worksheet.Column(1).Width = 10; 

                worksheet.Cells[1, 2].Value = "行数";
                worksheet.Column(2).Width = 10; 
                
                worksheet.Cells[1, 3].Value = "关键词";
                worksheet.Column(3).Width = 20; 
                
                worksheet.Cells[1, 4].Value = "原文本";
                worksheet.Column(4).Width = 50; 
                
                worksheet.Cells[1, 5].Value = "现文本";
                worksheet.Column(5).Width = 50; 
                
                worksheet.Cells[1, 6].Value = "修改类型";
                worksheet.Column(6).Width = 10; 
                
                worksheet.Cells[1, 7].Value = "修改时间";
                worksheet.Column(7).Width = 10; 
                
                // 填入数据
                var timeString = DateTime.Now.ToString("D");

                int lineIndex = 2;
                foreach (var changeInfoKvp in changeInfoList)
                {
                    var changeFileName = changeInfoKvp.Key;
                    var infoList = changeInfoKvp.Value;
                    if(infoList == null || infoList.Count == 0)continue;
                    foreach (var changeInfo in changeInfoKvp.Value)
                    {
                        worksheet.Cells[lineIndex, 1].Value = changeFileName;
                        worksheet.Cells[lineIndex, 2].Value = (changeInfo.lineIndex+ExcelDataLineStartIndex).ToString();
                        worksheet.Cells[lineIndex, 3].Value = string.IsNullOrEmpty(changeInfo.key) ? "" : changeInfo.key;
                        worksheet.Cells[lineIndex, 4].Value = string.IsNullOrEmpty(changeInfo.originalValue) ? "" : changeInfo.originalValue;
                        worksheet.Cells[lineIndex, 5].Value = string.IsNullOrEmpty(changeInfo.newValue) ? "" : changeInfo.newValue;
                        worksheet.Cells[lineIndex, 6].Value = changeInfo.GetChangeTypeString();
                        worksheet.Cells[lineIndex, 7].Value = timeString;
                        lineIndex++;
                    }
                }

                var fileFullPath = $@"{folderPath}\{fileName}.xlsx";
                // 保存Excel文件
                if (folderPath!=null && !Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                FileInfo file = new FileInfo(fileFullPath);
                try
                {
                    package.SaveAs(file);
                }
                catch(Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }


        public static void ConvertToTranslateExcelFile(LocalizationFileData fileData, string folderPath)
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            if (!_localizationConfig)
            {
                ULog.LogError("找不到LocalizationConfig");
                return;
            }
            var currLanguageCode = _localizationConfig.OriginalLanguageCode;

            var getHeaderInfo = fileData.GetHeaderList(currLanguageCode,out var headerInfos);
            if (!getHeaderInfo)
            {
                ULog.LogError($"无法得到{fileData.fileName}表头数据");
                return;
            }
            
             // 创建ExcelPackage对象
            using (ExcelPackage package = new ExcelPackage())
            {
                // 添加一个工作表
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");
                
                // 设置单元格的值
                for (var index = 0; index < headerInfos.Count; index++)
                {
                    worksheet.Cells[1, index+1].Value = headerInfos[index].valueName;
                    worksheet.Cells[2, index+1].Value = headerInfos[index].valueType;
                    worksheet.Column(index+1).Width = headerInfos[index].width;
                }
                
                // 填入数据
                int lineIndex = ExcelDataLineStartIndex;
                foreach (var lineData in fileData.dataList)
                {
                    for (var index = 0; index < headerInfos.Count; index++)
                    {
                        var valueName = headerInfos[index].valueName;
                        if (valueName == LocalizationKeyName)
                        {
                            worksheet.Cells[lineIndex, index+1].Value = lineData.key;
                            continue;
                        }
                        if (valueName == LocalizeDataTips1)
                        {
                            worksheet.Cells[lineIndex, index+1].Value = lineData.tips;
                            worksheet.Cells[lineIndex, index+1].Style.WrapText = true;
                            continue;
                        }
                        if (valueName == currLanguageCode)
                        {
                            worksheet.Cells[lineIndex, index+1].Value = lineData.translatedValues[currLanguageCode];
                            worksheet.Cells[lineIndex, index+1].Style.WrapText = true;
                            continue;
                        }
                        if (lineData.translateDataAllValues.TryGetValue(valueName, out var translateDataValue))
                        {
                            worksheet.Cells[lineIndex, index+1].Value = translateDataValue;
                            worksheet.Cells[lineIndex, index+1].Style.WrapText = true;
                        }
                    }
                    lineIndex++;
                }

                var fileFullPath = $@"{folderPath}\{fileData.fileName}.xlsx";
                // 保存Excel文件
                if (folderPath!=null && !Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                FileInfo file = new FileInfo(fileFullPath);
                try
                {
                    package.SaveAs(file);
                }
                catch(Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
        
        public static void ConvertToExcelFile(LocalizationFileData fileData, string folderPath)
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            if (!_localizationConfig)
            {
                ULog.LogError("找不到LocalizationConfig");
                return;
            }

            var currLanguageCode = _localizationConfig.OriginalLanguageCode;
            Dictionary<string,int> languageStartCollIndex = new Dictionary<string,int>();
            
            // 创建ExcelPackage对象
            using (ExcelPackage package = new ExcelPackage())
            {
                // 添加一个工作表
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");
                
                // 设置单元格的值
                worksheet.Cells[1, 1].Value = "#var";
                worksheet.Cells[2, 1].Value = "#type";
                worksheet.Column(1).Width = 10; 
                
                worksheet.Cells[1, 2].Value = LocalizationKeyName;
                worksheet.Cells[2, 2].Value = "string";
                worksheet.Column(2).Width = 20;

                worksheet.Cells[1, 3].Value = LocalizeDataTips1;
                worksheet.Cells[2, 3].Value = "string";
                worksheet.Column(3).Width = 20;
                
                worksheet.Cells[1, 4].Value = currLanguageCode;
                worksheet.Cells[2, 4].Value = "string";
                worksheet.Column(4).Width = 50;
                languageStartCollIndex[currLanguageCode] = 4;
                
                int startIndex = 5;
                foreach (var languageData in _localizationConfig.languageDisplayDataList)
                {
                    var languageCode = languageData.languageCode;
                    if(languageCode == currLanguageCode)continue;
                    
                    worksheet.Cells[1, startIndex].Value = languageCode;
                    worksheet.Cells[2, startIndex].Value = "string";
                    languageStartCollIndex[languageCode] = startIndex;
                    worksheet.Column(startIndex).Width = 50; 

                    startIndex++;
                }
                
                // 填入数据
                int lineIndex = ExcelDataLineStartIndex;
                foreach (var lineData in fileData.dataList)
                {
                    // if (lineData.isEmpty)
                    // {
                    //     continue;
                    // }
                    worksheet.Cells[lineIndex, 2].Value = lineData.key;
                    worksheet.Cells[lineIndex, 3].Value = lineData.tips;
                    foreach (var langData in _localizationConfig.languageDisplayDataList)
                    {
                        var langCode = langData.languageCode;
                        var columnIndex = languageStartCollIndex[langCode];
                        worksheet.Cells[lineIndex, columnIndex].Value = lineData.translatedValues[langCode];
                        worksheet.Cells[lineIndex, columnIndex].Style.WrapText = true;
                    }
                    lineIndex++;
                }

                var fileFullPath = $@"{folderPath}\{fileData.fileName}.xlsx";
                // 保存Excel文件
                if (folderPath!=null && !Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                FileInfo file = new FileInfo(fileFullPath);
                try
                {
                    package.SaveAs(file);
                }
                catch(Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
        
    }
}