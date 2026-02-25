using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace U0UGames.Localization.Editor
{
    /// <summary>
    /// Addressables配置助手（每语言单文件 + 按语言分Group）
    /// </summary>
    public class LocalizationAddressableHelper
    {
        /// <summary>
        /// 设置聚合语言文件为Addressable（每语言一个文件）
        /// </summary>
        public static void SetLanguageDataAddressable(string filePath, string langCode)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string groupName = $"Localization_{langCode}";
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null)
                {
                    schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                    schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                }
            }

            string assetPath = UnityPathUtility.FullPathToAssetPath(filePath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid))
            {
                UnityEngine.Debug.LogError($"[Localization] 无法获取 GUID，assetPath 可能无效: {assetPath}（原始路径: {filePath}）");
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

            // 简化的地址格式
            string customAddress = $"{langCode}_all";
            entry.address = customAddress;

            string langLabel = langCode.ToLower();
            settings.AddLabel(langLabel);
            entry.SetLabel(langLabel, true, true);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true, true);
            
            UnityEngine.Debug.Log($"[Localization Phase 2] 语言聚合文件: {assetPath} -> Group: {groupName}, Address: {customAddress}");
            AssetDatabase.SaveAssets();
        }
    }
}
