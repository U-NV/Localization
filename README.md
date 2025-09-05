# U0UGames Localization System

一个功能完整的Unity本地化系统，支持AssetBundle、动态模块加载和多语言管理。

## ✨ 特性

- 🌍 **多语言支持** - 支持简体中文、繁体中文、日文、英文等多种语言
- 📦 **AssetBundle集成** - 基于AssetBundle的本地化资源管理
- 🔄 **动态模块加载** - 支持运行时动态加载和卸载本地化模块
- 🎯 **智能缓存** - 自动管理模块缓存，优化内存使用
- 🎨 **UI组件支持** - 内置Text、Image、Sprite等UI组件的本地化支持
- ⚡ **高性能** - 优化的数据结构和查找算法
- 🛠️ **编辑器工具** - 完整的编辑器工具链，支持Excel数据导入和配置管理
- 🤖 **AI智能翻译** - 集成AI翻译API，支持批量自动翻译，提高本地化效率

## 📦 安装

### 通过Git URL安装

1. 打开Unity Package Manager
2. 点击左上角的"+"按钮
3. 选择"Add package from git URL"
4. 输入以下URL：
   ```
   https://github.com/U-NV/Localization.git
   ```

### 通过本地包安装

1. 下载或克隆此仓库
2. 在Unity Package Manager中选择"Add package from disk"
3. 选择项目中的`package.json`文件

## 🚀 快速开始

### 1. 基本设置

首先创建本地化配置文件：

```csharp
// 在Editor中创建配置文件
var config = LocalizationConfig.GetOrCreateLocalizationConfig();
```

### 2. 获取本地化文本

```csharp
// 获取简单文本
string text = LocalizationManager.GetText("UI.MainMenu.StartButton");

// 获取带参数的文本
string welcomeText = LocalizationManager.GetTextWithArg("UI.Welcome", playerName, level);
```

### 3. 切换语言

```csharp
// 切换到简体中文
LocalizationManager.SwitchLanguage("zh-cn");

// 切换到英文
LocalizationManager.SwitchLanguage("en");
```

### 4. 获取本地化资源

```csharp
// 获取Sprite
Sprite icon = LocalizationManager.GetSprite("Icons.Player");

// 获取其他Unity对象
Texture2D texture = LocalizationManager.GetObject<Texture2D>("Textures.Background");
```

## 🎨 UI组件使用

### LocalizeText组件

```csharp
// 在Inspector中设置Key，或通过代码设置
localizeTextComponent.SetKey("UI.MainMenu.Title");
```

### LocalizeImage组件

```csharp
// 设置本地化图片
localizeImageComponent.SetKey("Images.Logo");
```

## 📁 项目结构

```
Assets/U0UGames/Localization/
├── Runtime/
│   ├── LocalizationManager.cs          # 核心管理器
│   ├── LocalizationConfig.cs           # 配置文件
│   ├── LocalizationDataModule.cs       # 数据模块
│   ├── LocalizationDataModuleManager.cs # 模块管理器
│   └── UI/                             # UI组件
│       ├── LocalizeText.cs
│       ├── LocalizeImage.cs
│       └── LocalizeSprite.cs
└── Editor/                             # 编辑器工具
    ├── LocalizationEditorWindow.cs
    ├── LocalizationConfigEditorWindow.cs
    └── LocalizationDataUtils.cs
```

## ⚙️ 配置说明

### 语言代码

系统支持以下语言代码：

- `zh-cn` - 简体中文
- `zh-CHT` - 繁体中文  
- `ja` - 日文
- `en` - 英文

### 数据格式

本地化数据使用JSON格式存储，支持嵌套结构：

```json
{
  "UI.MainMenu.Title": "主菜单",
  "UI.MainMenu.StartButton": "开始游戏",
  "UI.Settings.Audio": "音频设置"
}
```

### AssetBundle结构

```
StreamingAssets/
└── Localization/
    ├── zh-cn/          # 简体中文资源包
    │   ├── UI.json
    │   └── Game.json
    ├── en/             # 英文资源包
    │   ├── UI.json
    │   └── Game.json
    └── ja/             # 日文资源包
        ├── UI.json
        └── Game.json
```

## 🛠️ 编辑器工具

### 本地化编辑器窗口

通过菜单 `工具/本地化` 打开本地化编辑器窗口，该窗口包含四个功能标签页：

#### 配置标签页
- 管理支持的语言列表
- 配置默认模块
- 设置AssetBundle路径
- 管理Excel数据导入

#### 生成标签页
- 创建新的本地化文件
- 管理本地化数据结构

#### 翻译标签页
- 配置AI翻译API（支持DeepSeek等主流AI服务）
- 批量翻译文本到目标语言
- 自定义翻译提示词，优化翻译质量
- 支持一键翻译所有语言

#### 辅助工具标签页
- 从Excel导入本地化数据
- 生成AssetBundle
- 验证数据完整性

## 📝 API参考

### LocalizationManager

#### 静态方法

- `GetText(string textKey)` - 获取本地化文本
- `GetTextWithArg(string textKey, params object[] args)` - 获取带参数的本地化文本
- `GetSprite(string textKey)` - 获取本地化Sprite
- `GetObject<T>(string textKey)` - 获取本地化Unity对象
- `SwitchLanguage(string languageCode, List<string> textModules = null)` - 切换语言
- `GetRecommendLanguageCode()` - 获取推荐语言代码

#### 属性

- `Config` - 本地化配置
- `CurrLanguageCode` - 当前语言代码

### 事件系统

```csharp
// 监听语言切换事件
EventManager.AddListener<LocalizeLanguageChangeEvent>(OnLanguageChanged);

private void OnLanguageChanged(LocalizeLanguageChangeEvent evt)
{
    // 处理语言切换逻辑
}
```

## 🤖 AI智能翻译

### 配置AI翻译服务

1. **获取API密钥**
   - 注册并获取AI服务API密钥（推荐使用DeepSeek、OpenAI等）
   - 确保账户有足够的API调用额度

2. **配置API设置**
   ```csharp
   // 在LocalizationConfig中配置
   config.translateApiKey = "your-api-key-here";
   config.translateApiUrl = "https://api.deepseek.com"; // 或其他AI服务URL
   ```

3. **自定义翻译提示词**
   ```csharp
   // 可以自定义AI翻译的提示词，优化翻译质量
   config.translateAIPrompt = "请保持游戏文本的简洁性和可读性，确保翻译准确自然。";
   ```

### 使用AI翻译功能

1. **打开翻译窗口**
   - 菜单：`工具/本地化`，然后切换到"翻译"标签页

2. **选择翻译目标语言**
   - 从下拉列表中选择要翻译到的目标语言
   - 系统会自动从原始语言翻译到目标语言

3. **执行翻译**
   - **单语言翻译**：点击"翻译"按钮翻译到选定的语言
   - **批量翻译**：点击"翻译所有语言"按钮，一次性翻译到所有配置的语言

### 翻译质量优化

1. **提示词优化**
   - 根据项目特点自定义翻译提示词
   - 可以指定游戏类型、风格要求等

2. **分批翻译**
   - 对于大量文本，建议分批进行翻译
   - 系统支持重试机制，网络异常时会自动重试

3. **翻译结果验证**
   - 翻译完成后建议人工检查关键文本
   - 可以针对特定文本进行二次翻译优化

### 支持的AI服务

- **DeepSeek** - 推荐使用，性价比高
- **OpenAI GPT** - 翻译质量优秀
- **其他兼容OpenAI API格式的服务**

### 注意事项

- 确保网络连接稳定，翻译过程需要网络请求
- API调用可能产生费用，请注意使用量
- 翻译质量取决于AI服务的能力和提示词设置
- 建议对重要文本进行人工校对

## 🔧 高级用法

### 动态模块加载

```csharp
// 动态加载特定模块
var module = DataModuleManager.TryLoadDataModule("NewModule");

// 卸载不需要的模块
DataModuleManager.UnloadDataModule("OldModule");
```

### 自定义语言代码转换

系统内置了常用语言代码的转换，支持：

- `zh` → `zh-cn`
- `zh-hans` → `zh-cn`
- `zh-hant` → `zh-CHT`
- `ja-jp` → `ja`

## 🐛 故障排除

### 常见问题

1. **找不到本地化文本**
   - 检查Key是否正确
   - 确认对应模块已加载
   - 验证AssetBundle路径

2. **AssetBundle加载失败**
   - 检查StreamingAssets路径
   - 确认AssetBundle文件存在
   - 验证文件权限

3. **编辑器工具无法使用**
   - 确认在Editor文件夹中
   - 检查依赖项是否完整

4. **AI翻译功能问题**
   - 检查API密钥是否正确配置
   - 确认API URL格式正确（如：https://api.deepseek.com）
   - 验证网络连接和API服务状态
   - 检查API账户余额和调用限制
   - 查看Unity Console中的详细错误信息

## 📄 许可证

本项目采用MIT许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🤝 贡献

欢迎提交Issue和Pull Request来改进这个项目。

## 📞 支持

如果您遇到问题或有任何建议，请：

1. 查看[常见问题](#故障排除)
2. 提交[Issue](https://github.com/U-NV/Localization/issues)
3. 联系支持邮箱：support@u0ugames.com

## 📈 更新日志

### v1.1.0
- ✨ 新增AI智能翻译功能
- 🤖 支持DeepSeek、OpenAI等主流AI服务
- 🔄 批量翻译和一键翻译所有语言
- ⚙️ 自定义翻译提示词优化
- 🛠️ 完善的错误处理和重试机制
- 📝 更新文档和API说明

### v1.0.0
- 初始版本发布
- 支持多语言本地化
- AssetBundle集成
- 动态模块加载
- 完整的编辑器工具链
