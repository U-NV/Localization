using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace U0UGames.Localization.Editor.PropertyDrawer
{
    [CustomPropertyDrawer(typeof(LocalizeLanguageCode))]
    public class LocalizeLanguageCodePropertyDrawer: UnityEditor.PropertyDrawer
    {
        private List<string> GetLanguageCodeList()
        {
            var languageConfig = LocalizationManager.Config;
            return languageConfig.GetLanguageCodeList();
        }
        
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement rootElement = new VisualElement();

            var targetProperty = property.FindPropertyRelative("languageCode");
            if (targetProperty == null)
            {
                Debug.LogError($"{property}中找不到languageCode对应的成员");
                return rootElement;
            }

            var languageCodeList = GetLanguageCodeList();
            if (languageCodeList == null || languageCodeList.Count == 0)
            {
                return rootElement;
            }
            
            PopupField<string> selectorPopupField = new PopupField<string>()
            {
                value = targetProperty.stringValue
            };
            foreach (var code in languageCodeList)
            {
                selectorPopupField.choices.Add(code);
            }
            
            selectorPopupField.RegisterValueChangedCallback((evt) =>
            {
                targetProperty.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });
            // selectorPopupField.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            rootElement.Add(selectorPopupField);
            return rootElement;

        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var targetProperty = property.FindPropertyRelative("languageCode");
            if (targetProperty == null)
            {
                Debug.LogError($"{property}中找不到languageCode对应的成员");
                return;
            }
            
            var languageCodeList = GetLanguageCodeList();
            if (languageCodeList == null || languageCodeList.Count == 0)
            {
                return;
            }
            int selectedIndex = languageCodeList.IndexOf(targetProperty.stringValue);
            selectedIndex = EditorGUI.Popup(position, selectedIndex, languageCodeList.ToArray());
            if (selectedIndex >= 0 && selectedIndex < languageCodeList.Count)
            {
                targetProperty.stringValue = languageCodeList[selectedIndex];
            }
            EditorGUI.EndProperty();
            
        }
    }
    
    
}