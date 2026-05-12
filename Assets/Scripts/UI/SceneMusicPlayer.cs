using UnityEngine;
using UnityEngine.SceneManagement;

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

        ApplyMissionMusicOverride();

        if (!playOnStart || audioSource == null || musicClip == null)
        {
            return;
        }

        audioSource.clip = musicClip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    void ApplyMissionMusicOverride()
    {
        MissionData mission = MissionFlowState.CurrentMission;
        if (mission == null ||
            string.IsNullOrWhiteSpace(mission.surgeryMusicResource) ||
            SceneManager.GetActiveScene().name != "PatientScene")
        {
            return;
        }

        AudioClip missionClip = Resources.Load<AudioClip>(mission.surgeryMusicResource);
        if (missionClip != null)
        {
            musicClip = missionClip;
        }
        else
        {
            Debug.LogWarning("Mission surgery music not found at Resources/" + mission.surgeryMusicResource + ".");
        }
    }
}
