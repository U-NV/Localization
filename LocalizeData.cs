using System;

namespace U0UGames.Localization
{
    [System.Serializable]
    public class LocalizeData
    {
        public string key;
        [Obsolete]
        public string value;

        private LocalizeString _localizeString;
        public LocalizeString LocalizeString
        {
            get
            {
                if (_localizeString != null && _localizeString.localizationKey == key) return _localizeString;
                _localizeString = new LocalizeString(key);
                return _localizeString;
            }
        }
    }
}