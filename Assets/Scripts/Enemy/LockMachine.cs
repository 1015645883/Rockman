using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class LockMachine : MonoBehaviour
{
    [Header("显示屏相关")]
    public SpriteRenderer displayRenderer;   // 子物体显示屏
    public Sprite[] activeSprites;           // 六张循环播放的Sprite
    public Sprite offSprite;                 // 被摧毁后显示的Sprite
    public float frameInterval = 1f;         // 每秒切换一次

    [Header("关卡解锁元素")]
    public SpriteRenderer[] lockRenderers;   // 锁的SpriteRenderer
    public Sprite unlockedSprite;            // 解锁时替换的Sprite
    public GameObject doorObject;            // 门对象
    public AudioClip unlockSound;            // 解锁音效
    public AudioSource sfxSource;            // 播放音效的音源

    [Header("受伤反馈")]
    public AudioClip damageSound;            // 受伤音效
    private AudioSource damageAudioSource;   // 受伤音源
    public SpriteRenderer bodyRenderer;      // LockMachine主体SpriteRenderer

    [Header("生命值设置")]
    public int maxHealth = 10;
    private int currentHealth;

    [Header("效果设置")]
    public GameObject explosionEffect;       // 可选：摧毁特效

    private bool isDestroyed = false;
    private Coroutine displayCoroutine;
    private float lastDamageTime;
    private float damageCooldown = 0.1f;

    void Start()
    {
        currentHealth = maxHealth;

        if (displayRenderer != null && activeSprites.Length > 0)
            displayCoroutine = StartCoroutine(DisplayLoop());
    }

    private IEnumerator DisplayLoop()
    {
        int index = 0;
        while (!isDestroyed)
        {
            displayRenderer.sprite = activeSprites[index];
            index = (index + 1) % activeSprites.Length;
            yield return new WaitForSeconds(frameInterval);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;
        if (Time.time < lastDamageTime + damageCooldown) return;
        lastDamageTime = Time.time;

        // 播放受伤音效
        if (damageAudioSource == null)
        {
            damageAudioSource = gameObject.AddComponent<AudioSource>();
            damageAudioSource.playOnAwake = false;
            damageAudioSource.clip = damageSound;
        }
        if (damageSound != null)
            damageAudioSource.Play();

        // 扣血
        currentHealth -= amount;

        // 受伤闪烁反馈
        if (bodyRenderer != null)
            StartCoroutine(HitFlash());

        // 检查死亡
        if (currentHealth <= 0)
        {
            StartCoroutine(DestroySequence());
        }
    }

    private IEnumerator HitFlash()
    {
        // 统一管理要闪烁的sprite
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        if (bodyRenderer != null) renderers.Add(bodyRenderer);
        if (displayRenderer != null) renderers.Add(displayRenderer);

        // 如果没有任何Renderer，直接退出
        if (renderers.Count == 0) yield break;

        // 闪烁一次
        foreach (var r in renderers)
            r.enabled = false;

        yield return new WaitForSeconds(0.06f);

        foreach (var r in renderers)
            r.enabled = true;
    }


    private IEnumerator DestroySequence()
    {
        if (isDestroyed) yield break;
        isDestroyed = true;

        if (displayCoroutine != null)
            StopCoroutine(displayCoroutine);

        // 显示关闭画面
        if (displayRenderer != null && offSprite != null)
            displayRenderer.sprite = offSprite;

        // ✅ 随机爆炸序列
        float explosionDuration = 2f; // 总持续时间
        float explosionInterval = 0.1f; // 每0.1秒一次爆炸
        float timer = 0f;

        while (timer < explosionDuration)
        {
            if (explosionEffect != null)
            {
                // 随机偏移范围（例如 1.5 单位内）
                Vector2 randomOffset = new Vector2(
                    Random.Range(-4f, 4f),
                    Random.Range(-4f, 4f)
                );

                // 在LockMachine周围随机生成爆炸
                Instantiate(explosionEffect, transform.position + (Vector3)randomOffset, Quaternion.identity);
            }

            yield return new WaitForSeconds(explosionInterval);
            timer += explosionInterval;
        }

        // ✅ 爆炸结束后再执行解锁逻辑
        yield return StartCoroutine(UnlockRoutine());
    }


    private IEnumerator UnlockRoutine()
    {
        if (lockRenderers != null && unlockedSprite != null)
        {
            foreach (var lr in lockRenderers)
            {
                if (lr != null)
                    lr.sprite = unlockedSprite;
            }
        }

        if (sfxSource != null && unlockSound != null)
            sfxSource.PlayOneShot(unlockSound);

        yield return new WaitForSeconds(1f);

        if (doorObject != null)
            Destroy(doorObject);
    }

    // ✅ 以下是伤害判定逻辑
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroyed) return;

        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            if (bullet != null) bullet.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            TakeDamage(5);
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

        if (collision.CompareTag("SuperArm"))
            TakeDamage(4);

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                TakeDamage(2);
                iceArrow.DestroyBullet();
            }
        }

        if (collision.CompareTag("HyperBomb"))
            TakeDamage(5);

        if (collision.CompareTag("FireStorm"))
            TakeDamage(3);

        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            if (thunder != null)
            {
                TakeDamage(3);
                return;
            }

            if (smallThunder != null && Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
            {
                TakeDamage(2);
                smallThunder.lastDamageTime = Time.time;
                smallThunder.DestroyOnEnemyHit();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDestroyed) return;

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
}
