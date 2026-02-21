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
        public bool IsValid => !string.IsNullOrEmpty(lKey) && !string.IsNullOrEmpty(lValue);

        public LocalizeData(){

        }

        public LocalizeData(string key, string value){
            lKey = key;
            lValue = value;
            _localizeString = new LocalizeString(key);
        }

        public void SetLocalizeKey(string key){
            lKey = key;
            if(_localizeString!=null){
                _localizeString.localizationKey = key;
            }else{
                _localizeString = new LocalizeString(key);
            }
            lValue = _localizeString.Value;
        }

        internal void Clear()
        {
            lKey = "";
            lValue = "";
            _localizeString = null;
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