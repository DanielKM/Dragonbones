using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Utilities;

namespace RTSEngine.Audio
{
    /// <summary>
    /// Allows to define a set of AudioClip instances and retrieve one of them depending on the chosen type.
    /// </summary>
    [System.Serializable]
    public class AudioClipFetcher : Fetcher<AudioClip> 
    {
        [SerializeField, Tooltip("Enable cooldown for the audio clip before it gets played again.")]
        private GlobalTimeModifiedTimer cooldown = new GlobalTimeModifiedTimer(enabled: true, defaultValue: 2.0f);

        protected override void OnPreFetch()
        {
            cooldown.IsActive = true;
        }

        protected override bool CanFetch()
        {
            return !cooldown.IsActive;
        }
    }
}
