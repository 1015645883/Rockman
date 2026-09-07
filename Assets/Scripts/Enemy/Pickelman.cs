using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PickelmanEnemy : MonoBehaviour
{
    public enum State
    {
        Sleep,
        Wake
    }

    public State currentState = State.Sleep;
    public int health = 4;

    public GameObject pickaxePrefab;
    public Transform throwPoint;
    public float throwInterval = 1.5f;
    public float detectRadius = 6f;
    public float pickaxeSpeed = 8f;

    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    public string dieTrigger = "Die";

    public AudioClip damageSound;
    private AudioSource audioSource;

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

    private Transform player;

    private float throwTimer;
    private bool playerInRange;
    private SpriteRenderer spriteRenderer;

    private float lastDamageTime;
    public float damageCooldown = 1.5f;
    public int contactDamage = 1;
    public bool isFrozen = false;

    private IEnumerator DelayedFindPlayer()
    {
        while (player == null)
        {
            player = Player; // ✅ 使用上面的动态属性
            yield return new WaitForSeconds(0.1f);
        }

        SetAnimationState(currentState);
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // 先不马上找，启动协程延迟找玩家
        StartCoroutine(DelayedFindPlayer());
    }


    void Update()
    {
        if (isFrozen) return;
        if (player == null)
        {
            // ✅ 如果玩家丢失（例如切换角色后），尝试重新绑定
            player = Player;
            if (player == null) return;
        }

        CheckPlayerInRange();

        if (playerInRange)
        {
            FacePlayer();

            if (currentState == State.Sleep)
            {
                ChangeState(State.Wake);
                throwTimer = 0f;
            }

            throwTimer += Time.deltaTime;
            if (throwTimer >= throwInterval)
            {
                throwTimer = 0f;
                ThrowPickaxe();
            }
        }
        else
        {
            if (currentState == State.Wake)
            {
                ChangeState(State.Sleep);
            }
        }
    }

    void CheckPlayerInRange()
    {
        if (Player == null) return;
        playerInRange = Vector2.Distance(transform.position, player.position) <= detectRadius;
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        SetAnimationState(newState);
    }

    void SetAnimationState(State state)
    {
        if (!animator) return;

        if (state == State.Wake)
        {
            animator.ResetTrigger(sleepTrigger);
            animator.SetTrigger(wakeTrigger);
        }
        else
        {
            animator.ResetTrigger(wakeTrigger);
            animator.SetTrigger(sleepTrigger);
        }
    }

    void ThrowPickaxe()
    {
        if (!pickaxePrefab || !throwPoint || player == null) return;

        GameObject pickaxe = Instantiate(pickaxePrefab, throwPoint.position, Quaternion.identity);

        Vector2 direction = (player.position - throwPoint.position).normalized;

        Pickel pickel = pickaxe.GetComponent<Pickel>();
        if (pickel != null)
        {
            pickel.SetPlayer(player);
            pickel.Launch(direction);
        }
        else
        {
            Debug.LogWarning("生成的预制体上没有 Pickel 脚本");
        }
    }

    void FacePlayer()
    {
        if (player != null)
        {
            bool shouldFaceRight = player.position.x > transform.position.x;
            bool isFacingRight = transform.localScale.x < 0;

            if (shouldFaceRight != isFacingRight)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem hp = collision.GetComponent<HealthSystem>();
            hp?.TakeDamage(contactDamage);
            lastDamageTime = Time.time;
        }

        if (collision.CompareTag("Bullet"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == State.Wake)
            {
                Bullet bullet = collision.GetComponent<Bullet>();
                TakeDamage(1);
                if (bullet != null)
                { 
                bullet.DestroyBullet();
                 }
            }
            else
            {
                // Sleep状态下可以添加子弹被弹开的效果
                Bullet bullet = collision.GetComponent<Bullet>();
                bullet.Deflect(); // 假设Bullet有弹开的方法
            }
        }

        if (collision.CompareTag("ChargeBullet"))
        {
           Bullet bullet = collision.GetComponent<Bullet>();
           TakeDamage(3);
         
          
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (currentState == State.Wake && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(4);
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
                    TakeDamage(1); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(4);
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == State.Wake)
            {
                FireStorm fire = collision.GetComponent<FireStorm>();
                TakeDamage(1);
                if (fire != null) fire.DestroyBullet();
            }
            else
            {
                // Sleep状态下可以添加子弹被弹开的效果
                FireStorm fire = collision.GetComponent<FireStorm>();
                fire.Deflect(); // 假设Bullet有弹开的方法
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            // 只有在Wake状态下才会受到伤害
            if (currentState == State.Wake)
            {
                if (thunder != null)
                {
                    TakeDamage(2);
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
            else
            {
                if (thunder != null)
                {
                    thunder.DestroyOnEnemyHit();
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem hp = collision.GetComponent<HealthSystem>();
            hp?.TakeDamage(contactDamage);
            lastDamageTime = Time.time;
        }
        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (currentState == State.Wake && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 只有在Wake状态下才会受到伤害
            if (currentState == State.Wake)
            {
                FireStorm fire = collision.GetComponent<FireStorm>();
                TakeDamage(1);
                if (fire != null) fire.DestroyBullet();
            }
            else
            {
                // Sleep状态下可以添加子弹被弹开的效果
                FireStorm fire = collision.GetComponent<FireStorm>();
                fire.Deflect(); // 假设Bullet有弹开的方法
            }
        }
    }

    void TakeDamage(int amount)
    {


        if (damageSound)
        {
            audioSource?.PlayOneShot(damageSound);
        }

        // ✅ 播放受击闪烁效果
        if (spriteRenderer != null)
            StartCoroutine(HitFlash());
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
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
    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
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
        if (!throwPoint) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.DrawLine(throwPoint.position, throwPoint.position + Vector3.right * 2);
    }
}
