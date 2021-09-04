using UnityEditor;
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Utilities;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomEditor(typeof(CarriableUnit))]
    public class CarriableUnitEditor : FactionEntityTargetComponentEditor<CarriableUnit, IFactionEntity>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Carriable Unit"}
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IUnit - Target: IFactionEntity)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Carriable Unit":
                    EditorGUILayout.PropertyField(SO.FindProperty("allowDifferentFactions"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("ejectionTaskUI"));
                    break;
            }
        }
    }

    [CustomEditor(typeof(Healer))]
    public class HealerEditor : FactionEntityTargetComponentEditor<Healer, IFactionEntity>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Handling Progress"}
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IFactionEntity - Target: IFactionEntity)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnTargetSearchPickerInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"));
        }

        protected override void OnHandlingProgressInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressMaxDistance"));
            EditorGUILayout.PropertyField(SO.FindProperty("stoppingDistance"));
            EditorGUILayout.PropertyField(SO.FindProperty("healthPerProgress"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("inProgressObject"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressOverrideController"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressEnabledAudio"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("sourceEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetEffect"));
        }
    }

    [CustomEditor(typeof(Converter))]
    public class ConverterEditor : FactionEntityTargetComponentEditor<Converter, IFactionEntity>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Handling Progress"}
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IFactionEntity - Target: IFactionEntity)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnTargetSearchPickerInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"));
        }


        protected override void OnHandlingProgressInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressMaxDistance"));
            EditorGUILayout.PropertyField(SO.FindProperty("stoppingDistance"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("inProgressObject"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressOverrideController"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressEnabledAudio"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("sourceEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetEffect"));
        }

    }

    [CustomEditor(typeof(Rallypoint))]
    public class RallypointEditor : FactionEntityTargetComponentEditor<Rallypoint, IEntity>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Rallypoint"}
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IFactionEntity - Target: IEntity/Vector3)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Rallypoint":
                    EditorGUILayout.PropertyField(SO.FindProperty("spawnTransform"));
                    EditorGUILayout.PropertyField(SO.FindProperty("gotoTransform"));
                    EditorGUILayout.PropertyField(SO.FindProperty("forcedTerrainAreas"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("maxDistanceEnabled"));
                    if (SO.FindProperty("maxDistanceEnabled").boolValue == true)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(SO.FindProperty("maxDistance"));
                        EditorGUI.indentLevel--;
                    }
                    break;
            }
        }
    }

    [CustomEditor(typeof(DropOffSource))]
    public class DropOffSourceEditor : FactionEntityTargetComponentEditor<DropOffSource, IFactionEntity>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Dropoff Resources/Capacity" }
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IUnit - Target: IFactionEntity with IResourceDropOff)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Dropoff Resources/Capacity":
                    EditorGUILayout.PropertyField(SO.FindProperty("dropOffResources"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("totalMaxCapacity"));
                    break;
            }
        }

        protected override void OnTargetSearchPickerInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"));

            EditorGUILayout.PropertyField(SO.FindProperty("maxDropOffDistanceEnabled"));
            if (SO.FindProperty("maxDropOffDistanceEnabled").boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("maxDropOffDistance"));
                EditorGUI.indentLevel--;
            }
        }
    }

    [CustomEditor(typeof(ResourceCollector))]
    public class ResourceCollectorEditor : FactionEntityTargetComponentEditor<ResourceCollector, IResource>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Handling Progress", "Resource Collector"},
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IUnit - Target: IResource)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Resource Collector":
                    EditorGUILayout.PropertyField(SO.FindProperty("collectableResources"));
                    break;
            }
        }
    }

    [CustomEditor(typeof(Builder))]
    public class BuilderEditor : FactionEntityTargetComponentEditor<Builder, IBuilding>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Target Search/Picker", "Setting Target" },
            new string [] { "Handling Progress",  "Placement Tasks"},
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IUnit - Target: IBuilding)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Placement Tasks":
                    EditorGUILayout.PropertyField(SO.FindProperty("creationTasks"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("upgradeTargetCreationTasks"));
                    break;
            }
        }

        protected override void OnTargetSearchPickerInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"));
            EditorGUILayout.PropertyField(SO.FindProperty("restrictBuildingPlacementOnly"));

            EditorGUILayout.PropertyField(SO.FindProperty("maxDropOffDistanceEnabled"));
            if (SO.FindProperty("maxDropOffDistanceEnabled").boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("maxDropOffDistance"));
                EditorGUI.indentLevel--;
            }
        }

        protected override void OnHandlingProgressInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressMaxDistance"));
            EditorGUILayout.PropertyField(SO.FindProperty("healthPerProgress"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("inProgressObject"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressOverrideController"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressEnabledAudio"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("sourceEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetEffect"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("constructionAudio"));
        }
    }

    [CustomEditor(typeof(UnitMovement))]
    public class UnitMovementEditor : FactionEntityTargetComponentEditor<UnitMovement, IEntity>
    {
        private string[][] toolbars = new string[][] {
            new string [] { "General", "Setting Target" },
            new string [] { "Movement", "Rotation"}
        };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"Entity Component (Source: IUnit - Target: IEntity/Vector3)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            OnInspectorGUI(toolbars);
        }

        protected override void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
            EditorGUILayout.PropertyField(SO.FindProperty("priority"));
        }

        protected override void OnComponentSpecificInspectorGUI(string tabName)
        {
            switch(tabName)
            {
                case "Movement":
                    EditorGUILayout.PropertyField(SO.FindProperty("movableTerrainAreas"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("formation"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("speed"));
                    EditorGUILayout.PropertyField(SO.FindProperty("acceleration"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("mvtAudio"), new GUIContent("Movement Audio"));
                    EditorGUILayout.PropertyField(SO.FindProperty("invalidMvtPathAudio"), new GUIContent("Invalid Path Audio"));
                    break;

                case "Rotation":
                    EditorGUILayout.PropertyField(SO.FindProperty("mvtAngularSpeed"), new GUIContent("Angular Speed"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("canMoveRotate"));
                    if(SO.FindProperty("canMoveRotate").boolValue == false)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(SO.FindProperty("minMoveAngle"));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(SO.FindProperty("canIdleRotate"));
                    if (SO.FindProperty("canIdleRotate").boolValue == true)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(SO.FindProperty("smoothIdleRotation"));
                        if (SO.FindProperty("smoothIdleRotation").boolValue == true)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(SO.FindProperty("idleAngularSpeed"));
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }
                    break;
            }
        }
    }

    public class FactionEntityTargetComponentEditor<T, V> : TabsEditorBase<T> where T : FactionEntityTargetComponent<V> where V : IEntity
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        public override void OnInspectorGUI(string[][] toolbars)
        {
            base.OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {

            switch (tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Target Search/Picker":
                    OnTargetSearchPickerInspectorGUI();
                    break;
                case "Setting Target":
                    OnSettingTargetInspectorGUI();
                    break;
                case "Handling Progress":
                    OnHandlingProgressInspectorGUI();
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
            EditorGUILayout.PropertyField(SO.FindProperty("priority"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("requireIdleEntity"));
        }

        protected virtual void OnTargetSearchPickerInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("targetFinderData"));
        }

        protected virtual void OnSettingTargetInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("setTargetTaskUI"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("orderAudio"));
        }

        protected virtual void OnHandlingProgressInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("progressDuration"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressMaxDistance"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("inProgressObject"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressOverrideController"));
            EditorGUILayout.PropertyField(SO.FindProperty("progressEnabledAudio"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("sourceEffect"));
            EditorGUILayout.PropertyField(SO.FindProperty("targetEffect"));
        }

        protected virtual void OnComponentSpecificInspectorGUI(string tabName)
        {
        }
    }
}
