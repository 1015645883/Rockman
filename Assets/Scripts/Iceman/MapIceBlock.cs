using UnityEngine;
using System.Collections;

public class IceBlock : MonoBehaviour
{
    [Header("碎裂设置")]
    public Sprite crackedSprite;          // 第一步碎裂显示的Sprite
    public GameObject iceFragment1Prefab; // 碎裂生成的冰块1
    public GameObject iceFragment2Prefab; // 碎裂生成的冰块2
    public float firstCrackDelay = 2f;    // 第一步碎裂延迟
    public float finalBreakDelay = 2f;    // 第二步完全碎裂延迟（从第一步开始计时）
    private bool hitCooldown = false;
    private float hitCooldownTime = 0.05f; // 50ms 避免连击

    [Header("音效")]
    public AudioClip crackSfx;            // 第一步碎裂音效
    public AudioClip breakSfx;            // 完全碎裂音效
    private AudioSource audioSource;

    private SpriteRenderer spriteRenderer;
    private Coroutine breakCoroutine;
    private bool playerInside = false;

    public bool isCracked = false; // 是否进入第一阶段碎裂
    private bool isBroken = false; // 是否已经完全碎裂

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 🔹 检查玩家选择
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Default");
        if (selectedCharacter == "Fireman")
        {
            firstCrackDelay = 0.5f;
            finalBreakDelay = 0.5f;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 玩家靠近 → 启动计时碎裂
        if (collision.CompareTag("Player") && !playerInside)
        {
            playerInside = true;
            breakCoroutine = StartCoroutine(BreakSequence());
        }

        // 各种武器打到冰块
        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
            DamageIce();
        }
        else if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
            DamageIce();
        }
        else if (collision.CompareTag("RollingCutter"))
        {
            DamageIce();
        }
        else if (collision.CompareTag("SuperArm"))
        {
            DamageIce();
        }
        else if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null) iceArrow.DestroyBullet();
            DamageIce();
        }
        else if (collision.CompareTag("HyperBomb"))
        {
            DamageIce();
        }
        else if (collision.CompareTag("FireStorm"))
        {
            DamageIce();
        }
        else if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            if (thunder != null)
            {
                thunder.DestroyOnEnemyHit();
            }
            if (smallThunder != null)
            {
                smallThunder.DestroyOnEnemyHit();
            }
            DamageIce();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = false;
            if (breakCoroutine != null)
            {
                StopCoroutine(breakCoroutine);
                breakCoroutine = null;
            }
        }
    }

    /// <summary>
    /// 处理冰块被武器打碎
    /// </summary>
    private void DamageIce()
    {
        if (isBroken) return;
        if (hitCooldown) return; // 短暂无敌，忽略重复触发
        StartCoroutine(HitCooldown());

        if (!isCracked)
        {
            Crack(); // 进入裂开
        }
        else
        {
            BreakIce(); // 第二次攻击 → 爆碎
        }
    }

    private IEnumerator HitCooldown()
    {
        hitCooldown = true;
        yield return new WaitForSeconds(hitCooldownTime);
        hitCooldown = false;
    }

    private IEnumerator BreakSequence()
    {
        // 第一步碎裂
        yield return new WaitForSeconds(firstCrackDelay);
        if (!playerInside) yield break; // 玩家中途离开，终止

        if (!isCracked)
        {
            Crack();
        }

        // 第二步完全碎裂
        yield return new WaitForSeconds(finalBreakDelay);
        if (!playerInside) yield break; // 玩家中途离开，终止

        BreakIce();
    }

    /// <summary>
    /// 裂开阶段（换 Sprite + 播放音效）
    /// </summary>
    private void Crack()
    {
        isCracked = true;
        if (crackedSprite != null)
            spriteRenderer.sprite = crackedSprite;

        if (crackSfx != null && audioSource != null)
            audioSource.PlayOneShot(crackSfx);
    }

    private void BreakIce()
    {
        if (isBroken) return; // 已经碎裂过就不再执行
        isBroken = true;      // ✅ 标记为完全碎裂

        // 播放碎裂音效
        if (breakSfx != null)
        {
            audioSource.PlayOneShot(breakSfx);
        }

        // 生成碎片
        Vector3 pos = transform.position;

        if (iceFragment1Prefab != null)
        {
            GameObject p1 = Instantiate(iceFragment1Prefab, pos, Quaternion.identity);
            Rigidbody2D rb1 = p1.AddComponent<Rigidbody2D>();
            rb1.gravityScale = 1f;
            rb1.velocity = new Vector2(Random.Range(-2f, -1f), Random.Range(1f, 2f));
            Destroy(p1, 3f);
        }

        if (iceFragment2Prefab != null)
        {
            GameObject p2 = Instantiate(iceFragment2Prefab, pos, Quaternion.identity);
            Rigidbody2D rb2 = p2.AddComponent<Rigidbody2D>();
            rb2.gravityScale = 1f;
            rb2.velocity = new Vector2(Random.Range(1f, 2f), Random.Range(1f, 2f));
            Destroy(p2, 3f);
        }

        // 移除物理与可见部分
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) Destroy(col);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        gameObject.tag = "Untagged";
        gameObject.layer = 0; // Default 层

        Destroy(gameObject, 1f);
    }
}
