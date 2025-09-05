using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace U0UGames.Localization.Editor.AutoTranslate
{
    public class BatchV3DemoInternalTest
    {
        public static string Main(string appKey,string appSecret,string from,string to, string []textList)
        {
            Dictionary<String, String> dic = new Dictionary<String, String>();
            string url = "https://openapi.youdao.com/v2/api";
            // string[] qArray = { "待输入的文字1", "待输入的文字2", "待输入的文字3" };
            string[] qArray = textList;
            // string appKey = "您的应用ID";
            // string appSecret = "您的应用密钥";
            string salt = DateTime.Now.Millisecond.ToString();
            // dic.Add("from", "源语言");
            // dic.Add("to", "目标语言");
            dic.Add("from", from);
            dic.Add("to", to);
            dic.Add("signType", "v3");
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            long millis = (long)ts.TotalMilliseconds;
            string curtime = Convert.ToString(millis / 1000);
            dic.Add("curtime", curtime);
            string signStr = appKey + Truncate(string.Join("", qArray)) + salt + curtime + appSecret;
            ;
            string sign = ComputeHash(signStr, new SHA256CryptoServiceProvider());
            dic.Add("appKey", appKey);
            dic.Add("salt", salt);
            dic.Add("sign", sign);
            // dic.Add("vocabId", "您的用户词表ID");
            return Post(url, dic, qArray);
        }
        
        // 您的应用ID
        private static string APP_KEY = "1df2c10d0960c93b";
        // 您的应用密钥
        private static string APP_SECRET = "RikyRxaFrmcLhexkvJH3nkhhlejE9U7h";

        //https://ai.youdao.com/DOCSIRMA/html/trans/api/plwbfy/index.html
        public class YouDaoTranslateResult
        {
            public class TranslateResult
            {
                // q字段中的原文句子
                public string query;
                // i字段对应的译文句子
                public string translation;
                // q字段实际翻译语言方向
                public string type;
                // q字段语言方向核实结果
                public string verifyResult;
            }
            // 返回结果代码
            public int errorCode;
            public List<int> errorIndex;
            public List<TranslateResult> translateResults;
        }

        private static readonly Dictionary<string, string> languageLookup = new Dictionary<string, string>()
        {
            { "zh-cn", "zh-CHS" },
            { "zh-tw", "zh-CHT" }
        };
        
        public static string[] Translate(string from, string to, string[] textList)
        {
            if (textList == null || textList.Length == 0) return null;
            
            if (languageLookup.TryGetValue(from.ToLower(), out var newFrom))
            {
                from = newFrom;
            }
            if (languageLookup.TryGetValue(to.ToLower(), out var newTo))
            {
                to = newTo;
            }
            
            var json = Main(APP_KEY, APP_SECRET, from, to, textList);
            YouDaoTranslateResult youDaoResult = JsonConvert.DeserializeObject<YouDaoTranslateResult>(json);
            if (youDaoResult != null)
            {
                if (youDaoResult.errorIndex != null && youDaoResult.errorIndex.Count > 0)
                {
                    Debug.LogError(
                        $"ErrorCode:{youDaoResult.errorCode} Index:{string.Join(",", youDaoResult.errorIndex)}\n https://ai.youdao.com/DOCSIRMA/html/trans/api/plwbfy/index.html");
                    return null;
                }
                
                if(youDaoResult.translateResults == null || youDaoResult.translateResults.Count == 0)
                {
                    Debug.LogError($"ErrorCode:{youDaoResult.errorCode} 无法翻译\n https://ai.youdao.com/DOCSIRMA/html/trans/api/plwbfy/index.html");
                    return null;
                }
                
                List<string> result = new List<string>(youDaoResult.translateResults.Count);
                foreach (var translateResult in youDaoResult.translateResults)
                {
                    result.Add(translateResult.translation);
                }
                return result.ToArray();
            }

            return null;
        }
        
        protected static string ComputeHash(string input, HashAlgorithm algorithm)
        {
            Byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);
            return BitConverter.ToString(hashedBytes).Replace("-", "");
        }

        protected static string Post(string url, Dictionary<String, String> dic, string[] qArray)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in dic)
            {
                if (i > 0)
                    builder.Append("&");
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }

            foreach (var item in qArray)
            {
                builder.Append("&");
                builder.AppendFormat("q={0}", System.Web.HttpUtility.UrlEncode(item));
            }

            byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result.ToString();
            // Console.WriteLine(result);
        }

        protected static string Truncate(string q)
        {
            if (q == null)
            {
                return null;
            }

            int len = q.Length;
            return len <= 20 ? q : (q.Substring(0, 10) + len + q.Substring(len - 10, 10));
        }
    }
}


