using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace U0UGames.Localization
{
    public class LocalizationDataModule
    {
        private string _name;
        public string Name => _name;
        private string _languageCode;
        private List<Object> _activeAssetList = new List<Object>();
        private Dictionary<string, UnityEngine.Object> _activeAssetLookup = new Dictionary<string, Object>();

        // 优化：使用单层字典直接查询，避免两次字典查找
        // 对于100条左右的数据，单层字典查询效率更高（1次哈希 vs 2次哈希）
        private Dictionary<string, string> _dictLookup = new Dictionary<string, string>();

        // private float _lastUseTime = 0;
        // public float LastUseTime => _lastUseTime;
        
        public LocalizationDataModule(string name, string languageCode)
        {
            _name = name;
            _languageCode = languageCode;
        }


        // private AsyncOperationHandle<TextAsset> currentModuleHandle; 
        // private string LoadDataAsync()
        // {
        //     string address = $"{_languageCode}/{_name}";
        //     Debug.Log($"[Localization] 开始加载本地化文件，地址: {address}");
            
        //     try
        //     {
        //         AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(address);
        //         TextAsset jsonAsset = handle.WaitForCompletion();
                
        //         if (handle.Status == AsyncOperationStatus.Succeeded)
        //         {
        //             // 加载成功，记录句柄以便以后释放，并返回文本内容
        //             currentModuleHandle = handle;
        //             // TextAsset jsonAsset = handle.Result;
        //             if (jsonAsset != null && !string.IsNullOrEmpty(jsonAsset.text))
        //             {
        //                 Debug.Log($"[Localization] 成功加载本地化文件: {address}, 内容长度: {jsonAsset.text.Length}");
        //                 return jsonAsset.text;
        //             }
        //             else
        //             {
        //                 Debug.LogError($"[Localization] 本地化文件内容为空: {address}");
        //                 Addressables.Release(handle);
        //                 return null;
        //             }
        //         }
        //         else
        //         {
        //             Debug.LogError($"[Localization] 无法加载本地化文件: {address}, 状态: {handle.Status}, 错误: {handle.OperationException?.Message ?? "未知错误"}");
        //             if (handle.IsValid())
        //             {
        //                 Addressables.Release(handle);
        //             }
        //             return null;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"[Localization] 加载本地化文件时发生异常: {address}, 异常: {ex.Message}\n{ex.StackTrace}");
        //         return null;
        //     }
        // }

        public bool LoadData(string jsonText)
        {
            _haveLoadData = false;
            _activeAssetList.Clear();
            _activeAssetLookup.Clear();
            _dictLookup.Clear();
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError($"[Localization] 找不到本地化文件: {_languageCode}/{_name}，请检查 Addressables 配置");
                return false;
            }

            Dictionary<string, string> textLookup;

            try
            {
                textLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] 解析 JSON 失败，模块: {_name}, 错误: {ex.Message}");
                return false;
            }
            
            if (textLookup == null || textLookup.Count == 0)
            {
                Debug.LogError($"[Localization] 无法解析本地化模块: {_name}，JSON 内容为空或格式错误");
                return false;
            }
            
            BuildSearchTree(textLookup);
            _haveLoadData = true;
            Debug.Log($"[Localization] 成功加载本地化数据模块: {Name}，包含 {textLookup.Count} 条数据");
            return true;
        }

        private bool _haveLoadData = false;
        // public bool LoadData()
        // {
        //     _haveLoadData = false;
        //     _activeAssetList.Clear();
        //     _activeAssetLookup.Clear();
        //     _dictLookup.Clear();
            
        //     if (string.IsNullOrEmpty(_languageCode))
        //     {
        //         Debug.LogWarning($"[Localization] 本地化模块 {_name} 的语言代码为空");
        //         return false;
        //     }

        //     if (string.IsNullOrEmpty(_name))
        //     {
        //         Debug.LogWarning($"[Localization] 本地化模块名称为空，语言代码: {_languageCode}");
        //         return false;
        //     }

        //     var jsonText = LoadDataAsync();

        //     // // 直接从StreamingAssetsPath读取JSON文件
        //     // string jsonPath = LocalizationManager.GetJsonDataFullPath(_languageCode, _name);
            
            
        //     // if (!File.Exists(jsonPath))
        //     // {
        //     //     Debug.LogError($"找不到本地化文件: {jsonPath}");
        //     //     return false;
        //     // }
            
        //     // string jsonText = null;
        //     // try
        //     // {
        //     //     jsonText = File.ReadAllText(jsonPath);
        //     // }
        //     // catch (Exception e)
        //     // {
        //     //     Debug.LogException(e);
        //     //     return false;
        //     // }

        //     if (string.IsNullOrEmpty(jsonText))
        //     {
        //         Debug.LogError($"[Localization] 找不到本地化文件: {_languageCode}/{_name}，请检查 Addressables 配置");
        //         return false;
        //     }
            
        //     Dictionary<string, string> textLookup = null;
        //     try
        //     {
        //         textLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonText);
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"[Localization] 解析 JSON 失败，模块: {_name}, 错误: {ex.Message}");
        //         return false;
        //     }
            
        //     if (textLookup == null || textLookup.Count == 0)
        //     {
        //         Debug.LogError($"[Localization] 无法解析本地化模块: {_name}，JSON 内容为空或格式错误");
        //         return false;
        //     }
            
        //     BuildSearchTree(textLookup);
        //     _haveLoadData = true;
        //     Debug.Log($"[Localization] 成功加载本地化数据模块: {Name}，包含 {textLookup.Count} 条数据");
        //     return true;
        // }
        private void BuildSearchTree(Dictionary<string, string> dataDictionary)
        {
            // 优化：直接存储完整key，避免嵌套字典结构
            // 对于小规模数据（~100条），单层字典查询效率更高
            foreach (var kvp in dataDictionary)
            {
                // 直接存储原始key，保持向后兼容
                _dictLookup[kvp.Key] = kvp.Value;
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

            // if (currentModuleHandle.IsValid())
            // {
            //     Addressables.Release(currentModuleHandle);
            // }

            Debug.Log("Unload Localize Data Module: " + Name);
        }

        // public bool TryGetLocalizeString(string key, out string value)
        // {
        //     LocalizationManager.ProcessLocalizeKey(key, out string prefix, out string keyword);
        //     return TryGetLocalizeString(prefix,keyword,out value);
        // }
        public bool TryGetLocalizeString(string keyword, out string value)
        {
            // _lastUseTime = Time.time;
            value = keyword;
            if (!_haveLoadData)
            {
                Debug.LogWarning("Localization Data Module is not loaded: " + Name);
                return false;
            }
            
            if (string.IsNullOrEmpty(keyword))
            {
                return false;
            }
            
            if (_dictLookup.TryGetValue(keyword, out var result))
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
        public bool TryGetLocalizeObject<T>(string keyword, out T value) where T:Object
        {
            value = null;
            if (!_haveLoadData)
            {
                return false;
            }
            
            if (!TryGetLocalizeString(keyword, out string assetPath))
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