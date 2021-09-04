using UnityEngine;
using UnityEditor;

namespace RTSEngine.EditorOnly
{
    [CustomPropertyDrawer(typeof(IntRange)), CustomPropertyDrawer(typeof(FloatRange))]
    public class RangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var minLabelRect = new Rect(position.x, position.y, 35, EditorGUIUtility.singleLineHeight);
            var minRect = new Rect(position.x + 45, position.y, position.width - 45, EditorGUIUtility.singleLineHeight);
            var maxLabelRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, 35, EditorGUIUtility.singleLineHeight);
            var maxRect = new Rect(position.x + 45, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width - 45, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(minLabelRect, "Min");
            EditorGUI.PropertyField(minRect, property.FindPropertyRelative("_min"), GUIContent.none);
            EditorGUI.LabelField(maxLabelRect, "Max");
            EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("_max"), GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
        }
    }
}
