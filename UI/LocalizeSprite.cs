using System;
using System.Collections.Generic;
using UnityEngine;
using U0UGames.Localization;

namespace U0UGames.Localization.UI
{
    public class LocalizeSprite:LocalizeComponent
    {
        [Serializable]
        public class LocalizeSpriteConfig
        {
            public LocalizeLanguageCode languageCode;
            public Sprite sprite;
        }

        [SerializeField] private Sprite _fallbackSprite;
        [SerializeField] private List<LocalizeSpriteConfig> _spriteConfigs;
        [SerializeField] private Sprite currSprite;
        public event Action<Sprite> OnSpriteChanged;
        public Sprite CurrSprite{
            get
            {
                if (currSprite == null) currSprite = _fallbackSprite;
                return currSprite;
            }
        }

        protected virtual void SetSprite(Sprite sprite)
        {
            OnSpriteChanged?.Invoke(sprite);
            currSprite = sprite;
        }
        public void Awake()
        {
            var currLanguage = LocalizationManager.CurrLanguageCode;
            if (string.IsNullOrEmpty(currLanguage))
            {
                SetSprite(_fallbackSprite);
            }
            else
            {
                RefreshComponent();
            }
        }
        public override void RefreshComponent()
        {
            var currLanguage = LocalizationManager.CurrLanguageCode;
            foreach (var config in _spriteConfigs)
            {
                if (config.languageCode.LanguageCode == currLanguage)
                {
                    SetSprite(config.sprite);
                    break;
                }
            }
        }
    }
}