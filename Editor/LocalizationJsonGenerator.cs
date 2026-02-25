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
            if (string.IsNullOrEmpty(_localizationConfig.translateDataFolderRootPath))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误",
                    "翻译数据文件夹路径为空。\n请先在【本地化配置】界面选择“翻译表格文件夹”。",
                    "确认");
                return;
            }

            var translateFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);
            if (string.IsNullOrEmpty(translateFolderFullPath) || !Directory.Exists(translateFolderFullPath))
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError(
                    "[Localization] 翻译数据文件夹不存在，无法生成 Json。\n" +
                    $"  存储路径: '{_localizationConfig.translateDataFolderRootPath}'\n" +
                    $"  还原完整路径: '{translateFolderFullPath}'\n" +
                    $"  RootFolderPath: '{UnityPathUtility.RootFolderPath}'\n" +
                    $"  Application.dataPath: '{Application.dataPath}'\n" +
                    "  解决：请在【本地化配置】界面重新选择正确的“翻译表格文件夹”。");
                EditorUtility.DisplayDialog("错误",
                    $"翻译数据文件夹不存在：\n{translateFolderFullPath}\n\n请到【本地化配置】重新选择正确的文件夹路径。",
                    "确认");
                return;
            }

            var topXlsx = Directory.GetFiles(translateFolderFullPath, "*.xlsx");
            var topJson = Directory.GetFiles(translateFolderFullPath, "*.json");
            if (topXlsx.Length == 0 && topJson.Length == 0)
            {
                var deepXlsx = Directory.GetFiles(translateFolderFullPath, "*.xlsx", SearchOption.AllDirectories);
                var deepJson = Directory.GetFiles(translateFolderFullPath, "*.json", SearchOption.AllDirectories);

                EditorUtility.ClearProgressBar();
                Debug.LogError(
                    "[Localization] 翻译数据文件夹中未找到任何可用于生成 Json 的文件（*.xlsx / *.json）。\n" +
                    $"  文件夹: '{translateFolderFullPath}'\n" +
                    $"  根目录统计: xlsx={topXlsx.Length}, json={topJson.Length}\n" +
                    $"  递归统计: xlsx={deepXlsx.Length}, json={deepJson.Length}\n" +
                    "  解决：请确认翻译数据文件是否放在根目录，或在【本地化配置】中选择到真正包含文件的目录。");

                var extra = (deepXlsx.Length > 0 || deepJson.Length > 0)
                    ? "\n\n提示：检测到文件存在于子文件夹中，但当前生成只读取根目录。请把文件移动到该目录根部，或改为选择子文件夹作为“翻译表格文件夹”。"
                    : "";
                EditorUtility.DisplayDialog("错误",
                    $"翻译数据文件夹中未找到任何 *.xlsx / *.json：\n{translateFolderFullPath}{extra}",
                    "确认");
                return;
            }

            var translateDataFileList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(
                currLanguageCode, _localizationConfig.translateDataFolderRootPath);
            
            if (translateDataFileList == null || translateDataFileList.Count == 0)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError(
                    "[Localization] 未能从翻译数据文件夹解析出任何有效数据，无法生成 Json。\n" +
                    $"  文件夹: '{translateFolderFullPath}'\n" +
                    "  解决：请检查文件格式是否正确（xlsx/json），以及文件内容是否可被解析。");
                EditorUtility.DisplayDialog("错误",
                    $"翻译数据文件夹未解析出任何有效数据：\n{translateFolderFullPath}\n\n请检查文件格式/内容是否正确。",
                    "确认");
                return;
            }

            foreach (var dataFile in translateDataFileList)
            {
                foreach (var dataline in dataFile.dataList)
                {
                    AggregateLineData(dataline);
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
            AssetDatabase.Refresh();
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
