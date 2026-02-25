using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public class LocalizationExcelExporter
    {
        private readonly LocalizationConfig _localizationConfig;
        private Dictionary<string, List<ExcelLineChangeInfo>> _excelChangeInfoLookup = new();
        private Dictionary<string, string> _rawDataKeyVisitMark = new();

        public LocalizationExcelExporter(LocalizationConfig config)
        {
            _localizationConfig = config;
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

        private void AddChangeInfo(string fileName, ExcelLineChangeInfo info)
        {
            if (!_excelChangeInfoLookup.TryGetValue(fileName, out var list))
            {
                list = new List<ExcelLineChangeInfo>();
                _excelChangeInfoLookup[fileName] = list;
            }
            list.Add(info);
        }

        private void FillSameKeyLineData(string currLanguageCode,
            string fileName, int rawDataLineIndex,
            LocalizationDataUtils.LocalizeLineData rawDataLine,
            LocalizationDataUtils.LocalizeLineData targetTranslateData)
        {
            foreach (var languageData in _localizationConfig.languageDisplayDataList)
            {
                var languageCode = languageData.languageCode;
                if (currLanguageCode == languageCode)
                {
                    var rawText = rawDataLine.translatedValues[languageCode];
                    var translateText = targetTranslateData.translatedValues[languageCode];
                    if (rawText != translateText)
                    {
                        var changeTips = GetOriginTextChangeTips(translateText, rawText);
                        var tips2 = rawDataLine.tips2;
                        tips2 ??= "";
                        if (!tips2.EndsWith(changeTips))
                        {
                            rawDataLine.tips2 += string.IsNullOrEmpty(rawDataLine.tips2) ? changeTips : "\n" + changeTips;
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
                rawDataLine.translatedValues[languageCode] = targetTranslateData.translatedValues[languageCode];
            }
        }

        private bool TryFillDataWithSameKey(string currLanguageCode,
            LocalizationDataUtils.LocalizationFileData translateFileData,
            LocalizationDataUtils.LocalizeLineData rawDataLine, int rawDataLineIndex)
        {
            if (rawDataLine == null) return false;
            var lineDataKey = rawDataLine.key;
            if (string.IsNullOrEmpty(lineDataKey)) return false;
            var sameKeyTranslateDataList = translateFileData.GetDataListByKey(lineDataKey);
            if (sameKeyTranslateDataList == null || sameKeyTranslateDataList.Count == 0)
            {
                return false;
            }

            if (sameKeyTranslateDataList.Count > 1)
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
            foreach (var languageData in _localizationConfig.languageDisplayDataList)
            {
                var languageCode = languageData.languageCode;
                if (currLanguageCode == languageCode) continue;
                rawDataLine.translatedValues[languageCode] = targetTranslateDataLine.translatedValues[languageCode];
            }
            return true;
        }

        private void FillRawDataWithTranslateData(string currLanguageCode,
            LocalizationDataUtils.LocalizationFileData rawExcelFileData,
            LocalizationDataUtils.LocalizationFileData sameNameTranslateData)
        {
            for (var index = 0; index < rawExcelFileData.dataList.Count; index++)
            {
                LocalizationDataUtils.LocalizeLineData rawDataLine = rawExcelFileData.dataList[index];
                if (TryFillDataWithSameKey(currLanguageCode, sameNameTranslateData, rawDataLine, index))
                {
                    continue;
                }

                if (TryFillDataWithSameOriginText(currLanguageCode, sameNameTranslateData, rawDataLine, index))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(rawDataLine.key))
                {
                    rawDataLine.translateDataAllValues = rawDataLine.translatedValues;
                    continue;
                }
                rawDataLine.translateDataAllValues.Clear();
                rawDataLine.tips2 ??= "";
                var newTextTips = GetNewTextTips();
                if (!rawDataLine.tips2.EndsWith(newTextTips))
                {
                    rawDataLine.tips2 += string.IsNullOrEmpty(rawDataLine.tips2) ? newTextTips : "\n" + newTextTips;
                }
                AddChangeInfo(rawExcelFileData.fileName, new ExcelLineChangeInfo(index, rawDataLine.key)
                {
                    originalValue = "",
                    newValue = rawDataLine.translatedValues[currLanguageCode],
                    isNew = true
                });
            }
        }

        private void SaveExcelChangeInfoList(string folderName)
        {
            if (!_localizationConfig.saveExportDiffFile)
            {
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

        public void ExportTranslateFile(string currLanguageCode)
        {
            string info = "将原始数据表格 导出为 翻译表格";

            EditorUtility.DisplayProgressBar("导出", info, 0);
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
                EditorUtility.ClearProgressBar();
                return;
            }

            _excelChangeInfoLookup.Clear();
            _rawDataKeyVisitMark.Clear();

            for (var index = 0; index < rawDataList.Count; index++)
            {
                var rawExcelFileData = rawDataList[index];
                if (!rawExcelFileData.IsValid())
                {
                    string errorInfo = $"文件[{rawExcelFileData.fileName}]中找不到任何有效的数据，跳过此文件的导出工作。";
                    Debug.LogError(errorInfo);
                    continue;
                }

                LocalizationDataUtils.LocalizationFileData sameNameTranslateData = null;
                foreach (var translateData in translateDataList)
                {
                    if (translateData.fileName == rawExcelFileData.fileName)
                    {
                        sameNameTranslateData = translateData;
                        break;
                    }
                }

                if (sameNameTranslateData != null)
                {
                    FillRawDataWithTranslateData(currLanguageCode, rawExcelFileData, sameNameTranslateData);
                }
                else
                {
                    RemoveNotUseValue(rawExcelFileData);
                }

                LocalizationDataUtils.ConvertToTranslateExcelFile(rawExcelFileData, translateDataFolderFullPath);
                float progress = (float)index / (float)rawDataList.Count;
                EditorUtility.DisplayProgressBar("导出翻译表格", info, progress);
            }

            var folderName = Path.GetDirectoryName(translateDataFolderFullPath);
            SaveExcelChangeInfoList(folderName);
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        public void CheckRepeatKeyword(string currLanguageCode, out bool haveRepeatKeyword)
        {
            _excelChangeInfoLookup.Clear();
            _rawDataKeyVisitMark.Clear();
            var rawDataFileList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.excelDataFolderRootPath);
            if (rawDataFileList == null)
            {
                haveRepeatKeyword = false;
                return;
            }
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
            haveRepeatKeyword = _excelChangeInfoLookup.Count > 0;
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
        }
    }
}
