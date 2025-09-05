using System;
using System.Collections.Generic;
using UnityEngine;

namespace U0UGames.Localization.UI
{
    public class LocalizeSprite:MonoBehaviour
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
                UpdateImage();
            }
        }
        public void UpdateImage()
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
        
        private bool _isListening = false;

        private void OnEnable()
        {
            StartListening();
            UpdateImage();
        }
        private void OnDisable()
        {
            StopListening();
        }
        private void OnDestroy()
        {
            StopListening();
        }
        public void StartListening()
        {
            if(_isListening)return;
            _isListening = true;
            LocalizationManager.OnLanguageChanged += LanguageChangeEventHandle;
        }
        public void StopListening()
        {
            if(!_isListening)return;
            _isListening = false;
            LocalizationManager.OnLanguageChanged -= LanguageChangeEventHandle;
        }
        private void LanguageChangeEventHandle(string languageCode)
        {
            UpdateImage();
        }
    }
}