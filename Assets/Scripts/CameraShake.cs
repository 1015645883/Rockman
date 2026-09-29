using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator Shake(float duration, float magnitude)
    {
        StageCameraController stageCamera = GetComponent<StageCameraController>();
        if (stageCamera != null)
        {
            float shakeElapsed = 0f;
            while (shakeElapsed < duration)
            {
                stageCamera.SetShakeOffset(new Vector2(
                    Random.Range(-1f, 1f) * magnitude,
                    Random.Range(-1f, 1f) * magnitude));
                shakeElapsed += Time.deltaTime;
                yield return null;
            }

            stageCamera.SetShakeOffset(Vector2.zero);
            yield break;
        }

        Vector3 originalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
