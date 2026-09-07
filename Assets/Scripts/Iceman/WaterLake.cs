using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WaterLake : MonoBehaviour
{
    [Header("Splash 特效")]
    public GameObject splashPrefab;     // 进入/退出时生成的特效
    public float splashY = 0f;          // 水面固定 Y 轴
    [Header("音效")]
    public AudioClip splashSfx;         // 水花音效
    private AudioSource audioSource;

    private Collider2D detectionCollider;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        detectionCollider = GetComponent<Collider2D>();
        if (!detectionCollider.isTrigger)
        {
            detectionCollider.isTrigger = true; // 确保是 Trigger
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Application.isPlaying) return;
        if (other.CompareTag("Player"))
        {
            SpawnSplash(other.transform.position.x);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!Application.isPlaying) return;
        if (other.CompareTag("Player"))
        {
            SpawnSplash(other.transform.position.x);
        }
    }

    private void SpawnSplash(float xPosition)
    {
        if (splashPrefab == null) return;

        // 场景正在关闭时不生成
        if (!Application.isPlaying || !gameObject.scene.isLoaded) return;

        Vector3 spawnPos = new Vector3(xPosition, splashY, 0f);
        Instantiate(splashPrefab, spawnPos, Quaternion.identity);
        if (splashSfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(splashSfx);
        }
    }

}
