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
        private AssetBundle _languageAssetBundle;
        public AssetBundle LanguageAssetBundle => _languageAssetBundle;
        private TextAsset _jsonTextAsset;
        private List<Object> _activeAssetList = new List<Object>();
        private Dictionary<string, UnityEngine.Object> _activeAssetLookup = new Dictionary<string, Object>();

        private Dictionary<string, Dictionary<string, string>> _dictLookup =
            new Dictionary<string, Dictionary<string, string>>();

        private float _lastUseTime = 0;
        public float LastUseTime => _lastUseTime;
        
        public LocalizationDataModule(string name,AssetBundle languageAssetBundle)
        {
            _name = name;
            _languageAssetBundle = languageAssetBundle;
        }

        private bool _haveLoadData = false;
        public bool LoadData()
        {
            _haveLoadData = false;
            _activeAssetList.Clear();
            _activeAssetLookup.Clear();
            _dictLookup.Clear();
            
            if (_languageAssetBundle == null)
            {
                Debug.LogWarning($"本地化模块{_name}, 中数据包为空");
                return false;
            }
            
            TextAsset jsonAsset = null;
            try
            {
                jsonAsset = _languageAssetBundle.LoadAsset<TextAsset>(_name);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            // 如果找不到json数据，或者加载失败，释放已经加载了的资源
            if (jsonAsset == null)
            {
                Debug.LogError($"找不到本地化模块json数据文件：{_name}");
                return false;
            }
            
            _jsonTextAsset = jsonAsset;
            var textLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(_jsonTextAsset.text);
            if (textLookup == null || textLookup.Count == 0)
            {
                Debug.Log("无法解析本地化模块 " + _jsonTextAsset.name);
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
            
            if (_activeAssetList.Count>0)
            {
                foreach (var asset in _activeAssetList)
                {
                    Resources.UnloadAsset(asset);
                }
                _activeAssetList.Clear();
            }
            
            if (_languageAssetBundle != null && _jsonTextAsset != null)
            {
                _languageAssetBundle.Unload(_jsonTextAsset);
            }
            else
            {
                Debug.LogWarning($"本地化模块{_name} AssetBundle卸载失败, 中数据包为空");
            }

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