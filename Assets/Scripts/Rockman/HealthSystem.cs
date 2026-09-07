using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 28;
    public int currentHealth;
    public bool isInvincible = false;
    private float invincibilityDuration = 1.5f;
    private bool isDead = false;

    [Header("复活点设置")]
    [SerializeField] private Vector3 initialRespawnPoint = new Vector3(0, 0, 0); // ✅ Inspector 可控初始复活点

    // 公共访问属性
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public delegate void HealthChanged(int currentHealth, int maxHealth);
    public event HealthChanged OnHealthChanged;

    private AudioSource audioSource;
    public AudioSource bgmSource;
    public AudioClip Dead;
    public GameObject damageTextPrefab;
    public AudioClip[] reviveVoices;
    public ScreenFader screenFader;

    private SpecialLevelManager specialLevelManager;

    private void Awake()
    {
        // 尝试找到特殊关卡的管理器
        specialLevelManager = FindObjectOfType<SpecialLevelManager>();
    }

    private void Start()
    {
        // ✅ 初始化复活点：如果之前没有存档点，就用 Inspector 中设定的默认点
        if (Checkpoint.lastCheckpointPosition == Vector3.zero)
        {
            Checkpoint.lastCheckpointPosition = initialRespawnPoint;
        }

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 查找 EventSystem -> ScreenFader
        GameObject eventSystemObj = GameObject.Find("EventSystem");
        if (eventSystemObj != null)
        {
            screenFader = eventSystemObj.GetComponent<ScreenFader>();
            if (screenFader == null)
                Debug.LogWarning("[HealthSystem] 在 EventSystem 对象上未找到 ScreenFader 组件！");
        }

        // 查找 BGMPlayer -> AudioSource
        GameObject bgmObj = GameObject.Find("BGMPlayer");
        if (bgmObj != null)
        {
            bgmSource = bgmObj.GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                Debug.LogWarning("[HealthSystem] 找到 BGMPlayer 但没有 AudioSource 组件！");
            }
        }
    }


    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            StartRespawn();
        }
    }

    public void StartRespawn()
    {
        StartCoroutine(RespawnAtCheckpoint());
    }

    private IEnumerator RespawnAtCheckpoint()
    {
        isDead = false;
        // ✅ 黑屏淡出
        if (screenFader != null)
            yield return screenFader.FadeIn();

        // ✅ 确定复活位置（支持特殊关卡）
        initialRespawnPoint = Checkpoint.lastCheckpointPosition;
        SpecialLevelManager specialLevelManager = FindObjectOfType<SpecialLevelManager>();
        if (specialLevelManager != null)
        {
            if (specialLevelManager.TryGetRespawnPoint(gameObject, out Vector3 specialPoint))
            {
                initialRespawnPoint = specialPoint;
            }
        }

        // ✅ 移动到复活点
        transform.position = initialRespawnPoint;


        // ✅ 重置 Boss 状态
        if (BossTriggerHasInstance())
        {
            BossTrigger.instance.ResetBossToInitialState();
        }

        // ✅ 重置电梯（在黑屏时执行，玩家看不到）
        foreach (var lift in FindObjectsOfType<LiftController>())
        {
            lift.ResetLift();
        }

        yield return new WaitForSeconds(0.5f); // 可选：增加一点节奏感

        BombManager bombManager = FindObjectOfType<BombManager>();
        if (bombManager != null)
        {
            bombManager.ResetBombs();
        }

        // ✅ 恢复血量
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // ✅ 启用碰撞器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // ✅ 启用刚体
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
        }
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = false;
        // ✅ 确保角色可见
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.SetLayerWeight(1, 0);
            animator.Play("Idle", 0, 0f);
        }
        // ✅ 黑屏淡入
        if (screenFader != null)
            yield return screenFader.FadeOut();
        // ✅ 播放复活语音
        if (reviveVoices != null && reviveVoices.Length > 0)
        {
            AudioClip reviveClip = reviveVoices[Random.Range(0, reviveVoices.Length)];
            AudioSource reviveSource = gameObject.AddComponent<AudioSource>();
            reviveSource.clip = reviveClip;
            reviveSource.playOnAwake = false;
            reviveSource.Play();
        }
        // ✅ 播放 Reborn 动画并监听输入
        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.SetLayerWeight(1, 0);
            animator.Play("Reborn", 0, 0f);
            yield return new WaitForSeconds(0.5f);
            animator.Play("Idle", 0, 0f);
            animator.SetTrigger("StartMoving");
        }
        // ✅ 启用控制脚本
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.isDead = false; 
        }

        Debug.Log("玩家已复活");
    }


    private bool BossTriggerHasInstance()
    {
        return BossTrigger.instance != null;
    }


    public void TakeDamage(int damageAmount)
    {
        // 获取 PlayerMovement 脚本引用
        PlayerMovement movement = GameObject.FindWithTag("Player")?.GetComponent<PlayerMovement>();

        // 如果 movement 为 null，则仍然允许受伤（安全性考虑）
        if (movement != null)
        {
            // ⚠️ 逻辑顺序：先判断是否是 C 冲刺中（优先级最高）
            if (movement.currentShapeKey == "C" && movement.isDashing)
            {
                return; // C 冲刺中不受伤
            }

        }

        // 然后判断全局无敌状态
        if (isInvincible)
        {
            return;
        }

        // 这里判断是否有 BluesSpecial 组件
        BluesSpecial bluesSpecial = GetComponent<BluesSpecial>();
        if (bluesSpecial != null)
        {
            damageAmount = Mathf.CeilToInt(damageAmount * 1.5f);  // 伤害加深50%
        }

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        //生成伤害数字
        if (damageTextPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 1.5f, 0); // 敌人头顶位置
            GameObject popup = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(damageAmount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine()); // 启动无敌协程
        }
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("玩家已死亡");

        // 触发死亡事件（如果有其他脚本需要监听）
        OnHealthChanged?.Invoke(0, maxHealth);

        // 禁止操作
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.isDead = true;
        }

        // 禁用碰撞器和物理效果
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        

        // 播放死亡动画（如果有）
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = Dead;
        audioSource.playOnAwake = false;
        audioSource.Play();
        Animator animator = GetComponent<Animator>();
        if (playerMovement.deadVoices.Length > 0)
        {
            AudioClip randomDeadVoice = playerMovement.deadVoices[Random.Range(0, playerMovement.deadVoices.Length)];
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = randomDeadVoice;
            audioSource.playOnAwake = false;
            audioSource.Play();
        }
        if (animator != null)
        {
            // 先清空所有状态
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsHurt", false);
            animator.SetBool("Dash", false);
            animator.SetLayerWeight(1, 0);
            // 设置死亡标志（推荐在 Animator 设置 IsDead 或直接用 Explosion Trigger）
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");

        }

        // 延迟销毁对象（给动画时间播放）
        StartCoroutine(WaitForRespawn());
    }
    private IEnumerator WaitForRespawn()
    {
        Animator animator = GetComponent<Animator>();
        // 等待 2.1 秒，播放死亡动画等
        yield return new WaitForSeconds(2.1f);
        if (animator != null)
        {
            animator.enabled = false; // 停止 Animator 组件，使动画不再播放
        }
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false; // 禁用 SpriteRenderer 使角色不可见
        }
        // 2.1 秒后允许按 R 键重生
        isDead = true; // 标记死亡
    }
}
