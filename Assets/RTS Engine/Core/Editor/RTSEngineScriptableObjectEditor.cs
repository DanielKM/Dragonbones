using UnityEditor;

using RTSEngine.ResourceExtension;
using RTSEngine.NPC;
using RTSEngine.UI;
using RTSEngine.Faction;
using RTSEngine.Movement;
using RTSEngine.Terrain;

namespace RTSEngine.EditorOnly
{
    [CustomEditor(typeof(ResourceTypeInfo))]
    public class ResourceTypeScriptableObjectEditor : RTSEngineScriptableObjectEditor { }

    [CustomEditor(typeof(NPCType))]
    public class NPCTypewScriptableObjectEditor : RTSEngineScriptableObjectEditor { }

    [CustomEditor(typeof(EntityComponentTaskUIAsset))]
    public class EntityComponentTaskUIScriptableObjectEditor : RTSEngineScriptableObjectEditor { }

    [CustomEditor(typeof(FactionTypeInfo))]
    public class FactionTypeScriptableObjectEditor : RTSEngineScriptableObjectEditor { } 

    [CustomEditor(typeof(MovementFormationType))]
    public class MovementFormationTypeScriptableObjectEditor : RTSEngineScriptableObjectEditor { } 

    [CustomEditor(typeof(TerrainAreaType))]
    public class TerrainAreaTypeScriptableObjectEditor : RTSEngineScriptableObjectEditor { } 

    public abstract class RTSEngineScriptableObjectEditor : Editor
    {
        private SerializedObject target_SO;

        public void OnEnable()
        {
            target_SO = new SerializedObject(target as RTSEngineScriptableObject);
            RTSEditorHelper.RefreshAssetFiles(true, target as RTSEngineScriptableObject);
        }

        public override void OnInspectorGUI()
        {
            target_SO.Update();

            DrawDefaultInspector();

            target_SO.ApplyModifiedProperties();
        }
    }
}
