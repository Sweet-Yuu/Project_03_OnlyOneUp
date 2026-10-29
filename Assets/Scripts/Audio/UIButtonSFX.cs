using UnityEngine;

public class UIButtonSFX : MonoBehaviour
{
    public static UIButtonSFX Instance;
    public AudioSource audio;
    public AudioClip buttonHover;
    public AudioClip selectSound;
    public AudioClip clickSound;
    public AudioClip collectCoin;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayClickSound()
    {
        audio.PlayOneShot(clickSound);
        audio.PlayOneShot(selectSound);
    }

    public void PlayHoverSound()
    {
        if (audio == null || buttonHover == null) return;
        audio.PlayOneShot(buttonHover);
    }

    public void PlayCollectCoin()
    {
        if (audio == null || collectCoin == null) return;
        audio.PlayOneShot(collectCoin);
    }
}
