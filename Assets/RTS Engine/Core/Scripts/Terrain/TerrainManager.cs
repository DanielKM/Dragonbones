using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Utilities;
using RTSEngine.Cameras;

namespace RTSEngine.Terrain
{
    public class TerrainManager : MonoBehaviour, ITerrainManager
    {
        #region Attributes
        [SerializeField, Tooltip("Add an element for each terrain area type you have in the map (ground, air, water, etc..).")]
        private TerrainAreaType[] areas = new TerrainAreaType[0];
        public IEnumerable<TerrainAreaType> Areas => areas;
        // key: unique code of the terrain area type.
        private IReadOnlyDictionary<string, TerrainAreaType> areasDic = null;

        [SerializeField, Min(1.0f), Tooltip("Approximation of the map size. This value is used by NPC factions to determine the amount of territory it aims to control within the map.")]
        private float mapSize = 16900;
        public float MapSize => mapSize;

        // Height Caching
        [Header("Height Caching"), SerializeField, Tooltip("Defines the lower-left corner of the map as a boundary for caching height values.")]
        private Int2D lowerLeftCorner = new Int2D { x = 0, y = 0 };
        [SerializeField, Tooltip("Defines the upper-right corner of the map as a boundary for caching height values.")]
        private Int2D upperRightCorner = new Int2D { x = 120, y = 120 };

        [SerializeField, Tooltip("Starting the lower left corner of the map up, move by this distance value to cache height values each time until reaching the upper right corner of the map."), Min(0)]
        private int heightCacheDistance = 1;

        [SerializeField, Tooltip("When sampling terrain's height to cache, this offset is added to the raycast source position which will head downwards to detect the terrain object and sample the height."), Min(1)]
        private int heightCacheSampleOffset = 10;

        // Dictionary that holds the cached height values where:
        // key (string): unique identifier of the terrain area type
        // value: A dictionary that has Int2D positions as a key and the height as a float value for each position.
        private Dictionary<string, Dictionary<Int2D, float>> heightCacheDict;

        // Game services
        protected IMovementManager mvtMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; } 

        // Other components
        protected IGameManager gameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.mvtMgr = this.gameMgr.GetService<IMovementManager>();
            this.logger = this.gameMgr.GetService<IGameLoggingService>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 

            if (!logger.RequireTrue(areas.Length > 0,
              $"[{GetType().Name}] The 'Areas' field must at least have one element!"))
                return;
            else if (!logger.RequireValid(areas,
              $"[{GetType().Name}] The 'Areas' field has some invalid elements!"))
                return;
            else if (!logger.RequireTrue(areas.Distinct().Count() == areas.Length,
              $"[{GetType().Name}] The 'Areas' field can not have duplicate elements!"))
                return;

            areasDic = areas
                .ToDictionary(area => area.Key, area => area);

            CacheHeightValues();
        }
        #endregion

        #region Caching Terrain Height Values
        private void CacheHeightValues()
        {
            if (!logger.RequireTrue(heightCacheDistance >= 0,
              $"[{GetType().Name}] The height cache movement distance (field: 'Height Cache Distance') must be > 0."))
                return;

            heightCacheDict = new Dictionary<string, Dictionary<Int2D, float>>();

            foreach (TerrainAreaType areaType in areas)
            {
                heightCacheDict.Add(areaType.Key, new Dictionary<Int2D, float>());

                for (int x = lowerLeftCorner.x; x < upperRightCorner.x; x += heightCacheDistance)
                    for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += heightCacheDistance)
                    {
                        // Each search cell instance is added to the dictionary after it is created for easier direct access using coordinates in the future.
                        Int2D nextPosition = new Int2D
                        {
                            x = x,
                            y = y
                        };

                        GetTerrainAreaPosition(
                            new Vector3(nextPosition.x, areaType.BaseHeight + heightCacheSampleOffset, nextPosition.y),
                            areaType.Key,
                            out Vector3 outPosition);

                        heightCacheDict[areaType.Key].Add(nextPosition, outPosition.y);
                    }
            }
        }

        public ErrorMessage TryGetCachedHeight(Vector3 position, IEnumerable<TerrainAreaType> areaTypes, out float height)
        {
            // When no specific terrain area is supplied then check all of the possible ones.
            if (!areaTypes.Any())
                areaTypes = areas;

            height = position.y;

            // Find the coordinates of the potential search cell where the input position is in
            Int2D nextPosition = new Int2D
            {
                x = (((int)position.x - lowerLeftCorner.x) / heightCacheDistance) * heightCacheDistance + lowerLeftCorner.x,
                y = (((int)position.z - lowerLeftCorner.y) / heightCacheDistance) * heightCacheDistance + lowerLeftCorner.y
            };

            foreach (TerrainAreaType area in areaTypes)
                if (heightCacheDict[area.Key].TryGetValue(nextPosition, out height))
                    return ErrorMessage.none;

            logger.Log(
                $"[{GetType().Name}] Unable to get the height for position: {position}! Consider increasing the size of the height sampling by modifying the lower left corner and top right corner fields.",
                source: this);
            return ErrorMessage.terrainHeightCacheNotFound;
        }
        #endregion

        public bool ScreenPointToTerrainPoint(Vector3 screenPoint, IEnumerable<TerrainAreaType> areaTypes, out Vector3 terrainPoint)
        {
            int layers = 0;
            foreach (TerrainAreaType area in (areaTypes.IsValid() && areaTypes.Any()) ? areaTypes : areas)
                layers = layers | area.Layers;

            if(Physics.Raycast(mainCameraController.MainCamera.ScreenPointToRay(screenPoint), out RaycastHit hit, Mathf.Infinity, layers))
            {
                terrainPoint = hit.point;
                return true;
            }

            terrainPoint = Vector3.zero;
            return false;
        }

        #region Sampling Height
        public float SampleHeight(Vector3 position, IMovementComponent refMvtComp)
            => SampleHeight(position, refMvtComp.TerrainAreas);

        // The navLayerMask in this case represents the layer defined by the pathfinding system
        public float SampleHeight(Vector3 position, IEnumerable<TerrainAreaType> areaTypes)
        {
            TryGetCachedHeight(position, areaTypes, out float height);
            return height;
        }
        #endregion

        #region Handling Terrain Areas
        public bool IsTerrainArea(GameObject obj)
            => IsTerrainArea(obj, areas);

        public bool IsTerrainArea(GameObject obj, IEnumerable<TerrainAreaType> areaTypes)
        {
            if (!areaTypes.IsValid() || !areaTypes.Any())
                return true;

            return areaTypes.Any(area => IsTerrainArea(obj, area.Key));
        }

        public bool IsTerrainArea(GameObject obj, TerrainAreaType areaType) => IsTerrainArea(obj, areaType.Key);

        public bool IsTerrainArea(GameObject obj, string areaKey)
        {
            if (!logger.RequireTrue(areasDic.TryGetValue(areaKey, out TerrainAreaType areaType),
              $"[{GetType().Name}] The input area key: {areaKey} has not been registered for this map!"))
                return false;

            return areaType.Layers == (areaType.Layers | (1 << obj.layer));
        }

        public bool GetTerrainAreaPosition(Vector3 inPosition, IEnumerable<TerrainAreaType> areaTypes, out Vector3 outPosition)
        {
            outPosition = inPosition;

            if (!areaTypes.IsValid() || !areaTypes.Any())
                return true;

            foreach (TerrainAreaType area in areaTypes)
                if (GetTerrainAreaPosition(inPosition, area.Key, out outPosition))
                    return true;

            return false;
        }

        public bool GetTerrainAreaPosition(Vector3 inPosition, TerrainAreaType areaType, out Vector3 outPosition)
            => GetTerrainAreaPosition(inPosition, areaType.Key, out outPosition);

        public bool GetTerrainAreaPosition(Vector3 inPosition, string areaKey, out Vector3 outPosition)
        {
            outPosition = inPosition;

            if (!logger.RequireTrue(areasDic.TryGetValue(areaKey, out TerrainAreaType areaType),
              $"[{GetType().Name}] The input area key: {areaKey} has not been registered for this map!"))
                return false;

            inPosition.y += areaType.TestHeightOffset;

            // Create a ray that goes down vertically and attempt to find the terrain area.
            Ray downRay = new Ray(inPosition, Vector3.down);
            if (Physics.Raycast(downRay, out RaycastHit hit, Mathf.Infinity, areaType.Layers))
            {
                outPosition = hit.point;
                return true;
            }

            return false;
        }
        #endregion

        #region Drawing Terrain Height Caching Area 
#if UNITY_EDITOR
        [Header("Height Caching Gizmos")]
        public Color gizmoColor = Color.green;
        [Min(1.0f)]
        public float gizmoHeight = 1.0f;

        private void OnDrawGizmosSelected()
        {
            if (heightCacheDistance <= 0)
                return;

            Gizmos.color = gizmoColor;
            Vector3 size = new Vector3(upperRightCorner.x - lowerLeftCorner.x, gizmoHeight, upperRightCorner.y - lowerLeftCorner.y);
            Gizmos.DrawWireCube(new Vector3(lowerLeftCorner.x + size.x/2.0f, 0.0f, lowerLeftCorner.y + size.z/2.0f), size);
        }
#endif
        #endregion

    }
}