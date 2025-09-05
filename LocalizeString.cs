using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace U0UGames.Localization
{
    [Serializable]
    public class LocalizeString
    {
        public static readonly LocalizeString Empty = new LocalizeString("");
        
        public static bool IsShowLocalizeKey = true;

        
        [FormerlySerializedAs("_localizationKey")] [SerializeField] public string localizationKey;
        public bool IsEmpty => string.IsNullOrEmpty(localizationKey);
        // [SerializeField] private bool _canLocalized = true;
        public LocalizeString(string value, bool canLocalized = true)
        {
            localizationKey = value;
            // _canLocalized = canLocalized;
        }

        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(localizationKey);
        }
        public string Value
        {
            get
            {
                // if (!_canLocalized)
                // {
                //     return localizationKey;
                // }
                
                if (string.IsNullOrEmpty(localizationKey)) return "";
                // 找到key对应的本地化字段并返回，如果找不到则返回key
                string text = LocalizationManager.GetText(localizationKey);
                return text;
            }
        }
    }
}