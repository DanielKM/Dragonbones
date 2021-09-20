using UnityEditor;

using RTSEngine.EntityComponent;
using RTSEngine.Utilities;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomEditor(typeof(UpgradeLauncher))]
    public class UpgradeLauncherEditor : PendingTaskEntityComponentDrawer<UpgradeLauncher>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Tasks" },
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }

        protected override void OnGeneralInspectorGUI()
        {
            base.OnGeneralInspectorGUI();
        }

        protected override void OnTasksInspectorGUI()
        {
            base.OnTasksInspectorGUI();

            EditorGUILayout.PropertyField(SO.FindProperty("upgradeTasks"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("entityTargetUpgradeTasks"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("entityComponentTargetUpgradeTasks"));
        }
    }

    [CustomEditor(typeof(UnitCreator))]
    public class UnitCreatorEditor : PendingTaskEntityComponentDrawer<UnitCreator>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Tasks" },
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IFactionEntity)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnGeneralInspectorGUI()
        {
            base.OnGeneralInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("spawnTransform"));
        }

        protected override void OnTasksInspectorGUI()
        {
            base.OnTasksInspectorGUI();

            EditorGUILayout.PropertyField(SO.FindProperty("creationTasks"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("upgradeTargetCreationTasks"));
        }
    }

    public class PendingTaskEntityComponentDrawer<T> : TabsEditorBase<T> where T : PendingTaskEntityComponentBase
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Tasks":
                    OnTasksInspectorGUI();
                    break;
                default:
                    OnComponentSpecificInspectorGUI(tabName);
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
        }

        protected virtual void OnTasksInspectorGUI()
        {
        }

        protected virtual void OnComponentSpecificInspectorGUI(string tabName)
        {
        }
    }
}
