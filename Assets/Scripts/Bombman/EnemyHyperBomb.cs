using UnityEngine;

public class EnemyHyperBomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    public GameObject explosionPrefab; // 指定 HyperBombExplosion 预制体
    public float lifeTime = 2f;        // 炸弹最长存在时间（防止飞太远）

    [Header("Control Settings")]
    public bool canExplode = true;     // 是否允许爆炸

    private void Start()
    {
        // 超时自动爆炸
        Invoke(nameof(Explode), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canExplode) return;

        // 碰到敌人或敌人武器
        if (collision.CompareTag("FireStorm"))
        {
            Explode();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canExplode) return;

        // 如果炸弹带 Rigidbody2D 并使用物理碰撞
        if (collision.collider.CompareTag("Ground")
            || collision.collider.CompareTag("IceFloor")
            || collision.collider.CompareTag("Spike")
            || collision.collider.CompareTag("Player"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (!canExplode) return; // 二次保护

        // 生成爆炸特效
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 销毁炸弹本体
        Destroy(gameObject);
    }
}
