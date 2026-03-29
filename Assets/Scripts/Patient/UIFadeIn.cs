using UnityEngine;
using UnityEngine.UI;

public class UIFadeIn : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private bool disableWhenFinished = true;

    void Start()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        if (fadeImage == null)
        {
            enabled = false;
            return;
        }

        fadeImage.raycastTarget = false;

        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;
    }

    void Update()
    {
        if (fadeImage == null || fadeDuration <= 0f)
        {
            return;
        }

        Color c = fadeImage.color;
        c.a -= Time.deltaTime / fadeDuration;
        c.a = Mathf.Clamp01(c.a);
        fadeImage.color = c;

        if (c.a <= 0f)
        {
            if (disableWhenFinished)
            {
                fadeImage.gameObject.SetActive(false);
            }

            enabled = false;
        }
    }
}
