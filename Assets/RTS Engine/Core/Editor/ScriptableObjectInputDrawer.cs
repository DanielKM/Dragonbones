using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

using RTSEngine.UI;
using RTSEngine.Movement;
using RTSEngine.Faction;
using RTSEngine.Terrain;
using RTSEngine.ResourceExtension;
using RTSEngine.NPC;
using RTSEngine.Controls;

namespace RTSEngine.EditorOnly
{
    public class ScriptableObjectInputDrawer<T> : PropertyDrawer where T : RTSEngineScriptableObject 
    {
        private int fieldsAmount = 3;

        private bool searchFoldout = true;
        private bool didSearch = false;
        private string searchText = "";
        private string[] searchExceptions = new string[] { "Unassigned" };

        public void Draw(Rect position, SerializedProperty property, GUIContent label, Texture texture = null)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            float height = position.height - EditorGUIUtility.standardVerticalSpacing * fieldsAmount * 1.5f;
            height /= fieldsAmount;

            float textureSize = texture != null
                ? height * 2.0f + EditorGUIUtility.standardVerticalSpacing
                : 0.0f;

            Rect nextRect = new Rect(
                position.x + textureSize * 1.5f,
                position.y,
                position.width - textureSize * 1.5f,
                height
            );

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.EndProperty();
                return;
            }

            if (!RTSEditorHelper.GetAssetFilesDictionary(out Dictionary<string, T> dictionary))
            {
                EditorGUI.LabelField(nextRect, label.text, $"Error fetching asset files! See console!");
                EditorGUI.EndProperty();
                return;
            }

            int index = dictionary.Values.ToList().IndexOf(property.objectReferenceValue as T);

            if (index < 0)
                index = 0;

            string[] keys = dictionary.Keys.ToArray();

            index = EditorGUI.Popup(nextRect, label.text, index, keys);

            property.objectReferenceValue = dictionary[keys[index]] as Object;

            nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;

            if(!didSearch)
            {
                didSearch = true;
                searchText = keys[index];
            }

            searchText = EditorGUI.TextField(nextRect, "Search", searchText);
            if(searchText != keys[index])
            {
                string[] results = RTSEditorHelper.GetMatchingStrings(searchText, keys, searchExceptions);

                nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;

                searchFoldout = EditorGUI.Foldout(nextRect, searchFoldout, $"Search results count: {results.Length}");

                if (searchFoldout)
                {
                    fieldsAmount = 4 + results.Length;

                    EditorGUI.indentLevel++;

                    foreach (string result in results)
                    {
                        nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;
                        if(GUI.Button(nextRect, result))
                            property.objectReferenceValue = dictionary[result] as Object;
                    }

                    EditorGUI.indentLevel--;
                }
                else
                    fieldsAmount = 4;
            }
            else
                fieldsAmount = 3;

            nextRect.y += height + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(nextRect, property, GUIContent.none);

            if (texture != null)
            {
                int lastIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Rect textureRect = new Rect(position.x + textureSize * 0.375f * lastIndent, position.y, textureSize, textureSize);

                EditorGUI.DrawTextureTransparent(textureRect, texture);
                EditorGUI.indentLevel = lastIndent;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing * 2f) * fieldsAmount; 
        }
    }

    [CustomPropertyDrawer(typeof(EntityComponentTaskUIAsset))]
    public class EntityComponentTaskUIDataDrawer : ScriptableObjectInputDrawer<EntityComponentTaskUIAsset>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EntityComponentTaskUIAsset source = (property.objectReferenceValue as EntityComponentTaskUIAsset);
            Draw(position, property, label,
                texture: source != null && source.Data.icon != null ? source.Data.icon.texture : null);
        }
    }

    [CustomPropertyDrawer(typeof(FactionTypeInfo))]
    public class FactionTypeDrawer : ScriptableObjectInputDrawer<FactionTypeInfo>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(ResourceTypeInfo))]
    public class ResourceTypeDrawer : ScriptableObjectInputDrawer<ResourceTypeInfo>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ResourceTypeInfo source = (property.objectReferenceValue as ResourceTypeInfo);
            Draw(position, property, label,
                texture: source != null && source.Icon != null ? source.Icon.texture : null);
        }
    }

    [CustomPropertyDrawer(typeof(MovementFormationType))]
    public class MovementFormationTypeDrawer : ScriptableObjectInputDrawer<MovementFormationType>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(TerrainAreaType))]
    public class TerrainAreaTypeDrawer : ScriptableObjectInputDrawer<TerrainAreaType>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(NPCType))]
    public class NPCTypeDrawer : ScriptableObjectInputDrawer<NPCType>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(ControlType))]
    public class ControlTypeDrawer : ScriptableObjectInputDrawer<ControlType>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }
    }

}
