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
        if (scene.name == "TitleScene" &&
            Object.FindAnyObjectByType<TitleOpeningController>() == null)
        {
            new GameObject("TitleOpeningController").AddComponent<TitleOpeningController>();
        }

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

        if (scene.name == "PatientScene")
        {
            if (Object.FindAnyObjectByType<SurgeryCameraTurnIn>() == null &&
                Camera.main != null)
            {
                Camera.main.gameObject.AddComponent<SurgeryCameraTurnIn>();
            }

            if (Object.FindAnyObjectByType<SurgeryForegroundLayer>() == null)
            {
                new GameObject("SurgeryForegroundLayer").AddComponent<SurgeryForegroundLayer>();
            }

            if (Object.FindAnyObjectByType<SurgeryLatticeBackground>() == null)
            {
                new GameObject("SurgeryLatticeBackground").AddComponent<SurgeryLatticeBackground>();
            }
        }
    }
}
