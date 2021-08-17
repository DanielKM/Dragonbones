using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomPropertyDrawer(typeof(EntityComponentCodeAttribute))]
    public class EntityComponentCodeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EntityComponentCodeAttribute customAttribute = attribute as EntityComponentCodeAttribute;

            IEntity entity;
            if(customAttribute.TargetEntity)
            {
                string path = customAttribute.EntityPath;
                if(customAttribute.StartFromParentPath)
                {
                    int lastDotIndex = property.propertyPath.LastIndexOf('.');
                    path = $"{property.propertyPath.Remove(lastDotIndex, property.propertyPath.Length - lastDotIndex)}.{path}";
                }

                entity = (property.serializedObject.FindProperty(path).objectReferenceValue as GameObject)?.GetComponent<IEntity>();
            }
            else
                entity = (property.serializedObject.targetObject as MonoBehaviour)?.GetComponent<IEntity>();

            if (property.propertyType != SerializedPropertyType.String
                || entity == null)
            {
                EditorGUI.LabelField(position, label.text,
                    $"[{GetType().Name}] No valid input!");
                EditorGUI.EndProperty();
                return;
            }

            IReadOnlyDictionary<string, IEntityComponent> components = entity.transform
                .GetComponentsInChildren<IEntityComponent>()
                .ToDictionary(component => component.Code, component => component);

            var keys = components.Keys.ToList();

            var displayKeys = components.Keys
                .Select(key => $"{entity.Code}.{key}")
                .ToList();

            if(keys.Count == 0)
            {
                EditorGUI.LabelField(position, label.text,
                    $"No components that implement {typeof(IEntityComponent).Name} are attached to the entity!");
                EditorGUI.EndProperty();
                return;
            }

            int index = keys.IndexOf(property.stringValue);
            if (index < 0)
                index = 0;

            index = EditorGUI.Popup(position, label.text, index, displayKeys.ToArray());
            property.stringValue = keys[index];

            EditorGUI.EndProperty();
        }
    }
}

