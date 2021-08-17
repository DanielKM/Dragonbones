using RTSEngine.EntityComponent;
using RTSEngine.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomEditor(typeof(UnitCarrier))]
    public class UnitCarrierEditor : TabsEditorBase<UnitCarrier>
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string [] { "General", "Adding", "Ejecting", "Calling" },
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
                case "Adding":
                    OnAddingInspectorGUI();
                    break;
                case "Ejecting":
                    OnEjectingInspectorGUI();
                    break;
                case "Calling":
                    OnCallingInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("capacity"));
            EditorGUILayout.PropertyField(SO.FindProperty("customUnitSlots"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("freeFactionBehaviour"));
        }

        protected virtual void OnAddingInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("addablePositions"));
            EditorGUILayout.PropertyField(SO.FindProperty("carrierPositions"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("forcedTerrainAreas"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("addUnitAudio"));
        }

        protected virtual void OnEjectingInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("ejectablePositions"));
            EditorGUILayout.PropertyField(SO.FindProperty("ejectOnDestroy"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("canEjectSingleUnit"));
            if (SO.FindProperty("canEjectSingleUnit").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("ejectSingleUnitTaskUI"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("canEjectAllUnits"));
            if (SO.FindProperty("canEjectAllUnits").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("ejectAllUnitsTaskUI"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("ejectUnitAudio"));
        }

        protected virtual void OnCallingInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("callUnitsRange"));
            EditorGUILayout.PropertyField(SO.FindProperty("callIdleOnly"));
            EditorGUILayout.PropertyField(SO.FindProperty("callAttackUnits"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("callUnitsTaskUI"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("callUnitsAudio"));
        }
    }
}
