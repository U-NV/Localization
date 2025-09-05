using System;
using System.Collections.Generic;

namespace U0UGames.Localization.UI
{
    public class LocalizeTextWithArg:LocalizeText
    {
        private List<Func<object>> _argList = new List<Func<object>>();

        public void SetArgs(List<Func<object>> argList)
        {
            _argList = argList;
        }
        
        public override void UpdateText()
        {
            if (localizeString == null || string.IsNullOrEmpty(localizeString.localizationKey))
            {
                Target.text = "";
                return;
            }

            if (_argList != null && _argList.Count > 0)
            {
                object[] args = new object[_argList.Count];
                for (var index = 0; index < _argList.Count; index++)
                {
                    var arg = _argList[index];
                    args[index] = arg.Invoke();
                }
                Target.text = LocalizationManager.GetTextWithArg(localizeString.localizationKey, args);

            }
            else
            {
                Target.text = LocalizationManager.GetText(localizeString.localizationKey);
            }

        }
    }
}