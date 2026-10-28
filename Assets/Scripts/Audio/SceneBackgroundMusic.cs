using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public sealed class SceneBackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioMixerGroup musicGroup;

    private void Start()
    {
        if (musicSource == null || musicClip == null || musicGroup == null)
        {
            Debug.LogError("SceneBackgroundMusic cần Music Source, Music Clip và nhóm BGM.", this);
            return;
        }
        musicSource.playOnAwake = false;
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    private void OnDestroy()
    {
        // AudioSource có thể sống trên object khác; không để nhạc scene cũ tiếp tục phát.
        if (musicSource != null && musicSource.clip == musicClip) musicSource.Stop();
    }
}
