using UnityEngine;

public class SceneMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (!playOnStart || audioSource == null || musicClip == null)
        {
            return;
        }

        audioSource.clip = musicClip;
        audioSource.loop = loop;
        audioSource.Play();
    }
}
