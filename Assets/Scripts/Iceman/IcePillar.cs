using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePillar : MonoBehaviour
{
    public int health = 5;
    public AudioClip damageSound;
    private AudioSource audioSource;
    private Animator animator;

    [Header("冰刺生成")]
    public GameObject iceSpikePrefab;

    // ⭐ Boss 相关
    private BossHealthSystem bossHealth;
    private bool isDying = false; // 防止重复执行

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        animator = GetComponent<Animator>();

        // ⭐ 自动寻找 Boss 并绑定死亡事件
        BindBossDeath();
    }

    // ==============================
    // ⭐ 绑定 Boss 死亡事件
    // ==============================
    void BindBossDeath()
    {
        // 找到场景里的 Boss（Iceman）
        IcemanController boss = FindObjectOfType<IcemanController>();
        if (boss != null)
        {
            bossHealth = boss.GetComponent<BossHealthSystem>();

            if (bossHealth != null)
            {
                bossHealth.OnBossDeathStart += OnBossDeathStart;
            }
        }
    }

    // ==============================
    // ⭐ Boss死亡时触发
    // ==============================
    private void OnBossDeathStart()
    {
        if (isDying) return;
        StopAllCoroutines(); // ⭐ 停止下落/生成等逻辑
        Die();
        isDying = true;
    }

    // ==============================
    // ⭐ 生成冰刺
    // ==============================
    public void SpawnIceSpikes()
    {
        if (iceSpikePrefab == null) return;

        Vector3 basePos = transform.position;

        Vector3 leftPos = new Vector3(basePos.x - 0.9f, basePos.y - 0.7f, basePos.z);
        Instantiate(iceSpikePrefab, leftPos, Quaternion.identity);

        Vector3 rightPos = new Vector3(basePos.x + 0.9f, basePos.y - 0.7f, basePos.z);
        Instantiate(iceSpikePrefab, rightPos, Quaternion.identity);
    }

    // ==============================
    // ⭐ 受击检测
    // ==============================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDying) return;

        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            if (bullet != null) bullet.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            TakeDamage(5);
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(2);
                cutter.lastDamageTime = Time.time;
            }

            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(2);
                shockWave.lastDamageTime = Time.time;
            }
        }

        if (collision.CompareTag("SuperArm")) TakeDamage(5);

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                TakeDamage(1);
                iceArrow.DestroyBullet();
            }
        }

        if (collision.CompareTag("HyperBomb")) TakeDamage(5);
        if (collision.CompareTag("FireStorm")) TakeDamage(5);
        if (collision.CompareTag("ThunderBeam")) TakeDamage(5);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDying) return;

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(2);
                cutter.lastDamageTime = Time.time;
            }

            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(2);
                shockWave.lastDamageTime = Time.time;
            }
        }

        if (collision.CompareTag("SuperArm")) TakeDamage(5);
        if (collision.CompareTag("HyperBomb")) TakeDamage(5);
        if (collision.CompareTag("FireStorm")) TakeDamage(5);
        if (collision.CompareTag("ThunderBeam")) TakeDamage(5);
    }

    // ==============================
    // ⭐ 受伤
    // ==============================
    void TakeDamage(int amount)
    {
        if (isDying) return;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (damageSound != null)
            audioSource.PlayOneShot(damageSound);

        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    // ==============================
    // ⭐ 自身死亡
    // ==============================
    void Die()
    {
        if (isDying) return;

        isDying = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        animator.SetTrigger("Die");

        Destroy(gameObject, 0.18f);
    }

    // ==============================
    // ⭐ 防止事件泄漏（非常重要）
    // ==============================
    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnBossDeathStart -= OnBossDeathStart;
        }
    }
}