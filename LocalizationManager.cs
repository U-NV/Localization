using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
            return Path.Combine(Application.dataPath, 
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
            result = ApplayGlossary(result);
            return result;
        }

        public static string GetTextWithArg(string textKey, params object[] args)
        {
            if (string.IsNullOrEmpty(textKey))
                return string.Empty;
                
            // 如果找不到关键词对应的翻译，直接返回默认结果
            if(!DataModuleManager.TryGetText(textKey,out var result))
            {
                return textKey;
            }
            
            // 找到则尝试添加参数
            try
            {
                // 使用StringBuilder优化字符串拼接
                _stringBuilder.Clear();
                _stringBuilder.AppendFormat(result, args);
                result = ApplayGlossary(_stringBuilder.ToString());
                return result;
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
            // 找不到关键词标记则直接返回原始文本
            var startIndex = text.IndexOf(GlossaryKeyStartMark);
            if(startIndex == -1)return text;
            // 从开始标记之后的位置查找结束标记，避免找到同一个标记
            var endIndex = text.IndexOf(GlossaryKeyEndMark, startIndex + 1);
            if(endIndex == -1)return text;

            // 连续的两个##， 跳过
            if(endIndex - startIndex <= 1){
                return text;
            }
            // 找到标记则提取关键词（包含开始和结束标记）
            var key = text[startIndex..(endIndex + 1)];
            // Debug.Log($"key:{key}");
            // 如果不包含这个关键词，直接返回原文本
            if(!_staticGlossaryLookup.TryGetValue(key, out var localzieData)){
                // Debug.LogWarning($"找不到术语\"{key}\"对应的翻译");
                return text;
            }

            // 包含则先查找缓存
            if(!glossaryCache.TryGetValue(key, out var translateResult)){
                translateResult = LocalizationManager.GetText(localzieData.LocalizeKey);
                glossaryCache[key] = translateResult;
            }
            // Debug.Log($"key:{key} value:{translateResult}");

            // 将关键词部分替换成翻译结果
            text = text.Replace(key, translateResult);

            // 递归处理，因为可能存在嵌套关键词
            text = ApplayGlossary(text);
            return text;
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
        // public static void ProcessLocalizeKey(string input, out string prefix, out string key)
        // {
        //     if (string.IsNullOrEmpty(input))
        //     {
        //         prefix = null;
        //         key = null;
        //         return;
        //     }
            
        //     // 直接解析，无缓存开销（因为大部分key只解析一次）
        //     int lastIndex = input.LastIndexOf(".", StringComparison.Ordinal);
        //     if (lastIndex > 0)
        //     {
        //         prefix = input.Substring(0, lastIndex).Trim();
        //         key = input.Substring(lastIndex+1).Trim();
        //     }
        //     else
        //     {
        //         prefix = null;
        //         key = input.Trim();
        //     }
        // }

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
        
        // public static bool TryGetLanguagePath(string languageCode, out string languagePath)
        // {
        //     languageCode = languageCode.ToLower();
        //     string outputPath = Path.Combine(Application.streamingAssetsPath, 
        //         LocalizationManager.LocalizationResourcesFolder, languageCode);
            
        //     if (Directory.Exists(outputPath))
        //     {
        //         languagePath = outputPath;
        //         return true;
        //     }
            
        //     Debug.LogError($"找不到语言类型{languageCode} 对应的文件夹");
        //     languagePath = null;
        //     return false;
        // }
        private static AsyncOperationHandle<IList<TextAsset>> languageCodeHandle;
        
        public static async Task PreloadLanguageByLabel(string langCode)
        {
            langCode = langCode.ToLower();
            Debug.Log($"[Localization] 开始预加载语言包 Label: {langCode}");
            
            // 1. 查找所有带有该 Label 的 TextAsset 资源句柄
            // 假设你的 JSON 文件在 Addressables 里都打上了 langCode (如 "CN") 的 Label
            languageCodeHandle = 
                Addressables.LoadAssetsAsync<TextAsset>(langCode, null);

            await languageCodeHandle.Task;

            if (languageCodeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // 检查结果是否为 null 或空
                if (languageCodeHandle.Result == null)
                {
                    Debug.LogError($"[Localization] 预加载 Label: {langCode} 失败！Result 为 null。可能原因：Addressables 中不存在该 Label 的资源。");
                    return;
                }

                if (languageCodeHandle.Result.Count == 0)
                {
                    Debug.LogWarning($"[Localization] 预加载 Label: {langCode} 完成，但未找到任何资源。请检查 Addressables 中是否正确设置了 Label: {langCode}");
                    return;
                }

                foreach (TextAsset jsonAsset in languageCodeHandle.Result)
                {
                    if (jsonAsset == null)
                    {
                        Debug.LogWarning($"[Localization] 发现 null 资源，跳过");
                        continue;
                    }
                    // 通过资源名称或地址识别模块名
                    // 假设地址格式是 "CN/QuestModule"，我们根据名称提取
                    string moduleName = jsonAsset.name; 
                    DataModuleManager.LoadDataModule(moduleName, jsonAsset);

                }
                Debug.Log($"[Localization] Label: {langCode} 预加载完成，共 {languageCodeHandle.Result.Count} 个模块");
            }
            else
            {
                // 输出详细的错误信息
                string errorMsg = $"[Localization] 预加载 Label: {langCode} 失败！";
                errorMsg += $"\n状态: {languageCodeHandle.Status}";
                
                if (languageCodeHandle.OperationException != null)
                {
                    errorMsg += $"\n异常信息: {languageCodeHandle.OperationException.Message}";
                    errorMsg += $"\n堆栈跟踪: {languageCodeHandle.OperationException.StackTrace}";
                }
                
                // 检查是否是找不到资源的问题
                if (languageCodeHandle.Status == AsyncOperationStatus.Failed)
                {
                    errorMsg += $"\n可能原因：";
                    errorMsg += $"\n1. Addressables 中不存在 Label 为 '{langCode}' 的资源";
                    errorMsg += $"\n2. Label 大小写不匹配（安卓平台对大小写敏感）";
                    errorMsg += $"\n3. Addressables 资源未正确打包到安卓平台";
                    errorMsg += $"\n4. Addressables 初始化未完成";
                }
                
                Debug.LogError(errorMsg);
            }
        }

        public static async void SwitchLanguage(string languageCode, List<string> textModules = null)
        {
            languageCode = ConvertCode(languageCode);
            if (!Config.inGameLanguageCodeList.Contains(languageCode))
            {
                Debug.LogError($"找不到语言类型：{languageCode}");
                return;
            }
            
            // 如果目标语言已经是当前语言，避免重复加载
            if (_currLanguageCode == languageCode)
            {
                return;
            }
            
            _currLanguageCode = languageCode;
            // if (!TryGetLanguagePath(_currLanguageCode, out var languagePath))
            // {
            //     return;
            // }

            if(languageCodeHandle.IsValid()){
                Addressables.Release(languageCodeHandle);
            }

            ReflashGlossaryCache();

            DataModuleManager.ChangeLanguage(languageCode);
            

            var initHandle = Addressables.InitializeAsync();
            await initHandle.Task;
            
            // 检查句柄是否有效，避免访问无效句柄导致异常
            if (initHandle.IsValid())
            {
                if (initHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("Addressables 初始化成功");
                    // 这里再开始加载你的本地化资源
                }
                else if (initHandle.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("Addressables 初始化失败: " + initHandle.OperationException);
                }
            }
            else
            {
                // 如果句柄无效，说明 Addressables 可能已经初始化过了
                Debug.Log("Addressables 已经初始化，跳过初始化步骤");
            }

            await PreloadLanguageByLabel(_currLanguageCode);

            // DataModuleManager.LoadDataModule(Config.defaultModuleNames);
            // DataModuleManager.LoadDataModule(textModules);


            // 触发语言切换事件
            OnLanguageChanged?.Invoke(languageCode);
        }
    

    }
}
