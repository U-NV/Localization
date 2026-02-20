using System;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using U0UGames.Localization;

namespace U0UGames.Localization.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizeText:LocalizeComponent
    {
        [SerializeField] private TMP_Text _target;
        public TMP_Text TargetText => _target;
        [SerializeField] protected LocalizeData localizeData ;
        public (string,string) GetLocalizeData()
        {
            return (localizeData.LocalizeKey, localizeData.LocalizeString.Value);
        }
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
                localizeData?.SetLocalizeKey(value.localizationKey);
                RefreshComponent();
            }
            get
            {
                return localizeData?.LocalizeString;
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
            localizeData.SetLocalizeKey(text.localizationKey);
            Target = target;
            RefreshComponent();
        }

        public void ForceShow(string text)
        {
            if (!Target) return;

            localizeData.Clear();
            Target.text = text;
            OnTextChange?.Invoke();
        }
        
        public override void RefreshComponent()
        {
            if (!Target) return;

            if (localizeData.LocalizeString == null || string.IsNullOrEmpty(localizeData.LocalizeString.localizationKey))
            {
                Target.text = "";
                return;
            }
            
            Target.text = localizeData.LocalizeString.Value;
            OnTextChange?.Invoke();
        }
    }
}