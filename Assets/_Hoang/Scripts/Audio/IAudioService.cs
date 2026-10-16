using UnityEngine;

public interface IAudioService
{
    void PlayMusic(AudioClip clip, bool isLooping = true);
    void StopMusic();
    void PauseMusic();
    void ResumeMusic();
    void PlaySFX(AudioClip clip, float volumeScale = 1f, float startTime = 0f);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
}
