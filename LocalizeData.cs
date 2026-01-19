using System;
using UnityEngine;

namespace U0UGames.Localization
{
    [System.Serializable]
    public class LocalizeData
    {
        [SerializeField] private string lKey;
        public string LocalizeKey => lKey;
        [SerializeField] private string lValue;

        public LocalizeData(){

        }

        public LocalizeData(string key, string value){
            lKey = key;
            lValue = value;
            _localizeString = new LocalizeString(key);
        }

        private LocalizeString _localizeString;
        public LocalizeString LocalizeString
        {
            get
            {
                if (_localizeString != null && _localizeString.localizationKey == lKey) return _localizeString;
                _localizeString = new LocalizeString(lKey);
                return _localizeString;
            }
        }
    }
}