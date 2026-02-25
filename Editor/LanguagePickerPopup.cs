using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public class LanguagePickerPopup : PopupWindowContent
    {
        private static CultureInfo[] _allCultures;

        private readonly Action<CultureInfo> _onSelected;
        private string   _search = "";
        private Vector2  _scroll;
        private CultureInfo[] _filtered;

        public LanguagePickerPopup(Action<CultureInfo> onSelected)
        {
            _onSelected = onSelected;

            if (_allCultures == null)
            {
                // NeutralCultures 含中性语言码（zh-Hant、zh-Hans、ja 等）
                // SpecificCultures 含国别特定码（zh-TW、zh-CN、ja-JP 等）
                // 过滤掉 Name 为空的不变量文化（Invariant Culture）
                _allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)
                    .Where(c => !string.IsNullOrEmpty(c.Name))
                    .OrderBy(c => c.Name)
                    .ToArray();
            }

            ApplyFilter("");
        }

        public override Vector2 GetWindowSize() => new Vector2(340, 320);

        public override void OnGUI(Rect rect)
        {
            // 搜索框
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("搜索", GUILayout.Width(36));
            var newSearch = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (newSearch != _search)
            {
                _search = newSearch;
                ApplyFilter(_search);
                _scroll = Vector2.zero;
            }
            EditorGUILayout.EndHorizontal();

            // 列表
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var culture in _filtered)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(culture.Name, GUILayout.Width(70));
                EditorGUILayout.LabelField(culture.NativeName);
                EditorGUILayout.EndHorizontal();

                var rowRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    _onSelected?.Invoke(culture);
                    editorWindow?.Close();
                    Event.current.Use();
                }

                // 悬停高亮
                if (Event.current.type == EventType.Repaint && rowRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.06f));
                    editorWindow?.Repaint();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void ApplyFilter(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                _filtered = _allCultures;
                return;
            }

            var lower = keyword.ToLowerInvariant();
            _filtered = _allCultures
                .Where(c => c.Name.ToLowerInvariant().Contains(lower)
                         || c.NativeName.ToLowerInvariant().Contains(lower)
                         || c.DisplayName.ToLowerInvariant().Contains(lower))
                .ToArray();
        }
    }
}
