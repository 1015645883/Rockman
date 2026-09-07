using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FiremanController : MonoBehaviour, IBossController
{
    private Rigidbody2D rb;
    private Animator animator;

    public Transform player;
    public GameObject firestormPrefab;       // 大 FireStorm
    public GameObject smallFirestormPrefab;  // 小 FireStorm（跳跃时发射的）
    public GameObject flamePillarPrefab;     // 火柱预制体
    public Transform firePoint;
    public Transform headPoint;

    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;
    public float flashInterval = 0.1f;
    private PlayerDamageEffect damageEffect;
    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;
    private BossHealthSystem healthSystem;
    public int health = 28;
    public float jumpForce = 8f;
    public float moveSpeed = 3f;

    public int damage = 2;
    private float lastDamageTime = 0f;
    private float damageCooldown = 1.5f;
    private float invincibilityTime = 1.5f;
    private float invincibilityTimer = 0f;
    private bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    public bool isInDialogue = true;

    private AudioSource audioSource;
    public AudioClip fireStormSound;
    public AudioClip damageSound;
    public AudioClip[] jumpVoices;
    public AudioClip[] attackVoices;
    public AudioClip[] hurtVoices;
    public AudioClip[] deadVoices;
    public AudioClip[] DeadVoices => deadVoices;

    [Range(0, 1)] public float voiceProbability = 0.7f;
    [Range(0, 1)] public float hurtVoiceProbability = 0.2f;

    void Start()
    {
        healthSystem = GetComponent<BossHealthSystem>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;
        damageEffect = GetComponent<PlayerDamageEffect>();
        StartCoroutine(FindPlayer());
    }

    private IEnumerator FindPlayer()
    {
        while (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) { player = p.transform; break; }
            yield return null;
        }
    }

    public void CheckAndStopCoroutines()
    {
        if (isDead)
        {
            StopAllCoroutines();
            Debug.Log($"{gameObject.name} 的所有协程已停止（因为 isDead = true）");
        }
    }

    void Update()
    {
        if (!isDead && !isInDialogue) FacePlayer();
        if (isDead) StopAllCoroutines();
        CheckGround();

        if (isGrounded)
        {
            animator.SetBool("IsJumping", false);
        }
        else
        {
            animator.SetBool("IsJumping", true);
        }

    }
    void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(0.5f, 1.7f), 0f, groundLayer);
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded)
        {
            animator.SetBool("IsJumping", false);
        }
    }
    private IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(0.5f);
        if (isInDialogue || isDead) yield break;

        while (!isDead)
        {
            yield return StartCoroutine(Action1_FireStormBurst());
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(Action2_JumpAndSmallFireStorm());
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(Action3_FlamePillars());
        }
    }
    public IEnumerator ChooseAction() 
    {
        yield return new WaitForSeconds(0.5f); 
        yield break; 
    }
    // -------------------
    // 动作1：三连 FireStorm
    // -------------------
    private IEnumerator Action1_FireStormBurst()
    {
        PlayRandomVoice(attackVoices);
        animator.SetBool("IsAttacking", true);

        int jumpIndex = Random.Range(0, 3); // 提前决定哪一次会跳跃

        for (int i = 0; i < 3; i++)
        {
            if (i == jumpIndex) // 只有这一发会跳跃
            {
                rb.velocity = new Vector2(0, jumpForce);
                animator.SetBool("IsJumping", true);
                yield return new WaitForSeconds(0.2f);
                animator.SetBool("IsJumping", false);
            }
            audioSource.PlayOneShot(fireStormSound);
            // ✅ 实例化火球
            GameObject firestorm = Instantiate(firestormPrefab, firePoint.position, Quaternion.identity);

            // ✅ 设置水平方向（根据 Boss 面向）
            EnemyFireStorm firestormScript = firestorm.GetComponent<EnemyFireStorm>();
            if (firestormScript != null)
            {
                Vector2 dir = transform.localScale.x < 0 ? Vector2.right : Vector2.left;
                firestormScript.SetDirection(dir);
            }

            yield return new WaitForSeconds(0.9f);
        }


        animator.SetBool("IsAttacking", false);
    }



    // -------------------
    // 动作2：跳向玩家并发射小火球
    // -------------------
    private IEnumerator Action2_JumpAndSmallFireStorm()
    {
        animator.SetBool("IsJumping", true);
        PlayRandomVoice(jumpVoices);

        // 向玩家跳跃
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * moveSpeed, jumpForce * 1.2f);

        yield return new WaitForSeconds(0.3f);
        audioSource.PlayOneShot(fireStormSound);
        // 在半空发射小火球
        GameObject small = Instantiate(smallFirestormPrefab, headPoint.position, Quaternion.identity);
        PlayRandomVoice(attackVoices);

        // ⏳ 等待落地
        yield return new WaitUntil(() => isGrounded);

        // ✅ 落地后清除水平速度
        rb.velocity = new Vector2(0f, rb.velocity.y);

        yield return new WaitForSeconds(1f);
    }


    // -------------------
    // 动作3：火柱喷发
    // -------------------
    private IEnumerator Action3_FlamePillars()
    {
        animator.Play("SpecialAttack");
        PlayRandomVoice(attackVoices);
        float pillarSpacing = 4f; // 火柱间距
        int count = 4;

        int dir = transform.localScale.x < 0 ? 1 : -1;

        // 火柱贴地
        float groundY = groundCheck != null ? groundCheck.position.y : transform.position.y;

        // 先计算所有火柱X坐标
        float[] pillarXs = new float[count];
        for (int i = 1; i <= count; i++)
        {
            pillarXs[i - 1] = transform.position.x + pillarSpacing * i * dir;
        }

        // 检查是否所有火柱都不在战场范围
        bool allOutOfBounds = true;
        foreach (float x in pillarXs)
        {
            if (x >= 201f && x <= 220f)
            {
                allOutOfBounds = false;
                break;
            }
        }

        // 如果全部超出边界，就反向
        if (allOutOfBounds)
        {
            dir *= -1;
            for (int i = 0; i < count; i++)
            {
                pillarXs[i] = transform.position.x + pillarSpacing * (i + 1) * dir;
            }
        }

        // 实例化火柱
        for (int i = 0; i < count; i++)
        {
            audioSource.PlayOneShot(fireStormSound);
            Vector3 pos = new Vector3(pillarXs[i], groundY - 0.4f, 0f);
            Instantiate(flamePillarPrefab, pos, Quaternion.identity);
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(4f);
        animator.Play("Idle");
    }




    // -----------------------------------
    // 其它通用方法（面向玩家、语音等）
    // -----------------------------------
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

    public void PlayRandomVoice(AudioClip[] voices)
    {
        if (voices == null || voices.Length == 0 || Random.value > voiceProbability) return;
        AudioClip clip = voices[Random.Range(0, voices.Length)];
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    public void SetDialogueState(bool state)
    {
        isInDialogue = state;
        if (!isInDialogue) StartCoroutine(BehaviorLoop());
    }

    void TakeDamage(Vector2 enemyPosition)
    {
        if (isHurt) return;

        isHurt = true;
        invincibilityTimer = invincibilityTime;
        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityFlash(invincibilityTime));
        }
        if (damageEffect != null)
        {
            damageEffect.PlayDamageEffects();
        }
        // 播放受伤音效和语音
        if (damageSound != null)
            audioSource.PlayOneShot(damageSound);
        if (hurtVoices.Length > 0 && Random.value <= hurtVoiceProbability)
        {
            PlayRandomVoice(hurtVoices);
        }


        StartCoroutine(ResetInvincibility());
    }

    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(invincibilityTime);
        isHurt = false;
    }

    IEnumerator InvincibilityFlash(float duration)
    {
        float timer = 0f;
        int state = 0;

        while (timer < duration)
        {
            switch (state % 3)
            {
                case 0: // 正常显示
                    spriteRenderer.enabled = true;
                    overlayRenderer.enabled = false;
                    break;
                case 1: // 显示受伤叠加
                    spriteRenderer.enabled = false;
                    overlayRenderer.enabled = true;
                    break;
                case 2: // 隐藏角色
                    spriteRenderer.enabled = false;
                    overlayRenderer.enabled = false;
                    break;
            }

            state++;
            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        // 恢复正常
        spriteRenderer.enabled = true;
        overlayRenderer.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return; // 确保碰撞体存在

        // 处理玩家碰撞
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            EnemyFireStorm fire = collision.GetComponent<EnemyFireStorm>();

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                if (bullet != null)
                {
                    TakeDamage(bullet.transform.position);
                    bullet.DestroyBullet();
                }
                if (fire != null)
                {
                    TakeDamage(fire.transform.position);
                    fire.DestroyBullet();
                }
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("ChargeBullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("RollingCutter"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                return;
            }

            // 尝试获取 RollingCutter 组件
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null)
            {
                bossHealth.TakeDamage(1); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(2); // shockWave 的伤害
                TakeDamage(shockWave.transform.position);
                return;
            }

            Debug.LogWarning("RollingCutter 标签命中但没有匹配的脚本组件");
        }
        if (collision.CompareTag("SuperArm"))
        {
            // 尝试获取四种可能的脚本
            SuperArm1 arm1 = collision.GetComponent<SuperArm1>();
            SuperArm2 arm2 = collision.GetComponent<SuperArm2>();
            SuperArmFoot foot = collision.GetComponent<SuperArmFoot>();
            GStone gstone = collision.GetComponent<GStone>();

            // 获取BossHealthSystem
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            Vector3 hitPos = collision.transform.position;

            if (arm1 != null || foot != null)
            {
                // SuperArm1 和 SuperArmFoot 处理同样伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(2);
                    TakeDamage(hitPos);
                }
                else
                {
                    Debug.Log("Boss 处于无敌状态，SuperArm1/SuperArmFoot 攻击无效");
                }
            }
            else if (arm2 != null)
            {
                // SuperArm2 忽略无敌，造成伤害，适合连击
                bossHealth.TakeDamage(2, ignoreInvincibility: true);
                TakeDamage(hitPos);
            }
            else if (gstone != null)
            {
                // GStone 造成较低伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(hitPos);
                }
                else
                {
                    Debug.Log("Boss 处于无敌状态，GStone 攻击无效");
                }
            }
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow == null) return;

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害
                bossHealth.TakeDamage(4);
                TakeDamage(iceArrow.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 获取 Boss 生命系统
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 FireStorm
            FireStorm fire = collision.GetComponent<FireStorm>();

            // 护盾型 FireStorm
            FireStormShield shield = collision.GetComponent<FireStormShield>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，FireStorm 穿过");
                return;
            }

            // 飞行型 FireStorm：造成伤害并销毁
            if (fire != null && shield == null)
            {
                bossHealth.TakeDamage(1);
                TakeDamage(fire.transform.position);
                fire.DestroyBullet();
                return;
            }

            // 护盾型 FireStorm：有攻击冷却，不销毁
            if (shield != null)
            {
                // 检查冷却
                if (Time.time >= shield.lastDamageTime + shield.damageCooldown)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(shield.transform.position);
                    shield.lastDamageTime = Time.time;
                }
                return;
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            // 先获取子弹组件
            HyperBombExplosion bomb = collision.GetComponent<HyperBombExplosion>();
            if (bomb == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bomb.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 ThunderBeam
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();

            // 小Beam
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，ThunderBeam 穿过");
                return;
            }

            // 飞行型 ThunderBeam：造成伤害并销毁
            if (thunder != null && smallThunder == null)
            {
                bossHealth.TakeDamage(3);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(2); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
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
        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            EnemyFireStorm fire = collision.GetComponent<EnemyFireStorm>();

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                if (bullet != null)
                {
                    TakeDamage(bullet.transform.position);
                    bullet.DestroyBullet();
                }
                if (fire != null)
                {
                    TakeDamage(fire.transform.position);
                    fire.DestroyBullet();
                }
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("ChargeBullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
            }
            else
            {

            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("RollingCutter"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            if (isHurt)
            {
                return;
            }

            // 尝试获取 RollingCutter 组件
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null)
            {
                bossHealth.TakeDamage(1); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(2); // shockWave 的伤害
                TakeDamage(shockWave.transform.position);
                return;
            }

            Debug.LogWarning("RollingCutter 标签命中但没有匹配的脚本组件");
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow == null) return;

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害
                bossHealth.TakeDamage(4);
                TakeDamage(iceArrow.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 获取 Boss 生命系统
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 FireStorm
            FireStorm fire = collision.GetComponent<FireStorm>();

            // 护盾型 FireStorm
            FireStormShield shield = collision.GetComponent<FireStormShield>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，FireStorm 穿过");
                return;
            }

            // 飞行型 FireStorm：造成伤害并销毁
            if (fire != null && shield == null)
            {
                bossHealth.TakeDamage(1);
                TakeDamage(fire.transform.position);
                fire.DestroyBullet();
                return;
            }

            // 护盾型 FireStorm：有攻击冷却，不销毁
            if (shield != null)
            {
                // 检查冷却
                if (Time.time >= shield.lastDamageTime + shield.damageCooldown)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(shield.transform.position);
                    shield.lastDamageTime = Time.time;
                }
                return;
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            // 先获取子弹组件
            HyperBombExplosion bomb = collision.GetComponent<HyperBombExplosion>();
            if (bomb == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bomb.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 ThunderBeam
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();

            // 小Beam
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，ThunderBeam 穿过");
                return;
            }

            // 飞行型 ThunderBeam：造成伤害并销毁
            if (thunder != null && smallThunder == null)
            {
                bossHealth.TakeDamage(3);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(3); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
        }
    }
}
