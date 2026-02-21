using System;
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
    public class LocalizationJsonGenerator
    {
        private readonly LocalizationConfig _localizationConfig;
        private Dictionary<string, JsonDataModule> _jsonDataModuleLookup = new();

        public LocalizationJsonGenerator(LocalizationConfig config)
        {
            _localizationConfig = config;
        }

        private class JsonDataModule
        {
            public string moduleName;
            private Dictionary<string, Dictionary<string, string>> languageKvpLookup = new();

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
                if (folderPath != null && !Directory.Exists(folderPath))
                {
                    if (File.Exists(folderPath))
                    {
                        File.Delete(folderPath);
                    }
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
            if (string.IsNullOrEmpty(languageKey)) return;
            string moduleName = LocalizationManager.GetModuleName(lineData.key);
            if (string.IsNullOrEmpty(moduleName)) return;
            if (!_jsonDataModuleLookup.TryGetValue(moduleName, out var dataModule))
            {
                dataModule = new JsonDataModule(moduleName);
                _jsonDataModuleLookup[moduleName] = dataModule;
            }
            foreach (var kvp in lineData.translatedValues)
            {
                var languageCode = kvp.Key;
                var languageValue = kvp.Value;
                dataModule.AddData(languageCode, languageKey, languageValue);
            }
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
                Debug.LogError("[Addressables] 未找到 BuildScriptPackedMode 构建器！请检查 Addressables 配置。");
            }
        }

        public void GenerateJsonFiles()
        {
            EditorUtility.DisplayProgressBar("导出Json文件", "导出Json文件", 0);

            var languageCodeList = _localizationConfig.GetLanguageCodeList();
            var currLanguageCode = _localizationConfig.OriginalLanguageCode;

            _jsonDataModuleLookup.Clear();
            foreach (var languageCode in languageCodeList)
            {
                string folderPath = LocalizationManager.GetJsonFolderFullPath(languageCode);
                UnityPathUtility.DeleteAllFile(folderPath, false);
            }

            var translateDataFileList = LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(currLanguageCode, _localizationConfig.translateDataFolderRootPath);
            if (translateDataFileList != null)
            {
                foreach (var dataFile in translateDataFileList)
                {
                    foreach (var dataline in dataFile.dataList)
                    {
                        AddToJsonDataModule(dataline);
                    }
                }
            }

            var moduleList = _jsonDataModuleLookup.Values.ToArray();
            var moduleCount = moduleList.Length;
            for (int i = 0; i < moduleCount; i++)
            {
                var jsonModule = moduleList[i];
                jsonModule.SaveJsonFile();
                EditorUtility.DisplayProgressBar("导出Json文件", $"导出Json文件:{jsonModule.moduleName}", i / (float)moduleCount);
            }
            AssetDatabase.SaveAssets();
            BuildAddressables();
            EditorUtility.ClearProgressBar();
        }
    }
}
