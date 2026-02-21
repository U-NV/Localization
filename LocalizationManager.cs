using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace U0UGames.Localization
{
    /// <summary>
    /// 本地化管理器 - 扁平化查询版本
    /// 核心变更：
    /// 1. 移除模块名解析路由，改为单次字典查找
    /// 2. Addressables按语言分Group加载
    /// 3. 每语言单文件聚合
    /// </summary>
    public static class LocalizationManager
    {
        public static event Action<string> OnLanguageChanged;
        public const string LocalizationResourcesFolder = "Localization";
        public const string LocalizationConfigFileName = @"LocalizationConfig";
        
        public const string SimplifiedChineseCode = "zh-cn";
        public const string TraditionalChineseCode = "zh-CHT";
        public const string JapaneseCode = "ja";
        public const string EnglishCode = "en";
        public static readonly string DefaultLanguageCode = SimplifiedChineseCode;

        // 配置
        private static LocalizationConfig _config = null;
        public static LocalizationConfig Config{
            get
            {
                if (_config != null)
                {
                    return _config;
                }
                
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

        // 数据管理器 - 扁平化查询
        private static readonly LocalizationDataModuleManager DataModuleManager = new LocalizationDataModuleManager();
        
        // 字符串优化
        private static readonly StringBuilder _stringBuilder = new StringBuilder(256);
        
        // 获取语言文件夹路径
        public static string GetJsonFolderFullPath(string languageCode)
        {
            return Path.Combine(Application.dataPath, 
                LocalizationManager.LocalizationResourcesFolder, languageCode);
        }
        
        /// <summary>
        /// 获取语言聚合文件路径（Phase 2）
        /// </summary>
        public static string GetLanguageDataFullPath(string languageCode)
        {
            return Path.Combine(
                GetJsonFolderFullPath(languageCode),
                $"{languageCode}_all.json");
        }
        
        /// <summary>
        /// 获取翻译文本 - 单次字典查找，无GC分配
        /// </summary>
        public static string GetText(string textKey)
        {
            if (string.IsNullOrEmpty(textKey))
                return string.Empty;
                
            // 单次字典查找，无模块名解析开销
            DataModuleManager.TryGetText(textKey, out var result);
            result = ApplayGlossary(result);
            return result;
        }

        public static string GetTextWithArg(string textKey, params object[] args)
        {
            if (string.IsNullOrEmpty(textKey))
                return string.Empty;
                
            if(!DataModuleManager.TryGetText(textKey, out var result))
            {
                return textKey;
            }
            
            try
            {
                _stringBuilder.Clear();
                _stringBuilder.AppendFormat(result, args);
                result = ApplayGlossary(_stringBuilder.ToString());
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Localization] String format failed: {textKey}, Error: {ex.Message}");
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
        
        // 资源获取 - Phase 2后将迁移到独立资源管理器
        public static Sprite GetSprite(string textKey) => DataModuleManager.GetSprite(textKey);
        public static T GetObject<T>(string textKey) where T:UnityEngine.Object => DataModuleManager.GetObject<T>(textKey);

        // 初始化
        private static bool _isInit = false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static async Task Start()
        {
            if(_isInit)return;
            _isInit = true;

            DataModuleManager.Init(Config);
            InitGlossary(Config.Glossary);

            SwitchLanguage("zh-cn");
        }

        private static Dictionary<string, LocalizeData> _staticGlossaryLookup = new Dictionary<string, LocalizeData>();
        private static Dictionary<string, string> glossaryCache = new Dictionary<string, string>();
        private static Dictionary<string, string> dynamicGlossaryCache = new Dictionary<string, string>();
        private static void InitGlossary(LocalizationGlossary glossary)
        {
            if(glossary == null)return;
            foreach(var data in glossary.glossaryDataList){
                _staticGlossaryLookup[data.keyword] = data.data;
            }
            glossaryCache.Clear();
            dynamicGlossaryCache.Clear();
        }

        public static void AddStaticGlossary(string keyword, LocalizeData data){
            _staticGlossaryLookup[keyword] = data;
        }
        public static void RemoveStaticGlossary(string keyword){
            _staticGlossaryLookup.Remove(keyword);
        }

        public static void UpdateDynamicGlossary(string keyword, string value){
            _staticGlossaryLookup[keyword] = new LocalizeData(value, value);
            glossaryCache[keyword] = value;
            dynamicGlossaryCache[keyword] = value;
        }

        public static void RemoveDynamicGlossary(string keyword){
            _staticGlossaryLookup.Remove(keyword);
            glossaryCache.Remove(keyword);
            dynamicGlossaryCache.Remove(keyword);
        }

        public static void ReflashGlossaryCache(){
            glossaryCache.Clear();
            foreach(var dynamicGlossary in dynamicGlossaryCache){
                glossaryCache[dynamicGlossary.Key] = dynamicGlossary.Value;
            }
        }

        private static readonly char GlossaryKeyStartMark = '#';
        private static readonly char GlossaryKeyEndMark = '#';
        private static string ApplayGlossary(string text){
            if(string.IsNullOrEmpty(text))return text;
            var startIndex = text.IndexOf(GlossaryKeyStartMark);
            if(startIndex == -1)return text;
            var endIndex = text.IndexOf(GlossaryKeyEndMark, startIndex + 1);
            if(endIndex == -1)return text;

            if(endIndex - startIndex <= 1){
                return text;
            }
            var key = text[startIndex..(endIndex + 1)];
            if(!_staticGlossaryLookup.TryGetValue(key, out var localzieData)){
                return text;
            }

            if(!glossaryCache.TryGetValue(key, out var translateResult)){
                translateResult = LocalizationManager.GetText(localzieData.LocalizeKey);
                glossaryCache[key] = translateResult;
            }

            text = text.Replace(key, translateResult);
            text = ApplayGlossary(text);
            return text;
        }
        
        // 语言代码转换
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
        
        // Addressables加载句柄 - Phase 2: 单文件加载
        private static AsyncOperationHandle<TextAsset> languageDataHandle;
        
        /// <summary>
        /// Phase 2: 加载语言聚合文件
        /// 每语言一个文件，单次加载
        /// </summary>
        public static async Task LoadLanguageData(string langCode)
        {
            langCode = langCode.ToLower();
            string address = $"{langCode}_all";
            
            Debug.Log($"[Localization] 开始加载语言数据: {address}");
            
            languageDataHandle = Addressables.LoadAssetAsync<TextAsset>(address);
            await languageDataHandle.Task;

            if (languageDataHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var jsonAsset = languageDataHandle.Result;
                if (jsonAsset == null || string.IsNullOrEmpty(jsonAsset.text))
                {
                    Debug.LogError($"[Localization] 加载 {address} 失败：内容为空");
                    return;
                }

                // 解析JSON到扁平字典
                bool success = DataModuleManager.LoadLanguageDataFromJson(jsonAsset.text);
                if (success)
                {
                    Debug.Log($"[Localization] {address} 加载完成，共 {DataModuleManager.GetTextCount()} 条文本");
                }
            }
            else
            {
                string errorMsg = $"[Localization] 加载 {address} 失败！\n状态: {languageDataHandle.Status}";
                if (languageDataHandle.OperationException != null)
                {
                    errorMsg += $"\n异常: {languageDataHandle.OperationException.Message}";
                }
                Debug.LogError(errorMsg);
            }
        }

        public static async void SwitchLanguage(string languageCode)
        {
            languageCode = ConvertCode(languageCode);
            if (!Config.inGameLanguageCodeList.Contains(languageCode))
            {
                Debug.LogError($"[Localization] 不支持的语言类型：{languageCode}");
                return;
            }
            
            if (_currLanguageCode == languageCode)
            {
                return;
            }
            
            _currLanguageCode = languageCode;

            if(languageDataHandle.IsValid()){
                Addressables.Release(languageDataHandle);
            }

            ReflashGlossaryCache();
            DataModuleManager.ChangeLanguage(languageCode);
            
            await LoadLanguageData(_currLanguageCode);

            OnLanguageChanged?.Invoke(languageCode);
        }
    }
}
