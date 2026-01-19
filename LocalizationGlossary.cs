using System.Collections;
using System.Collections.Generic;
using U0UGames.Excel2SO;
using U0UGames.Localization;
using UnityEngine;

public class LocalizationGlossary : ScriptableObject, IExcelLineData
{
    [System.Serializable]
    public class GlossaryData{
        public string keyword;
        public LocalizeData data;
    }

    public List<GlossaryData> glossaryDataList;

    public void ProcessData()
    {
        return;
    }
}
