# Changelog

所有版本的重要更改都将记录在本文件中。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

---

## [1.1.25] - 2026-03-02

### Changes
- 添加 CHANGELOG.md

---

## [1.1.24] - 2026-03-02

### Changes
- d0ca272 refactor: migrate translateApiKey to EditorPrefs to prevent leaking secrets via version control
- 827a8c6 refactor: 将 OnLoadOver 重命名为 OnDataLoadOver
- 3bb973e feat: 添加本地化数据加载状态 IsDataLoaded 及 OnLoadOver 事件
- 7a3b13c feat: 添加 textAnimSpeed 配置及编辑器支持
- 29575bd 删除回收并导出的设计
- 0e3b0bc Update LocalizationDataUtils.cs
- a8729ad 让变动记录写入tips1中
- f18d31d 删除语言码强制小写的设定
- b70c035 导出excel时，为不同变动状态标记颜色
- 3413bb4 Delete AutoTranslate.meta
- bcc4660 完善报错提示
- 4b40e01 修复路径报错
- c615a4e 修复文件不存在报错
- 94782da Update LocalizeText.cs
- 414b602 LocalizeData添加有效性属性
- 15a6c610 修复显示错误
- c1fb37b 删除LocalizationDataModule设计
- eda2d88 重构编辑器代码，修复ab包必须设置为default的bug
- f1c0eb6 增加错误提示
- 17b5977 跳过全部为空的localizeData

---

[1.1.25]: https://github.com/U-NV/Localization/releases/tag/v1.1.25
[1.1.24]: https://github.com/U-NV/Localization/releases/tag/v1.1.24
