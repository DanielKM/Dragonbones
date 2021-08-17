using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Entities;
using RTSEngine.ResourceExtension;

namespace RTSEngine.EditorOnly.ResourceExtension
{
    [CustomPropertyDrawer(typeof(ResourceTypeValue))]
    public class ResourceTypeValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect amountRect = new Rect(position.x, position.y, position.width / 2, EditorGUIUtility.singleLineHeight);
            Rect capacityRect = new Rect(position.x + position.width / 2, position.y, position.width / 2, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), new GUIContent("Amount"), true);
            EditorGUI.PropertyField(capacityRect, property.FindPropertyRelative("capacity"), new GUIContent("Capacity"), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
