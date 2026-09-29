using UnityEngine;
using System.Collections;

public class IceBlock : MonoBehaviour
{
    [Header("碎裂设置")]
    public Sprite crackedSprite;
    public GameObject iceFragment1Prefab;
    public GameObject iceFragment2Prefab;
    public float firstCrackDelay = 2f;
    public float finalBreakDelay = 2f;

    private bool hitCooldown = false;
    private float hitCooldownTime = 0.05f;

    [Header("音效")]
    public AudioClip crackSfx;
    public AudioClip breakSfx;
    private AudioSource audioSource;

    private SpriteRenderer spriteRenderer;
    private Coroutine breakCoroutine;
    private bool playerInside = false;

    public bool isCracked = false;
    private bool isBroken = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 检查玩家选择
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Default");

        if (selectedCharacter == "Fireman")
        {
            firstCrackDelay = 0.5f;
            finalBreakDelay = 0.5f;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 已经碎裂，直接忽略后续所有碰撞
        if (isBroken)
            return;

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

            if (bullet != null)
                bullet.DestroyBullet();

            DamageIce();
        }
        else if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();

            if (bullet != null)
                bullet.DestroyBullet();

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

            if (iceArrow != null)
                iceArrow.DestroyBullet();

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
                thunder.DestroyOnEnemyHit();

            if (smallThunder != null)
                smallThunder.DestroyOnEnemyHit();

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
        if (isBroken)
            return;

        if (hitCooldown)
            return;

        StartCoroutine(HitCooldown());

        if (!isCracked)
        {
            Crack();
        }
        else
        {
            BreakIce();
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

        if (!playerInside)
            yield break;

        if (!isCracked)
        {
            Crack();
        }

        // 第二步完全碎裂
        yield return new WaitForSeconds(finalBreakDelay);

        if (!playerInside)
            yield break;

        BreakIce();
    }

    /// <summary>
    /// 裂开阶段
    /// </summary>
    private void Crack()
    {
        if (isBroken)
            return;

        isCracked = true;

        if (crackedSprite != null)
            spriteRenderer.sprite = crackedSprite;

        if (crackSfx != null && audioSource != null)
            audioSource.PlayOneShot(crackSfx);
    }

    /// <summary>
    /// 完全碎裂
    /// </summary>
    private void BreakIce()
    {
        if (isBroken)
            return;

        // =========================================================
        // 先标记已经碎裂
        // 防止这一帧后续还有碰撞事件继续调用 DamageIce()
        // =========================================================
        isBroken = true;
        playerInside = false;

        // 停止玩家进入触发后的计时协程
        if (breakCoroutine != null)
        {
            StopCoroutine(breakCoroutine);
            breakCoroutine = null;
        }

        // =========================================================
        // 播放碎裂音效
        // =========================================================
        if (breakSfx != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSfx);
        }

        // =========================================================
        // 生成碎片
        // =========================================================
        Vector3 pos = transform.position;

        if (iceFragment1Prefab != null)
        {
            GameObject p1 = Instantiate(
                iceFragment1Prefab,
                pos,
                Quaternion.identity
            );

            Rigidbody2D rb1 = p1.GetComponent<Rigidbody2D>();

            if (rb1 == null)
                rb1 = p1.AddComponent<Rigidbody2D>();

            rb1.gravityScale = 1f;
            rb1.velocity = new Vector2(
                Random.Range(-2f, -1f),
                Random.Range(1f, 2f)
            );

            Destroy(p1, 3f);
        }

        if (iceFragment2Prefab != null)
        {
            GameObject p2 = Instantiate(
                iceFragment2Prefab,
                pos,
                Quaternion.identity
            );

            Rigidbody2D rb2 = p2.GetComponent<Rigidbody2D>();

            if (rb2 == null)
                rb2 = p2.AddComponent<Rigidbody2D>();

            rb2.gravityScale = 1f;
            rb2.velocity = new Vector2(
                Random.Range(1f, 2f),
                Random.Range(1f, 2f)
            );

            Destroy(p2, 3f);
        }

        // =========================================================
        // 关键修复
        // 碎裂瞬间关闭所有 Collider2D
        // 包括子物体上的 Collider2D
        // =========================================================
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        // =========================================================
        // 关闭自身及子物体的所有 Renderer
        // 防止还有残留显示
        // =========================================================
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // =========================================================
        // 停止自身 Rigidbody2D 的物理作用
        // =========================================================
        Rigidbody2D[] rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);

        foreach (Rigidbody2D rigidbody in rigidbodies)
        {
            rigidbody.simulated = false;
        }

        // =========================================================
        // 修改 Tag / Layer
        // =========================================================
        gameObject.tag = "Untagged";
        gameObject.layer = 0;

        // =========================================================
        // 最后销毁物体
        // 不再等待 1 秒
        // =========================================================
        Destroy(gameObject);
    }
}