using UnityEngine;

namespace U0UGames.Localization
{   
    public abstract class LocalizeComponent : MonoBehaviour
    {
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
        protected virtual void OnEnable()
        {
            StartListening();
            RefreshComponent();
        }

        public abstract void RefreshComponent();

        protected virtual void OnDisable()
        {
            StopListening();
        }

        private void OnDestroy()
        {
            StopListening();
        }

        private void LanguageChangeEventHandle(string languageCode)
        {
            RefreshComponent();
        }
    }
}