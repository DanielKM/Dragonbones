using System;
using System.Collections.Generic;
using System.Linq;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.ResourceExtension;

namespace RTSEngine.NPC.ResourceExtension
{
    public class NPCBorderResourceTracker
    {
        #region Attributes
        // A list that holds resources that aren't being collected by this faction inside the territory one building center.
        private List<IResource> idleResources = new List<IResource>();
        public IEnumerable<IResource> IdleResources => idleResources.ToArray();

        // A list that holds resources that are currently being exploited inside the territory of one building center.
        private List<IResource> exploitedResources = new List<IResource>();
        public IEnumerable<IResource> ExploitedResources => exploitedResources.ToArray();

        #endregion

        #region Initializing/Terminating
        public NPCBorderResourceTracker()
        {
            idleResources = new List<IResource>();
            exploitedResources = new List<IResource>();
        }
        #endregion

        #region Adding/Removing Border Resources
        public IResource GetIdleResourceOfType(ResourceTypeInfo resourceType) => idleResources.FirstOrDefault(resource => resource.ResourceType == resourceType);

        public bool Add(IResource newResource, float resourceExploitChance)
        {
            if (!exploitedResources.Contains(newResource) && !idleResources.Contains(newResource))
            {
                newResource.Health.EntityDead += HandleExploitedOrIdleResourceDead;

                if (UnityEngine.Random.Range(0.0f, 1.0f) <= resourceExploitChance)
                {
                    exploitedResources.Add(newResource);
                    return true;
                }
                else
                {
                    idleResources.Add(newResource);
                    return false;
                }
            }

            return false;
        }

        private void HandleExploitedOrIdleResourceDead(IEntity resource, DeadEventArgs args)
        {
            Remove(resource as IResource);
        }

        public void Remove(IResource resource)
        {
            exploitedResources.Remove(resource);
            idleResources.Remove(resource);
        }

        public bool AttemptReplaceResource(IResource emptyResource, out IResource replacementResource)
        {
            replacementResource = null;

            if (!exploitedResources.Contains(emptyResource))
                return false;

            // Attempt to find a resource type that's idle and of the same type as the empty resource
            replacementResource = GetIdleResourceOfType(emptyResource.ResourceType);

            if (!replacementResource.IsValid())
                return false;

            idleResources.Remove(replacementResource);
            exploitedResources.Add(replacementResource);

            return true;
        }
        #endregion
    }
}
