using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    /// <summary>
    /// 本地化JSON生成器 - Phase 2版本
    /// 每语言生成单一聚合JSON文件，包含所有模块数据
    /// </summary>
    public class LocalizationJsonGenerator
    {
        private readonly LocalizationConfig _localizationConfig;
        
        // 每语言一个聚合字典
        private Dictionary<string, Dictionary<string, string>> _languageData = new();

        public LocalizationJsonGenerator(LocalizationConfig config)
        {
            _localizationConfig = config;
        }

        /// <summary>
        /// 添加数据到对应语言的聚合字典
        /// </summary>
        private void AddLanguageData(string languageCode, string key, string value)
        {
            if (!_languageData.TryGetValue(languageCode, out var dict))
            {
                dict = new Dictionary<string, string>();
                _languageData[languageCode] = dict;
            }
            dict[key] = value;
        }

        /// <summary>
        /// 从Excel行数据提取并聚合
        /// </summary>
        private void AggregateLineData(LocalizationDataUtils.LocalizeLineData lineData)
        {
            if (string.IsNullOrEmpty(lineData.key)) return;
            
            foreach (var kvp in lineData.translatedValues)
            {
                var languageCode = kvp.Key;
                var languageValue = kvp.Value;
                AddLanguageData(languageCode, lineData.key, languageValue);
            }
        }

        /// <summary>
        /// 保存语言聚合文件
        /// </summary>
        private void SaveLanguageFile(string languageCode)
        {
            if (!_languageData.TryGetValue(languageCode, out var data))
            {
                Debug.LogWarning($"[Localization] No data for language: {languageCode}");
                return;
            }

            string folderPath = LocalizationManager.GetJsonFolderFullPath(languageCode);
            string fileName = $"{languageCode}_all.json";
            string fullPath = Path.Combine(folderPath, fileName);

            // 确保目录存在
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 序列化为JSON
            var jsonContent = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(fullPath, jsonContent);

            // 注册Addressable（Phase 2版本）
            LocalizationAddressableHelper.SetLanguageDataAddressable(fullPath, languageCode);

            Debug.Log($"[Localization] 已生成语言聚合文件: {languageCode}, {data.Count}条文本, 路径: {fullPath}");
        }

        public static void BuildAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings is null");
                return;
            }

            var packedModeBuilder = settings.DataBuilders
                .FirstOrDefault(b => b.GetType().Name.Contains("BuildScriptPackedMode"));

            if (packedModeBuilder != null)
            {
                int buildIndex = settings.DataBuilders.IndexOf(packedModeBuilder);
                settings.ActivePlayerDataBuilderIndex = buildIndex;
                Debug.Log($"[Addressables] 已设置构建器索引: Player={buildIndex}");
                AddressableAssetSettings.CleanPlayerContent();
                AddressableAssetSettings.BuildPlayerContent();
                Debug.Log("[Addressables] 物理 Bundle 构建完成！");
            }
            else
            {
                Debug.LogError("[Addressables] 未找到 BuildScriptPackedMode 构建器！");
            }
        }

        /// <summary>
        /// Phase 2: 生成每语言单文件
        /// </summary>
        public void GenerateJsonFiles()
        {
            EditorUtility.DisplayProgressBar("导出Json文件", "正在收集数据...", 0);

            var languageCodeList = _localizationConfig.GetLanguageCodeList();
            var currLanguageCode = _localizationConfig.OriginalLanguageCode;

            // 清空旧数据
            _languageData.Clear();

            // 清理旧文件
            foreach (var languageCode in languageCodeList)
            {
                string folderPath = LocalizationManager.GetJsonFolderFullPath(languageCode);
                if (Directory.Exists(folderPath))
                {
                    UnityPathUtility.DeleteAllFile(folderPath, false);
                }
            }

            // 从翻译数据收集
            var translateDataFileList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(
                currLanguageCode, _localizationConfig.translateDataFolderRootPath);
            
            if (translateDataFileList != null)
            {
                foreach (var dataFile in translateDataFileList)
                {
                    foreach (var dataline in dataFile.dataList)
                    {
                        AggregateLineData(dataline);
                    }
                }
            }

            // 保存每个语言的聚合文件
            var langCount = languageCodeList.Count;
            for (int i = 0; i < langCount; i++)
            {
                var langCode = languageCodeList[i];
                SaveLanguageFile(langCode);
                EditorUtility.DisplayProgressBar("导出Json文件", $"正在生成: {langCode}", (i + 1) / (float)langCount);
            }

            AssetDatabase.SaveAssets();
            BuildAddressables();
            EditorUtility.ClearProgressBar();

            // 统计信息
            int totalEntries = 0;
            foreach (var kvp in _languageData)
            {
                totalEntries += kvp.Value.Count;
            }
            Debug.Log($"[Localization] 导出完成！共 {languageCodeList.Count} 个语言文件，总计 {totalEntries} 条文本");
        }

    }
}
