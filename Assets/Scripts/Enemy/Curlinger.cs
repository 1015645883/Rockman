using System.Collections;
using UnityEngine;

public class Curlinger : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 2f;           // 基础移动速度
    private bool movingForward = true;     // 当前是否正向移动
    private bool reflected = false;        // 是否已被SuperArm打回去

    [Header("血量与伤害")]
    public int health = 10;
    public int damageFromSuperArm = 999;
    public int damageFromOthers = 5;
    public int damage = 2;
    private float damageCooldown = 1.4f;
    private float lastDamageTime;

    [Header("爆炸特效")]
    public GameObject explosionPrefab;

    [Header("组件引用")]
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    public bool isFrozen = false;
    private Coroutine lifeTimerCoroutine; // 生命周期计时器

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // ✅ 启动生命周期计时（10秒后自动死亡）
        lifeTimerCoroutine = StartCoroutine(StartLifeTimer(10f));
    }

    void Update()
    {
        Move();
    }

    private void Move()
    {
        float direction = movingForward ? 1f : -1f;
        rb.velocity = new Vector2(moveSpeed * direction, rb.velocity.y);

        if (sr != null)
            sr.flipX = !movingForward;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            if (reflected) return;  // 被打回状态下不再伤害玩家

            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }
        if (reflected && (collision.CompareTag("Enemy") || collision.CompareTag("Machine")))
        {
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // 尝试也让敌人爆炸
            Curlinger otherCurlinger = collision.GetComponent<Curlinger>();
            if (otherCurlinger != null)
            {
                otherCurlinger.Die();
            }
            else
            {

            }

            Destroy(gameObject);
            return;
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
            var superArmScript = (MonoBehaviour)collision.GetComponent<SuperArm1>()
                     ?? collision.GetComponent<SuperArm2>();
            if (superArmScript != null)
            {
                ReflectBack(collision);
            }
            else
            {
                TakeDamage(damageFromOthers);
            }
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
            if (reflected) return;  // 被打回状态下不再伤害玩家

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

    private void ReflectBack(Collider2D collision)
    {
        if (reflected) return;

        float hitDirection = collision.transform.position.x - transform.position.x;

        // ✅ 只有从右边来的拳头才反弹
        if (hitDirection > 0)
        {
            reflected = true;
            movingForward = false;
            moveSpeed *= 2f;

            if (animator != null)
                animator.speed *= 2f;

            SetTagRecursively(gameObject, "SuperArm");

            // ✅ 重新开始生命周期计时（重置为10秒）
            if (lifeTimerCoroutine != null)
                StopCoroutine(lifeTimerCoroutine);
            lifeTimerCoroutine = StartCoroutine(StartLifeTimer(10f));
        }
        else
        {
            TakeDamage(damageFromOthers);
        }
    }

    private IEnumerator StartLifeTimer(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null) Die();
    }

    private void SetTagRecursively(GameObject obj, string newTag)
    {
        obj.tag = newTag;
        foreach (Transform child in obj.transform)
            SetTagRecursively(child.gameObject, newTag);
    }

    private void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
        else if (sr != null)
        {
            StartCoroutine(HitFlash());
        }
    }

    private IEnumerator HitFlash()
    {
        sr.enabled = false;
        yield return new WaitForSeconds(0.06f);
        sr.enabled = true;
    }

    private void Die()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}