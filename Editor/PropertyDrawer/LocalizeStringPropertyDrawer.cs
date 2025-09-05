using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace U0UGames.Localization.Editor.PropertyDrawer
{
    [CustomPropertyDrawer(typeof(LocalizeString))]
    public class LocalizeStringPropertyDrawer : UnityEditor.PropertyDrawer
    {
        private  static readonly GUIContent SearchIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small");
        protected virtual bool IsShowLocalizeKey => LocalizeString.IsShowLocalizeKey;
        protected virtual bool IsShowVarName => true;
        protected virtual string KeyFieldName => "localizationKey";
        private VisualElement KeyField(SerializedProperty property)
        {
            VisualElement keyVisual = new VisualElement();
            keyVisual.style.flexDirection = FlexDirection.Row;

            Image webIconImage = new Image();
            webIconImage.image = SearchIcon.image;
            keyVisual.Add(webIconImage);

            var keyProperty = property.FindPropertyRelative(KeyFieldName);
            if (keyProperty == null)
            {
                keyVisual.Add(new Label("找不到关键词"));
                return keyVisual;
            }
            // PropertyField nameTextField = new PropertyField(key);
            TextField nameTextField = new TextField
            {
                value = keyProperty.stringValue
            };
            nameTextField.bindingPath = keyProperty.propertyPath;
            nameTextField.Bind(property.serializedObject);
            
            nameTextField.style.flexShrink = 1;
            nameTextField.style.flexGrow = 1;
            nameTextField.style.justifyContent = Justify.SpaceBetween;
            nameTextField.style.alignSelf = Align.Stretch;
            nameTextField.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            keyVisual.Add(nameTextField);
            return keyVisual;
        }

        private VisualElement ValueField(SerializedProperty property)
        {
            VisualElement valueVisual = new VisualElement();
            var keyProperty = property.FindPropertyRelative(KeyFieldName);
            if (keyProperty == null)
            {
                valueVisual.Add(new Label("找不到关键词"));
                return valueVisual;
            }

            string localizeKey = keyProperty.stringValue;
            var result = LocalizationManager.GetText(localizeKey);
            TextField nameTextField = new TextField
            {
                value = result
            };
            nameTextField.style.flexShrink = 1;
            nameTextField.style.flexGrow = 1;
            nameTextField.style.justifyContent = Justify.SpaceBetween;
            nameTextField.style.alignSelf = Align.Stretch;
            nameTextField.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            nameTextField.SetEnabled(false);
            valueVisual.Add(nameTextField);
            return valueVisual;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement rootElement = new VisualElement();
            rootElement.AddToClassList("unity-base-field");
            rootElement.AddToClassList("unity-object-field");
            rootElement.AddToClassList("unity-base-field__aligned");
            rootElement.AddToClassList("unity-base-field__inspector-field");
            
            // rootElement.style.justifyContent = Justify.SpaceBetween;
            // rootElement.style.alignSelf = Align.Stretch;
            // rootElement.style.flexGrow = 1;
            // rootElement.style.flexShrink = 1;
            // rootElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            if(IsShowVarName){
                var prefixLabel = new Label(property.displayName);
                prefixLabel.AddToClassList("unity-base-field__label");
                prefixLabel.AddToClassList("unity-object-field__label");
                prefixLabel.AddToClassList("unity-property-field__label");
                // prefixLabel.style.flexBasis = new StyleLength(StyleKeyword.Auto);
                prefixLabel.style.width = new StyleLength(new Length(40, LengthUnit.Percent));
                // prefixLabel.style.flexGrow = 1;
                rootElement.Add(prefixLabel);
            }
            
            if (IsShowLocalizeKey)
            {
                var keyVisual = KeyField(property);
                rootElement.Add(keyVisual);
            }
            else
            {
                var keyVisual = ValueField(property);
                rootElement.Add(keyVisual);
            }
            return rootElement;
        }

        // public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        // {
        //     EditorGUI.BeginProperty(position, label, property);
        //     
        //     label.image = SearchIcon.image;
        //     var localizationKeyProperty = property.FindPropertyRelative("localizationKey");
        //     if (localizationKeyProperty != null)
        //     {
        //         Rect keyInputRect = new Rect(position.x, position.y,
        //             position.width, EditorGUIUtility.singleLineHeight);
        //         EditorGUI.PropertyField(keyInputRect, localizationKeyProperty,label);
        //     }
        //     
        //     EditorGUI.EndProperty();
        // }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property,label, true);
        }
    }
}