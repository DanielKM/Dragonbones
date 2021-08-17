using UnityEditor;

using RTSEngine.UI;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomEditor(typeof(EntityComponentTaskUIAsset)), CanEditMultipleObjects]
    public class EntityComponentTaskUIDataEditor : Editor
    {
        private SerializedObject target_SO;

        public void OnEnable()
        {
            target_SO = new SerializedObject(target as EntityComponentTaskUIAsset);
            RTSEditorHelper.RefreshAssetFiles(true, target as EntityComponentTaskUIAsset);
        }

        public override void OnInspectorGUI()
        {
            target_SO.Update(); //Always update the Serialized Object.

            EditorGUILayout.PropertyField(target_SO.FindProperty("data.code"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.enabled"));

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.displayType"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.icon"));
            EditorGUILayout.HelpBox("Make sure that the index of the task panel category is valid regarding the defined categories (check UIManager -> Task Panel)", UnityEditor.MessageType.Info);
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.panelCategory"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.forceSlot"));
            if (target_SO.FindProperty("data.forceSlot").boolValue)
            {
                EditorGUILayout.HelpBox("Make sure that the amount of the pre-created tasks in the panel category is enough (check UIManager -> Task Panel)", UnityEditor.MessageType.Info);
                EditorGUILayout.PropertyField(target_SO.FindProperty("data.slotIndex"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.tooltipEnabled"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.description"));
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.hideTooltipOnClick"));

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.reloadTime"));

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(target_SO.FindProperty("data.cursor"), true);

            target_SO.ApplyModifiedProperties(); //Apply all modified properties always at the end of this method.
        }
    }
}
