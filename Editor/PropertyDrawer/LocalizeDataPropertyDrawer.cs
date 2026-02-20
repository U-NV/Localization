using UnityEditor;
using UnityEngine;

namespace U0UGames.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocalizeData))]
    public class LocalizeDataPropertyDrawer : UnityEditor.PropertyDrawer
    {
        private const float Padding = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 获取属性
            SerializedProperty lKeyProp = property.FindPropertyRelative("lKey");
            SerializedProperty lValueProp = property.FindPropertyRelative("lValue");

            // 计算每一行的高度
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // 1. 先绘制主标签（原始变量名），并获取剩余的绘制区域
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
            EditorGUI.LabelField(labelRect, label);

            // 2. 计算右侧输入区域（从 labelWidth 之后开始）
            float contentStartX = position.x + EditorGUIUtility.labelWidth;
            float contentWidth = position.width - EditorGUIUtility.labelWidth;

            // 3. 计算 Key 和 Value 的位置
            Rect keyRect = new Rect(contentStartX, position.y, contentWidth, lineHeight);
            Rect valueRect = new Rect(contentStartX, position.y + lineHeight + Padding, contentWidth, lineHeight);

            // 4. 绘制输入框。为了让输入框尽可能大，我们将内部标签宽度设小
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 45f; // 为内部的 "Key" 和 "Value" 标签留出较小空间

            EditorGUI.PropertyField(keyRect, lKeyProp, new GUIContent("Key"));
            EditorGUI.PropertyField(valueRect, lValueProp, new GUIContent("Value"));

            // 恢复原始标签宽度
            EditorGUIUtility.labelWidth = originalLabelWidth;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 两行高度 + 中间间距
            return (EditorGUIUtility.singleLineHeight * 2) + Padding;
        }
    }
}
