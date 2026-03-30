using UnityEngine;

public class HeartbeatAudioController : MonoBehaviour
{
    [SerializeField] private Patient patient;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private float healthyPitch = 0.85f;
    [SerializeField] private float criticalPitch = 1.5f;
    [SerializeField] private float lowBloodThreshold = 35f;
    [SerializeField] private float healthyBloodLevel = 100f;
    [SerializeField] private float minVolume = 0.2f;
    [SerializeField] private float maxVolume = 0.75f;

    void Start()
    {
        ResolveReferences();

        if (audioSource == null || heartbeatClip == null)
        {
            enabled = false;
            return;
        }

        audioSource.clip = heartbeatClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        UpdateHeartbeatAudio();
    }

    void Update()
    {
        UpdateHeartbeatAudio();
    }

    void ResolveReferences()
    {
        if (patient == null)
        {
            patient = FindAnyObjectByType<Patient>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void UpdateHeartbeatAudio()
    {
        if (patient == null || audioSource == null)
        {
            return;
        }

        float bloodLevel = Mathf.Clamp(patient.bloodLevel, 0f, healthyBloodLevel);
        float urgency = 1f - Mathf.InverseLerp(lowBloodThreshold, healthyBloodLevel, bloodLevel);

        audioSource.pitch = Mathf.Lerp(healthyPitch, criticalPitch, urgency);
        audioSource.volume = Mathf.Lerp(minVolume, maxVolume, urgency);
    }
}
