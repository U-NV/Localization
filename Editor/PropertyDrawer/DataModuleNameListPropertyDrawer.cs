//  using System.Collections.Generic;
// using System.Linq;
// using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
// using UnityEditor;
// using UnityEngine;
//
// namespace U0UGames.Localization.Editor
// {
//     [CustomPropertyDrawer(typeof(LocalizationModuleNameList))]
//     public class DataModuleNameListPropertyDrawer : UnityEditor.PropertyDrawer
//     {
//         private LocalizationConfig _config;
//
//         private LocalizationConfig Config
//         {
//             get
//             {
//                 if (_config != null) return _config;
//                 _config = LocalizationManager.Config;
//                 return _config;
//             }
//         }
//
//         private string[] _existModuleNames;
//
//         private string[] ExistModuleNames
//         {
//             get
//             {
//                 if (_existModuleNames != null && _existModuleNames.Length == Config.allExistModuleNames.Count)
//                 {
//                     return _existModuleNames;
//                 }
//                 _existModuleNames = Config.allExistModuleNames.ToArray();
//                 return _existModuleNames;
//             }
//         }
//             
//         private string _filter = "";
//         private float _propertyHeight = 0;
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             EditorGUI.BeginProperty(position, label, property);
//             Rect firstLineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//             EditorGUI.LabelField(firstLineRect, label);
//             
//             float positionYOffset = EditorGUIUtility.singleLineHeight;
//             SerializedProperty moduleNameListProperty = property.FindPropertyRelative("moduleNameList");
//
//             EditorGUI.indentLevel++;
//             
//             DrawAddBar(position,ref positionYOffset,moduleNameListProperty);
//             // positionYOffset += EditorGUIUtility.singleLineHeight/4;
//             DrawDataList(position,ref positionYOffset,moduleNameListProperty);
//             
//             EditorGUI.indentLevel--;
//             
//             EditorGUI.EndProperty();
//             _propertyHeight = Mathf.Max(_propertyHeight, positionYOffset);
//         }
//
//         private void DrawDataList(Rect position,ref float positionYOffset,SerializedProperty moduleNameListProperty)
//         {
//             // 绘制每个字符串元素的编辑框和移除按钮
//             int buttonSize = 20;
//             int listSize = moduleNameListProperty.arraySize;
//             for (int i = 0; i < listSize; i++)
//             {
//                 GUI.enabled = false;
//                 Rect elementPosition = new Rect(position.x, position.y + positionYOffset, position.width - buttonSize, EditorGUIUtility.singleLineHeight);
//                 SerializedProperty elementProperty = moduleNameListProperty.GetArrayElementAtIndex(i);
//                 EditorGUI.PropertyField(elementPosition, elementProperty, GUIContent.none);
//                 GUI.enabled = true;
//
//                 Rect removeButtonPosition = new Rect(position.x + position.width - buttonSize, position.y + positionYOffset, buttonSize, EditorGUIUtility.singleLineHeight);
//
//                 // 使用GUILayout.BeginChangeCheck和GUILayout.EndChangeCheck保证删除按钮的点击事件不会改变数组的大小
//                 EditorGUI.BeginChangeCheck();
//                 if (GUI.Button(removeButtonPosition, "-"))
//                 {
//                     moduleNameListProperty.DeleteArrayElementAtIndex(i);
//                     break;
//                 }
//                 EditorGUI.EndChangeCheck();
//                 
//                 positionYOffset += EditorGUIUtility.singleLineHeight;
//             }
//         }
//         private void DrawAddBar(Rect position,ref float positionYOffset,SerializedProperty moduleNameListProperty)
//         {
//                         // 搜索框
//             int indentWidth = EditorGUI.indentLevel * 15;
//             GUIContent searchIcon = EditorGUIUtility.IconContent("Search Icon");
//             int filterWidth = 150;
//             int searchIconWidth = (int)EditorGUIUtility.singleLineHeight + indentWidth;
//             Rect filterPosition = new Rect(position.x, position.y + positionYOffset, filterWidth, EditorGUIUtility.singleLineHeight);
//             Rect searchIconPosition = new Rect(filterPosition.x, filterPosition.y, searchIconWidth, EditorGUIUtility.singleLineHeight);
//             Rect textFieldPosition = new Rect(filterPosition.x+searchIconWidth - indentWidth, filterPosition.y, filterPosition.width-searchIconWidth+indentWidth, EditorGUIUtility.singleLineHeight);
//             EditorGUI.LabelField(searchIconPosition, searchIcon);
//             _filter = EditorGUI.TextField(textFieldPosition, _filter);
//             // 下拉框
//             int filterSpace = 5;
//             string[] filteredModuleOptions = ExistModuleNames.Where(option => option.ToLower().Contains(_filter.ToLower())).ToArray();
//             Rect dropdownPosition = new Rect(position.x + filterWidth + filterSpace - indentWidth, position.y + positionYOffset, position.width-filterWidth-filterSpace+indentWidth, EditorGUIUtility.singleLineHeight);
//             int selectedIndex = EditorGUI.Popup(dropdownPosition, -1, filteredModuleOptions);
//             if (selectedIndex >= 0)
//             {
//                 bool isExit = false;
//                 var selectValue = filteredModuleOptions[selectedIndex];
//                 int listSize = moduleNameListProperty.arraySize;
//                 for (int i = 0; i < listSize; i++)
//                 {
//                     SerializedProperty newElementProperty = moduleNameListProperty.GetArrayElementAtIndex(moduleNameListProperty.arraySize - 1);
//                     if (selectValue == newElementProperty.stringValue)
//                     {
//                         isExit = true;
//                         break;
//                     }
//                 }
//
//                 if (!isExit)
//                 {
//                     moduleNameListProperty.arraySize++;
//                     SerializedProperty newElementProperty = moduleNameListProperty.GetArrayElementAtIndex(moduleNameListProperty.arraySize - 1);
//                     newElementProperty.stringValue = selectValue;
//                 }
//             }
//             positionYOffset += EditorGUIUtility.singleLineHeight;
//         }
//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//         {
//             return _propertyHeight;
//         }
//     }
// }