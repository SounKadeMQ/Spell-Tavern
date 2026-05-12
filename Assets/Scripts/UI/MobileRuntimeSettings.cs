using UnityEngine;

public class MobileRuntimeSettings : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private float canvasScanInterval = 0.5f;
    private float nextCanvasScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<MobileRuntimeSettings>() != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject("MobileRuntimeSettings");
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<MobileRuntimeSettings>();
    }

    void Awake()
    {
        ApplyPerformanceSettings();
    }

    void Update()
    {
        if (Time.unscaledTime < nextCanvasScanTime)
        {
            return;
        }

        nextCanvasScanTime = Time.unscaledTime + Mathf.Max(0.1f, canvasScanInterval);
        AttachSafeAreaFitters();
    }

    void ApplyPerformanceSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        if (Application.isMobilePlatform)
        {
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;
            QualitySettings.particleRaycastBudget = 0;
        }
    }

    void AttachSafeAreaFitters()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null ||
                canvas.renderMode == RenderMode.WorldSpace ||
                canvas.GetComponent<SafeAreaCanvasFitter>() != null)
            {
                continue;
            }

            canvas.gameObject.AddComponent<SafeAreaCanvasFitter>();
        }
    }
}
