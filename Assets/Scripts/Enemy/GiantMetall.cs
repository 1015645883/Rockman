using System.Collections;
using UnityEngine;

public class GiantMetall : MonoBehaviour
{
    [Header("基础属性")]
    public int health = 20;
    public float fireInterval = 5f;
    private bool isDestroyed = false;
    private bool isActive = false;
    public int damage = 4;
    private float damageCooldown = 1.4f;
    private float lastDamageTime;
    public bool isFrozen = false;

    [Header("攻击设置")]
    public float bulletSpeed = 6f;
    public float[] fireAngles = { 0f, 90f, 180f, 270f }; // 可在 Inspector 中修改
    public Transform firePoint; // 子弹生成位置，可为空则使用自身位置

    [Header("引用设置")]
    public GameObject bulletPrefab;
    public GameObject explosionEffect;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;
    public AudioClip damageSound;
    public AudioClip deadSound;

    private Coroutine fireCoroutine;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        fireCoroutine = StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        yield return new WaitForSeconds(1f);
        while (!isDestroyed)
        {
            if (!isFrozen)
            {
                FireInFourDirections();
            }
            yield return new WaitForSeconds(fireInterval);
        }
    }

    /// <summary>
    /// 遍历所有角度调用 FireAtAngle()
    /// </summary>
    private void FireInFourDirections()
    {
        if (bulletPrefab == null || fireAngles == null || fireAngles.Length == 0) return;

        foreach (float angle in fireAngles)
        {
            FireAtAngle(angle);
        }

        if (!isActive) isActive = true;
    }

    /// <summary>
    /// 按角度发射子弹，角度相对于自身朝向
    /// </summary>
    private void FireAtAngle(float angle)
    {
        if (!bulletPrefab) return;

        // 计算世界角度（相对自身旋转）
        float worldAngle = transform.eulerAngles.z + angle;
        Quaternion rotation = Quaternion.Euler(0, 0, worldAngle);

        // 生成子弹
        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, rotation);

        // 设置速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb)
            rb.velocity = rotation * Vector2.right * bulletSpeed;
    }




    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        if (collision.CompareTag("Bullet"))
        {
            TakeDamage(1);
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            TakeDamage(4);
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(1);
                shockWave.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("SuperArm"))
        {
            // 检查是否是 GStone（即不受伤害的对象）
            if (collision.GetComponent<GStone>() != null)
            {
                // 拥有 GStone 脚本，不受伤害
                return;
            }

            // 否则正常受伤
            TakeDamage(5);
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen) // 如果敌人未被冻结
                {
                    iceArrow.Freeze(this); // 调用冰冻方法
                    TakeDamage(1);
                    iceArrow.DestroyBullet();
                }
                else // 已被冻结，再次接触造成大量伤害
                {
                    TakeDamage(3); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(5);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(4);
            FireStorm fire = collision.GetComponent<FireStorm>();
            if (fire != null) fire.DestroyBullet();
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            if (thunder != null)
            {
                TakeDamage(4);
                thunder.DestroyOnEnemyHit();
                return;
            }
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    TakeDamage(4);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 玩家与敌人持续重叠时才伤害玩家
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;  // 更新最后一次伤害时间
            }
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(1);
                shockWave.lastDamageTime = Time.time;
            }
        }
    }
    // ✅ 受伤逻辑整合模板
    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;

        health -= amount;

        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        if (!isActive) isActive = true;

        if (health <= 0)
        {
            Die();
        }

        if (spriteRenderer != null)
            StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        // 先隐藏
        spriteRenderer.enabled = false;

        // 等待0.1秒
        yield return new WaitForSeconds(0.06f);

        // 显示回来
        spriteRenderer.enabled = true;

        yield return null;
    }

    private void Die()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        StopAllCoroutines();

        // 禁用碰撞与物理
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        if (audioSource != null && deadSound != null)
            audioSource.PlayOneShot(deadSound);

        // 开始爆炸协程
        StartCoroutine(DestroySequence());
    }

    // ✅ 爆炸 + 渐隐销毁逻辑
    private IEnumerator DestroySequence()
    {
        float explosionDuration = 2f;
        float explosionInterval = 0.1f;
        float timer = 0f;

        Color startColor = spriteRenderer.color;

        while (timer < explosionDuration)
        {
            // 随机爆炸效果
            if (explosionEffect != null)
            {
                Vector2 randomOffset = new Vector2(
                    Random.Range(-4f, 4f),
                    Random.Range(-4f, 4f)
                );
                Instantiate(explosionEffect, transform.position + (Vector3)randomOffset, Quaternion.identity);
            }

            // 渐隐
            float alpha = Mathf.Lerp(1f, 0f, timer / explosionDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return new WaitForSeconds(explosionInterval);
            timer += explosionInterval;
        }

        Destroy(gameObject);
    }
}
