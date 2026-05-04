using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenUI : MonoBehaviour
{
    [SerializeField] private string missionSelectSceneName = "ChapterSelect";
    [SerializeField] private string debugUnlockSequence = "DEBUG";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip debugUnlockJingle;

    private int debugSequenceIndex;

    void Awake()
    {
        DebugModeState.Disable();
    }

    void Update()
    {
        if (string.IsNullOrEmpty(debugUnlockSequence))
        {
            return;
        }

        foreach (char inputChar in Input.inputString)
        {
            if (!char.IsLetter(inputChar))
            {
                continue;
            }

            char normalizedChar = char.ToUpperInvariant(inputChar);
            if (normalizedChar == debugUnlockSequence[debugSequenceIndex])
            {
                debugSequenceIndex++;

                if (debugSequenceIndex >= debugUnlockSequence.Length)
                {
                    DebugModeState.Enable();
                    debugSequenceIndex = 0;

                    if (audioSource != null && debugUnlockJingle != null)
                    {
                        audioSource.PlayOneShot(debugUnlockJingle);
                    }

                    Debug.Log("Debug mode enabled.");
                }
            }
            else
            {
                debugSequenceIndex = normalizedChar == debugUnlockSequence[0] ? 1 : 0;
            }
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(missionSelectSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
