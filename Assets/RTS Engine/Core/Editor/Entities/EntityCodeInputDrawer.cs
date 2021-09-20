using RTSEngine.Entities;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomPropertyDrawer(typeof(EntityCodeInputAttribute))]
    public class EntityCodeInputDrawer : PropertyDrawer
    {
        private int fieldsAmount = 3;

        private bool searchFoldout = false;
        private string[] searchExceptions = new string[] { 
            "new_unit_unit",
            "new_building_unit",
            "new_resource_unit"
        };

        private void Draw (Rect position, SerializedProperty property, GUIContent label, string attributeName)
        {
            //to be used for codes and categories
            label = EditorGUI.BeginProperty(position, label, property);

            float height = position.height - EditorGUIUtility.standardVerticalSpacing * fieldsAmount * 1.5f;
            height /= fieldsAmount;

            Rect nextRect = new Rect(position.x, position.y, position.width, height);

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(nextRect, $"Use [{attributeName}] with string fields where you input a code for an entity.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.PropertyField(nextRect, property, label);

            if (RTSEditorHelper.GetEntities() == null)
            {
                nextRect.height = height * 2;
                nextRect.y += height * EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.HelpBox(nextRect, $"Can not fetch the entities placed under the '*/Resources/Prefabs/' path.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            if(!RTSEditorHelper.GetEntities().TryGetValue(property.stringValue, out IEntity entity))
            {
                nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;

                string[] results = RTSEditorHelper.GetMatchingStrings(property.stringValue, RTSEditorHelper.GetEntities().Keys.ToArray(), searchExceptions);

                searchFoldout = EditorGUI.Foldout(nextRect, searchFoldout, $"Suggestions: {results.Length}");

                if (searchFoldout)
                {
                    fieldsAmount = 4 + results.Length;

                    EditorGUI.indentLevel++;

                    foreach (string result in results)
                    {
                        nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;
                        if (GUI.Button(nextRect, result))
                            property.stringValue = result;
                    }

                    EditorGUI.indentLevel--;
                }
                else
                    fieldsAmount = 4;

                nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;
                nextRect.height = height * 2;
                EditorGUI.HelpBox(nextRect, $"Entity code: '{property.stringValue}' has not been defined for any entity prefab that exists under the '*/Resources/Prefabs' path.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            nextRect.height = height * 2;
            nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;
            fieldsAmount = 4;
            EditorGUI.HelpBox(nextRect, $"Entity code: '{property.stringValue}' is defined for a valid entity:\n(Name: '{entity.Name}', Category: '{entity.Category}'), Radius: '{entity.Radius}'.", MessageType.Info);

            GUI.enabled = false;
            nextRect.height = height;
            nextRect.y += (height + EditorGUIUtility.standardVerticalSpacing) * 2;
            EditorGUI.ObjectField(nextRect, entity?.gameObject, typeof(IEntity), false);
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, typeof(EntityCodeInputAttribute).Name);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return fieldsAmount * (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing * 1.5f);
        }
    }
}
