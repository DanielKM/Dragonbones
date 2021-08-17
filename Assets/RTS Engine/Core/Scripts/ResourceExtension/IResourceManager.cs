using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.ResourceExtension
{
    public interface IResourceManager : IPreRunGameService
    {
        IEnumerable<IResource> AllResources { get; }
        bool CanAutoCollect { get; }

        IReadOnlyDictionary<int, IFactionSlotResourceManager> FactionResources { get; }

        bool HasResources(ResourceInput resourceInput, int factionID);
        bool HasResources(IEnumerable<ResourceInput> resourceInputArray, int factionID);
        bool HasResources(IEnumerable<ResourceInputRange> resourceInputArray, int factionID);

        void UpdateResource(int factionID, IEnumerable<ResourceInput> resourceInputArray, bool add);
        void UpdateResource(int factionID, ResourceInput resourceInput, bool add);

        ErrorMessage CreateResource(IResource resourcePrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitResourceParameters initParams);
        IResource CreateResourceLocal(IResource resourcePrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitResourceParameters initParams);

        bool IsResourceTypeValidInGame(ResourceInput resourceInput, int factionID);
    }
}