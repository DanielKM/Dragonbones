using RTSEngine.EntityComponent;
using RTSEngine.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomEditor(typeof(ResourceGenerator))]
    public class ResourceGeneratorEditor : TabsEditorBase<ResourceGenerator>
    {
        protected override Int2D tabID {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string [] { "General", "Resource Generation",  "Events" },
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
                case "Resource Generation":
                    OnResourceGenerationInspectorGUI();
                    break;
                case "Events":
                    OnEventsInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("period"));
        }

        protected virtual void OnResourceGenerationInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("resources"));
            EditorGUILayout.PropertyField(SO.FindProperty("requiredResources"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionThreshold"));
            EditorGUILayout.PropertyField(SO.FindProperty("stopGeneratingOnThresholdMet"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("autoCollect"));
            if (SO.FindProperty("autoCollect").boolValue == false)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SO.FindProperty("collectionTaskUI"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("collectionAudio"));
        }

        protected virtual void OnEventsInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("onThresholdMet"));
            EditorGUILayout.PropertyField(SO.FindProperty("onCollected"));
        }
    }
}
