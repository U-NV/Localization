using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    public class LocalizationEditorWindow : EditorWindow
    {
        private static class EditorPrefsKey
        {
            public const string ToolBarModeIndex = "ToolBarModeIndex";
        }

        private LocalizationConfigEditorWindow _configEditorWindow = new LocalizationConfigEditorWindow();
        private LocalizationNewFileGenerateWindow _fileGenerateWindow = new LocalizationNewFileGenerateWindow();
        private LocalizationDataProcessWindow _toolsWindow = new LocalizationDataProcessWindow();

        
        [MenuItem("工具/本地化")]
        static void Open()
        {
            LocalizationEditorWindow window = EditorWindow.GetWindow<LocalizationEditorWindow>("本地化");
            window.minSize = new Vector2(100, 100);
        }

        private void Init()
        {
            _mode = EditorPrefs.GetInt(EditorPrefsKey.ToolBarModeIndex);

        }
        private void OnEnable()
        {
            Init();
            _fileGenerateWindow.Init();
            _configEditorWindow.Init();
            _toolsWindow.Init();
        }
        
        private int _mode = 0;
        private string[] toolBarOption = new string[]
        {
            "配置","生成","辅助工具"
        };
        void OnGUI()
        {
            _mode = GUILayout.Toolbar(_mode,toolBarOption);
            EditorPrefs.SetInt(EditorPrefsKey.ToolBarModeIndex, _mode);

            EditorGUILayout.BeginVertical();
            switch (_mode)
            {
                case 0:
                    _configEditorWindow.OnGUI();
                    break;
                case 1:
                    _fileGenerateWindow.OnGUI();
                    break;
                case 2:
                    _toolsWindow.OnGUI();
                    break;
                default:
                    break;
            }
            EditorGUILayout.EndVertical();
        }
    }
}