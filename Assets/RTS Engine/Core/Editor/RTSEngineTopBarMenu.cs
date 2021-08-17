using UnityEngine;
using UnityEditor;

namespace RTSEngine.EditorOnly
{
    public class RTSEngineTopBarMenu
    {
        private const string NewMapConfigsPrefabName = "new_map_configs";

        private const string NewUnitPrefabName = "new_unit";
        private const string NewBuildingPrefabName = "new_building";
        private const string NewResourcePrefabName = "new_resource";

        private const string NewEffectObjectPrefabName = "new_effect_object";
        private const string NewAttackObjectPrefabName = "new_attack_object";

        [MenuItem("RTS Engine/Configure New Map", false, 51)]
        private static void ConfigNewMapOption()
        {
            //destroy the objects in the current scene:
            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>() as GameObject[])
                Object.DestroyImmediate(obj);

            GameObject newMap = Object.Instantiate(UnityEngine.Resources.Load(NewMapConfigsPrefabName, typeof(GameObject))) as GameObject;

            newMap.transform.DetachChildren();

            Object.DestroyImmediate(newMap);

            Debug.Log("[RTS Engine] You have successfully configured this map! Please make sure to bake the navigation mesh before starting the game!");
        }

        [MenuItem("RTS Engine/New Unit", false, 101)]
        private static void NewUnitOption()
        {
            NewEntity(NewUnitPrefabName);
        }

        [MenuItem("RTS Engine/New Building", false, 102)]
        private static void NewBuildingOption()
        {
            NewEntity(NewBuildingPrefabName);
        }

        [MenuItem("RTS Engine/New Resource", false, 103)]
        private static void NewResourceOption()
        {
            NewEntity(NewResourcePrefabName);
        }

        private static void NewEntity(string prefabName)
        {
            Object.Instantiate(Resources.Load($"Prefabs/{prefabName}", typeof(GameObject)));

            Debug.Log("[RTS Engine] Make sure to save your new entity as a prefab in a path that ends with '../Resources/Prefabs'!");
        }

        [MenuItem("RTS Engine/New Effect Object", false, 154)]
        private static void NewEffectObjecct()
        {
            Object.Instantiate(Resources.Load(NewEffectObjectPrefabName, typeof(GameObject)));

            Debug.Log("[RTS Engine] Make sure to save your new effect object as a prefab!");
        }

        [MenuItem("RTS Engine/New Attack Object", false, 154)]
        private static void NewAttackObject()
        {
            Object.Instantiate(Resources.Load(NewAttackObjectPrefabName, typeof(GameObject)));

            Debug.Log("[RTS Engine] Make sure to save your new effect object as a prefab!");
        }

        [MenuItem("RTS Engine/Documentation", false, 201)]
        private static void DocOption()
        {
            Application.OpenURL("http://soumidelrio.com/docs/unity-rts-engine/");
        }

        [MenuItem("RTS Engine/Review", false, 202)]
        private static void ReviewOption()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/templates/packs/rts-engine-79732");
        }
    }
}
