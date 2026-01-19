using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    /// <summary>
    /// Addressables 构建检查工具
    /// 用于验证构建时是否包含了 Addressables 资源
    /// </summary>
    public static class AddressablesBuildChecker
    {
        /// <summary>
        /// 检查 Addressables 配置和构建状态
        /// </summary>
        [MenuItem("Tools/Addressables/检查构建配置")]
        public static void CheckBuildConfiguration()
        {
            Debug.Log("========== Addressables 构建配置检查 ==========");
            
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("❌ 未找到 AddressableAssetSettings！请先配置 Addressables。");
                return;
            }

            // 1. 检查是否启用自动构建
            bool buildWithPlayer = !settings.BuildAddressablesWithPlayerBuild.Equals(AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer);
            Debug.Log($"📦 构建时自动构建 Addressables: {(buildWithPlayer ? "✅ 已启用" : "❌ 未启用")}");
            
            if (!buildWithPlayer)
            {
                Debug.LogWarning("⚠️ 如果未启用自动构建，需要在构建 Player 前手动执行：");
                Debug.LogWarning("   Addressables -> Build -> New Build -> Default Build Script");
            }

            // 2. 检查构建器配置
            int activeBuilderIndex = settings.ActivePlayerDataBuilderIndex;
            if (activeBuilderIndex >= 0 && activeBuilderIndex < settings.DataBuilders.Count)
            {
                var builder = settings.DataBuilders[activeBuilderIndex];
                Debug.Log($"🔧 当前使用的构建器: {builder.GetType().Name} (索引: {activeBuilderIndex})");
            }
            else
            {
                Debug.LogError($"❌ 构建器索引无效: {activeBuilderIndex}");
            }

            // 3. 检查构建路径
            string buildPath = settings.profileSettings.GetValueByName(settings.activeProfileId, "Local.BuildPath");
            string runtimePath = settings.profileSettings.GetValueByName(settings.activeProfileId, "Local.LoadPath");
            Debug.Log($"📁 构建路径: {buildPath}");
            Debug.Log($"📁 运行时路径: {runtimePath}");

            // 4. 检查是否有构建产物
            string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
            string actualBuildPath = buildPath.Replace("[BuildTarget]", platformName);
            actualBuildPath = actualBuildPath.Replace("[UnityEngine.AddressableAssets.Addressables.BuildPath]", 
                Path.Combine(Application.dataPath, "..", "Library", "com.unity.addressables"));
            
            // 尝试解析路径
            if (actualBuildPath.Contains("["))
            {
                // 如果还有未解析的变量，使用默认路径
                actualBuildPath = Path.Combine(Application.dataPath, "..", "Library", "com.unity.addressables", "aa", platformName);
            }

            Debug.Log($"📂 实际构建路径: {actualBuildPath}");
            
            if (Directory.Exists(actualBuildPath))
            {
                var files = Directory.GetFiles(actualBuildPath, "*", SearchOption.AllDirectories);
                Debug.Log($"✅ 找到构建产物: {files.Length} 个文件");
                
                // 检查关键文件
                bool hasCatalog = false;
                bool hasSettings = false;
                bool hasBundles = false;
                
                foreach (var file in files)
                {
                    if (file.EndsWith("catalog.json") || file.EndsWith("catalog.bin"))
                        hasCatalog = true;
                    if (file.EndsWith("settings.json"))
                        hasSettings = true;
                    if (file.EndsWith(".bundle"))
                        hasBundles = true;
                }
                
                Debug.Log($"   - Catalog 文件: {(hasCatalog ? "✅" : "❌")}");
                Debug.Log($"   - Settings 文件: {(hasSettings ? "✅" : "❌")}");
                Debug.Log($"   - Bundle 文件: {(hasBundles ? "✅" : "❌")}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 构建路径不存在: {actualBuildPath}");
                Debug.LogWarning("   请先执行 Addressables 构建！");
            }

            // 5. 检查 StreamingAssets（构建后应该包含 Addressables 资源）
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (Directory.Exists(streamingAssetsPath))
            {
                var streamingFiles = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
                Debug.Log($"📦 StreamingAssets 文件夹: {streamingFiles.Length} 个文件");
                
                bool hasAddressablesInStreaming = false;
                foreach (var file in streamingFiles)
                {
                    if (file.Contains("catalog") || file.Contains("settings") || file.EndsWith(".bundle"))
                    {
                        hasAddressablesInStreaming = true;
                        break;
                    }
                }
                
                Debug.Log($"   - 包含 Addressables 资源: {(hasAddressablesInStreaming ? "✅" : "❌")}");
                
                if (!hasAddressablesInStreaming && buildWithPlayer)
                {
                    Debug.LogWarning("⚠️ StreamingAssets 中未找到 Addressables 资源！");
                    Debug.LogWarning("   这可能是正常的，因为构建后会自动清理。");
                }
            }

            // 6. 检查语言资源组
            Debug.Log("\n📋 语言资源组检查:");
            var groups = settings.groups;
            int localizationGroupCount = 0;
            foreach (var group in groups)
            {
                if (group != null && group.name.Contains("Localization"))
                {
                    localizationGroupCount++;
                    int entryCount = 0;
                    foreach (var entry in group.entries)
                    {
                        if (entry != null) entryCount++;
                    }
                    Debug.Log($"   - {group.name}: {entryCount} 个资源");
                }
            }
            
            if (localizationGroupCount == 0)
            {
                Debug.LogWarning("⚠️ 未找到本地化资源组！");
            }

            Debug.Log("========== 检查完成 ==========");
        }

        /// <summary>
        /// 验证构建后的 APK 是否包含 Addressables 资源（需要手动检查）
        /// </summary>
        [MenuItem("Tools/Addressables/验证构建产物")]
        public static void VerifyBuildArtifacts()
        {
            Debug.Log("========== Addressables 构建产物验证 ==========");
            
            // 检查 Player 构建数据路径
            string playerBuildDataPath = Path.Combine(Application.dataPath, "..", "Temp", "com.unity.addressables", "aa");
            string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
            string fullPath = Path.Combine(playerBuildDataPath, platformName);
            
            Debug.Log($"🔍 检查路径: {fullPath}");
            
            if (Directory.Exists(fullPath))
            {
                var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                Debug.Log($"✅ 找到 {files.Length} 个文件");
                
                // 列出关键文件
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Contains("catalog") || fileName.Contains("settings") || fileName.EndsWith(".bundle"))
                    {
                        Debug.Log($"   📄 {fileName} (大小: {new FileInfo(file).Length / 1024} KB)");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ 路径不存在: {fullPath}");
                Debug.LogWarning("   这可能是正常的，因为构建后会自动清理临时文件。");
            }
            
            Debug.Log("\n💡 验证构建后的 APK 是否包含资源的方法：");
            Debug.Log("   1. 使用 APK 解压工具（如 7-Zip）解压 APK");
            Debug.Log("   2. 检查 assets/bin/Data/StreamingAssets 文件夹");
            Debug.Log("   3. 应该包含以下文件：");
            Debug.Log("      - catalog.json 或 catalog.bin");
            Debug.Log("      - settings.json");
            Debug.Log("      - *.bundle 文件（本地资源）");
            
            Debug.Log("========== 验证完成 ==========");
        }

        /// <summary>
        /// 诊断构建后 StreamingAssets 中缺少 Addressables 资源的原因
        /// </summary>
        [MenuItem("Tools/Addressables/诊断 StreamingAssets 缺少资源原因")]
        public static void DiagnoseStreamingAssetsMissingResources()
        {
            Debug.Log("========== StreamingAssets 资源缺失诊断 ==========");
            
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("❌ 未找到 AddressableAssetSettings！请先配置 Addressables。");
                return;
            }

            string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
            bool hasIssues = false;
            List<string> issues = new List<string>();
            List<string> solutions = new List<string>();

            // 1. 检查清理设置
            // Debug.Log("\n【1】检查清理设置:");
            // bool cleanupEnabled = settings.CleanupStreamingAssetsAfterBuilds;
            // Debug.Log($"   CleanupStreamingAssetsAfterBuilds: {(cleanupEnabled ? "✅ 已启用" : "❌ 未启用")}");
            
            // if (cleanupEnabled)
            // {
            //     Debug.LogWarning("   ⚠️ 已启用自动清理！构建完成后会自动删除 StreamingAssets 中的 Addressables 资源。");
            //     Debug.LogWarning("   这是正常行为，资源会在构建过程中被复制到最终构建产物中。");
            // }

            // 2. 检查构建产物是否存在
            Debug.Log("\n【2】检查构建产物:");
            string buildPath = settings.profileSettings.GetValueByName(settings.activeProfileId, "Local.BuildPath");
            string actualBuildPath = ResolveBuildPath(buildPath, platformName);
            
            Debug.Log($"   构建路径: {actualBuildPath}");
            
            if (Directory.Exists(actualBuildPath))
            {
                var files = Directory.GetFiles(actualBuildPath, "*", SearchOption.AllDirectories);
                Debug.Log($"   ✅ 构建产物存在: {files.Length} 个文件");
                
                // 检查关键文件
                var catalogFiles = files.Where(f => f.Contains("catalog")).ToList();
                var settingsFiles = files.Where(f => f.Contains("settings.json")).ToList();
                var bundleFiles = files.Where(f => f.EndsWith(".bundle")).ToList();
                
                Debug.Log($"   - Catalog 文件: {catalogFiles.Count} 个");
                Debug.Log($"   - Settings 文件: {settingsFiles.Count} 个");
                Debug.Log($"   - Bundle 文件: {bundleFiles.Count} 个");
                
                if (catalogFiles.Count == 0)
                {
                    hasIssues = true;
                    issues.Add("构建产物中缺少 catalog 文件");
                    solutions.Add("执行 Addressables -> Build -> New Build -> Default Build Script");
                }
                
                if (settingsFiles.Count == 0)
                {
                    hasIssues = true;
                    issues.Add("构建产物中缺少 settings.json 文件");
                    solutions.Add("执行 Addressables -> Build -> New Build -> Default Build Script");
                }
                
                if (bundleFiles.Count == 0)
                {
                    hasIssues = true;
                    issues.Add("构建产物中缺少 bundle 文件");
                    solutions.Add("检查资源组配置，确认资源设置为 Local 而非 Remote");
                }
            }
            else
            {
                hasIssues = true;
                issues.Add($"构建产物路径不存在: {actualBuildPath}");
                solutions.Add("执行 Addressables -> Build -> New Build -> Default Build Script");
                Debug.LogError($"   ❌ 构建产物路径不存在: {actualBuildPath}");
            }

            // 3. 检查资源组配置（Local vs Remote）
            Debug.Log("\n【3】检查资源组配置:");
            var groups = settings.groups;
            int localGroupCount = 0;
            int remoteGroupCount = 0;
            int localizationGroupCount = 0;
            
            foreach (var group in groups)
            {
                if (group == null) continue;
                
                var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundledSchema != null)
                {
                    var buildPathId = bundledSchema.BuildPath.Id;
                    var loadPathId = bundledSchema.LoadPath.Id;
                    
                    string buildPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, buildPathId);
                    string loadPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, loadPathId);
                    
                    bool isRemote = buildPathValue.Contains("Remote") || loadPathValue.Contains("Remote");
                    
                    if (isRemote)
                    {
                        remoteGroupCount++;
                        Debug.LogWarning($"   ⚠️ {group.name}: 配置为 Remote（不会包含在构建中）");
                    }
                    else
                    {
                        localGroupCount++;
                        Debug.Log($"   ✅ {group.name}: 配置为 Local");
                    }
                    
                    if (group.name.Contains("Localization"))
                    {
                        localizationGroupCount++;
                        if (isRemote)
                        {
                            hasIssues = true;
                            issues.Add($"本地化资源组 '{group.name}' 被配置为 Remote");
                            solutions.Add($"将 '{group.name}' 的 Build & Load Paths 设置为 Local");
                        }
                    }
                }
            }
            
            Debug.Log($"   本地组: {localGroupCount} 个");
            Debug.Log($"   远程组: {remoteGroupCount} 个");
            Debug.Log($"   本地化组: {localizationGroupCount} 个");

            // 4. 检查 StreamingAssets 文件夹
            Debug.Log("\n【4】检查 StreamingAssets 文件夹:");
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            
            if (Directory.Exists(streamingAssetsPath))
            {
                var streamingFiles = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);
                Debug.Log($"   文件总数: {streamingFiles.Length} 个");
                
                // 检查 Addressables 相关文件
                var aaFiles = streamingFiles.Where(f => 
                    f.Contains("catalog") || 
                    f.Contains("settings") || 
                    f.EndsWith(".bundle") ||
                    f.Contains("aa/") ||
                    f.Contains("Addressables")).ToList();
                
                Debug.Log($"   Addressables 相关文件: {aaFiles.Count} 个");
                
                if (aaFiles.Count == 0)
                {
                    Debug.LogWarning("   ⚠️ StreamingAssets 中未找到 Addressables 资源");
                    
                    // 列出 StreamingAssets 中的所有文件（用于调试）
                    if (streamingFiles.Length > 0)
                    {
                        Debug.Log("   当前 StreamingAssets 中的文件:");
                        foreach (var file in streamingFiles.Take(10))
                        {
                            Debug.Log($"      - {Path.GetFileName(file)}");
                        }
                        if (streamingFiles.Length > 10)
                        {
                            Debug.Log($"      ... 还有 {streamingFiles.Length - 10} 个文件");
                        }
                    }
                }
                else
                {
                    Debug.Log("   ✅ 找到 Addressables 资源:");
                    foreach (var file in aaFiles.Take(5))
                    {
                        var relativePath = file.Replace(streamingAssetsPath, "").TrimStart('\\', '/');
                        Debug.Log($"      - {relativePath}");
                    }
                    if (aaFiles.Count > 5)
                    {
                        Debug.Log($"      ... 还有 {aaFiles.Count - 5} 个文件");
                    }
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ StreamingAssets 文件夹不存在");
                Debug.LogWarning("   这可能是正常的，如果从未构建过 Player");
            }

            // 5. 检查自动构建设置
            Debug.Log("\n【5】检查自动构建设置:");
            bool buildWithPlayer = !settings.BuildAddressablesWithPlayerBuild.Equals(AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer);
            Debug.Log($"   Build Addressables on Player Build: {(buildWithPlayer ? "✅ 已启用" : "❌ 未启用")}");
            
            if (!buildWithPlayer)
            {
                hasIssues = true;
                issues.Add("未启用自动构建 Addressables");
                solutions.Add("在 Addressables -> Settings -> Build -> Build Addressables on Player Build 中启用自动构建");
            }

            // 6. 检查构建器配置
            Debug.Log("\n【6】检查构建器配置:");
            int activeBuilderIndex = settings.ActivePlayerDataBuilderIndex;
            if (activeBuilderIndex >= 0 && activeBuilderIndex < settings.DataBuilders.Count)
            {
                var builder = settings.DataBuilders[activeBuilderIndex];
                Debug.Log($"   ✅ 当前构建器: {builder.GetType().Name} (索引: {activeBuilderIndex})");
                
                // 检查是否是 Packed 模式
                if (!builder.GetType().Name.Contains("Packed"))
                {
                    Debug.LogWarning("   ⚠️ 当前构建器不是 Packed 模式，可能不会生成 Bundle 文件");
                }
            }
            else
            {
                hasIssues = true;
                issues.Add($"构建器索引无效: {activeBuilderIndex}");
                solutions.Add("在 Addressables -> Settings -> Build 中设置正确的 Active Player Data Builder Index");
            }

            // 7. 总结和建议
            Debug.Log("\n========== 诊断总结 ==========");
            
            if (hasIssues)
            {
                Debug.LogError("❌ 发现以下问题:");
                for (int i = 0; i < issues.Count; i++)
                {
                    Debug.LogError($"   {i + 1}. {issues[i]}");
                }
                
                Debug.Log("\n💡 建议的解决方案:");
                for (int i = 0; i < solutions.Count; i++)
                {
                    Debug.Log($"   {i + 1}. {solutions[i]}");
                }
            }
            else
            {
                Debug.Log("✅ 未发现明显问题");
                Debug.Log("   如果构建后仍然缺少资源，请检查:");
                Debug.Log("   1. 构建日志中是否有 Addressables 相关错误");
                Debug.Log("   2. 构建后的实际产物（APK/EXE）中是否包含资源");
                Debug.Log("   3. 运行时日志中的 Addressables 初始化信息");
            }
            
            Debug.Log("\n📝 重要提示:");
            Debug.Log("   - 如果启用了 CleanupStreamingAssetsAfterBuilds，构建完成后会清理 StreamingAssets");
            Debug.Log("   - 这是正常行为，资源会在构建过程中被复制到最终构建产物中");
            Debug.Log("   - 要验证资源是否正确打包，需要检查实际的构建产物（APK/EXE）");
            Debug.Log("   - 对于 PC 端，检查构建输出文件夹中的 StreamingAssets 子文件夹");
            Debug.Log("   - 对于移动端，解压 APK/IPA 检查 assets/bin/Data/StreamingAssets 文件夹");
            
            Debug.Log("========== 诊断完成 ==========");
        }

        /// <summary>
        /// 解析构建路径
        /// </summary>
        private static string ResolveBuildPath(string buildPath, string platformName)
        {
            string actualPath = buildPath;
            actualPath = actualPath.Replace("[BuildTarget]", platformName);
            actualPath = actualPath.Replace("[UnityEngine.AddressableAssets.Addressables.BuildPath]", 
                Path.Combine(Application.dataPath, "..", "Library", "com.unity.addressables"));
            
            // 如果还有未解析的变量，使用默认路径
            if (actualPath.Contains("["))
            {
                actualPath = Path.Combine(Application.dataPath, "..", "Library", "com.unity.addressables", "aa", platformName);
            }
            
            return actualPath;
        }
    }
}

