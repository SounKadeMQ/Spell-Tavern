using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampaignSceneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BootstrapScene(SceneManager.GetActiveScene());
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapScene(scene);
    }

    static void BootstrapScene(Scene scene)
    {
        if (scene.name == "ChapterSelect" &&
            Object.FindAnyObjectByType<MissionSelectController>() == null)
        {
            new GameObject("MissionSelectController").AddComponent<MissionSelectController>();
        }

        if (scene.name == "VNScene" &&
            Object.FindAnyObjectByType<IntermissionChapterController>() == null)
        {
            new GameObject("IntermissionChapterController").AddComponent<IntermissionChapterController>();
        }
    }
}
