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
        
        public override void RefreshComponent()
        {
            if (localizeData.LocalizeString == null || string.IsNullOrEmpty(localizeData.LocalizeString.localizationKey))
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
                Target.text = LocalizationManager.GetTextWithArg(localizeData.LocalizeString.localizationKey, args);

            }
            else
            {
                Target.text = LocalizationManager.GetText(localizeData.LocalizeString.localizationKey);
            }

        }
    }
}