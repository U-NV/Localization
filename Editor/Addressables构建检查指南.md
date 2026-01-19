# Addressables 构建检查指南

## 如何确认构建时包含了 Addressables 资源

### 方法一：使用检查工具（推荐）

#### 基础检查
1. 在 Unity 编辑器中，点击菜单：`Tools -> Addressables -> 检查构建配置`
2. 查看 Console 输出的检查结果
3. 如果发现问题，按照提示修复

#### 深度诊断（推荐用于排查 StreamingAssets 缺少资源问题）
1. 在 Unity 编辑器中，点击菜单：`Tools -> Addressables -> 诊断 StreamingAssets 缺少资源原因`
2. 工具会自动检查以下内容：
   - ✅ 清理设置（CleanupStreamingAssetsAfterBuilds）
   - ✅ 构建产物是否存在
   - ✅ 资源组配置（Local vs Remote）
   - ✅ StreamingAssets 文件夹内容
   - ✅ 自动构建设置
   - ✅ 构建器配置
3. 查看详细的诊断报告和解决方案建议

### 方法二：手动检查步骤

#### 1. 检查 Addressables 配置

**打开 Addressables 窗口：**
- `Window -> Asset Management -> Addressables -> Groups`

**检查以下设置：**

- **Build Addressables on Player Build**
  - 位置：`Addressables -> Settings -> Build -> Build Addressables on Player Build`
  - 如果启用：构建 Player 时会自动构建 Addressables
  - 如果未启用：需要手动构建（见步骤 2）

- **Active Player Data Builder**
  - 位置：`Addressables -> Settings -> Build -> Active Player Data Builder Index`
  - 应该选择 `BuildScriptPackedMode`（索引通常是 3）
  - 这是生成实际 Bundle 文件的构建器

#### 2. 执行 Addressables 构建

**如果未启用自动构建，需要手动执行：**

1. 打开 `Addressables -> Groups` 窗口
2. 点击 `Build -> New Build -> Default Build Script`
3. 等待构建完成
4. 检查 Console 是否有错误

**构建产物位置：**
- `Library/com.unity.addressables/aa/[Platform]/`
- 应该包含：
  - `catalog.json` 或 `catalog.bin`
  - `settings.json`
  - `*.bundle` 文件

#### 3. 检查构建路径配置

**检查 Profile 设置：**
- `Addressables -> Settings -> Profiles`
- 确认 `Local.BuildPath` 和 `Local.LoadPath` 配置正确

**默认路径：**
- Build Path: `[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]`
- Load Path: `{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]`

#### 4. 验证构建后的 APK

**方法 A：检查 StreamingAssets（构建时）**

在构建过程中，Addressables 资源会被复制到：
- `Assets/StreamingAssets/aa/[Platform]/`

**注意：** 构建完成后，这些文件会被自动清理（如果启用了 `CleanupStreamingAssetsAfterBuilds`）

**方法 B：解压 APK 检查（构建后）**

1. 使用 APK 解压工具（如 7-Zip、WinRAR）
2. 解压构建好的 APK 文件
3. 检查路径：`assets/bin/Data/StreamingAssets/aa/[Platform]/`
4. 应该包含：
   - ✅ `catalog.json` 或 `catalog.bin`
   - ✅ `settings.json`
   - ✅ `*.bundle` 文件（本地资源）

**如果缺少这些文件，说明资源未正确打包！**

#### 5. 运行时验证

在代码中添加日志，检查资源是否可访问：

```csharp
// 在 PreloadLanguageByLabel 方法中已添加诊断功能
// 运行安卓包后，查看日志：
// - 如果 Label 存在，会显示 "找到 Label 'zh-cn' 对应的 X 个资源位置"
// - 如果 Label 不存在，会显示详细的错误信息和检查步骤
```

### 常见问题排查

#### 问题 1：构建时找不到 Label

**原因：**
- Label 名称不匹配（大小写敏感）
- 资源未正确设置 Label
- Addressables 未正确构建

**解决方法：**
1. 检查 Addressables Groups 窗口，确认每个资源的 Label 设置
2. 确认 Label 名称与代码中的 `langCode` 完全一致（小写）
3. 重新执行 Addressables 构建

#### 问题 2：PC 正常，安卓失败

**原因：**
- 编辑器模式使用虚拟模式，不依赖实际 Bundle
- 安卓运行时需要真实的 Bundle 文件

**解决方法：**
1. 确认 `ActivePlayerDataBuilderIndex` 设置为 `BuildScriptPackedMode`
2. 执行完整的 Addressables 构建
3. 确认构建产物存在于 `Library/com.unity.addressables/aa/Android/`

#### 问题 3：构建后 StreamingAssets 中缺少资源

**原因：**
- 资源被标记为 Remote 而非 Local
- 构建路径配置错误
- 构建时出错但未发现
- 启用了 `CleanupStreamingAssetsAfterBuilds`（这是正常行为）
- 未执行 Addressables 构建

**解决方法：**
1. **使用诊断工具**：`Tools -> Addressables -> 诊断 StreamingAssets 缺少资源原因`
2. 检查资源组的 `Build & Load Paths` 设置，确认资源设置为 `Local` 而非 `Remote`
3. 检查构建日志，确认没有错误
4. 确认已执行 Addressables 构建（如果未启用自动构建）
5. **重要**：如果启用了 `CleanupStreamingAssetsAfterBuilds`，构建完成后会清理 StreamingAssets，这是正常行为
   - 资源会在构建过程中被复制到最终构建产物中
   - 要验证资源是否正确打包，需要检查实际的构建产物（APK/EXE）
   - 对于 PC 端：检查构建输出文件夹中的 `StreamingAssets` 子文件夹
   - 对于移动端：解压 APK/IPA 检查 `assets/bin/Data/StreamingAssets` 文件夹

### 检查清单

在构建安卓包前，确认以下项目：

- [ ] Addressables 配置已正确设置
- [ ] 所有语言资源已添加到 Addressables Groups
- [ ] 每个资源的 Label 已正确设置（小写，如 `zh-cn`）
- [ ] 已执行 Addressables 构建（如果未启用自动构建）
- [ ] 构建产物存在于 `Library/com.unity.addressables/aa/Android/`
- [ ] 构建产物包含 `catalog.json`、`settings.json` 和 `.bundle` 文件
- [ ] 构建 Player 时没有 Addressables 相关错误

### 快速验证命令

在 Unity Console 中运行检查工具：
```
Tools -> Addressables -> 检查构建配置          # 基础检查
Tools -> Addressables -> 诊断 StreamingAssets 缺少资源原因  # 深度诊断（推荐）
Tools -> Addressables -> 验证构建产物          # 验证构建产物
```

### 如何确认构建后 StreamingAssets 中缺少资源的原因

#### 步骤 1：运行诊断工具
1. 点击菜单：`Tools -> Addressables -> 诊断 StreamingAssets 缺少资源原因`
2. 查看 Console 输出的详细诊断报告

#### 步骤 2：检查诊断结果
诊断工具会检查以下关键点：

1. **清理设置**
   - 如果 `CleanupStreamingAssetsAfterBuilds` 已启用，构建完成后会自动清理
   - 这是正常行为，资源会在构建过程中被复制到最终构建产物中

2. **构建产物**
   - 检查 `Library/com.unity.addressables/aa/[Platform]/` 是否存在
   - 检查是否包含 `catalog.json`、`settings.json` 和 `.bundle` 文件

3. **资源组配置**
   - 检查资源组是否配置为 `Local` 而非 `Remote`
   - Remote 资源不会包含在构建中

4. **StreamingAssets 文件夹**
   - 列出当前 StreamingAssets 中的所有文件
   - 检查是否包含 Addressables 相关文件

5. **自动构建设置**
   - 检查是否启用了自动构建
   - 如果未启用，需要手动执行构建

#### 步骤 3：根据诊断结果采取行动
- 如果发现问题，按照工具提供的解决方案逐一修复
- 如果未发现问题，检查实际的构建产物（APK/EXE）而不是编辑器中的 StreamingAssets

### 相关文件位置

- Addressables 配置：`Assets/AddressableAssetsData/AddressableAssetSettings.asset`
- 构建产物：`Library/com.unity.addressables/aa/[Platform]/`
- 运行时路径：`StreamingAssets/aa/[Platform]/`（构建时临时）
- 检查工具：`Assets/U0UGames/Localization/Editor/AddressablesBuildChecker.cs`


