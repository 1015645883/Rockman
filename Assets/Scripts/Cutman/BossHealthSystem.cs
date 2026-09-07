using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement; // ✅ 需要加这个才能获取场景名

public interface IBossController
{
    bool IsHurt { get; }                // 是否无敌
    AudioClip[] DeadVoices { get; }      // 死亡语音数组
    IEnumerator ChooseAction(); // 统一要求Boss都有ChooseAction行为
    void SetDialogueState(bool state);

}

public class BossHealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 28;
    public int currentHealth;

    [Header("特效预制体")]
    public GameObject explosionPrefab;   //  爆炸预制体
    public GameObject damageTextPrefab;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public delegate void HealthChanged(int currentHealth, int maxHealth);
    public event HealthChanged OnHealthChanged;
    public event Action OnBossDeath;
    public event Action OnBossDeathStart;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;
    private AudioSource audioSource;
    public AudioClip WillDead; // 爆炸音效
    public AudioClip Dead; // 爆炸音效
    private bool remainsCollected = false; // 标记残骸是否已经被收集
    public float collectDistance = 0.4f; // 玩家接近残骸多少距离算重合
    private IBossController bossController;

    private void Awake()
    {
        bossController = GetComponent<IBossController>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHealthInstant(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount, bool ignoreInvincibility = false)
    {
        // 如果不忽略无敌，才判断 Boss 是否处于无敌
        if (!ignoreInvincibility && bossController != null && bossController.IsHurt)
        {
            Debug.Log($"{gameObject.name} 处于无敌状态，伤害无效");
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 生成伤害数字
        if (damageTextPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 1.5f, 0); // 敌人头顶位置
            GameObject popup = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(damageAmount);
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(DeathSequence());
        }
    }


    private IEnumerator DeathSequence()
    {
        Debug.Log($"{gameObject.name} 即将死亡演出");
        OnBossDeathStart?.Invoke();
        // ⚡️ 强制播放一次 Hurt 动画
        audioSource.PlayOneShot(WillDead);
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Hurt", 0, 0f); // 播放 Hurt 动画，从头开始
            animator.Play("Hurt", 1, 0f); // 播放 Hurt 动画，从头开始
        }
        // ⚡️ 在死亡演出一开始就标记 Boss 已死亡并冻结它
        PauseBossOnDeath(); // ✅ 改成调用新方法
        // 玩家冻结
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null) player.canMove = false;
        // 停止BGM
        BossTrigger.StopBGM();
        // 全局时间暂停（不影响 WaitForSecondsRealtime）
        Time.timeScale = 0f;

        // 停顿 1.5 秒
        yield return new WaitForSecondsRealtime(1.2f);
        // 关闭子物体
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
        // 禁用碰撞器和刚体
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        // 恢复时间流动
        Time.timeScale = 1f;
        // 解冻玩家
        if (player != null) player.canMove = true;
        // 播放死亡语音
        AudioSource voiceSource = null;
        if (bossController != null && bossController.DeadVoices != null && bossController.DeadVoices.Length > 0)
        {
            AudioClip randomDeadVoice = bossController.DeadVoices[UnityEngine.Random.Range(0, bossController.DeadVoices.Length)];
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.clip = randomDeadVoice;
            voiceSource.playOnAwake = false;
            voiceSource.Play();
        }

        // 循环生成爆炸特效（持续 2 秒左右）
        float explosionDuration = 2f;
        float timer = 0f;
        while (timer < explosionDuration)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.5f; // 半径 1.5
            Vector3 spawnPos = transform.position + (Vector3)offset;
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
            }
            yield return new WaitForSeconds(0.2f);
            timer += 0.2f;
        }

        // 继续执行原 Die() 逻辑
        FinalizeDeath(voiceSource);

        // 解冻玩家
        if (player != null) player.canMove = true;
    }
    /// <summary>
    /// 在 Boss 死亡时调用：标记 isDead，停协程，冻结动画和速度
    /// </summary>
    private void PauseBossOnDeath() // ✅ 新方法
    {
        if (bossController is MonoBehaviour bossMono)
        {
            // 标记 isDead = true
            var bossType = bossMono.GetType();
            var isDeadField = bossType.GetField("isDead");
            if (isDeadField != null)
            {
                isDeadField.SetValue(bossMono, true);
            }

            // 停止 Boss 自身的所有协程
            bossMono.StopAllCoroutines();
            spriteRenderer.enabled = true;
            overlayRenderer.enabled = false;
        }

        // 冻结动画
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 0f; // 暂停动画
        }

        // 清零速度
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true; // 避免重力再作用
        }
    }

    private void FinalizeDeath(AudioSource voiceSource)
    {
        Debug.Log($"{gameObject.name} 已死亡");

        OnHealthChanged?.Invoke(0, maxHealth);
        OnBossDeath?.Invoke();

        // 播放死亡音效
        audioSource.PlayOneShot(Dead);
        // 动画触发
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetTrigger("Explosion");
        }

        // 等 Explosion 播放完再播放 Dead 动画
        StartCoroutine(PlayDeadAnimation(rb, animator, voiceSource));
    }

    private IEnumerator PlayDeadAnimation(Rigidbody2D rb, Animator animator, AudioSource voiceSource)
    {
        // Explosion 动画假设 2 秒
        yield return new WaitForSeconds(2f);

        // 新增判断：如果 Boss 和 玩家是同一个角色
        if (ShouldSkipRemains())
        {
            Debug.Log("玩家角色与Boss相同 → 不掉落残骸，直接通关。");

            // 记录Boss已被击败
            PlayerPrefs.SetInt(gameObject.name + "Defeated", 1);
            PlayerPrefs.Save();

            // 销毁Boss对象
            Destroy(gameObject);

            // 触发通关逻辑
            FindObjectOfType<LevelClearManager>()?.OnBossDefeated();

            yield break; // 结束协程
        }

        // 播放 Dead 动画
        if (animator != null)
            animator.Play("Dead", 0, 0f); // Base Layer
            animator.Play("Dead", 1, 0f); // Top Layer


        // 恢复刚体，让残骸掉落
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.simulated = true;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // 残骸保护时间（1.5秒）——玩家暂时不能收集
        float protectionTime = 1.5f;
        float timer = 0f;

        while (!remainsCollected)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(player.transform.position, transform.position);

                // 只有保护时间结束后才允许收集
                if (timer >= protectionTime && distance <= collectDistance)
                {
                    remainsCollected = true;
                    OnPlayerCollectBossRemains();
                }
            }

            timer += Time.deltaTime;
            yield return null; // 每帧检查
        }
    }

    private bool ShouldSkipRemains()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player == null) return false;

        // ⚡️ 特殊情况：WilyStage1 → 永远跳过残骸
        if (currentScene == "WilyStage1")
        {
            return true;
        }

        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Rockman");

        // 建立 场景 ↔ 角色 的映射表
        switch (currentScene)
        {
            case "CutmanStage":
                return selectedCharacter == "Cutman";
            case "GutsmanStage":
                return selectedCharacter == "Gutsman";
            case "IcemanStage":
                return selectedCharacter == "Iceman";
            case "BombmanStage":
                return selectedCharacter == "Bombman";
            case "FiremanStage":
                return selectedCharacter == "Fireman";
            case "ElecmanStage":
                return selectedCharacter == "Elecman";
            default:
                return false;
        }
    }


    public void OnPlayerCollectBossRemains()
    {
        PlayerPrefs.SetInt(gameObject.name + "Defeated", 1);
        PlayerPrefs.Save();

        FindObjectOfType<LevelClearManager>()?.OnBossDefeated();

        // 可选择隐藏残骸
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // 或者直接销毁
        // Destroy(gameObject);
    }

}

