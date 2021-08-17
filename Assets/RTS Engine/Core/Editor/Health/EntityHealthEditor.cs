using UnityEditor;
using UnityEngine;

using RTSEngine.Health;
using RTSEngine.Utilities;

namespace RTSEngine.EditorOnly.Health
{
    [CustomEditor(typeof(UnitHealth))]
    public class UnitEditor : EntityHealthEditor<UnitHealth>
    {
        protected override void OnGeneralInspectorGUI()
        {
            base.OnGeneralInspectorGUI();

            EditorGUILayout.PropertyField(SO.FindProperty("stopMovingOnDamage"));
        }
    }

    [CustomEditor(typeof(BuildingHealth))]
    public class BuildingEditor : EntityHealthEditor<BuildingHealth>
    {
        protected override void OnHealthStatesInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("constructionStates"));
            EditorGUILayout.PropertyField(SO.FindProperty("constructionCompleteState"));

            EditorGUILayout.Space();

            base.OnHealthStatesInspectorGUI();
        }
    }

    [CustomEditor(typeof(ResourceHealth))]
    public class ResourceEditor : EntityHealthEditor<ResourceHealth>
    {
        protected override void OnHealthStatesInspectorGUI()
        {
            base.OnHealthStatesInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectedState"));
        }
    }

    public class EntityHealthEditor<T> : TabsEditorBase<T> where T : EntityHealth
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string[] {"General", "Destruction", "Health States" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Destruction":
                    OnDestructionInspectorGUI();
                    break;
                case "Health States":
                    OnHealthStatesInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("maxHealth"));
            EditorGUILayout.PropertyField(SO.FindProperty("initialHealth"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("canIncrease"));
            EditorGUILayout.PropertyField(SO.FindProperty("canDecrease"));
            EditorGUILayout.PropertyField(SO.FindProperty("canBeAttacked"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("hoverHealthBarY"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("hitEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("hitAudio"));
        }

        protected virtual void OnDestructionInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("destroyObject"));
            EditorGUILayout.PropertyField(SO.FindProperty("destroyObjectDelay"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("destroyAward"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("destructionEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("destructionAudio"));
        }

        protected virtual void OnHealthStatesInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("states"));
        }
    }
}
