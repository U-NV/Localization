using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace U0UGames.Localization
{
    [Serializable]
    public class LocalizeLanguageCode:IEquatable<LocalizeLanguageCode>
    {
        [SerializeField] private string languageCode;
        public string LanguageCode=>languageCode;
        public bool Equals(LocalizeLanguageCode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return languageCode == other.languageCode;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LocalizeLanguageCode)obj);
        }

        public override int GetHashCode()
        {
            return (languageCode != null ? languageCode.GetHashCode() : 0);
        }
    }
}