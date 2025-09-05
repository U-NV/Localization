using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace U0UGames.Localization
{
    public class LocalizationDataModule
    {
        private string _name;
        public string Name => _name;
        private string _languageCode;
        private List<Object> _activeAssetList = new List<Object>();
        private Dictionary<string, UnityEngine.Object> _activeAssetLookup = new Dictionary<string, Object>();

        private Dictionary<string, Dictionary<string, string>> _dictLookup =
            new Dictionary<string, Dictionary<string, string>>();

        private float _lastUseTime = 0;
        public float LastUseTime => _lastUseTime;
        
        public LocalizationDataModule(string name, string languageCode)
        {
            _name = name;
            _languageCode = languageCode;
        }

        private bool _haveLoadData = false;
        public bool LoadData()
        {
            _haveLoadData = false;
            _activeAssetList.Clear();
            _activeAssetLookup.Clear();
            _dictLookup.Clear();
            
            if (string.IsNullOrEmpty(_languageCode))
            {
                Debug.LogWarning($"本地化模块{_name}, 语言代码为空");
                return false;
            }
            
            // 直接从StreamingAssetsPath读取JSON文件
            string jsonPath = LocalizationManager.GetJsonFolderFullPath(_languageCode);
            
            
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"找不到本地化文件: {jsonPath}");
                return false;
            }
            
            string jsonText = null;
            try
            {
                jsonText = File.ReadAllText(jsonPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError($"本地化文件为空: {jsonPath}");
                return false;
            }
            
            var textLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
            if (textLookup == null || textLookup.Count == 0)
            {
                Debug.LogError($"无法解析本地化模块: {_name}");
                return false;
            }
            
            BuildSearchTree(textLookup);
            _haveLoadData = true;
            Debug.Log("Load Localize Data Module: " + Name);
            return true;
        }
        private void BuildSearchTree(Dictionary<string, string> dataDictionary)
        {
            foreach (var kvp in dataDictionary)
            {
                LocalizationManager.ProcessLocalizeKey(kvp.Key, out string prefix, out string keyword);
                if (string.IsNullOrEmpty(prefix))
                {
                    Debug.LogWarning($"本地化关键词 {kvp.Key} 结构错误");
                    continue;
                }

                if (!_dictLookup.TryGetValue(prefix, out var textLookup))
                {
                    textLookup = new Dictionary<string, string>();
                    _dictLookup[prefix] = textLookup;
                }
                textLookup[keyword] = kvp.Value;
            }
        }
        public void Unload()
        {
            _haveLoadData = false;
            _dictLookup.Clear();
            
            if (_activeAssetList.Count > 0)
            {
                foreach (var asset in _activeAssetList)
                {
                    Resources.UnloadAsset(asset);
                }
                _activeAssetList.Clear();
            }
            
            _activeAssetLookup.Clear();

            Debug.Log("Unload Localize Data Module: " + Name);
        }

        // public bool TryGetLocalizeString(string key, out string value)
        // {
        //     LocalizationManager.ProcessLocalizeKey(key, out string prefix, out string keyword);
        //     return TryGetLocalizeString(prefix,keyword,out value);
        // }
        public bool TryGetLocalizeString(string prefix, string keyword, out string value)
        {
            _lastUseTime = Time.time;
            value = $"{prefix}.{keyword}";
            if (!_haveLoadData)
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }
            
            if (_dictLookup.TryGetValue(prefix, out var textLookup)
                && textLookup.TryGetValue(keyword, out var result))
            {
                value = result;
                return true;
            }
            
            return false;
        }
        
        // public bool TryGetLocalizeObject<T>(string key, out T value) where T:Object
        // {
        //     LocalizationManager.ProcessLocalizeKey(key, out string prefix, out string keyword);
        //     return TryGetLocalizeObject<T>(prefix,keyword,out value);
        // }
        public bool TryGetLocalizeObject<T>(string prefix, string keyword, out T value) where T:Object
        {
            value = null;
            if (!_haveLoadData)
            {
                return false;
            }
            
            if (!TryGetLocalizeString(prefix, keyword, out string assetPath))
            {
                return false;
            }
            if (_activeAssetLookup.TryGetValue(assetPath, out Object cachedObject))
            {
                if (cachedObject is T cachedTargetObject)
                {
                    value = cachedTargetObject;
                    return true;
                }
            }
            
            var obj = Resources.Load(assetPath);
            if (obj is T targetObject)
            {
                value = targetObject;
                _activeAssetList.Add(targetObject);
                _activeAssetLookup[assetPath] = targetObject;
                return true;
            }
            
            if(obj!=null)
            {
                Resources.UnloadAsset(obj);
            }
            
            return false;
        }
    }
}