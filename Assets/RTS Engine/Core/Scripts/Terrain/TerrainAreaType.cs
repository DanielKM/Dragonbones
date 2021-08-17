using UnityEngine;

namespace RTSEngine.Terrain
{
    [CreateAssetMenu(fileName = "NewTerrainArea", menuName = "RTS Engine/Terrain Area Type", order = 3)]
    public class TerrainAreaType : RTSEngineScriptableObject
    {
        [SerializeField, Tooltip("Provide a unique key for your terrain area, this is used by the RTS Engine components to identify the terrain area.")]
        private string key = "unique_key";
        public override string Key => key;

        [SerializeField, Tooltip("All layers that the game objects of the terrain area uses must be added here.")]
        private LayerMask layers = new LayerMask();
        public LayerMask Layers => layers;

        [SerializeField, Tooltip("This values allows to determine the initial height that the Terrain Manager would start searching at when it is searching for a valid position on this terrain area.")]
        private float baseHeight = 0.0f;
        public float BaseHeight => baseHeight;

        [SerializeField, Tooltip("When testing whether a position is inside this area type (using a raycast with a downwards direction), this offset value is added to the test position in order to avoid situations where the test position (usually detected via mouse click on terrain area) is too close to the area type."), Min(0.0f)]
        private float testHeightOffset = 1.0f;
        public float TestHeightOffset => testHeightOffset;
    }
}
