using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace U0UGames.Localization
{
    public class LocalizationDataModuleManager
    {
        private LocalizationConfig _config;
        private LocalizationConfig Config
        {
            get
            {
                if (_config != null) return _config;
                _config = LocalizationManager.Config;
                return _config;
            }    
        }

        private int _maxDynamicModuleCount = 20;
        public void Init(LocalizationConfig config)
        {
            _config = config;
            // _currLanguageAssetBundle = null;
            DataModuleLookup.Clear();
            DataModuleList.Clear();
            // _dynamicLoadDataModuleList.Clear();
            _maxDynamicModuleCount = Config.MaxDynamicModuleCount;
        }

        private string _currLanguageCode;
        public void ChangeLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                Debug.LogError("languageCode is null or empty");
                return;
            }

            // 数据是相同，无需更新
            if (_currLanguageCode == languageCode)
            {
                return;
            }
            
            // 清空所有本地化模块
            foreach (var module in DataModuleList)
            {
                module.Unload();
            }
            // _dynamicLoadDataModuleList.Clear();
            DataModuleList.Clear();
            DataModuleLookup.Clear();

            _currLanguageCode = languageCode;
        }
        
        
        private readonly Dictionary<string,LocalizationDataModule> DataModuleLookup = new Dictionary<string,LocalizationDataModule>();
        private readonly List<LocalizationDataModule> DataModuleList = new List<LocalizationDataModule>();

        public void LoadDataModule(string moduleName, TextAsset jsonAsset)
        {
            if (!DataModuleLookup.TryGetValue(moduleName, out var module))
            {
                module = new LocalizationDataModule(moduleName, _currLanguageCode);
                DataModuleLookup[moduleName] = module;
                DataModuleList.Add(module);
            }
            // 此时数据已经加载好了，我们直接传入 Text 让 Module 初始化
            module.LoadData(jsonAsset.text);
        }

        // public LocalizationDataModule TryLoadDataModule(string moduleName)
        // {
        //     if (string.IsNullOrEmpty(_currLanguageCode))
        //     {
        //         ChangeLanguage(LocalizationManager.DefaultLanguageCode);
        //         return null;
        //     }

        //     if (string.IsNullOrEmpty(_currLanguageCode))
        //     {
        //         Debug.LogError("当前语言代码为空");
        //         return null;
        //     }

        //     // 如果缓存中存在所需模块，直接返回
        //     if (DataModuleLookup.TryGetValue(moduleName, out var existModule))
        //     {
        //         return existModule;
        //     }
            
        //     // 创建新的空模块
        //     LocalizationDataModule module = new LocalizationDataModule(moduleName, _currLanguageCode);
        //     DataModuleList.Add(module);
        //     DataModuleLookup[moduleName] = module;
            
        //     // 同步等待异步加载完成（注意：这会阻塞当前线程）
        //     try
        //     {
        //         bool success = module.LoadData(); 
        
        //         if (!success)
        //         {
        //             Debug.LogError($"[Localization] 模块 {moduleName} 加载失败");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"[Localization] 加载模块 {moduleName} 时发生异常: {ex.Message}\n{ex.StackTrace}");
        //     }

        //     // 返回新模块
        //     return module;
        // }
        // public void LoadDataModule(List<string> moduleNameList)
        // {
        //     if(moduleNameList == null || moduleNameList.Count == 0)return;
        //     foreach (var name in moduleNameList)
        //     {
        //         TryLoadDataModule(name);
        //     }
        // }


        private void UnloadDataModule(LocalizationDataModule module)
        {
            module.Unload();
            DataModuleLookup.Remove(module.Name);
            DataModuleList.Remove(module);
            // _dynamicLoadDataModuleList.Remove(module);
        }
        public void UnloadDataModule(string moduleName)
        {
            if (DataModuleLookup.TryGetValue(moduleName, out var module))
            {
                UnloadDataModule(module);
            }
        }
        public void UnloadDataModule(List<string> moduleNameList)
        {
            if(moduleNameList == null || moduleNameList.Count == 0)return;
            foreach (var name in moduleNameList)
            {
                UnloadDataModule(name);
            }
        }
        
        public bool TryGetText(string textKey, out string result)
        {
            result = textKey;
            string moduleName = LocalizationManager.GetModuleName(textKey);
            if (string.IsNullOrEmpty(moduleName))
            {
                return false;
            }
            if (DataModuleLookup.TryGetValue(moduleName, out var module))
            {
                if (module.TryGetLocalizeString(textKey, out string value))
                {
                    if (value != null)
                    {
                        result = value;
                        return true;
                    }
                    return false;
                }
            }else{
                Debug.LogWarning($"找不到本地化模块：{moduleName}");
            }
            return false;
        }

        public Sprite GetSprite(string key)
        {
            foreach (var module in DataModuleList)
            {
                if (module.TryGetLocalizeObject<Sprite>(key, out Sprite value))
                {
                    return value;
                }
            }
            return null;
        }

        public T GetObject<T>(string key) where T:UnityEngine.Object
        {
            foreach (var module in DataModuleList)
            {
                if (module.TryGetLocalizeObject<T>(key, out T value))
                {
                    return value;
                }
            }
            return null;
        }
    }
}