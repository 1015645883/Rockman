using UnityEngine;

public class DestructibleRock : MonoBehaviour
{
    [Header("爆炸特效设置")]
    [Tooltip("爆炸风特效预制体")]
    public GameObject explosionEffectPrefab;

    [Header("声音效果")]
    [Tooltip("爆炸音效")]
    public AudioClip explosionSound;
    [Tooltip("音效播放器")]
    public AudioSource audioSource;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SuperArm"))
        {
            DestroyRock();
        }
        if (collision.CompareTag("HyperBomb"))
        {
            DestroyRock();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("SuperArm"))
        {
            DestroyRock();
        }
        if (collision.CompareTag("HyperBomb"))
        {
            DestroyRock();
        }
    }

    /// <summary>
    /// 销毁石头并生成爆炸特效
    /// </summary>
    private void DestroyRock()
    {
        // 生成爆炸特效
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, transform.rotation);
        }

        // 播放爆炸音效
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
        }

        // 销毁石头对象
        Destroy(gameObject);
    }

}