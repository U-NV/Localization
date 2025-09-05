using System;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace U0UGames.Localization.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizeText:MonoBehaviour
    {
        [SerializeField] private TMP_Text _target;
        public TMP_Text TargetText => _target;
        [SerializeField] protected LocalizeString localizeString;
        private void OnValidate()
        {
            _target ??= GetComponent<TMP_Text>();
        }
        protected TMP_Text Target
        {
            get
            {
                if (gameObject == null) return null;
                if (!_target)
                {
                    _target = GetComponent<TMP_Text>();
                }
                return _target;
            }
            set => _target = value;
        }
        public LocalizeString text
        {
            set
            {
                localizeString = value;
                UpdateText();
            }
            get
            {
                return localizeString;
            }
        }

        public float preferredWidth
        {
            get
            {
                if (!Target) return 0;
                return Target.preferredWidth;
            }
        }

        public event Action OnTextChange;
        
        public Color color
        {
            get
            {
                if (!Target) return new Color(0, 0, 0, 0);
                return Target.color;
            }
            set
            {
                if (!Target) return;
                Target.color = value;
            }
        }

        public void Init(LocalizeString text, TMP_Text target)
        {
            localizeString = text;
            Target = target;
            UpdateText();
        }

        public void ForceShow(string text)
        {
            if (!Target) return;

            localizeString = LocalizeString.Empty;
            Target.text = text;
            OnTextChange?.Invoke();
        }
        
        public virtual void UpdateText()
        {
            if (!Target) return;

            if (localizeString == null || string.IsNullOrEmpty(localizeString.localizationKey))
            {
                Target.text = "";
                return;
            }
            
            Target.text = localizeString.Value;
            OnTextChange?.Invoke();
        }
        
        private void OnDestroy()
        {
            StopListening();
        }

        private void OnEnable()
        {
            StartListening();
            UpdateText();
        }

        private void OnDisable()
        {
            StopListening();
        }

        private bool isListening = false;
        public void StartListening()
        {
            if(isListening)return;
            isListening = true;
            LocalizationManager.OnLanguageChanged += LanguageChangeEventHandle;
        }

        public void StopListening()
        {
            if(!isListening)return;
            isListening = false;
            LocalizationManager.OnLanguageChanged -= LanguageChangeEventHandle;
        }

        private void LanguageChangeEventHandle(string languageCode)
        {
            UpdateText();
        }
    }
}