using RTSEngine.Entities;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomPropertyDrawer(typeof(EntityCodeInputAttribute))]
    public class EntityCodeInputDrawer : PropertyDrawer
    {
        private void Draw (Rect position, SerializedProperty property, GUIContent label, string attributeName)
        {
            //to be used for codes and categories
            label = EditorGUI.BeginProperty(position, label, property);

            float height = position.height - EditorGUIUtility.standardVerticalSpacing * 6;

            Rect inputRect = new Rect(position.x, position.y, position.width, height / 4);
            Rect helpBoxRect = new Rect(position.x, position.y + height / 4 + EditorGUIUtility.standardVerticalSpacing, position.width, height / 2);
            Rect resultRect = new Rect(position.x, position.y + (3 * height) / 4 + EditorGUIUtility.standardVerticalSpacing * 2, position.width, height / 4);

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(inputRect, $"Use [{attributeName}] with string fields where you input a code for an entity.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.PropertyField(inputRect, property, label);

            if (RTSEditorHelper.GetEntities() == null)
            {
                EditorGUI.HelpBox(helpBoxRect, $"Can not fetch the entities placed under the '*/Resources/Prefabs/' path.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            if(!RTSEditorHelper.GetEntities().TryGetValue(property.stringValue, out IEntity entity))
            {
                EditorGUI.HelpBox(helpBoxRect, $"Entity code: '{property.stringValue}' has not been defined for any entity prefab that exists under the '*/Resources/Prefabs' path.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.HelpBox(helpBoxRect, $"Entity code: '{property.stringValue}' is defined for a valid entity:\n(Name: '{entity.Name}', Category: '{entity.Category}'), Radius: '{entity.Radius}'.", MessageType.Info);

            GUI.enabled = false;
            EditorGUI.ObjectField(resultRect, entity?.gameObject, typeof(IEntity), false);
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, typeof(EntityCodeInputAttribute).Name);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 4 * (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing * 1.5f);
        }
    }
}
