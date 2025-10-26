using System;

namespace U0UGames.Localization
{
    [System.Serializable]
    public class LocalizeData
    {
        public string lKey;
        [Obsolete]
        public string lValue;

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