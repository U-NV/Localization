using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using static U0UGames.Localization.Editor.LocalizationDataUtils;
using Object = UnityEngine.Object;

namespace U0UGames.Localization.Editor
{
    // DeepSeek API 响应模型
    // DeepSeek 流式响应模型
    public class DeepSeekStreamResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public StreamChoice[] choices;
    }

    public class StreamChoice
    {
        public int index;
        public Delta delta;
        public string finish_reason;
    }

    public class Delta
    {
        public string role;
        public string content;
    }

    public class DeepSeekResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public Choice[] choices;
        public Usage usage;
    }

    public class Choice
    {
        public int index;
        public Message message;
        public string finish_reason;
    }

    public class Message
    {
        public string role;
        public string content;
    }

    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    public class LocalizationTranslateWindow
    {
        public class EditorPrefsKey
        {
            public const string TranslateToIndex = "LocalizationTranslateWindow.TranslateToIndex";
        }
        private LocalizationConfig _localizationConfig;
        private int _translateToIndex = 1;

        public void Init()
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            _translateToIndex = 
                EditorPrefs.GetInt(EditorPrefsKey.TranslateToIndex);
        }

        public void OnGUI()
        {
            if (!_localizationConfig.IsValid())
            {
                EditorGUILayout.LabelField("Error: 没有配置任何语言，请先在配置界面添加语言", EditorStyles.helpBox);
                return;
            }
            

           
            EditorGUILayout.LabelField("翻译", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            _localizationConfig.translateApiKey = EditorGUILayout.TextField("翻译API Key", _localizationConfig.translateApiKey);
            _localizationConfig.translateApiUrl = EditorGUILayout.TextField("翻译API URL", _localizationConfig.translateApiUrl);
            
            // 显示API URL格式提示
            if (!string.IsNullOrEmpty(_localizationConfig.translateApiUrl))
            {
                if (!Uri.TryCreate(_localizationConfig.translateApiUrl, UriKind.Absolute, out _))
                {
                    EditorGUILayout.HelpBox("API URL格式无效！请使用完整的绝对地址，例如：https://api.deepseek.com", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("API URL格式正确，系统会自动添加 /chat/completions 端点", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请输入翻译API的基础URL地址，例如：https://api.deepseek.com", MessageType.Warning);
            }
            
            EditorGUILayout.LabelField("翻译AI提示");
            _localizationConfig.translateAIPrompt = EditorGUILayout.TextArea(_localizationConfig.translateAIPrompt, GUILayout.Height(60));
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_localizationConfig);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            
                var originalLanguageCode = _localizationConfig.OriginalLanguageCode;
                
                EditorGUILayout.LabelField($"将{originalLanguageCode}翻译为");

                var allLanguageCodeList = new List<string>(_localizationConfig.languageDisplayDataList.Count+1);
                foreach (var generateConfig in _localizationConfig.languageDisplayDataList)
                {
                    allLanguageCodeList.Add(generateConfig.languageCode);
                }
                _translateToIndex = EditorGUILayout.Popup(_translateToIndex, 
                    allLanguageCodeList.ToArray());
                if (_translateToIndex < 0 || _translateToIndex >= allLanguageCodeList.Count)
                {
                    EditorGUILayout.LabelField("无效的语言");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                
                EditorPrefs.SetInt(EditorPrefsKey.TranslateToIndex,_translateToIndex);
                var selectedLanguageCode = allLanguageCodeList[_translateToIndex];
            
            EditorGUILayout.EndHorizontal();
            {
                if (GUILayout.Button("翻译"))
                {
                    if(selectedLanguageCode == originalLanguageCode)return;
                    _ = TranslateAsync(originalLanguageCode, selectedLanguageCode);
                }
            }
            {
                if (GUILayout.Button("翻译所有语言"))
                {
                    _ = TranslateAllLanguagesAsync(originalLanguageCode, allLanguageCodeList);
                }
            }

            
            EditorGUILayout.EndVertical();

        }

        private async Task TranslateAsync(string srcLanguageCode, string targetLanguageCode)
        {
            try
            {
                await Translate(srcLanguageCode, targetLanguageCode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"翻译过程中发生错误：{ex.Message}");
            }
        }

        private async Task TranslateAllLanguagesAsync(string originalLanguageCode, List<string> allLanguageCodeList)
        {
            try
            {
                foreach (var otherLanguageCode in allLanguageCodeList)
                {
                    if(otherLanguageCode == originalLanguageCode)continue;
                    await Translate(originalLanguageCode, otherLanguageCode);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"批量翻译过程中发生错误：{ex.Message}");
            }
        }

        public class TranslationDataRequest
        {
            public int num;
            public string[] texts;
        }
        private string BuildSystemPrompt(string from, string to)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"你是专业的游戏本地化翻译助手。请将输入文本从 {from} 翻译为 {to}。");
            sb.AppendLine();
            sb.AppendLine("【翻译规则】");
            sb.AppendLine($"1. 译文须准确、自然、流畅，符合 {to} 的游戏用语表达习惯");
            sb.AppendLine("2. 保持原文语气、标点和换行符（\\n）等格式，不得增删内容");
            sb.AppendLine("3. 严格原样保留 Unity 富文本标签（如 <color=#FF0000>文字</color>、<b>、<size=24> 等），不翻译标签名称及属性值");
            sb.AppendLine("4. 严格原样保留被两个井号包围的占位符（如 #PlayerName#、#ItemCount# 等），不做任何修改");
            sb.AppendLine("5. 输出 texts 数组的元素数量必须与输入的 num 值完全一致，顺序一一对应，不可合并或拆分条目");
            sb.AppendLine("6. 只输出符合格式的 JSON，不附加任何解释、注释或 Markdown 代码块标记");

            sb.AppendLine();
            sb.AppendLine("【输入 / 输出格式】");
            sb.AppendLine("输入：{\"num\": 3, \"texts\": [\"原文1\", \"原文2\", \"原文3\"]}");
            sb.AppendLine("输出：{\"num\": 3, \"texts\": [\"译文1\", \"译文2\", \"译文3\"]}");

            if (!string.IsNullOrEmpty(_localizationConfig.translateAIPrompt))
            {
                sb.AppendLine();
                sb.AppendLine("【附加要求】");
                sb.AppendLine(_localizationConfig.translateAIPrompt);
            }

            return sb.ToString();
        }

        private async Task<string[]> TranslateByAI(string from, string to, string[] textList, Action<string> onStreamContent = null)
        {
            
            if (string.IsNullOrEmpty(_localizationConfig.translateApiUrl))
            {
                Debug.LogError("翻译API URL未配置");
                return null;
            }

            if (string.IsNullOrEmpty(_localizationConfig.translateApiKey))
            {
                Debug.LogError("翻译API Key未配置");
                return null;
            }

            // 验证API URL是否为有效的绝对URI
            string fullApiUrl = _localizationConfig.translateApiUrl;
            
            // 如果URL不包含完整路径，自动添加chat/completions端点
            if (!fullApiUrl.EndsWith("/chat/completions"))
            {
                if (!fullApiUrl.EndsWith("/"))
                {
                    fullApiUrl += "/";
                }
                fullApiUrl += "chat/completions";
            }
            
            if (!Uri.TryCreate(fullApiUrl, UriKind.Absolute, out Uri apiUri))
            {
                Debug.LogError($"翻译API URL格式无效：{fullApiUrl}。请确保URL是完整的绝对地址，例如：https://api.deepseek.com/chat/completions");
                return null;
            }

            // 重试机制：最多重试3次
            int maxRetries = 3;
            for (int retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        Debug.Log($"翻译请求重试第 {retryCount} 次...");
                        // 重试前等待一段时间，使用指数退避
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1)));
                    }

                    TranslationDataRequest translationRequest = new TranslationDataRequest();
                    translationRequest.num = textList.Length;
                    translationRequest.texts = textList;
                    var jsonStr = JsonConvert.SerializeObject(translationRequest, Formatting.Indented);

                    using (var httpClient = new HttpClient())
                    {
                    // 设置超时时间为5分钟，AI翻译可能需要较长时间
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    
                    // 设置请求头，按照DeepSeek API要求
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_localizationConfig.translateApiKey}");
                    
                    var requestBody = new
                    {
                        model = "deepseek-chat",
                        messages = new[]
                        {
                            new
                            {
                                role = "system",
                                content = BuildSystemPrompt(from, to)
                            },
                            new
                            {
                                role = "user",
                                content = $"{jsonStr}"
                            }
                        },
                        response_format = new
                        {
                            type = "json_object"
                        },
                        max_tokens = 8000,
                        stream = true
                    };

                    var json = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    Debug.Log($"发送翻译请求：{from} -> {to}，文本数量：{textList.Length}\n报文:{json}");
                    
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullApiUrl)
                    {
                        Content = content
                    };
                    var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var contentBuilder = new StringBuilder();
                        var finishReason = string.Empty;

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = await reader.ReadLineAsync();
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    continue;
                                }

                                if (!line.StartsWith("data: "))
                                {
                                    continue;
                                }

                                var data = line.Substring(6).Trim();
                                if (data == "[DONE]")
                                {
                                    break;
                                }

                                try
                                {
                                    var streamResponse = JsonConvert.DeserializeObject<DeepSeekStreamResponse>(data);
                                    var choice = streamResponse?.choices?.FirstOrDefault();
                                    if (choice == null)
                                    {
                                        continue;
                                    }

                                    if (!string.IsNullOrEmpty(choice.finish_reason))
                                    {
                                        finishReason = choice.finish_reason;
                                    }

                                    var deltaText = choice.delta?.content;
                                    if (!string.IsNullOrEmpty(deltaText))
                                    {
                                        contentBuilder.Append(deltaText);
                                        onStreamContent?.Invoke(contentBuilder.ToString());
                                    }
                                }
                                catch (JsonException)
                                {
                                    // 流式分片中偶发异常片段时跳过，继续接收后续分片
                                }
                            }
                        }

                        if (finishReason == "length")
                        {
                            Debug.LogError($"AI响应被截断（finish_reason=length），本批次共{textList.Length}条文本，输出超出token限制。请减小 MaxTextSize 或 MaxTextCount。");
                            return null;
                        }

                        var translatedContent = contentBuilder.ToString();
                        Debug.Log($"AI返回的翻译内容：{translatedContent}");
                        try
                        {
                            var translationResult = JsonConvert.DeserializeObject<TranslationDataRequest>(translatedContent);
                            if (translationResult?.texts != null && translationResult.texts.Length == textList.Length)
                            {
                                Debug.Log($"翻译成功，共{translationResult.texts.Length}条文本");
                                return translationResult.texts;
                            }

                            Debug.LogError($"翻译结果数量不匹配：期望{textList.Length}，实际{translationResult?.texts?.Length ?? 0}");
                            Debug.LogError($"AI返回的原始内容：{translatedContent}");
                        }
                        catch (JsonException ex)
                        {
                            Debug.LogError($"解析流式翻译结果失败：{ex.Message}");
                            Debug.LogError($"AI返回的原始内容：{translatedContent}");
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"翻译API请求失败：{response.StatusCode} - {errorContent}");
                        
                        // 尝试解析错误响应
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorContent);
                            if (errorResponse != null)
                            {
                                if (errorResponse.TryGetValue("error_msg", out var errorMsg) && errorMsg != null)
                                {
                                    Debug.LogError($"API错误详情：{errorMsg}");
                                }
                                else if (errorResponse.TryGetValue("error", out var error) && error != null)
                                {
                                    Debug.LogError($"API错误详情：{error}");
                                }
                            }
                        }
                        catch
                        {
                            // 如果无法解析错误响应，使用原始错误内容
                        }
                    }
                    
                    // 如果成功执行到这里，说明请求成功，跳出重试循环
                    // 注意：实际的返回语句在上面的成功处理逻辑中
                    }
                }
                catch (TaskCanceledException ex)
                {
                    if (ex.CancellationToken.IsCancellationRequested)
                    {
                        Debug.LogError("翻译请求被用户取消");
                        return null; // 用户取消，不重试
                    }
                    else
                    {
                        Debug.LogError($"翻译请求超时（第 {retryCount + 1} 次尝试）：{ex.Message}");
                        if (retryCount == maxRetries - 1)
                        {
                            Debug.LogError("所有重试都失败了。请检查网络连接或尝试减少翻译文本数量。");
                            return null;
                        }
                        // 继续下一次重试
                    }
                }
                catch (HttpRequestException ex)
                {
                    Debug.LogError($"网络请求失败（第 {retryCount + 1} 次尝试）：{ex.Message}");
                    if (retryCount == maxRetries - 1)
                    {
                        Debug.LogError("所有重试都失败了。请检查网络连接和API配置。");
                        return null;
                    }
                    // 继续下一次重试
                }
                catch (Exception ex)
                {
                    Debug.LogError($"翻译过程中发生错误（第 {retryCount + 1} 次尝试）：{ex.Message}");
                    if (retryCount == maxRetries - 1)
                    {
                        Debug.LogError($"所有重试都失败了。堆栈跟踪：{ex.StackTrace}");
                        return null;
                    }
                    // 继续下一次重试
                }
            }

            return null;
        }

        private async Task<bool> TranslateInter(string srcLanguageCode, string targetLanguageCode, List<string> needTranslateTextList,List<LocalizeLineData> needTranslateDataList, Action<string> onStreamContent = null)
        {
            var translateResult = await TranslateByAI(srcLanguageCode, targetLanguageCode,
                needTranslateTextList.ToArray(), onStreamContent);
            if (translateResult != null)
            {
                Debug.Log($"开始更新{needTranslateDataList.Count}条翻译数据到内存中...");
                for (int i = 0; i < needTranslateDataList.Count; i++)
                {
                    var originalText = needTranslateDataList[i].translatedValues[srcLanguageCode];
                    var translatedText = translateResult[i];
                    
                    needTranslateDataList[i].translatedValues[targetLanguageCode] = translatedText;
                    var oldTips = needTranslateDataList[i].tips2;
                    if(string.IsNullOrEmpty(oldTips) || !oldTips.Contains("AI"))
                        needTranslateDataList[i].tips2 = oldTips + "AI";
                }
                Debug.Log($"翻译数据已成功更新到内存中，共{needTranslateDataList.Count}条");

                return true;
            }

            return false;
        }
        
        public const int MaxTextSize = 2000;
        public const int MaxTextCount = 50;
        public async Task Translate(string srcLanguageCode, string targetLanguageCode)
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            if (_localizationConfig == null)
            {
                Debug.LogWarning("Localization config not initialized.");
                return;
            }
            
            var currLocalizationFileDataList = 
                LocalizationDataUtils.GetAllLocalizationDataFromDataFolder(srcLanguageCode,_localizationConfig.translateDataFolderRootPath);
            var translateDataFolderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.translateDataFolderRootPath);
            if (!Directory.Exists(translateDataFolderFullPath))
            {
                Directory.CreateDirectory(translateDataFolderFullPath);
            }

            if(currLocalizationFileDataList == null || currLocalizationFileDataList.Count == 0)return;
            foreach (var fileData in currLocalizationFileDataList)
            {
                var fileName = fileData.fileName;

                int totalIndex = 0;
                while (totalIndex < fileData.dataList.Count)
                {
                    EditorUtility.DisplayProgressBar($"将{srcLanguageCode}翻译至{targetLanguageCode}",$"正在翻译{fileName}",totalIndex/(float)fileData.dataList.Count);

                    int textSize = 0;
                    List<LocalizeLineData> needTranslateDataList = new List<LocalizeLineData>();
                    List<string> needTranslateTextList = new List<string>();
                    
                    // 收集翻译数据包（同时限制总字符数和条数）
                    for (var startIndex = totalIndex; startIndex < fileData.dataList.Count; startIndex++)
                    {
                        var kvpData = fileData.dataList[startIndex];
                        var key = kvpData.key;
                        var originalText = kvpData.translatedValues[srcLanguageCode];
                        var targetText = kvpData.translatedValues[targetLanguageCode];
                        if (!string.IsNullOrEmpty(key) && string.IsNullOrEmpty(targetText) && !string.IsNullOrEmpty(originalText))
                        {
                            var newTextLength = originalText.Length;
                            var newDataSize = textSize + newTextLength;
                            if (newDataSize > MaxTextSize || needTranslateTextList.Count >= MaxTextCount)
                            {
                                break;
                            }
                            textSize += newTextLength;
                            needTranslateDataList.Add(kvpData);
                            needTranslateTextList.Add(originalText);
                        }
                        totalIndex++;
                    }

                    // 将收集到的数据提交翻译
                    if (textSize <= MaxTextSize && textSize>=0 && needTranslateTextList.Count > 0)
                    {
                        bool success = await TranslateInter(srcLanguageCode, targetLanguageCode,
                            needTranslateTextList, needTranslateDataList, content =>
                            {
                                var showText = content.Length > 80 ? "..." + content.Substring(content.Length - 80) : content;
                                showText = showText.Replace("\n", " ").Replace("\r", "");
                                EditorUtility.DisplayProgressBar(
                                    $"将{srcLanguageCode}翻译至{targetLanguageCode}",
                                    $"正在翻译{fileName} (AI接收中): {showText}", 
                                    totalIndex/(float)fileData.dataList.Count);
                            });
                        if (!success)
                        {
                            Debug.LogError($"翻译失败:{fileName} ({srcLanguageCode}翻译至{targetLanguageCode})");
                        }
                    }
                }

                Debug.Log($"开始保存翻译结果到文件：{fileData.fileName}");
                LocalizationDataUtils.ConvertToExcelFile(
                    fileData,
                    translateDataFolderFullPath
                );
                Debug.Log($"翻译结果已保存到：{translateDataFolderFullPath}\\{fileData.fileName}.xlsx");
                
                // 刷新Unity资源数据库，确保文件更改被识别
                AssetDatabase.Refresh();

            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}