using UnityEngine;
using System.Collections;

public class BombmanController : MonoBehaviour, IBossController
{
    [Header("基础组件")]
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;
    private AudioSource audioSource;
    private PlayerDamageEffect damageEffect;
    private BossHealthSystem healthSystem;

    [Header("玩家与检测")]
    public Transform player;
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Boss属性")]
    public int health = 28;
    public float moveSpeed = 4f;
    public float jumpForce = 11f;
    public int damage = 2;

    [Header("受伤与无敌")]
    private bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    public float invincibilityTime = 1.5f;
    private float invincibilityTimer;
    public float flashInterval = 0.1f;
    private bool isSafeHeight => transform.position.y < 34.5f;

    [Header("行为控制")]
    public bool isInDialogue = true;

    [Header("音效与语音")]
    public AudioClip damageSound;
    public AudioClip jumpSound;
    public AudioClip bombThrowSound;
    public AudioClip[] jumpVoices;
    public AudioClip[] attackVoices;
    public AudioClip[] hurtVoices;
    public AudioClip[] deadVoices;
    public AudioClip[] DeadVoices => deadVoices;

    [Range(0, 1)] public float voiceProbability = 0.7f;
    [Range(0, 1)] public float hurtVoiceProbability = 0.2f;

    [Header("攻击预制体")]
    public GameObject hyperBombPrefab;
    public Transform hyperBombSpawnPoint;
    public GameObject bigHyperBombPrefab;
    public Transform bigHyperBombSpawnPoint;

    private int behaviorStage = 0; // 当前动作组：0=第一组，1=第二组，2=第三组
    private int[] behaviorSequence = { 0, 1, 0, 2 }; // 0=第一组,1=第二组,2=第三组
    private int sequenceIndex = 0;

    private float lastDamageTime = 0f;
    private float damageCooldown = 1.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;
        audioSource = GetComponent<AudioSource>();
        damageEffect = GetComponent<PlayerDamageEffect>();
        healthSystem = GetComponent<BossHealthSystem>();

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

    void Update()
    {
        if (!isDead && !isInDialogue) FacePlayer();
        CheckGround();

        if (isDead) StopAllCoroutines();
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

    private IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(0.5f);
        if (isInDialogue || isDead) yield break;

        while (!isDead)
        {
            behaviorStage = behaviorSequence[sequenceIndex];

            switch (behaviorStage)
            {
                case 0:
                    for (int i = 0; i < 4; i++)
                    {
                        yield return StartCoroutine(FirstGroupAction());
                        yield return new WaitForSeconds(0.5f);
                    }
                    break;
                case 1:
                    yield return StartCoroutine(SecondGroupAction());
                    break;
                case 2:
                    yield return StartCoroutine(ThirdGroupAction());
                    break;
            }

            // 更新下一个动作
            sequenceIndex = (sequenceIndex + 1) % behaviorSequence.Length;
        }
    }

    public IEnumerator ChooseAction()
    {
        yield return new WaitForSeconds(0.5f);
        yield break;
    }

    private IEnumerator FirstGroupAction()
    {
        if (player == null) yield break;

        FacePlayer();

        int jumpType = Random.Range(0, 3); // 0=原地 1=前跳 2=后跳

        // 重置动画
        animator.SetBool("IsFrontJumping", false);
        animator.SetBool("IsBackJumping", false);
        animator.SetBool("IsAttacking", false);

        Vector2 jumpDir = Vector2.zero;

        switch (jumpType)
        {
            case 0: // 原地攻击
                animator.SetBool("IsAttacking", true);
                // **原地攻击必丢炸弹**
                yield return StartCoroutine(ThrowHyperBombCoroutine());
                break;

            case 1: // 前跳
                animator.SetBool("IsFrontJumping", true);
                jumpDir = Vector2.right * (transform.localScale.x > 0 ? -1 : 1);
                break;

            case 2: // 后跳
                animator.SetBool("IsBackJumping", true);
                jumpDir = Vector2.right * (transform.localScale.x > 0 ? 1 : -1);
                break;
        }

        // 如果是跳跃动作
        if (jumpDir != Vector2.zero)
        {
            float randomX = Random.Range(0.85f, 1.15f);
            rb.velocity = new Vector2(jumpDir.x * moveSpeed * randomX, jumpForce);

            PlayRandomVoice(jumpVoices);
            if (jumpSound != null) audioSource.PlayOneShot(jumpSound);

            // **跳跃过程中才有概率丢炸弹**
            yield return new WaitForSeconds(0.2f);
            if (Random.value < 0.7f)
            {
                yield return StartCoroutine(ThrowHyperBombCoroutine());
            }
        }

        // 等待落地
        yield return new WaitUntil(() => isGrounded);
        rb.velocity = new Vector2(0f, rb.velocity.y);

        // 重置动画状态
        animator.SetBool("IsFrontJumping", false);
        animator.SetBool("IsBackJumping", false);
        animator.SetBool("IsAttacking", false);
    }


    private IEnumerator ThrowHyperBombCoroutine()
    {
        animator.SetBool("IsAttacking", true);
        PlayRandomVoice(attackVoices);
        yield return new WaitForSeconds(0.2f);
        if (hyperBombPrefab != null && hyperBombSpawnPoint != null && player != null)
        {
            Vector3 spawnPos = hyperBombSpawnPoint.position + Vector3.up * 0.2f;
            GameObject bomb = Instantiate(hyperBombPrefab, spawnPos, Quaternion.identity);
            Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
            if (bombRb != null)
            {
                // 水平方向
                float horizontalDir = player.position.x > spawnPos.x ? 1f : -1f;

                // 根据距离缩放水平力
                float distanceX = Mathf.Abs(player.position.x - spawnPos.x);
                float maxForceX = 7f;
                float forceX = distanceX >= 12f ? maxForceX : maxForceX * (distanceX / 12f);

                float verticalForce = 7f;
                Vector2 force = new Vector2(horizontalDir * forceX, verticalForce);
                bombRb.AddForce(force, ForceMode2D.Impulse);
            }

            if (bombThrowSound != null) audioSource.PlayOneShot(bombThrowSound);
        }

        yield return new WaitForSeconds(0.3f);
        animator.SetBool("IsAttacking", false);
    }



    private IEnumerator SecondGroupAction()
    {
        // 1. 起跳
        animator.SetBool("IsFrontJumping", true);
        PlayRandomVoice(jumpVoices);
        rb.velocity = new Vector2(0, jumpForce * 0.5f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // 取消地面碰撞，避免卡住边缘

        // 2. 等待到达 y = 30
        yield return new WaitUntil(() => transform.position.y <= 30f);

        // 3. 悬停在 (原始x, 30)
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        transform.position = new Vector3(transform.position.x, 30f, 0);

        // 4. 投掷炸弹组合
        yield return StartCoroutine(ThrowBomb(new Vector2(257, 35)));
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(ThrowBomb(new Vector2(267, 35)));
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(ThrowBomb(new Vector2(260, 35), new Vector2(264, 35)));
        yield return new WaitForSeconds(1f);

        // 5. 恢复物理，掉回战场
        rb.isKinematic = false;
        rb.velocity = new Vector2(0, 18f);
        yield return new WaitForSeconds(1f);
        if (col != null) col.enabled = true;
        // 6. 等待落地
        yield return new WaitUntil(() => isGrounded);

        animator.SetBool("IsFrontJumping", false);
        animator.Play("Idle");
    }

    private IEnumerator ThrowBomb(params Vector2[] targets)
    {
        PlayRandomVoice(attackVoices);

        foreach (var target in targets)
        {
            if (hyperBombPrefab != null)
            {
                GameObject bomb = Instantiate(hyperBombPrefab, transform.position, Quaternion.identity);

                EnemyHyperBomb bombScript = bomb.GetComponent<EnemyHyperBomb>();
                Collider2D bombCol = bomb.GetComponent<Collider2D>();
                if (bombScript != null) bombScript.canExplode = false;
                if (bombCol != null) bombCol.enabled = false;

                Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
                if (bombRb != null)
                {
                    Vector2 startPos = transform.position;
                    Vector2 targetPos = target;

                    float g = Mathf.Abs(Physics2D.gravity.y * bombRb.gravityScale);

                    // 你可以调这个值，控制炸弹飞多久到达目标
                    float flightTime = 1.2f;

                    float dx = targetPos.x - startPos.x;
                    float dy = targetPos.y - startPos.y;

                    float vx = dx / flightTime;
                    float vy = (dy + 0.5f * g * flightTime * flightTime) / flightTime;

                    Vector2 velocity = new Vector2(vx, vy);
                    bombRb.velocity = velocity;
                }

                if (bombScript != null)
                {
                    StartCoroutine(EnableBombWhenHighEnough(bomb, bombScript, bombCol));
                }
            }
        }

        yield return null;
    }


    private IEnumerator EnableBombWhenHighEnough(GameObject bomb, EnemyHyperBomb bombScript, Collider2D bombCol)
    {
        // 等待炸弹升到 36f
        while (bomb != null && bomb.transform.position.y < 36f)
        {
            yield return null;
        }

        if (bomb != null)
        {
            if (bombScript != null)
                bombScript.canExplode = true;

            if (bombCol != null)
                bombCol.enabled = true; // 恢复碰撞
        }
    }


    private IEnumerator ThirdGroupAction()
    {
        animator.Play("SpecialAttack");
        PlayRandomVoice(attackVoices);
        Rigidbody2D rbBoss = GetComponent<Rigidbody2D>();

        // 临时加大Boss阻力，防止被炸弹推开
        float originalDrag = rbBoss.drag;
        float originalAngularDrag = rbBoss.angularDrag;
        rbBoss.drag = 100f;
        rbBoss.angularDrag = 100f;
        if (bigHyperBombPrefab != null && bigHyperBombSpawnPoint != null && player != null)
        {
            GameObject bigBomb = Instantiate(bigHyperBombPrefab, bigHyperBombSpawnPoint.position, Quaternion.identity);
            BigHyperBomb bigBombScript = bigBomb.GetComponent<BigHyperBomb>();
            Rigidbody2D rbBomb = bigBomb.GetComponent<Rigidbody2D>();

            if (bigBombScript != null)
            {
                bigBombScript.canBounce = false; // 生成时禁用反弹
            }

            if (rbBomb != null)
                rbBomb.isKinematic = true;

            // 等待1秒
            yield return new WaitForSeconds(1f);

            if (rbBomb != null)
            {
                rbBomb.isKinematic = false; // 恢复物理
                Vector2 dir = (player.position - bigHyperBombSpawnPoint.position).normalized;
                Vector2 force = new Vector2(dir.x * 60f, 50f);
                rbBomb.AddForce(force, ForceMode2D.Impulse);
            }

            if (bigBombScript != null)
            {
                bigBombScript.canBounce = true; // 丢出后允许反弹
            }
        }

        yield return new WaitForSeconds(2f);
        animator.Play("Idle");
        yield return new WaitForSeconds(2f);
        // 恢复Boss原本阻力
        rbBoss.drag = originalDrag;
        rbBoss.angularDrag = originalAngularDrag;
    }



    void TakeDamage(Vector2 enemyPosition)
    {
        if (isHurt) return;
        // 如果Y轴低于安全高度，不受伤
        if (isSafeHeight) return;
        isHurt = true;
        invincibilityTimer = invincibilityTime;

        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityFlash(invincibilityTime));
        }

        if (damageEffect != null) damageEffect.PlayDamageEffects();

        if (damageSound != null) audioSource.PlayOneShot(damageSound);
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
                case 0: spriteRenderer.enabled = true; overlayRenderer.enabled = false; break;
                case 1: spriteRenderer.enabled = false; overlayRenderer.enabled = true; break;
                case 2: spriteRenderer.enabled = false; overlayRenderer.enabled = false; break;
            }
            state++;
            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        spriteRenderer.enabled = true;
        overlayRenderer.enabled = false;
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
        // 如果Y轴低于安全高度，不受伤
        if (isSafeHeight) return;
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
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(3); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(4); // shockWave 的伤害
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
                    bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(3, ignoreInvincibility: true);
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
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(4);
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
                    bossHealth.TakeDamage(4);
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
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }
        // 如果Y轴低于安全高度，不受伤
        if (isSafeHeight) return;
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
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(3); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(4); // shockWave 的伤害
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
                bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(4);
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
                    bossHealth.TakeDamage(4);
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

    public void CheckAndStopCoroutines()
    {
        if (isDead) StopAllCoroutines();
    }
}
