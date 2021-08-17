using RTSEngine.Determinism;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.Determinism
{
    [CustomPropertyDrawer(typeof(TimeModifiedFloat)), CustomPropertyDrawer(typeof(TimeModifiedTimer))]

    public class TimeModifiedDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EditorGUI.PropertyField(position, property.FindPropertyRelative("value"), label);

            EditorGUI.EndProperty();
        }
    }
}
