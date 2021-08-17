using System.Collections.Generic;
using System.Linq;
using System;

using UnityEditor;
using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;
using RTSEngine.Entities;
using RTSEngine.Upgrades;

namespace RTSEngine.EditorOnly
{
    [InitializeOnLoad]
    public class RTSEngineWindow : EditorWindow
    {
        // Left view
        private Vector2 leftViewScrollPos = Vector2.zero;
        private const float leftViewWidth = 255.0f;
        private string[] leftViewOptions = new string[] { 
            "Entities",

            "Entity Upgrades",
            "Entity Component Upgrades",
            
            "Task UI" 
        };
        private int lastLeftViewOptionID = 0;
        private int leftViewOptionID = 0;

        private Vector2 rightViewScrollPos = Vector2.zero;

        private static RTSEngineWindow currWindow = null;
        private float TextureSize = 64.0f;

        // Entity related
        private int categoryMask = -1;
        private string[] currentCategoriesArray = new string[] { "" };
        private IEnumerable<IEntity> currentCategoriesEntities = Enumerable.Empty<IEntity>();

        private int entitySortBy = 0;
        private string[] entitySortByOptions = new string[] { "Code", "Category" };

        public bool factionEntityIncluded = true;
        private int factionEntityMask = 0;
        private IDictionary<string, Type> factionEntityOptions = new Dictionary<string, Type>() {
            { "Attack", typeof(IAttackComponent) },
            { "Rallypoint", typeof(IRallypoint) },
            {  "Drop Off Target", typeof(IDropOffTarget) },
            { "Unit Creator", typeof(IUnitCreator) },
            { "Upgrade Launcher", typeof(IUpgradeLauncher) },
            { "Healer", typeof(Healer) },
            { "Converter", typeof(Converter) },
            { "Resource Generator", typeof(IResourceCollector) },
            { "Unit Carrier", typeof(IUnitCarrier) }
        };

        public bool unitIncluded = true;
        private int unitMask = 0;
        private IDictionary<string, Type> unitOptions = new Dictionary<string, Type>() {
            { "Builder", typeof(IBuilder) },
            { "Resource Collector", typeof(IResourceCollector) },
            { "Drop Off Source", typeof(IDropOffSource) },
            { "CarriableUnit", typeof(ICarriableUnit) },
        };

        public bool buildingIncluded = true;
        private int buildingMask = 0;
        private IDictionary<string, Type> buildingOptions = new Dictionary<string, Type>() {
            { "", typeof(IMonoBehaviour) } 
        };

        public bool resourceIncluded = true;
        private int resourceMask = 0;
        private IDictionary<string, Type> resourceOptions = new Dictionary<string, Type>() {
            { "", typeof(IMonoBehaviour) }
        };

        int nextFactionEntityMask = 0;
        int nextUnitMask = 0;
        int nextBuildingMask = 0;
        int nextResourceMask = 0;

        bool nextFactionEntityIncluded = true;
        bool nextUnitIncluded = true;
        bool nextBuildingIncluded = true;
        bool nextResourceIncluded = true;

        private string[] excludedEntityCategories = new string[] { "new_unit_category", "new_building_category", "new_resource_category" };
        private bool excludedEntityCategoriesFoldout = false;

        public RTSEngineWindow()
        {
            RTSEditorHelper.OnRTSPrefabsAndAssetsReload += HandleRTSPrefabsAndAssetsReload;
        }

        private void HandleRTSPrefabsAndAssetsReload()
        {
            RefreshEntitiesToDisplay();
        }

        [MenuItem("RTS Engine/RTS Engine Menu", priority = 1)]
        public static void ShowWindow()
        {
            currWindow = (RTSEngineWindow)EditorWindow.GetWindow(typeof(RTSEngineWindow), false, "RTS Engine");
            currWindow.minSize = new Vector2(700.0f, 300.0f);
            currWindow.Show();
        }

        void OnEnable()
        {
            RefreshEntitiesToDisplay();
        }

        private void OnDisable()
        {
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();

            OnLeftView();

            EditorGUI.TextArea(new Rect(leftViewWidth, 0.0f, 1f, Screen.height), "", GUI.skin.verticalSlider);

            OnRightView();

            GUILayout.EndHorizontal();

            Repaint();
        }

        private void OnLeftView()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.Space();


            lastLeftViewOptionID = leftViewOptionID;
            lastLeftViewOptionID = EditorGUILayout.Popup(leftViewOptionID, leftViewOptions);
            if(lastLeftViewOptionID != leftViewOptionID)
            {
                leftViewOptionID = lastLeftViewOptionID;
                RefreshEntitiesToDisplay();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUI.TextArea(new Rect(0.0f, EditorGUIUtility.singleLineHeight * 2, leftViewWidth, 1.0f), "", GUI.skin.horizontalSlider);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            leftViewScrollPos = GUILayout.BeginScrollView(leftViewScrollPos, GUILayout.Width(leftViewWidth));

            switch(leftViewOptions[leftViewOptionID])
            {
                case "Entities":
                case "Entity Upgrades":
                case "Entity Component Upgrades":
                    OnEntitiesLeftView();
                    break;

                case "Task UI":
                    OnEntitiesLeftView();
                    break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void OnEntitiesLeftView()
        {
            nextFactionEntityMask = factionEntityMask;
            nextUnitMask = unitMask;
            nextBuildingMask = buildingMask;
            nextResourceMask = resourceMask;

            nextFactionEntityIncluded = factionEntityIncluded;
            nextUnitIncluded = unitIncluded;
            nextBuildingIncluded = buildingIncluded;
            nextResourceIncluded = resourceIncluded;

            nextFactionEntityIncluded = EditorGUILayout.ToggleLeft("Faction Entities", factionEntityIncluded);

            if(factionEntityIncluded)
            {
                EditorGUI.indentLevel++;

                nextFactionEntityMask = EditorGUILayout.MaskField(
                    factionEntityMask,
                    factionEntityOptions.Keys.ToArray());

                nextUnitIncluded = EditorGUILayout.ToggleLeft("Units", unitIncluded);
                if(unitIncluded)
                {
                    EditorGUI.indentLevel++;

                    nextUnitMask = EditorGUILayout.MaskField(
                        unitMask,
                        unitOptions.Keys.ToArray());

                    EditorGUI.indentLevel--;
                }

                nextBuildingIncluded = EditorGUILayout.ToggleLeft("Buildings", buildingIncluded);
                if(buildingIncluded)
                {
                    EditorGUI.indentLevel++;

                    nextBuildingMask = EditorGUILayout.MaskField(
                        buildingMask,
                        buildingOptions.Keys.ToArray());

                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            nextResourceIncluded = EditorGUILayout.ToggleLeft("Resources", resourceIncluded);

            if (resourceIncluded)
            {
                EditorGUI.indentLevel++;

                nextResourceMask = EditorGUILayout.MaskField(
                    resourceMask,
                    resourceOptions.Keys.ToArray());

                EditorGUI.indentLevel--;
            }

            if(nextFactionEntityMask != factionEntityMask || nextFactionEntityIncluded != factionEntityIncluded
                || nextUnitMask != unitMask || nextUnitIncluded != unitIncluded
                || nextBuildingMask != buildingMask || nextBuildingIncluded != buildingIncluded
                || nextResourceMask != resourceMask || nextResourceIncluded != resourceIncluded)
            {
                RefreshEntitiesToDisplay();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Count: {currentCategoriesEntities.Count()}", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            if(excludedEntityCategoriesFoldout = EditorGUILayout.Foldout(excludedEntityCategoriesFoldout, "Excluded Categories:"))
            {
                EditorGUI.indentLevel++;

                foreach (string excludedCategory in excludedEntityCategories)
                    EditorGUILayout.LabelField($"'{excludedCategory}'");

                EditorGUI.indentLevel--;
            }
        }

        private void RefreshEntitiesToDisplay()
        {
            factionEntityIncluded = nextFactionEntityIncluded;
            unitIncluded = nextUnitIncluded;
            buildingIncluded = nextBuildingIncluded;
            resourceIncluded = nextResourceIncluded;

            factionEntityMask = nextFactionEntityMask;
            unitMask = nextUnitMask;
            buildingMask = nextBuildingMask;
            resourceMask = nextResourceMask;

            currentCategoriesEntities = RTSEditorHelper.GetEntities().Values
                .Where(entity =>
                {
                    if (entity.GetComponent<IResource>().IsValid() && !entity.GetComponent<IFactionEntity>().IsValid())
                        return false;

                    bool isEntityAllowed = false;
                    if (!factionEntityIncluded)
                    {
                        if (entity.GetComponent<IFactionEntity>().IsValid())
                            return false;
                    }
                    else
                    {
                        isEntityAllowed = CheckEntityOptions(entity, factionEntityOptions, factionEntityMask);
                        if (!unitIncluded)
                        {
                            if (entity.GetComponent<IUnit>().IsValid())
                                return false;
                        }
                        else
                            isEntityAllowed = isEntityAllowed && CheckEntityOptions(entity, unitOptions, unitMask);

                        if (!buildingIncluded)
                        {
                            if (entity.GetComponent<IBuilding>().IsValid())
                                return false;
                        }
                        else
                            isEntityAllowed = isEntityAllowed && CheckEntityOptions(entity, buildingOptions, buildingMask);
                    }

                    return isEntityAllowed;
                });

            currentCategoriesEntities = currentCategoriesEntities
                .Concat(
                    RTSEditorHelper.GetEntities().Values
                    .Where(entity =>
                    {
                        if (!resourceIncluded || !entity.GetComponent<IResource>().IsValid())
                            return false;
                        else
                            return CheckEntityOptions(entity, resourceOptions, resourceMask);
                    })
                );

            currentCategoriesEntities = currentCategoriesEntities
                .Where(entity => !excludedEntityCategories.Intersect(entity.Category).Any())
                .Distinct();

            switch(leftViewOptions[leftViewOptionID])
            {
                case "Entity Upgrades":
                    currentCategoriesEntities = currentCategoriesEntities
                        .Where(entity => entity.GetComponent<EntityUpgrade>().IsValid());
                    break;

                case "Entity Component Upgrades":
                    currentCategoriesEntities = currentCategoriesEntities
                        .Where(entity => entity.GetComponent<EntityComponentUpgrade>().IsValid());
                    break;
            }

            currentCategoriesArray = currentCategoriesEntities
                .SelectMany(entity => entity.Category)
                .Distinct()
                .ToArray();

            if (currentCategoriesArray.Length == 0)
                currentCategoriesArray = new string[] { "" };

            categoryMask = -1;
        }

        private bool CheckEntityOptions(IEntity entity, IDictionary<string, Type> options, int optionsMask)
        {
            if (optionsMask == 0)
                return true;

            return options.All(kvp => {
                int nextLayer = 1 << Array.IndexOf(options.Keys.ToArray(), kvp.Key);
                if ((optionsMask & nextLayer) != 0 && entity.GetComponentInChildren(kvp.Value) == null)
                    return false;
                return true;
            });
        }

        private void OnRightView()
        {
            GUILayout.BeginVertical();

            OnRightViewTop();

            EditorGUI.TextArea(new Rect(leftViewWidth, EditorGUIUtility.singleLineHeight * 2, Screen.width - leftViewWidth, 1.0f), "", GUI.skin.horizontalSlider);

            EditorGUILayout.Space();
            rightViewScrollPos = GUILayout.BeginScrollView(rightViewScrollPos, GUILayout.Width(Screen.width - leftViewWidth));

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            OnRightViewBottom();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void OnRightViewTop()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.MinHeight(EditorGUIUtility.singleLineHeight*2), GUILayout.MaxWidth(10.0f));

            GUILayout.BeginVertical();

            switch(leftViewOptions[leftViewOptionID])
            {
                case "Entities":
                case "Entity Upgrades":
                case "Entity Component Upgrades":

                    categoryMask = EditorGUILayout.MaskField(
                        new GUIContent("Categories:"),
                        categoryMask,
                        currentCategoriesArray
                    );

                    entitySortBy = EditorGUILayout.Popup(new GUIContent("Sort By:"), entitySortBy, entitySortByOptions);

                        break;
                case "Task UI":

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    break;
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void OnRightViewBottom()
        {
            switch(leftViewOptions[leftViewOptionID])
            {
                case "Entities":
                case "Entity Upgrades":
                case "Entity Component Upgrades":

                    OnEntitiesMenu();

                        break;
                case "Task UI":

                    break;
            }
        }

        private void OnEntitiesMenu()
        {
            IEnumerable<IEntity> entities = currentCategoriesEntities
                .Where(entity =>
                {
                    foreach (string category in entity.Category)
                    {
                        int categoryLayer = 1 << Array.IndexOf(currentCategoriesArray, category);
                        if ((categoryMask & categoryLayer) != 0)
                            return true;
                    }
                    return false;
                })
                .OrderBy(entity => entitySortByOptions[entitySortBy] == "Code" ? entity.Code : entity.Category.FirstOrDefault());

            float CodeWidth = TextureSize * 8.0f;

            foreach (IEntity nextEntity in entities)
            {
                OnPreElement();


                switch (leftViewOptions[leftViewOptionID])
                {
                    case "Entities":
                        if (nextEntity.Icon.IsValid())
                            EditorGUILayout.LabelField(new GUIContent(nextEntity.Icon.texture), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));
                        else
                            EditorGUILayout.LabelField(new GUIContent(""), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));

                        GUILayout.BeginVertical();

                        EditorGUILayout.LabelField($"Code:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));
                        EditorGUILayout.LabelField($"Category:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        EditorGUILayout.SelectableLabel($"{nextEntity.Code}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        EditorGUILayout.SelectableLabel($"{String.Join(",", nextEntity.Category)}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(nextEntity.gameObject, typeof(IEntity), allowSceneObjects: false, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize * 2.0f));
                        GUI.enabled = true;

                        GUILayout.EndVertical();

                        break;

                    case "Entity Upgrades":

                        IEntity targetEntity = nextEntity.GetComponent<EntityUpgrade>().UpgradeTarget;

                        if (nextEntity.Icon.IsValid())
                            EditorGUILayout.LabelField(new GUIContent(nextEntity.Icon.texture), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));
                        else
                            EditorGUILayout.LabelField(new GUIContent(""), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));

                        GUILayout.BeginVertical();

                        EditorGUILayout.LabelField($"Code:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));
                        EditorGUILayout.LabelField($"Category:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        EditorGUILayout.SelectableLabel($"{nextEntity.Code}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        EditorGUILayout.SelectableLabel($"{String.Join(",", nextEntity.Category)}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(nextEntity.gameObject, typeof(IEntity), allowSceneObjects: false, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize * 2.0f));
                        GUI.enabled = true;

                        GUILayout.EndVertical();

                        EditorGUILayout.LabelField($"  ->", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));

                        if (targetEntity.Icon.IsValid())
                            EditorGUILayout.LabelField(new GUIContent(targetEntity.Icon.texture), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));
                        else
                            EditorGUILayout.LabelField(new GUIContent(""), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));

                        GUILayout.BeginVertical();

                        EditorGUILayout.LabelField($"Code:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));
                        EditorGUILayout.LabelField($"Category:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        if (targetEntity.IsValid())
                        {
                            EditorGUILayout.SelectableLabel($"{targetEntity.Code}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                            EditorGUILayout.SelectableLabel($"{String.Join(",", nextEntity.Category)}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        }
                        else
                        {
                            EditorGUILayout.SelectableLabel($"TARGET MISSING", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                            EditorGUILayout.SelectableLabel($"TARGET MISSING", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        }

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(targetEntity?.gameObject, typeof(IEntity), allowSceneObjects: false, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize * 2.0f));
                        GUI.enabled = true;

                        GUILayout.EndVertical();

                        break;

                    case "Entity Component Upgrades":

                        IEntityComponent sourceComp = nextEntity.GetComponent<EntityComponentUpgrade>().SourceComponent;
                        IEntityComponent targetComp = nextEntity.GetComponent<EntityComponentUpgrade>().UpgradeTarget;

                        if (nextEntity.Icon.IsValid())
                            EditorGUILayout.LabelField(new GUIContent(nextEntity.Icon.texture), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));
                        else
                            EditorGUILayout.LabelField(new GUIContent(""), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));

                        GUILayout.BeginVertical();

                        EditorGUILayout.LabelField($"Code:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));
                        EditorGUILayout.LabelField($"Category:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        EditorGUILayout.SelectableLabel($"{nextEntity.Code}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        EditorGUILayout.SelectableLabel($"{String.Join(",", nextEntity.Category)}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(sourceComp?.gameObject, typeof(IEntity), allowSceneObjects: false, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize * 2.0f));
                        GUI.enabled = true;

                        GUILayout.EndVertical();

                        // 

                        GUILayout.BeginVertical();

                        EditorGUILayout.LabelField($"Source Component:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.0f));
                        EditorGUILayout.LabelField($"Target Component:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.0f));

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        if (sourceComp.IsValid())
                            EditorGUILayout.SelectableLabel($"{sourceComp.Code}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        else
                            EditorGUILayout.SelectableLabel($"SOURCE MISSING", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        if (targetComp.IsValid())
                            EditorGUILayout.SelectableLabel($"{targetComp.Code}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                        else
                            EditorGUILayout.SelectableLabel($"TARGET MISSING", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));

                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();

                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(targetComp?.gameObject, typeof(IEntity), allowSceneObjects: false, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize * 2.0f));
                        GUI.enabled = true;

                        GUILayout.EndVertical();

                        break;
                }

                OnPostElement();
            }
        }

        private void OnPreElement()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.MinHeight(EditorGUIUtility.singleLineHeight*2), GUILayout.MaxWidth(10.0f));
        }

        private void OnPostElement()
        {
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("", GUILayout.MaxHeight(1.0f), GUILayout.MaxWidth(1.0f));
            EditorGUILayout.TextArea("",GUI.skin.horizontalSlider);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}
