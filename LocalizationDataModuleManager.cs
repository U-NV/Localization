using System;
using System.Collections.Generic;
using UnityEngine;

namespace U0UGames.Localization
{
    /// <summary>
    /// 本地化数据存储管理器 - 扁平化查询版本
    /// 将所有文本数据存储在单一字典中，消除模块查找开销
    /// </summary>
    public class LocalizationDataModuleManager
    {
        public void Init(LocalizationConfig config)
        {
            _textLookup.Clear();
            _activeAssetLookup.Clear();
            _activeAssetList.Clear();
        }

        private string _currLanguageCode;
        public void ChangeLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                Debug.LogError("[Localization] languageCode is null or empty");
                return;
            }

            // 数据相同，无需更新
            if (_currLanguageCode == languageCode)
            {
                return;
            }
            
            // 清空文本数据
            _textLookup.Clear();
            _activeAssetLookup.Clear();
            _activeAssetList.Clear();
            _currLanguageCode = languageCode;
        }
        
        // 扁平化文本查找表 - 所有模块的文本合并到一个字典
        private readonly Dictionary<string, string> _textLookup = new Dictionary<string, string>(8192);
        private readonly Dictionary<string, UnityEngine.Object> _activeAssetLookup = new Dictionary<string, UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _activeAssetList = new List<UnityEngine.Object>();

        /// <summary>
        /// 加载语言数据 - 合并所有模块到单一字典
        /// </summary>
        public void LoadLanguageData(Dictionary<string, string> textData)
        {
            if (textData == null) return;
            
            foreach (var kvp in textData)
            {
                _textLookup[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 从JSON文本加载语言数据
        /// </summary>
        public bool LoadLanguageDataFromJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("[Localization] JSON text is null or empty");
                return false;
            }

            try
            {
                var textData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
                if (textData == null || textData.Count == 0)
                {
                    Debug.LogWarning("[Localization] JSON data is empty");
                    return false;
                }
                
                LoadLanguageData(textData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] Failed to parse JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取文本 - 单次字典查找，无模块解析开销
        /// </summary>
        public bool TryGetText(string textKey, out string result)
        {
            if (string.IsNullOrEmpty(textKey))
            {
                result = string.Empty;
                return false;
            }
            
            // 单次字典查找，无字符串解析开销
            if (_textLookup.TryGetValue(textKey, out result))
            {
                return true;
            }
            
            result = textKey;
            return false;
        }

        /// <summary>
        /// 获取当前语言的所有key数量
        /// </summary>
        public int GetTextCount() => _textLookup.Count;

        /// <summary>
        /// 检查key是否存在
        /// </summary>
        public bool ContainsKey(string key) => _textLookup.ContainsKey(key);

        // 资源对象加载 - 通过文案key映射到资源路径
        public Sprite GetSprite(string key)
        {
            return GetObject<Sprite>(key);
        }

        public T GetObject<T>(string key) where T:UnityEngine.Object
        {
            if (!_textLookup.TryGetValue(key, out var assetPath) || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            if (_activeAssetLookup.TryGetValue(assetPath, out var cachedObject))
            {
                return cachedObject as T;
            }

            var obj = Resources.Load(assetPath);
            if (obj is T targetObject)
            {
                _activeAssetLookup[assetPath] = targetObject;
                _activeAssetList.Add(targetObject);
                return targetObject;
            }

            if (obj != null)
            {
                Resources.UnloadAsset(obj);
            }
            return null;
        }
    }
}
