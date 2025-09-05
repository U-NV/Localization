using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using U0UGames.Framework.EventManager;

using Object = UnityEngine.Object;

namespace U0UGames.Localization
{
    public class LocalizeLanguageChangeEvent:GameEvent{
    
    }
    public static class LocalizationManager
    {
        public const string LocalizationResourcesFolder = "Localization";
        public const string LocalizationConfigFileName = @"LocalizationConfig";
        
        public const string SimplifiedChineseCode = "zh-cn";
        public const string TraditionalChineseCode = "zh-CHT";
        public const string JapaneseCode = "ja";
        public const string EnglishCode = "en";
        public static readonly string DefaultLanguageCode = EnglishCode;

        // 配置
        private static LocalizationConfig _config = null;
        public static LocalizationConfig Config{
            get
            {
                if (_config != null)
                {
                    return _config;
                }
                
                // 加载配置文件
                // if (LocalizationConfigResourcePath == null) return null;
                LocalizationConfig config = Resources.Load<LocalizationConfig>(LocalizationConfigFileName);
                if(config==null)return null;
                
                _config = config;
                return config;
            }
        }
        private static string _currLanguageCode;
        public static string CurrLanguageCode
        {
            get
            {
                if (string.IsNullOrEmpty(_currLanguageCode))
                {
                    _currLanguageCode = DefaultLanguageCode;
                }
                return _currLanguageCode;
            }
        }


        // 数据
        private static readonly LocalizationDataModuleManager DataModuleManager = new LocalizationDataModuleManager();
        public static string GetText(string textKey)
        {
            DataModuleManager.TryGetText(textKey,out var result);
            return result;
        }

        public static string GetTextWithArg(string textKey, params object[] args)
        {
            // 如果找不到关键词对应的翻译，直接返回默认结果
            if(!DataModuleManager.TryGetText(textKey,out var result))
            {
                return result;
            }
            
            // 找到则尝试添加参数
            try
            {
                result = string.Format(result, args);
            }
            catch
            {
                return $"{result}:{args}";
            }
            return result;
        }
        public static Sprite GetSprite(string textKey) => DataModuleManager.GetSprite(textKey);
        public static T GetObject<T>(string textKey) where T:UnityEngine.Object => DataModuleManager.GetObject<T>(textKey);

        // 初始
        private static bool _isInit = false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Start()
        {
            if(_isInit)return;
            _isInit = true;
            
            DataModuleManager.Init(Config);
            SwitchLanguage("zh-cn");
        }
        
        // 关键词分析
        // public const string DefaultModuleName = "Null";
        public static string GetModuleName(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            int index = key.IndexOf(".", StringComparison.Ordinal);
            if (index > 0)
            {
                return key.Substring(0, index).Trim();
            }
            return null;
        }
        public static void ProcessLocalizeKey(string input, out string prefix, out string key)
        {
            int lastIndex = input.LastIndexOf(".", StringComparison.Ordinal);
            if (lastIndex > 0)
            {
                prefix = input.Substring(0, lastIndex).Trim();
                key = input.Substring(lastIndex+1).Trim();
            }
            else
            {
                prefix = null;
                key = input.Trim();
            }
        }

        // 切换语言
        // 区域代码参考 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
        private static readonly Dictionary<string, string> _codeConvert = new Dictionary<string, string>()
        {
            // 简体中文
            { "zh-hans", SimplifiedChineseCode },
            { "zh", SimplifiedChineseCode },
            { "zh-cn", SimplifiedChineseCode },
            { "zh-sg", SimplifiedChineseCode },
            // 繁体中文
            { "zh-hant", TraditionalChineseCode },
            { "zh-hk", TraditionalChineseCode },
            { "zh-mo", TraditionalChineseCode },
            { "zh-tw", TraditionalChineseCode },
            { "zh-cht", TraditionalChineseCode },
            // 日文
            { "ja", JapaneseCode },
            { "ja-jp", JapaneseCode },
        };
        public static string ConvertCode(string code)
        {
            code = code.ToLower();
            if (_codeConvert.TryGetValue(code, out var value))
            {
                return value;
            }
            if (code.Contains("en")) return EnglishCode;
            return code;
        }

        public static string GetRecommendLanguageCode()
        {
            var currLanguage = System.Globalization.CultureInfo.InstalledUICulture.Name;
            currLanguage = currLanguage.ToLower();
            currLanguage = ConvertCode(currLanguage);
            var recommendLanguageCode = LocalizationManager.DefaultLanguageCode;
            if(Config.inGameLanguageCodeList.Contains(currLanguage))
            {
                recommendLanguageCode = currLanguage;
            }
            return recommendLanguageCode;
        }
        
        private static readonly LocalizeLanguageChangeEvent LocalizeLanguageChangeEvent = new LocalizeLanguageChangeEvent();
        
        public static bool TryGetLanguageAssetBundle(string languageCode, out AssetBundle assetBundle)
        {
            // 尝试找到
            languageCode = languageCode.ToLower();
            var allBundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var exitBundle in allBundles)
            {
                if (exitBundle.name == languageCode)
                {
                    assetBundle = exitBundle;
                    return true;
                }
            }
            // 尝试加载
            string outputPath = Path.Combine(Application.streamingAssetsPath, 
                LocalizationManager.LocalizationResourcesFolder, languageCode);
            
            var bundle = AssetBundle.LoadFromFile(outputPath);
            if (bundle != null)
            {
                assetBundle = bundle;
                return true;
            }
            
            Debug.LogError($"找不到语言类型{languageCode} 对应的数据包");
            assetBundle = null;
            return false;
        }
        
        public static void SwitchLanguage(string languageCode, List<string> textModules = null)
        {
            languageCode = ConvertCode(languageCode);
            if (!Config.inGameLanguageCodeList.Contains(languageCode))
            {
                Debug.LogError($"找不到语言类型：{languageCode}");
                return;
            }
            
            _currLanguageCode = languageCode;
            if (!TryGetLanguageAssetBundle(_currLanguageCode, out var assetBound))
            {
                return;
            }

            Object.DontDestroyOnLoad(assetBound);
            DataModuleManager.ChangeAssetBundle(assetBound, languageCode);
            DataModuleManager.LoadDataModule(Config.defaultModuleNames);
            DataModuleManager.LoadDataModule(textModules);
            EventManager.Broadcast(LocalizeLanguageChangeEvent);
        }

    }
}
