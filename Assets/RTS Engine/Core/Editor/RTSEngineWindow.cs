using System.Collections.Generic;
using System.Linq;
using System;

using UnityEditor;
using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;
using RTSEngine.Entities;
using RTSEngine.Upgrades;
using RTSEngine.UI;
using RTSEngine.Faction;
using RTSEngine.NPC;
using RTSEngine.Movement;
using RTSEngine.Terrain;
using RTSEngine.Controls;
using UnityEditorInternal;

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

            "Task UI",
            "Faction Type",
            "Resource Type",
            "NPC Type",
            "Movement Formation Type",
            "Terrain Area Type",
            "Control Type"
        };
        private int lastLeftViewOptionID = 0;
        private int leftViewOptionID = 0;

        private Vector2 rightViewScrollPos = Vector2.zero;

        private static RTSEngineWindow currWindow = null;
        private float TextureSize = 64.0f;

        private float IndentSpace = 10.0f;

        // Entity related
        private int entityCategoryMask = -1;
        private string[] currentEntityCategoriesArray = new string[] { "" };
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

        // Task UI related
        private int taskCategoryMask = -1;
        private int[] currentTaskCategoriesArray = new int[0];

        private int taskSortByOptionID = 0;
        private string[] taskSortByOptions = new string[] { "Code", "Category", "Slot Index", "Force Slot?" };

        // Resource Type related
        private int resourceTypeCategoryMask = -1;
        private string[] currentResourceTypeCategoriesArray = new string[] { "With Capacity", "Without Capacity" };

        // Shared
        private int sharedSortByOptionID = 0;
        private string[] sharedSortByOptions = new string[] { "Code" };

        // Search related
        private string searchInput = "";
        private string[] searchResults = new string[0];
        private string[] searchExceptions = new string[] { "Unassigned" };

        public RTSEngineWindow()
        {
            RTSEditorHelper.OnRTSPrefabsAndAssetsReload += HandleRTSPrefabsAndAssetsReload;
        }

        private void HandleRTSPrefabsAndAssetsReload()
        {
            RefreshEntitiesToDisplay();
            RefreshTaskUIToDisplay();
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
            RefreshTaskUIToDisplay();
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
                RefreshTaskUIToDisplay();
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
                    OnScriptableObjectLeftView<EntityComponentTaskUIAsset>();
                    break;
                case "Faction Type":
                    OnScriptableObjectLeftView<FactionTypeInfo>();
                    break;
                case "Resource Type":
                    OnScriptableObjectLeftView<ResourceTypeInfo>();
                    break;
                case "NPC Type":
                    OnScriptableObjectLeftView<NPCType>();
                    break;
                case "Movement Formation Type":
                    OnScriptableObjectLeftView<MovementFormationType>();
                    break;
                case "Terrain Area Type":
                    OnScriptableObjectLeftView<TerrainAreaType>();
                    break;
                case "Control Type":
                    OnScriptableObjectLeftView<ControlType>();
                    break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void OnEntitiesLeftView()
        {
            searchInput = EditorGUILayout.TextField("Search", searchInput);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Count: {searchResults.Count()}", EditorStyles.boldLabel);

            EditorGUILayout.Space();

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

            searchResults = RTSEditorHelper.GetMatchingStrings(
                searchInput,
                currentCategoriesEntities.Select(entity => entity.Code).ToArray(),
                searchExceptions
            );

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

            currentEntityCategoriesArray = currentCategoriesEntities
                .SelectMany(entity => entity.Category)
                .Distinct()
                .ToArray();

            if (currentEntityCategoriesArray.Length == 0)
                currentEntityCategoriesArray = new string[] { "" };

            entityCategoryMask = -1;
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
            EditorGUILayout.LabelField("", GUILayout.MinHeight(EditorGUIUtility.singleLineHeight*2), GUILayout.MaxWidth(IndentSpace));

            GUILayout.BeginVertical();

            switch(leftViewOptions[leftViewOptionID])
            {
                case "Entities":
                case "Entity Upgrades":
                case "Entity Component Upgrades":

                    entityCategoryMask = EditorGUILayout.MaskField(
                        new GUIContent("Categories:"),
                        entityCategoryMask,
                        currentEntityCategoriesArray,
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

                    entitySortBy = EditorGUILayout.Popup(
                        new GUIContent("Sort By:"),
                        entitySortBy,
                        entitySortByOptions,
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

                        break;
                case "Task UI":

                    taskCategoryMask = EditorGUILayout.MaskField(
                        new GUIContent("Panel Categories:"),
                        taskCategoryMask,
                        currentTaskCategoriesArray.Select(category => category.ToString()).ToArray(),
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

                    taskSortByOptionID = EditorGUILayout.Popup(
                        new GUIContent("Sort By:"),
                        taskSortByOptionID,
                        taskSortByOptions,
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

                    break;

                case "Resource Type":

                    resourceTypeCategoryMask = EditorGUILayout.MaskField(
                        new GUIContent("Capacity:"),
                        resourceTypeCategoryMask,
                        currentResourceTypeCategoriesArray,
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

                    sharedSortByOptionID = EditorGUILayout.Popup(
                        new GUIContent("Sort By:"),
                        sharedSortByOptionID,
                        sharedSortByOptions,
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

                    break;

                default:

                    sharedSortByOptionID = EditorGUILayout.Popup(
                        new GUIContent("Sort By:"),
                        sharedSortByOptionID,
                        sharedSortByOptions,
                        GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f)
                    );

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
                    OnScriptableObjectMenu<EntityComponentTaskUIAsset>(hasTexture: true, texturePropertyPath: "data.icon");
                    break;

                case "Faction Type":
                    OnScriptableObjectMenu<FactionTypeInfo>();
                    break;

                case "Resource Type":
                    OnScriptableObjectMenu<ResourceTypeInfo>(hasTexture: true, texturePropertyPath: "icon");
                    break;

                case "NPC Type":
                    OnScriptableObjectMenu<NPCType>();
                    break;
                case "Movement Formation Type":
                    OnScriptableObjectMenu<MovementFormationType>();
                    break;
                case "Terrain Area Type":
                    OnScriptableObjectMenu<TerrainAreaType>();
                    break;
                case "Control Type":
                    OnScriptableObjectMenu<ControlType>();
                    break;
            }
        }

        private void OnEntitiesMenu()
        {
            IEnumerable<IEntity> entities = searchResults
                .Select(result => RTSEditorHelper.GetEntities()[result])
                .Where(entity =>
                {
                    foreach (string category in entity.Category)
                    {
                        int categoryLayer = 1 << Array.IndexOf(currentEntityCategoriesArray, category);
                        if ((entityCategoryMask & categoryLayer) != 0)
                            return true;
                    }
                    return false;
                })
                .OrderBy(entity => entitySortByOptions[entitySortBy] == "Code" ? entity.Code : entity.Category.FirstOrDefault());

            float CodeWidth = TextureSize * 8.0f;

            foreach (IEntity nextEntity in entities)
            {
                OnPreElement();

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f));

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

                GUILayout.EndHorizontal();

                OnPostElement();
            }
        }

        private void OnPreElement()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.MinHeight(EditorGUIUtility.singleLineHeight*2), GUILayout.MaxWidth(IndentSpace));
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

        private void OnScriptableObjectLeftView<T>() where T : RTSEngineScriptableObject
        {
            if(!RTSEditorHelper.GetAssetFilesDictionary<T>(out var assetsDic))
                    return;

            searchInput = EditorGUILayout.TextField("Search", searchInput);

            EditorGUILayout.Space();

            searchResults = RTSEditorHelper.GetMatchingStrings(
                searchInput,
                assetsDic.Keys.ToArray(),
                searchExceptions
            );

            EditorGUILayout.LabelField($"Count: {searchResults.Length}", EditorStyles.boldLabel);
        }

        private void RefreshTaskUIToDisplay()
        {
            if(!RTSEditorHelper.GetAssetFilesDictionary<EntityComponentTaskUIAsset>(out var assetsDic))
                    return;

            currentTaskCategoriesArray = assetsDic.Values
               .Where(asset => asset.IsValid())
               .Select(asset => asset.Data.panelCategory)
               .OrderBy(category => category)
               .Distinct()
               .ToArray();

            taskCategoryMask = -1;
        }

        private void OnScriptableObjectMenu<T>(bool hasTexture = false, string texturePropertyPath = "path") where T : RTSEngineScriptableObject
        {
            if (!RTSEditorHelper.GetAssetFilesDictionary<T>(out var assetsDic))
            {
                EditorGUILayout.LabelField($"Unable to fetch asset files!", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.0f));
                return;
            }

            IEnumerable<T> assetsFiltered = searchResults
                .Select(result => assetsDic[result])
                .Where(asset =>
                {
                    if (!asset.IsValid())
                        return false;

                    if (asset is EntityComponentTaskUIAsset)
                    {
                        int categoryLayer = 1 << Array.IndexOf(currentTaskCategoriesArray, (asset as EntityComponentTaskUIAsset).Data.panelCategory);
                        if ((taskCategoryMask & categoryLayer) != 0)
                            return true;
                        return false;
                    }
                    else if (asset is ResourceTypeInfo)
                    {
                        string hasCapacityString = (asset as ResourceTypeInfo).HasCapacity ? "With Capacity" : "Without Capacity";
                        int categoryLayer = 1 << Array.IndexOf(currentResourceTypeCategoriesArray, hasCapacityString);
                        if ((resourceTypeCategoryMask & categoryLayer) != 0)
                            return true;
                        return false;
                    }
                    else
                        return true;
                })
                .OrderBy(asset =>
                {
                    if (asset is EntityComponentTaskUIAsset)
                    {
                        EntityComponentTaskUIAsset taskAsset = asset as EntityComponentTaskUIAsset;
                        switch (taskSortByOptions[taskSortByOptionID])
                        {
                            case "Category":
                                return taskAsset.Data.panelCategory.ToString();
                            case "Slot Index":
                                return taskAsset.Data.slotIndex.ToString();
                            case "Force Slot?":
                                return taskAsset.Data.forceSlot.ToString();
                        }
                    }

                    return asset.Key;
                });


            float CodeWidth = TextureSize * 8.0f;

            foreach(T asset in assetsFiltered)
            {
                OnPreElement();

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width - leftViewWidth - IndentSpace * 2.0f));

                bool isTextureFound = false;

                if (hasTexture)
                {
                    SerializedObject assetSO = new SerializedObject(asset);
                    UnityEngine.Object textureObject = assetSO.FindProperty(texturePropertyPath).objectReferenceValue;

                    if (textureObject.IsValid())
                    {
                        EditorGUILayout.LabelField(new GUIContent((textureObject as Sprite).texture), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));
                        isTextureFound = true;
                    }
                }

                if(!isTextureFound)
                    EditorGUILayout.LabelField(new GUIContent(""), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize));

                GUILayout.BeginVertical();

                EditorGUILayout.LabelField($"Code:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize));
                if (asset is EntityComponentTaskUIAsset)
                    EditorGUILayout.LabelField($"Category (Slot - Forced?):", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.5f));
                else if (asset is ResourceTypeInfo)
                {
                    ResourceTypeInfo resourceAsset = asset as ResourceTypeInfo;
                    if(resourceAsset.HasCapacity)
                        EditorGUILayout.LabelField($"Starting Amount/Capacity:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.5f));
                    else
                        EditorGUILayout.LabelField($"Starting Amount:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.5f));
                }
                else if (asset is TerrainAreaType)
                    EditorGUILayout.LabelField($"Layer Mask:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.5f));
                else if (asset is ControlType)
                    EditorGUILayout.LabelField($"Default Key:", EditorStyles.boldLabel, GUILayout.MaxWidth(TextureSize * 2.5f));

                GUILayout.EndVertical();

                GUILayout.BeginVertical();

                EditorGUILayout.SelectableLabel($"{asset.Key}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                if (asset is EntityComponentTaskUIAsset)
                {
                    EntityComponentTaskUIAsset taskAsset = asset as EntityComponentTaskUIAsset;
                    EditorGUILayout.SelectableLabel($"{taskAsset.Data.panelCategory} ({taskAsset.Data.slotIndex} - {taskAsset.Data.forceSlot})", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                }
                else if (asset is ResourceTypeInfo)
                {
                    ResourceTypeInfo resourceAsset = asset as ResourceTypeInfo;
                    if(resourceAsset.HasCapacity)
                        EditorGUILayout.SelectableLabel($"{resourceAsset.StartingAmount.amount}/{resourceAsset.StartingAmount.capacity}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                    else
                        EditorGUILayout.SelectableLabel($"{resourceAsset.StartingAmount.amount}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                }
                else if (asset is TerrainAreaType)
                {
                    TerrainAreaType areaAsset = asset as TerrainAreaType;
                    EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(areaAsset.Layers), InternalEditorUtility.layers, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                }
                else if (asset is ControlType)
                {
                    ControlType controlAsset = asset as ControlType;
                    EditorGUILayout.EnumPopup(controlAsset.DefaultKeyCode, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight), GUILayout.MaxWidth(CodeWidth));
                }

                GUILayout.EndVertical();

                GUILayout.BeginVertical();

                GUI.enabled = false;
                EditorGUILayout.ObjectField(asset, typeof(IEntity), allowSceneObjects: false, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2), GUILayout.MaxWidth(TextureSize * 2.0f));
                GUI.enabled = true;

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                OnPostElement();
            }
        }

    }
}
