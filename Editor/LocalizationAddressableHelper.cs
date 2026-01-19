using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using U0UGames.Localization.Editor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public class LocalizationAddressableHelper
{
    public static void SetJsonAddressable(string filePath, string langCode, string moduleName)
    {
        // 1. 获取 Addressable 设置文件
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        // 2. 找到或创建一个特定的 Group
        string groupName = "LocalizationData";
        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (group == null)
        {
            // 创建组时添加必要的schemas：BundledAssetGroupSchema (Content Packing & Loading) 和 ContentUpdateGroupSchema
            group = settings.CreateGroup(groupName, false, false, true, null);
            // var schema = group.GetSchema<BundledAssetGroupSchema>();
            // schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            // schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        }

        // 3. 获取文件的 GUID
        string assetPath = UnityPathUtility.FullPathToAssetPath(filePath);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        // 4. 为该资源创建或更新 Entry
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

        // 5. 设置自定义地址：例如 "CN/Quest_01"
        string customAddress = $"{langCode}/{moduleName}";
        entry.address = customAddress;

        settings.AddLabel(langCode.ToLower());
        entry.SetLabel(langCode.ToLower(), true, true);

        // 6. 保存设置
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true, true);
        
        UnityEngine.Debug.Log($"[Localization] 已将 {assetPath} 设置为 Address: {customAddress}");
        AssetDatabase.SaveAssets();
    }
}