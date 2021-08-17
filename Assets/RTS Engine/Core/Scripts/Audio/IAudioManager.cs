using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.Audio
{
    public interface IAudioManager
    {
        bool IsMusicActive { get; }
        AudioData Data { get; }

        void OnMusicVolumeSliderUpdated();
        void OnSFXVolumeSliderUpdated();
        void PlayMusic();
        void PlaySFX(AudioClipFetcher clip, bool loop = false);
        void PlaySFX(AudioClip clip, bool loop = false);
        void PlaySFX(AudioSource source, AudioClip clip, bool loop = false);
        void PlaySFX(AudioSource source, AudioClipFetcher fetcher, bool loop = false);
        void PlaySFX(IEntity entity, AudioClip clip, bool loop = false);
        void StopMusic();
        void StopSFX();
        void StopSFX(AudioSource source);
        void UpdateMusicVolume(float volume);
        void UpdateSFXVolume(float volume);
    }

}