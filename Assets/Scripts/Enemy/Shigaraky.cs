using UnityEngine;
using System.Collections;

public class Shigaraky : MonoBehaviour
{
    [Header("属性")]
    public int health = 10;
    public int damage = 2;
    private float damageCooldown = 1.4f;
    private float lastDamageTime;

    [Header("爆炸特效")]
    public GameObject explosionPrefab;

    [Header("生成相关")]
    public GameObject curlingPrefab;      // Curling 小怪预制体
    public Transform spawnPoint;          // 生成位置（可为空，默认为自身位置）
    public float spawnInterval = 2f;      // 生成间隔时间（秒）

    private bool playerDetected = false;
    private Coroutine spawnCoroutine;

    private SpriteRenderer sr;
    public bool isFrozen = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // --- 提供给检测器调用 ---
    public void SetPlayerDetected(bool detected)
    {
        if (playerDetected == detected) return; // 状态未变化，不重复执行
        playerDetected = detected;

        if (detected)
        {
            // 玩家进入 → 开始循环生成 Curling
            if (spawnCoroutine == null)
                spawnCoroutine = StartCoroutine(SpawnLoop());
        }
        else
        {
            // 玩家离开 → 停止生成
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (playerDetected)
        {
            SpawnCurling();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCurling()
    {
        if (curlingPrefab == null)
        {
            Debug.LogWarning("未指定 Curling 预制体！");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Instantiate(curlingPrefab, pos, Quaternion.identity);
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

    private void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            // ✅ 播放受击闪烁效果
            if (sr != null)
                StartCoroutine(HitFlash());
        }
    }

    private IEnumerator HitFlash()
    {
        if (sr == null) yield break;

        // 先隐藏
        sr.enabled = false;

        // 等待0.1秒
        yield return new WaitForSeconds(0.06f);

        // 显示回来
        sr.enabled = true;

        yield return null;
    }

    private void Die()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

}
