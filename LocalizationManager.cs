using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using Object = UnityEngine.Object;

namespace U0UGames.Localization
{
    public static class LocalizationManager
    {
        public static event Action<string> OnLanguageChanged;
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
        
        // 字符串优化（移除缓存，因为大部分key只查询一次）
        private static readonly StringBuilder _stringBuilder = new StringBuilder(256);
        
        public static string GetJsonFolderFullPath(string languageCode)
        {
            return Path.Combine(Application.streamingAssetsPath, 
                LocalizationManager.LocalizationResourcesFolder, languageCode);
        }
        public static string GetJsonDataFullPath(string languageCode, string moduleName)
        {
            return Path.Combine(
                LocalizationManager.GetJsonFolderFullPath(languageCode),
                $"{moduleName}.json");
        }

        /// <summary>
        /// 获取翻译文本（无缓存版本，适用于大部分key只查询一次的场景）
        /// </summary>
        public static string GetText(string textKey)
        {
            if (string.IsNullOrEmpty(textKey))
                return string.Empty;
                
            // 直接获取翻译结果，无缓存开销
            DataModuleManager.TryGetText(textKey, out var result);
            return result;
        }

        public static string GetTextWithArg(string textKey, params object[] args)
        {
            if (string.IsNullOrEmpty(textKey))
                return string.Empty;
                
            // 如果找不到关键词对应的翻译，直接返回默认结果
            if(!DataModuleManager.TryGetText(textKey,out var result))
            {
                return result;
            }
            
            // 找到则尝试添加参数
            try
            {
                // 使用StringBuilder优化字符串拼接
                _stringBuilder.Clear();
                _stringBuilder.AppendFormat(result, args);
                return _stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                // 改进错误处理，记录具体错误信息
                Debug.LogWarning($"格式化字符串失败: {textKey}, 错误: {ex.Message}");
                _stringBuilder.Clear();
                _stringBuilder.Append(result);
                _stringBuilder.Append(":");
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0) _stringBuilder.Append(",");
                    _stringBuilder.Append(args[i]?.ToString() ?? "null");
                }
                return _stringBuilder.ToString();
            }
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
            if (string.IsNullOrEmpty(input))
            {
                prefix = null;
                key = null;
                return;
            }
            
            // 直接解析，无缓存开销（因为大部分key只解析一次）
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
        
        public static bool TryGetLanguagePath(string languageCode, out string languagePath)
        {
            languageCode = languageCode.ToLower();
            string outputPath = Path.Combine(Application.streamingAssetsPath, 
                LocalizationManager.LocalizationResourcesFolder, languageCode);
            
            if (Directory.Exists(outputPath))
            {
                languagePath = outputPath;
                return true;
            }
            
            Debug.LogError($"找不到语言类型{languageCode} 对应的文件夹");
            languagePath = null;
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
            if (!TryGetLanguagePath(_currLanguageCode, out var languagePath))
            {
                return;
            }

            DataModuleManager.ChangeLanguage(languageCode);
            DataModuleManager.LoadDataModule(Config.defaultModuleNames);
            DataModuleManager.LoadDataModule(textModules);
  
            // 触发语言切换事件
            OnLanguageChanged?.Invoke(languageCode);
        }
    

    }
}
