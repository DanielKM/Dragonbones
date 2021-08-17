using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Entities;

namespace RTSEngine.EditorOnly.EntityComponent
{
    public class FactionEntityCreationTaskDrawer<T, V> : PropertyDrawer where T : FactionEntityCreationTask<V> where V : IFactionEntity
    {
        public void Draw(Rect position, SerializedProperty property, GUIContent label, string taskTitlePrefix)
        {
            property
                .FindPropertyRelative("taskTitle")
                .stringValue = property.FindPropertyRelative("prefabObject").objectReferenceValue.IsValid()
                ? $"{taskTitlePrefix}: {(property.FindPropertyRelative("prefabObject").objectReferenceValue as GameObject).GetComponent<V>().Code}"
                : $"{taskTitlePrefix}: Prefab Unassigned";

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }

    [CustomPropertyDrawer(typeof(BuildingCreationTask))]
    public class BuildingCreationTaskDrawer : FactionEntityCreationTaskDrawer<BuildingCreationTask, IBuilding>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, taskTitlePrefix: "Place Building");
        }
    }

    [CustomPropertyDrawer(typeof(UnitCreationTask))]
    public class UnitCreationTaskDrawer : FactionEntityCreationTaskDrawer<UnitCreationTask, IUnit>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label, taskTitlePrefix: "Create Unit");
        }
    }
}
