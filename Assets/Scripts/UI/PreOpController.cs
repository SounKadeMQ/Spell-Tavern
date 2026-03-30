using UnityEngine;
using UnityEngine.SceneManagement;

public class PreOpController : MonoBehaviour
{
    [SerializeField] private string patientSceneName = "PatientScene";
    [SerializeField] private string titleSceneName = "TitleScene";

    public void BeginOperation()
    {
        SceneManager.LoadScene(patientSceneName);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
