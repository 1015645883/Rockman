using UnityEngine;
using System;

public class HyperBomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    public GameObject explosionPrefab; // 指定 HyperBombExplosion 预制体
    public float lifeTime = 2f;        // 炸弹最长存在时间（防止飞太远）
    public Action OnDestroyCallback;  // 子弹销毁时的回调

    private void Start()
    {
        // 超时自动爆炸
        Invoke(nameof(Explode), lifeTime);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 碰到敌人或敌人武器
        if (collision.CompareTag("Enemy") || collision.CompareTag("EnemyWeapon") || collision.CompareTag("Machine"))
        {
            Explode();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果炸弹带 Rigidbody2D 并使用物理碰撞
        if (collision.collider.CompareTag("Ground")  || collision.collider.CompareTag("IceFloor") || collision.collider.CompareTag("Spike"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 生成爆炸特效
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 销毁炸弹本体
        OnDestroyCallback?.Invoke();  // 通知 PlayerShooting 子弹销毁
        Destroy(gameObject);
    }

}
