using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public Image fadeImage;
    public CanvasGroup canvasGroup;  // 如果你用CanvasGroup控制透明度的话
    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            SetAlphaInstant(0f);  // 初始透明
        }
    }

    // 立即设置透明度，不用协程
    public void SetAlphaInstant(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    // 普通淡入（受时间缩放影响）
    public IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            SetAlphaInstant(alpha);
            yield return null;
        }
        SetAlphaInstant(1f);
    }

    // 普通淡出（受时间缩放影响）
    public IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            SetAlphaInstant(alpha);
            yield return null;
        }
        SetAlphaInstant(0f);
    }

    // 不受时间缩放影响的淡入
    public IEnumerator FadeInUnscaled()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            SetAlphaInstant(alpha);
            yield return null;
        }
        SetAlphaInstant(1f);
    }

    // 不受时间缩放影响的淡出
    public IEnumerator FadeOutUnscaled()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            SetAlphaInstant(alpha);
            yield return null;
        }
        SetAlphaInstant(0f);
    }
}
