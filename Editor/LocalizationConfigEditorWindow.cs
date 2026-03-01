using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public class LocalizationConfigEditorWindow
    {
        private static class EditorPrefsKey
        {
            public const string GenerateConfigFoldout  = "GenerateConfigFoldout";
            public const string LanguageCodeFoldout    = "LanguageCodeFoldout";
            public const string LanguageCodeIndex      = "LanguageCodeIndex";
            public const string LanguageAliasFoldout   = "LanguageAliasFoldout";
        }

        private const float LabelWidth    = 110f;
        private const float SmallBtnWidth = 24f;

        private LocalizationConfig _localizationConfig;
        private bool _isGenerateConfigPanelFoldout;
        private bool _isLanguageCodePanelFoldout;
        private bool _isLanguageAliasPanelFoldout;
        private int  _languageCodeIndex;

        public void Init()
        {
            if (!_localizationConfig)
            {
                _localizationConfig = LocalizationConfig.GetOrCreateLocalizationConfig();
            }

            _isGenerateConfigPanelFoldout = EditorPrefs.GetBool(EditorPrefsKey.GenerateConfigFoldout);
            _isLanguageCodePanelFoldout   = EditorPrefs.GetBool(EditorPrefsKey.LanguageCodeFoldout);
            _isLanguageAliasPanelFoldout  = EditorPrefs.GetBool(EditorPrefsKey.LanguageAliasFoldout);
            _languageCodeIndex            = EditorPrefs.GetInt(EditorPrefsKey.LanguageCodeIndex);
        }

        // ── 数据路径 & 源语言 ──────────────────────────────────────────────
        private void ShowDataPathSection()
        {
            SectionHeader("数据路径");

            // 源数据文件夹
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("源数据文件夹", GUILayout.Width(LabelWidth));
            var selectedRaw = LocalizationDataUtils.SelectFolderBtn(_localizationConfig.excelDataFolderRootPath, "选择数据文件夹");
            if (!string.IsNullOrEmpty(selectedRaw))
            {
                _localizationConfig.excelDataFolderRootPath = UnityPathUtility.FullPathToRootFolderPath(selectedRaw);
            }
            EditorGUILayout.EndHorizontal();

            // 翻译输出文件夹
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("翻译表格输出文件夹", GUILayout.Width(LabelWidth));
            var selectedTranslate = LocalizationDataUtils.SelectFolderBtn(_localizationConfig.translateDataFolderRootPath, "选择数据文件夹");
            if (!string.IsNullOrEmpty(selectedTranslate))
            {
                _localizationConfig.translateDataFolderRootPath = UnityPathUtility.FullPathToRootFolderPath(selectedTranslate);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // 源语言选择
            if (!_localizationConfig.IsValid())
            {
                EditorGUILayout.HelpBox("尚未配置任何语言，请先在「语言列表」中添加至少一条语言配置。", MessageType.Warning);
                return;
            }

            var codeOptions = BuildLanguageCodeArray();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("源语言（原始数据）", GUILayout.Width(LabelWidth));
            var idx = EditorGUILayout.Popup(_localizationConfig.originalLanguageCodeIndex, codeOptions, GUILayout.Width(90));
            if (idx >= 0)
            {
                _localizationConfig.originalLanguageCodeIndex = idx;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── 语言列表配置 ───────────────────────────────────────────────────
        private Vector2 _generateConfigScrollPos;
        private void ShowGenerateConfigView()
        {
            _isGenerateConfigPanelFoldout = EditorGUILayout.Foldout(_isGenerateConfigPanelFoldout, "语言列表配置", true);
            EditorPrefs.SetBool(EditorPrefsKey.GenerateConfigFoldout, _isGenerateConfigPanelFoldout);

            if (!_isGenerateConfigPanelFoldout)
                return;

            var configList = _localizationConfig.languageDisplayDataList;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (configList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无语言配置，点击下方「+」按钮添加。", EditorStyles.miniLabel);
            }
            else
            {
                _generateConfigScrollPos = EditorGUILayout.BeginScrollView(_generateConfigScrollPos,
                    GUILayout.MaxHeight(180));

                for (int i = 0; i < configList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // 序号
                    EditorGUILayout.LabelField($"#{i + 1}", EditorStyles.boldLabel, GUILayout.Width(26));

                    // 语言代码（只读）
                    EditorGUILayout.LabelField("语言代码", GUILayout.Width(56));
                    GUI.enabled = false;
                    EditorGUILayout.TextField(configList[i].languageCode, GUILayout.Width(72));
                    GUI.enabled = true;

                    GUILayout.Space(4);

                    // 显示名称（可编辑）
                    EditorGUILayout.LabelField("显示名称", GUILayout.Width(56));
                    configList[i].displayName = EditorGUILayout.TextField(configList[i].displayName);

                    GUILayout.Space(4);

                    // 恢复默认显示名称
                    var canReset = TryGetCulture(configList[i].languageCode, out var resetCulture);
                    GUI.enabled = canReset;
                    if (GUILayout.Button(new GUIContent("↺", "恢复默认显示名称"), GUILayout.Width(24)))
                    {
                        configList[i].displayName = resetCulture.NativeName;
                    }
                    GUI.enabled = true;

                    // 文字动画速度（可编辑）
                    EditorGUILayout.LabelField("文字动画速度", GUILayout.Width(80));
                    configList[i].textAnimSpeed = EditorGUILayout.FloatField(configList[i].textAnimSpeed, GUILayout.Width(80));

                    // 选择语言弹窗
                    if (GUILayout.Button("选择…", GUILayout.Width(46)))
                    {
                        var capturedIndex = i;
                        var btnRect = GUILayoutUtility.GetLastRect();
                        PopupWindow.Show(btnRect, new LanguagePickerPopup(culture =>
                        {
                            configList[capturedIndex].languageCode = culture.Name;
                            configList[capturedIndex].displayName  = culture.NativeName;
                        }));
                    }

                    // 删除
                    bool removed = false;
                    if (GUILayout.Button("−", GUILayout.Width(SmallBtnWidth)))
                    {
                        configList.RemoveAt(i);
                        removed = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (removed) break;
                }

                EditorGUILayout.EndScrollView();
            }

            if (GUILayout.Button("＋  添加语言"))
            {
                configList.Add(new LocalizationConfig.LanguageConfig());
            }

            EditorGUILayout.EndVertical();
        }

        // ── 游戏内启用的语言 ───────────────────────────────────────────────
        private void ShowLanguageCodeView()
        {
            _isLanguageCodePanelFoldout = EditorGUILayout.Foldout(_isLanguageCodePanelFoldout, "游戏内启用的语言", true);
            EditorPrefs.SetBool(EditorPrefsKey.LanguageCodeFoldout, _isLanguageCodePanelFoldout);

            if (!_isLanguageCodePanelFoldout)
                return;

            var languageCodeList   = _localizationConfig.inGameLanguageCodeList;
            var enableLanguageCodes = BuildLanguageCodeArray();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 当前已启用列表
            if (languageCodeList == null || languageCodeList.Count == 0)
            {
                EditorGUILayout.LabelField("尚未启用任何语言，请从下方下拉框中选择并点击「+」。", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < languageCodeList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                    GUI.enabled = false;
                    EditorGUILayout.TextField(languageCodeList[i]);
                    GUI.enabled = true;

                    bool removed = false;
                    if (GUILayout.Button("−", GUILayout.Width(SmallBtnWidth)))
                    {
                        languageCodeList.RemoveAt(i);
                        removed = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (removed) break;
                }
            }

            GUILayout.Space(2);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // 分割线

            // 添加行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("选择语言", GUILayout.Width(LabelWidth - 40));

            if (enableLanguageCodes.Length == 0)
            {
                EditorGUILayout.LabelField("（请先在「语言列表」中配置语言）", EditorStyles.miniLabel);
            }
            else
            {
                _languageCodeIndex = EditorGUILayout.Popup(_languageCodeIndex, enableLanguageCodes);
                if (_languageCodeIndex >= 0)
                {
                    EditorPrefs.SetInt(EditorPrefsKey.LanguageCodeIndex, _languageCodeIndex);
                }

                if (GUILayout.Button("＋", GUILayout.Width(SmallBtnWidth + 8)))
                {
                    if (_languageCodeIndex >= 0 && _languageCodeIndex < enableLanguageCodes.Length)
                    {
                        var newCode = enableLanguageCodes[_languageCodeIndex];
                        if (!string.IsNullOrEmpty(newCode) && !languageCodeList.Contains(newCode))
                        {
                            languageCodeList.Add(newCode);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── 语言码别名映射 ─────────────────────────────────────────────────
        private Vector2 _aliasScrollPos;
        private void ShowLanguageCodeAliasView()
        {
            _isLanguageAliasPanelFoldout = EditorGUILayout.Foldout(_isLanguageAliasPanelFoldout, "语言码别名映射", true);
            EditorPrefs.SetBool(EditorPrefsKey.LanguageAliasFoldout, _isLanguageAliasPanelFoldout);

            if (!_isLanguageAliasPanelFoldout)
                return;

            var aliasList = _localizationConfig.languageCodeAliasList;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (aliasList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无别名映射，点击下方「+」按钮添加。", EditorStyles.miniLabel);
            }
            else
            {
                _aliasScrollPos = EditorGUILayout.BeginScrollView(_aliasScrollPos, GUILayout.MaxHeight(160));

                for (int i = 0; i < aliasList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    // fromCode — 只读显示 + 弹窗选择
                    EditorGUILayout.LabelField("别名", GUILayout.Width(28));
                    GUI.enabled = false;
                    EditorGUILayout.TextField(aliasList[i].fromCode ?? "", GUILayout.Width(80));
                    GUI.enabled = true;
                    if (GUILayout.Button("选择…", GUILayout.Width(46)))
                    {
                        var ci = i;
                        var btnRect = GUILayoutUtility.GetLastRect();
                        PopupWindow.Show(btnRect, new LanguagePickerPopup(culture =>
                        {
                            aliasList[ci].fromCode = culture.Name;
                        }));
                    }

                    EditorGUILayout.LabelField("→", GUILayout.Width(16));

                    // toCode — 只读显示 + 弹窗选择
                    EditorGUILayout.LabelField("映射到", GUILayout.Width(40));
                    GUI.enabled = false;
                    EditorGUILayout.TextField(aliasList[i].toCode ?? "", GUILayout.Width(80));
                    GUI.enabled = true;
                    if (GUILayout.Button("选择…", GUILayout.Width(46)))
                    {
                        var ci = i;
                        var btnRect = GUILayoutUtility.GetLastRect();
                        PopupWindow.Show(btnRect, new LanguagePickerPopup(culture =>
                        {
                            aliasList[ci].toCode = culture.Name;
                        }));
                    }

                    // 删除
                    bool removed = false;
                    if (GUILayout.Button("−", GUILayout.Width(SmallBtnWidth)))
                    {
                        aliasList.RemoveAt(i);
                        removed = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (removed) break;
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("＋  添加映射"))
            {
                aliasList.Add(new LocalizationConfig.LanguageCodeAlias
                {
                    fromCode = "",
                    toCode   = ""
                });
            }

            if (GUILayout.Button(new GUIContent("↺  重置为默认", "清空当前列表并恢复内置默认别名映射")))
            {
                if (EditorUtility.DisplayDialog(
                    "重置别名映射",
                    "将清空当前所有别名映射并恢复为内置默认值，是否继续？",
                    "确认重置", "取消"))
                {
                    aliasList.Clear();
                    aliasList.AddRange(LocalizationConfig.GetDefaultLanguageCodeAliasList());
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── 名词术语表 ─────────────────────────────────────────────────────
        private void ShowGlossaryView()
        {
            SectionHeader("翻译术语表");

            var tempGlossary = (LocalizationGlossary)EditorGUILayout.ObjectField(
                "术语表资源", _localizationConfig.Glossary, typeof(LocalizationGlossary), false);

            if (tempGlossary != null && tempGlossary != _localizationConfig.Glossary)
            {
                _localizationConfig.SetGlossary(tempGlossary);
                EditorUtility.SetDirty(_localizationConfig);
                AssetDatabase.SaveAssetIfDirty(_localizationConfig);
            }

            if (_localizationConfig.Glossary == null)
            {
                EditorGUILayout.HelpBox("尚未关联术语表，可创建新文件或直接拖入已有资源。", MessageType.Info);

                if (GUILayout.Button("新建术语表…"))
                {
                    var glossaryPath = EditorUtility.SaveFilePanelInProject(
                        "新建术语表",
                        "Glossary",
                        "asset",
                        "请选择术语表文件的保存位置");

                    if (!string.IsNullOrEmpty(glossaryPath))
                    {
                        var glossary = ScriptableObject.CreateInstance<LocalizationGlossary>();
                        AssetDatabase.CreateAsset(glossary, glossaryPath);
                        _localizationConfig.SetGlossary(glossary);

                        EditorUtility.SetDirty(glossary);
                        EditorUtility.SetDirty(_localizationConfig);
                        AssetDatabase.SaveAssetIfDirty(glossary);
                        AssetDatabase.SaveAssetIfDirty(_localizationConfig);
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        // ── 主绘制入口 ─────────────────────────────────────────────────────
        public void OnGUI()
        {
            GUILayout.Space(4);

            DrawPanel(ShowDataPathSection);

            GUILayout.Space(6);

            DrawPanel(ShowGenerateConfigView);

            GUILayout.Space(6);

            DrawPanel(ShowLanguageCodeView);

            GUILayout.Space(6);

            DrawPanel(ShowLanguageCodeAliasView);

            GUILayout.Space(6);

            DrawPanel(ShowGlossaryView);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_localizationConfig);
                AssetDatabase.SaveAssetIfDirty(_localizationConfig);
            }
        }

        // ── 工具方法 ───────────────────────────────────────────────────────
        private static void DrawPanel(System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            drawContent();
            GUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        private static void SectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetLastRect();
            rect.y += rect.height + 1;
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            GUILayout.Space(4);
        }

        private string[] BuildLanguageCodeArray()
        {
            var list = _localizationConfig.languageDisplayDataList;
            var result = new List<string>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if(string.IsNullOrEmpty(list[i].languageCode))
                {
                    continue;
                }
                result.Add(list[i].languageCode);
            }
            return result.ToArray();
        }

        private static bool TryGetCulture(string languageCode, out CultureInfo culture)
        {
            culture = null;
            if (string.IsNullOrEmpty(languageCode)) return false;
            try
            {
                culture = new CultureInfo(languageCode);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
