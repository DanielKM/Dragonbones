using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomPropertyDrawer(typeof(EnforceTypeAttribute))]

    public class EnforceTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EnforceTypeAttribute customAttribute = attribute as EnforceTypeAttribute;

            if(property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, $"[{GetType().Name}] No valid input!");
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.PropertyField(position, property, label);

            if (property.objectReferenceValue != null)
            {
                GameObject gameObject = property.objectReferenceValue as GameObject;
                // If the reference is not intended for a game object then it must intended for a component which is attached to a gameobject
                if (!gameObject.IsValid())
                    gameObject = (property.objectReferenceValue as Component)?.gameObject;

                if(customAttribute.EnforcedTypes.Any())
                    foreach (System.Type type in customAttribute.EnforcedTypes)
                    {
                        if (gameObject.GetComponent(type) == null)
                        {
                            property.objectReferenceValue = null;
                            break;
                        }
                    }

                if(gameObject.IsValid()
                    && (customAttribute.PrefabOnly 
                        && PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.NotAPrefab)
                        || (customAttribute.SameScene
                        && UnityEngine.SceneManagement.SceneManager.GetActiveScene() != gameObject.scene))
                    property.objectReferenceValue = null;
            }

            EditorGUI.EndProperty();
        }
    }
}
