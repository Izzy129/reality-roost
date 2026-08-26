#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfFlaggedAttribute))]
public class ShowIfFlaggedDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfFlaggedAttribute showIfFlagged = attribute as ShowIfFlaggedAttribute;

        SerializedProperty enumProp = property.serializedObject.FindProperty(showIfFlagged.enumFieldName);

        if (enumProp != null)
        {
            int enumValue = enumProp.intValue;
            bool shouldShow = (enumValue & showIfFlagged.flagValue) == showIfFlagged.flagValue;

            if (shouldShow) EditorGUI.PropertyField(position, property, label, true);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfFlaggedAttribute showIfFlagged = attribute as ShowIfFlaggedAttribute;
        SerializedProperty enumProp = property.serializedObject.FindProperty(showIfFlagged.enumFieldName);

        if (enumProp != null)
        {
            int enumValue = enumProp.intValue;
            bool shouldShow = (enumValue & showIfFlagged.flagValue) == showIfFlagged.flagValue;

            if (!shouldShow) return 0;
        }

        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif