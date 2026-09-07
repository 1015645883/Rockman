using UnityEngine;
using System.Collections;

public class Watcher : MonoBehaviour
{
    public enum WatcherState
    {
        Sleep,
        Wake
    }

    public WatcherState currentState = WatcherState.Sleep;

    [Header("基础设置")]
    public int health = 1;
    public float moveRange = 1.5f; // 上下巡逻范围
    public float moveSpeed = 1f;
    private Vector3 startPoint;
    private bool movingUp = true;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    [Header("攻击相关")]
    public GameObject wavePrefab;
    public Transform upFirePoint;
    public Transform downFirePoint;
    public float attackAnimationDelay = 1f;
    public float attackInterval = 2f;
    public enum AttackDirection { Left, Right }
    public AttackDirection attackDirection = AttackDirection.Left;

    [Header("状态检测")]
    public float detectionRadius = 3f;
    public LayerMask playerLayer;
    private Transform player;
    private bool playerInRange;
    private bool isAttacking;
    private Rigidbody2D rb;
    public bool isFrozen = false;

    [Header("动画和视觉")]
    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    public AudioClip Damage;

    private void Start()
    {
        startPoint = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        StartCoroutine(FindPlayer());

        SetAnimationState(currentState);
        UpdateSpriteFlip();
    }

    private IEnumerator FindPlayer()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            yield return null; // 等一帧再试
        }

        // 如果你需要依赖player的逻辑，可以放这里执行
    }


    void Update()
    {
        if (isFrozen)
        {
            return;
        }
        PatrolMovement();
        CheckPlayerInRange();

        if (playerInRange && currentState == WatcherState.Sleep && !isAttacking)
        {
            ChangeState(WatcherState.Wake);
            StartCoroutine(DelayedAttack());
        }
        else if (!playerInRange && currentState == WatcherState.Wake)
        {
            ChangeState(WatcherState.Sleep);
            StopAllCoroutines();
            isAttacking = false;
        }
    }

    void PatrolMovement()
    {
        if (currentState == WatcherState.Sleep)
        {
            Vector3 newPos = transform.position;
            float offset = moveSpeed * Time.deltaTime;

            if (movingUp)
                newPos.y += offset;
            else
                newPos.y -= offset;

            if (Mathf.Abs(newPos.y - startPoint.y) > moveRange)
                movingUp = !movingUp;

            transform.position = newPos;
        }
    }

    void CheckPlayerInRange()
    {
        if (player == null) return;
        playerInRange = Vector2.Distance(transform.position, player.position) <= detectionRadius;
    }

    void ChangeState(WatcherState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            SetAnimationState(newState);
        }
    }

    void SetAnimationState(WatcherState state)
    {
        if (animator == null) return;

        switch (state)
        {
            case WatcherState.Wake:
                animator.ResetTrigger(sleepTrigger);
                animator.SetTrigger(wakeTrigger);
                break;
            case WatcherState.Sleep:
                animator.ResetTrigger(wakeTrigger);
                animator.SetTrigger(sleepTrigger);
                break;
        }
    }

    IEnumerator DelayedAttack()
    {
        yield return new WaitForSeconds(attackAnimationDelay);
        isAttacking = true;

        while (playerInRange)
        {
            FireElectroWave();
            yield return new WaitForSeconds(attackInterval);
        }

        isAttacking = false;
    }

    void FireElectroWave()
    {
        if (wavePrefab == null || upFirePoint == null || downFirePoint == null) return;


        GameObject upWave = Instantiate(wavePrefab, upFirePoint.position, Quaternion.identity);
        GameObject downWave = Instantiate(wavePrefab, downFirePoint.position, Quaternion.identity);

        // 设置方向
        Vector2 direction = (attackDirection == AttackDirection.Left) ? Vector2.left : Vector2.right;

        ThunderWave upComponent = upWave.GetComponent<ThunderWave>();
        if (upComponent != null) upComponent.SetDirection(direction);

        ThunderWave downComponent = downWave.GetComponent<ThunderWave>();
        if (downComponent != null) downComponent.SetDirection(direction);

        UpdateSpriteFlip();
    }

    void UpdateSpriteFlip()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = (attackDirection == AttackDirection.Right);
    }

    void OnTriggerEnter2D(Collider2D collision)
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
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            bullet.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
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
            TakeDamage(1);
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen) // 如果敌人未被冻结
                {
                    iceArrow.Freeze(this); // 调用冰冻方法
                    iceArrow.DestroyBullet();
                }
                else // 已被冻结，再次接触造成大量伤害
                {
                    TakeDamage(1); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            TakeDamage(1);
        }
    }

    void OnTriggerStay2D(Collider2D collision)
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
            TakeDamage(1);
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            TakeDamage(1);
        }
    }

    void TakeDamage(int amount)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Damage;
            audioSource.playOnAwake = false;
        }

        audioSource.Play();
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        animator.SetTrigger("Die");
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.18f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        if (upFirePoint != null) Gizmos.DrawLine(upFirePoint.position, upFirePoint.position + Vector3.right * 0.5f);
        if (downFirePoint != null) Gizmos.DrawLine(downFirePoint.position, downFirePoint.position + Vector3.right * 0.5f);
    }
}
