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
        private HashSet<string> _matchedTranslateKeys = new();

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

            _matchedTranslateKeys.Add(lineDataKey);
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
            if (!string.IsNullOrEmpty(targetTranslateDataLine.key))
                _matchedTranslateKeys.Add(targetTranslateDataLine.key);

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
            _matchedTranslateKeys.Clear();

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

            // 检测翻译表格中已不存在于原始数据的条目（待删除）
            for (var i = 0; i < sameNameTranslateData.dataList.Count; i++)
            {
                var translateLine = sameNameTranslateData.dataList[i];
                if (string.IsNullOrEmpty(translateLine.key)) continue;
                if (_matchedTranslateKeys.Contains(translateLine.key)) continue;

                translateLine.translatedValues.TryGetValue(currLanguageCode, out var originText);
                AddChangeInfo(rawExcelFileData.fileName, new ExcelLineChangeInfo(i, translateLine.key)
                {
                    originalValue = originText ?? "",
                    newValue = "",
                    isDeleted = true,
                    deletedLineData = translateLine
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
            // 校验路径配置（避免“路径不存在/文件夹为空”时只报一句“找不到任何可以导出的文件”）
            if (string.IsNullOrEmpty(_localizationConfig.excelDataFolderRootPath))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误",
                    "原始数据文件夹路径为空。\n请先在【本地化配置】界面选择“原始表格文件夹”。",
                    "确认");
                return;
            }

            var rawDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.excelDataFolderRootPath);
            if (string.IsNullOrEmpty(rawDataFolderFullPath) || !Directory.Exists(rawDataFolderFullPath))
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError(
                    "[Localization] 原始数据文件夹不存在，无法导出翻译表格。\n" +
                    $"  存储路径: '{_localizationConfig.excelDataFolderRootPath}'\n" +
                    $"  还原完整路径: '{rawDataFolderFullPath}'\n" +
                    $"  RootFolderPath: '{UnityPathUtility.RootFolderPath}'\n" +
                    $"  Application.dataPath: '{Application.dataPath}'\n" +
                    "  解决：请在【本地化配置】界面重新选择正确的“原始表格文件夹”。");
                EditorUtility.DisplayDialog("错误",
                    $"原始数据文件夹不存在：\n{rawDataFolderFullPath}\n\n请到【本地化配置】重新选择正确的文件夹路径。",
                    "确认");
                return;
            }

            // 只统计根目录（当前导出逻辑也只读取根目录）
            var rawXlsx = Directory.GetFiles(rawDataFolderFullPath, "*.xlsx");
            var rawJson = Directory.GetFiles(rawDataFolderFullPath, "*.json");
            if (rawXlsx.Length == 0 && rawJson.Length == 0)
            {
                // 额外提示：如果文件在子目录里，会导致“找不到任何可以导出的文件”
                var rawXlsxDeep = Directory.GetFiles(rawDataFolderFullPath, "*.xlsx", SearchOption.AllDirectories);
                var rawJsonDeep = Directory.GetFiles(rawDataFolderFullPath, "*.json", SearchOption.AllDirectories);

                EditorUtility.ClearProgressBar();
                Debug.LogError(
                    "[Localization] 原始数据文件夹中未找到任何可导出的文件（*.xlsx / *.json）。\n" +
                    $"  文件夹: '{rawDataFolderFullPath}'\n" +
                    $"  根目录统计: xlsx={rawXlsx.Length}, json={rawJson.Length}\n" +
                    $"  递归统计: xlsx={rawXlsxDeep.Length}, json={rawJsonDeep.Length}\n" +
                    "  解决：请确认文件是否放在根目录，或在【本地化配置】中选择到真正包含文件的目录。");

                var extra = (rawXlsxDeep.Length > 0 || rawJsonDeep.Length > 0)
                    ? "\n\n提示：检测到文件存在于子文件夹中，但当前导出只读取根目录。请把文件移动到该目录根部，或改为选择子文件夹作为“原始表格文件夹”。"
                    : "";
                EditorUtility.DisplayDialog("错误",
                    $"原始数据文件夹中未找到任何 *.xlsx / *.json：\n{rawDataFolderFullPath}{extra}",
                    "确认");
                return;
            }

            if (string.IsNullOrEmpty(_localizationConfig.translateDataFolderRootPath))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误",
                    "翻译表格文件夹路径为空。\n请先在【本地化配置】界面选择“翻译表格文件夹”。",
                    "确认");
                return;
            }

            var translateDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);

            if (!Directory.Exists(translateDataFolderFullPath))
            {
                Directory.CreateDirectory(translateDataFolderFullPath);
            }

            var rawDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.excelDataFolderRootPath);
            var translateDataList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.translateDataFolderRootPath)
                                   ?? new List<LocalizationDataUtils.LocalizationFileData>();

            if (rawDataList == null || rawDataList.Count == 0)
            {
                Debug.LogError(
                    "[Localization] 找不到任何可以导出的文件。\n" +
                    $"  原始数据目录: '{rawDataFolderFullPath}'\n" +
                    $"  根目录统计: xlsx={rawXlsx.Length}, json={rawJson.Length}\n" +
                    "  解决：请检查目录是否正确、文件是否有效（非空、非临时文件）。");
                EditorUtility.ClearProgressBar();
                return;
            }

            _excelChangeInfoLookup.Clear();
            _rawDataKeyVisitMark.Clear();
            _matchedTranslateKeys.Clear();

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

                _excelChangeInfoLookup.TryGetValue(rawExcelFileData.fileName, out var fileChangeInfoList);
                LocalizationDataUtils.ConvertToTranslateExcelFileWithHighlight(
                    rawExcelFileData, fileChangeInfoList, translateDataFolderFullPath);
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
