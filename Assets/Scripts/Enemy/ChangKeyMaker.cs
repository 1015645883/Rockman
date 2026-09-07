using UnityEngine;
using System.Collections;

public class ChangKeyMaker : MonoBehaviour
{
    public enum State
    {
        Idle,
        Attack
    }

    [Header("基础属性")]
    public int health = 6;
    public float detectRadius = 7f;
    public float enterDelay = 2f;    // 玩家进入范围后的延迟
    public float spawnInterval = 5f; // 投掷间隔
    public Transform spawnPoint;
    public GameObject changKeyPrefab;

    [Header("动画参数")]
    public Animator animator;
    public string idleTrigger = "Idle";
    public string attackTrigger = "Attack";
    public string spawnTrigger = "Spawn";
    public string dieTrigger = "Die";
    public GameObject explosionPrefab; // 死亡时生成的爆炸预制体
    [Header("音效")]
    public AudioClip damageSound;
    private AudioSource audioSource;

    [Header("接触伤害")]
    public int Damage = 1;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    private Transform player;
    private State currentState = State.Idle;
    private float enterTimer = 0f;
    private float spawnTimer = 0f;

    private SpriteRenderer spriteRenderer;
    public bool isFrozen = false;

    // ✅ 动态获取玩家（支持普通与特殊关卡）
    private Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform; // 特殊关卡当前受控角色

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null; // 普通关卡
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        SetState(State.Idle);
    }

    void Update()
    {
        if (isFrozen) return;

        // ✅ 每帧动态更新玩家
        player = Player;
        if (player == null) return;

        bool playerInRange = Vector2.Distance(transform.position, player.position) <= detectRadius;

        if (playerInRange)
        {
            FacePlayer();

            // 玩家进入范围延迟 enterDelay 秒
            if (enterTimer < enterDelay)
            {
                enterTimer += Time.deltaTime;
                return;
            }

            // spawnTimer 用于控制间隔
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                // 播放投掷动画（动画事件会生成 ChangKey）
                SetState(State.Attack);
                spawnTimer = 0f;

                // 投掷完成后恢复 Idle
                StartCoroutine(ReturnToIdleAfterAnimation());
            }
        }
        else
        {
            SetState(State.Idle);
            enterTimer = 0f;
            spawnTimer = 0f;
        }
    }

    public void SpawnChangKeyByAnim()
    {
        if (!changKeyPrefab || !spawnPoint) return;
        GameObject enemyGO = Instantiate(changKeyPrefab, spawnPoint.position, Quaternion.identity);
        enemyGO.name = "ChangKey_" + enemyGO.GetInstanceID();
    }

    private IEnumerator ReturnToIdleAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        SetState(State.Idle);
    }

    void SetState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        if (animator == null) return;

        switch (newState)
        {
            case State.Idle:
                animator.SetTrigger(idleTrigger);
                break;
            case State.Attack:
                animator.SetTrigger(attackTrigger);
                break;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        bool shouldFaceRight = player.position.x > transform.position.x;
        bool isFacingRight = transform.localScale.x < 0;
        if (shouldFaceRight != isFacingRight)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // 以下伤害、冰冻、死亡逻辑保持不变
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem hp = collision.GetComponent<HealthSystem>();
            hp?.TakeDamage(Damage);
            lastDamageTime = Time.time;
        }
        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            bullet?.DestroyBullet();
        }
        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(3);
            bullet?.DestroyBullet();
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
        if (collision.CompareTag("SuperArm")) TakeDamage(3);
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen)
                {
                    iceArrow.Freeze(this);
                    TakeDamage(2);
                }
                else
                {
                    TakeDamage(4);
                }
                iceArrow.DestroyBullet();
            }
        }
        if (collision.CompareTag("HyperBomb")) TakeDamage(3);
        if (collision.CompareTag("FireStorm")) TakeDamage(2);
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
                    TakeDamage(2);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(Damage);
                lastDamageTime = Time.time;
            }
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
    }

    void TakeDamage(int amount)
    {
        if (damageSound) audioSource?.PlayOneShot(damageSound);
        if (spriteRenderer != null) StartCoroutine(HitFlash());

        health -= amount;
        if (health <= 0) Die();
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.enabled = true;
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null) drop.TryDrop();

        Destroy(gameObject, 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
