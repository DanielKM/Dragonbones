using UnityEditor;
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Utilities;
using RTSEngine.EntityComponent;
using RTSEngine.Movement;

namespace RTSEngine.EditorOnly.Entities
{
    [CustomEditor(typeof(Unit))]
    public class UnitEditor : EntityEditor<Unit>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity", "Faction Entity", "Unit" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    [CustomEditor(typeof(Building))]
    public class BuildingEditor : EntityEditor<Building>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity", "Faction Entity", "Building" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    [CustomEditor(typeof(Resource))]
    public class ResourceEditor : EntityEditor<Resource>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity",  "Resource" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    [CustomEditor(typeof(ResourceBuilding))]
    public class ResourceBuildingEditor : EntityEditor<ResourceBuilding>
    {
        private string[][] toolbars = new string[][] {
            new string[] { "Entity",  "Resource Building" }
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }
    }

    public class EntityEditor<T> : TabsEditorBase<T> where T : Entity
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "Entity":
                    OnEntityInspectorGUI();
                    break;
                case "Faction Entity":
                    OnFactionEntityInspectorGUI();
                    break;
                case "Unit":
                    OnUnitInspectorGUI();
                    break;
                case "Building":
                    OnBuildingInspectorGUI();
                    break;
                case "Resource":
                    OnResourceInspectorGUI();
                    break;
                case "Resource Building":
                    OnResourceBuildingInspectorGUI();
                    break;
            }
        }

        private void OnDisable()
        {
            RTSEditorHelper.SetEntities();
        }

        protected virtual void OnEntityInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("_name"));
            EditorGUILayout.PropertyField(SO.FindProperty("code"));

            string category = SO.FindProperty("category").stringValue;
            EditorGUILayout.PropertyField(SO.FindProperty("category"));
            if(category != SO.FindProperty("category").stringValue)
                RTSEditorHelper.SetEntities();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("description"));
            EditorGUILayout.PropertyField(SO.FindProperty("icon"));
            if (comp is Unit)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(SO.FindProperty("radius"));
            GUI.enabled = true;
            EditorGUILayout.PropertyField(SO.FindProperty("model"));
        }

        protected virtual void OnFactionEntityInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("isMainEntity"));
            EditorGUILayout.PropertyField(SO.FindProperty("isFactionLocked"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("initResources"));
            EditorGUILayout.PropertyField(SO.FindProperty("disableResources"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(SO.FindProperty("coloredRenderers"));
        }

        protected virtual void OnUnitInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("spawnLookAt"));
        }

        protected virtual void OnBuildingInspectorGUI()
        {
            EditorGUILayout.LabelField("No fields.");
        }

        protected virtual void OnResourceInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("resourceType"));
            EditorGUILayout.PropertyField(SO.FindProperty("mainColor"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("canCollect"));
            EditorGUILayout.PropertyField(SO.FindProperty("canCollectOutsideBorder"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionAudio"));
        }

        protected virtual void OnResourceBuildingInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("resourceType"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionAudio"));
        }
    }
}