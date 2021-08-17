using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Entities;
using RTSEngine.Logging;

namespace RTSEngine.Audio
{
    public abstract class AudioManagerBase : MonoBehaviour, IAudioManager
    {
        #region Attributes 
        //Music Loops:
        [SerializeField, Tooltip("Audio clips for music to be played during the game."), Header("Music")]
        private AudioClipFetcher music = new AudioClipFetcher();

        [SerializeField, Tooltip("Play the music audio clips as soon as the game starts.")]
        private bool playMusicOnStart = true;

        /// <summary>
        /// Are the music loops currently active and playing?
        /// </summary>
        public bool IsMusicActive { private set; get; } //is the music currently playing?

        [SerializeField, Tooltip("Volume of the music audio clips."), Range(0.0f, 1.0f)]
        private float musicVolume = 1.0f;

        [SerializeField, Tooltip("UI Slider that allows to modify the music loop's volume.")]
        private Slider musicVolumeSlider = null;

        [SerializeField, Tooltip("AudioSource component that plays the music loops.")]
        private AudioSource musicAudioSource = null;

        private Coroutine musicCoroutine; //references the music coroutine, responsible for playing music clips one after another

        //SFX:
        [SerializeField, Tooltip("AudioSource component that plays the global sound effects during the game."), Header("SFX")]
        private AudioSource globalSFXAudioSource = null;

        [SerializeField, Tooltip("Volume of the audio clips that play from the Global SFX Audio Source and from local audio sources."), Range(0.0f, 1.0f)]
        private float SFXVolume = 1.0f;

        [SerializeField, Tooltip("UI Slider that allows to modify the SFX loop's volume.")]
        private Slider SFXVolumeSlider = null;

        // read-only AudioData
        public AudioData Data => new AudioData
        {
            SFXVolume = SFXVolume,

            musicVolume = musicVolume
        };
        #endregion

        #region Initializing/Terminating
        protected void InitBase(ILoggingService logger)
        {

            logger.RequireValid(globalSFXAudioSource,
              $"[{GetType().Name}] 'Global SFX Audio Source' hasn't been assigned!",
              type: LoggingType.warning);

            logger.RequireValid(musicAudioSource,
              $"[{GetType().Name}] 'Music Audio Source' hasn't been assigned!",
              type: LoggingType.warning);

            IsMusicActive = false;
            if (playMusicOnStart == true) //if we're able to start playing the music on start
                PlayMusic();

            //set initial volume
            UpdateSFXVolume(SFXVolume);
            UpdateMusicVolume(musicVolume);
        }

        private void OnDestroy()
        {
            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region SFX
        /// <summary>
        /// Called when the local and global SFX volume slider's value is updated.
        /// </summary>
        public void OnSFXVolumeSliderUpdated()
        {
            UpdateSFXVolume(SFXVolumeSlider.value);
        }

        /// <summary>
        /// Updates the volume of the local SFX AudioSource instances (coming from units, buildings, resources and attack objects) and the global SFX AudioSource instance.
        /// </summary>
        /// <param name="volume">The new volume value for the local and global audio sources.</param>
        public void UpdateSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume); //volume's value can be only in [0.0, 1.0]

            if (globalSFXAudioSource)
                globalSFXAudioSource.volume = SFXVolume;

            if (SFXVolumeSlider)
                SFXVolumeSlider.value = SFXVolume;

            OnAudioDataUpdated();
        }

        public void PlaySFX(AudioSource source, AudioClipFetcher fetcher, bool loop = false) =>
            PlaySFX(source, fetcher.Fetch(), loop);

        public void PlaySFX(IEntity entity, AudioClip clip, bool loop = false) =>
            PlaySFX(entity.AudioSourceComponent, clip, loop);

        /// <summary>
        /// Plays an AudioClip instance on a given AudioSource instance (Used for local sound effects).
        /// </summary>
        /// <param name="source">AudioSource instance to play the clip.</param>
        /// <param name="clip">AudioClip instance to be played.</param>
        /// <param name="loop">When true, the audio clip will be looped.</param>
        public void PlaySFX(AudioSource source, AudioClip clip, bool loop = false)
        {
            if (clip == null) //in case no clip is assigned, do not continue.
                return;

            //make sure that there's a valid audio source before playing the clip
            if (source == null)
            {
                Debug.LogError($"[AudioManager] AudioSource is missing, can not play the audio clip.");
                return;
            }

            source.Stop(); //stop the current audio clip from playing.

            source.clip = clip;
            source.loop = loop;

            source.Play(); //play the new clip
        }

        public void PlaySFX(AudioClipFetcher fetcher, bool loop = false)
        {
            PlaySFX(globalSFXAudioSource, fetcher.Fetch(), loop);
        }

        /// <summary>
        /// Plays an AudioClip instance in the global SFX audio source (Used for global sound effects).
        /// </summary>
        /// <param name="clip">AudioClip instance to be played.</param>
        /// <param name="loop">When true, the audio clip will be looped.</param>
        public void PlaySFX(AudioClip clip, bool loop = false)
        {
            PlaySFX(globalSFXAudioSource, clip, loop);
        }

        /// <summary>
        /// Stops playing audio from an AudioSource instance (Used for local sound effects).
        /// </summary>
        /// <param name="source">AudioSource instance to stop.</param>
		public void StopSFX(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
        }

        /// <summary>
        /// Stops playing audio from the global SFX audio source.
        /// </summary>
        public void StopSFX()
        {
            globalSFXAudioSource.Stop();
        }
        #endregion

        #region Music Loops
        /// <summary>
        /// Called when the music's volume slider's value is updated.
        /// </summary>
        public void OnMusicVolumeSliderUpdated()
        {
            UpdateMusicVolume(musicVolumeSlider.value);
        }

        /// <summary>
        /// Updates the volume of the music loops.
        /// </summary>
        /// <param name="volume">The new volume value for the music loops.</param>
        public void UpdateMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);

            if (musicAudioSource)
                musicAudioSource.volume = musicVolume;

            if (musicVolumeSlider)
                musicVolumeSlider.value = musicVolume; //update the music's volume UI slider as well

            OnAudioDataUpdated();
        }

        /// <summary>
        /// Starts playing the music loops.
        /// </summary>
        public void PlayMusic()
        {
            if (!IsMusicActive)
                musicCoroutine = StartCoroutine(OnMusicCoroutine());
        }

        /// <summary>
        /// Coroutine that plays music loops.
        /// </summary>
        private IEnumerator OnMusicCoroutine()
        {
            if (music.Count <= 0 || musicAudioSource == null) //if no music clips have been assigned then do not play anything
                yield break;

            IsMusicActive = true;

            while (true)
            {
                //get the next audio clip and play it
                musicAudioSource.clip = music.Fetch();
                musicAudioSource.Play();

                //wait for the current music clip to end to play the next one:
                yield return new WaitForSeconds(musicAudioSource.clip.length);
            }
        }

        /// <summary>
        /// Stops playing music loops.
        /// </summary>
        public void StopMusic()
        {
            if (!IsMusicActive)
                return;

            StopCoroutine(musicCoroutine);
            IsMusicActive = false;
        }
        #endregion

        #region AudioData
        protected virtual void OnAudioDataUpdated() { }
        #endregion
    }
}