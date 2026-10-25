using UnityEngine;
public class UIButtonSFX : MonoBehaviour
{
    public static UIButtonSFX Instance;
    public AudioSource audio;
    public AudioClip buttonHover;
    public AudioClip selectSound;
    public AudioClip clickSound;
    void Awake()
    {
        if(Instance != null)
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
    }
}
