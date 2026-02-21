using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace U0UGames.Localization.Editor
{
    public class LocalizationProjectCollector
    {
        private readonly LocalizationConfig _localizationConfig;
        private static readonly string CollectedJsonFileName = "CollectedLocalizeData.json";

        public LocalizationProjectCollector(LocalizationConfig config)
        {
            _localizationConfig = config;
        }

        public void CollectLocalizeDataFromProject()
        {
            Dictionary<string, string> collectedData = new();
            int warningCount = 0;

            EditorUtility.DisplayProgressBar("收集本地化数据", "正在搜索 ScriptableObject...", 0f);

            string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            for (int i = 0; i < soGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(soGuids[i]);
                var scriptableObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (scriptableObj == null) continue;

                CollectLocalizeDataFromUnityObject(scriptableObj, $"ScriptableObject: {path}", collectedData, ref warningCount);

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("收集本地化数据",
                        $"正在搜索 ScriptableObject ({i}/{soGuids.Length})...",
                        (float)i / soGuids.Length * 0.3f);
                }
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                CollectLocalizeDataFromGameObject(prefab, $"预制件: {path}", collectedData, ref warningCount);

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("收集本地化数据",
                        $"正在搜索预制件 ({i}/{prefabGuids.Length})...",
                        0.3f + (float)i / prefabGuids.Length * 0.4f);
                }
            }

            var currentSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" });
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);

                    EditorUtility.DisplayProgressBar("收集本地化数据",
                        $"正在搜索场景 ({i + 1}/{sceneGuids.Length}): {Path.GetFileNameWithoutExtension(scenePath)}",
                        0.7f + (float)i / sceneGuids.Length * 0.3f);

                    try
                    {
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[本地化收集] 无法打开场景: {scenePath}，已跳过。\n{e.Message}");
                        continue;
                    }

                    foreach (var rootGo in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        CollectLocalizeDataFromGameObject(rootGo, $"场景: {scenePath}", collectedData, ref warningCount);
                    }
                }

                if (currentSceneSetup != null && currentSceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(currentSceneSetup);
                }
            }

            EditorUtility.ClearProgressBar();

            if (collectedData.Count > 0)
            {
                string json = JsonConvert.SerializeObject(collectedData, Formatting.Indented);
                var folderFullPath = UnityPathUtility.RootFolderPathToFullPath(_localizationConfig.excelDataFolderRootPath);
                if (!Directory.Exists(folderFullPath))
                {
                    Directory.CreateDirectory(folderFullPath);
                }

                string savePath = Path.Combine(folderFullPath, CollectedJsonFileName);
                File.WriteAllText(savePath, json);
                Debug.Log($"成功收集 {collectedData.Count} 条数据（{warningCount} 条警告），已保存至: {savePath}");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("未收集到任何有效的本地化数据。");
            }
        }

        private void CollectLocalizeDataFromUnityObject(Object target, string basePath,
            Dictionary<string, string> collectedData, ref int warningCount)
        {
            if (target == null) return;

            SerializedObject so;
            try
            {
                so = new SerializedObject(target);
            }
            catch
            {
                return;
            }

            string objPath = $"{basePath} [{target.GetType().Name}]";
            ScanSerializedObject(so, target, objPath, collectedData, ref warningCount);
            so.Dispose();
        }

        private void CollectLocalizeDataFromGameObject(GameObject go, string basePath,
            Dictionary<string, string> collectedData, ref int warningCount)
        {
            MonoBehaviour[] allComponents = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;

                SerializedObject so;
                try
                {
                    so = new SerializedObject(comp);
                }
                catch
                {
                    continue;
                }

                string objPath = $"{basePath} -> {GetGameObjectPath(comp.gameObject)} [{comp.GetType().Name}]";
                ScanSerializedObject(so, comp, objPath, collectedData, ref warningCount);
                so.Dispose();
            }
        }

        private void ScanSerializedObject(SerializedObject so, Object context, string basePath,
            Dictionary<string, string> collectedData, ref int warningCount)
        {
            SerializedProperty sp = so.GetIterator();
            bool enterChildren = true;
            while (sp.NextVisible(enterChildren))
            {
                if (sp.propertyType == SerializedPropertyType.Generic && sp.type == "LocalizeData")
                {
                    enterChildren = false;
                    ProcessSerializedLocalizeData(sp, context, basePath, collectedData, ref warningCount);
                }
                else
                {
                    enterChildren = true;
                }
            }
        }

        private void ProcessSerializedLocalizeData(SerializedProperty prop, Object context,
            string basePath, Dictionary<string, string> collectedData, ref int warningCount)
        {
            var lKeyProp = prop.FindPropertyRelative("lKey");
            var lValueProp = prop.FindPropertyRelative("lValue");
            if (lKeyProp == null || lValueProp == null) return;

            string key = lKeyProp.stringValue;
            string value = lValueProp.stringValue;
            string objPath = $"{basePath}.{prop.name}";

            if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(value))
            {
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[本地化收集] 关键词为空！对象路径: {objPath}", context);
                warningCount++;
                return;
            }

            if (collectedData.TryGetValue(key, out var existingValue))
            {
                if (existingValue != value)
                {
                    Debug.LogWarning($"[本地化收集] 关键词重复且值不同！Key: {key}, 当前值: \"{value}\", 已存在值: \"{existingValue}\"。\n对象路径: {objPath}", context);
                }
                warningCount++;
                return;
            }

            collectedData.Add(key, value);
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }
    }
}
