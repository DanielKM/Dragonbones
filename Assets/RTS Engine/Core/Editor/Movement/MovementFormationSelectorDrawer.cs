using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using RTSEngine.Movement;

namespace RTSEngine.EditorOnly.Movement
{
    [CustomPropertyDrawer(typeof(MovementFormationSelector))]
    public class MovementFormationSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            if(property.FindPropertyRelative("type").objectReferenceValue != property.FindPropertyRelative("lastType").objectReferenceValue)
            {
                SerializedProperty data = property.FindPropertyRelative("properties");

                data.FindPropertyRelative("floatProperties").ClearArray();
                data.FindPropertyRelative("intProperties").ClearArray();

                if (property.FindPropertyRelative("type").objectReferenceValue != null)
                {
                    SerializedObject type = new SerializedObject(property.FindPropertyRelative("type").objectReferenceValue);

                    for (int i = 0; i < type.FindProperty("floatProperties").arraySize; i++)
                    {
                        data.FindPropertyRelative("floatProperties").InsertArrayElementAtIndex(i);

                        data.FindPropertyRelative("floatProperties").GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue
                            = type.FindProperty("floatProperties").GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                        data.FindPropertyRelative("floatProperties").GetArrayElementAtIndex(i).FindPropertyRelative("value").floatValue 
                            = type.FindProperty("floatProperties").GetArrayElementAtIndex(i).FindPropertyRelative("value").floatValue;
                    }

                    for (int i = 0; i < type.FindProperty("intProperties").arraySize; i++)
                    {
                        data.FindPropertyRelative("intProperties").InsertArrayElementAtIndex(i);
                        data.FindPropertyRelative("intProperties").GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue
                            = type.FindProperty("intProperties").GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                        data.FindPropertyRelative("intProperties").GetArrayElementAtIndex(i).FindPropertyRelative("value").intValue
                            = type.FindProperty("intProperties").GetArrayElementAtIndex(i).FindPropertyRelative("value").intValue;
                    }
                }

                property.FindPropertyRelative("lastType").objectReferenceValue = property.FindPropertyRelative("type").objectReferenceValue;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
