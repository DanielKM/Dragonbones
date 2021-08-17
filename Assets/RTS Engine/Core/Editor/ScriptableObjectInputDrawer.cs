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
        public virtual bool DisplayExtra => false;
        public virtual int FieldsAmount => 2 + (DisplayExtra ? 1 : 0);

        public void Draw(Rect position, SerializedProperty property, GUIContent label, Texture texture = null)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            float height = position.height - EditorGUIUtility.standardVerticalSpacing * FieldsAmount * 2;

            float textureSize = texture != null
                ? (height / FieldsAmount) * 2.0f + EditorGUIUtility.standardVerticalSpacing
                : 0.0f;

            Rect popupRect = new Rect(
                position.x + textureSize * 1.5f,
                position.y,
                position.width - textureSize * 1.5f,
                height / FieldsAmount
            );

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.EndProperty();
                return;
            }

            if (!RTSEditorHelper.GetAssetFilesDictionary(out Dictionary<string, T> dictionary))
            {
                EditorGUI.LabelField(popupRect, label.text, $"Error fetching asset files! See console!");
                EditorGUI.EndProperty();
                return;
            }

            int index = dictionary.Values.ToList().IndexOf(property.objectReferenceValue as T);

            if (index < 0)
                index = 0;

            string[] keys = dictionary.Keys.ToArray();

            index = EditorGUI.Popup(popupRect, label.text, index, keys);

            property.objectReferenceValue = dictionary[keys[index]] as Object;

            Rect contentRect = new Rect(
                position.x + textureSize * 1.5f,
                position.y + height / FieldsAmount + EditorGUIUtility.standardVerticalSpacing,
                position.width - textureSize * 1.5f,
                height / FieldsAmount
            );
            EditorGUI.PropertyField(contentRect, property, GUIContent.none);


            if (texture != null)
            {
                int lastIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Rect textureRect = new Rect(position.x + textureSize * 0.375f * lastIndent, position.y, textureSize, textureSize);

                EditorGUI.DrawTextureTransparent(textureRect, texture);
                EditorGUI.indentLevel = lastIndent;
            }

            if (DisplayExtra)
            {
                EditorGUI.indentLevel++;
                if (property.objectReferenceValue == null)
                {
                    Rect noValueRect = new Rect(position.x, position.y + (height * 2) / FieldsAmount + EditorGUIUtility.standardVerticalSpacing, position.width, height / FieldsAmount);
                    EditorGUI.LabelField(noValueRect, "None");
                }
                else
                    DrawExtra(position, property, label, height);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        protected virtual void DrawExtra(Rect position, SerializedProperty property, GUIContent label, float height)
        {
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * FieldsAmount + EditorGUIUtility.standardVerticalSpacing * FieldsAmount * 2; 
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
        public override bool DisplayExtra => false;
        public override int FieldsAmount => 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }

        protected override void DrawExtra(Rect position, SerializedProperty property, GUIContent label, float height)
        {

            SerializedObject currSO = new SerializedObject(property.objectReferenceValue as ControlType);
            GUI.enabled = false;
            Rect keyCodeRect = new Rect(position.x, position.y + (height * 2) / FieldsAmount + EditorGUIUtility.standardVerticalSpacing, position.width, height / FieldsAmount);
            EditorGUI.PropertyField(keyCodeRect, currSO.FindProperty("defaultKeyCode"));
            GUI.enabled = true;
        }
    }

}
