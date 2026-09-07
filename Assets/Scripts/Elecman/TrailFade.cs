using System.Collections;
using UnityEngine;

public class TrailFade : MonoBehaviour
{
    [Header("渐隐参数")]
    public float fadeDuration = 0.5f; // 渐隐时间
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("TrailFade 需要 SpriteRenderer 组件");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        Color originalColor = sr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(originalColor.a, 0f, t));
            yield return null;
        }

        // 完全透明后销毁
        Destroy(gameObject);
    }
}
