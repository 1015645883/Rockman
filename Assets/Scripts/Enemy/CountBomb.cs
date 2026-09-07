using UnityEngine;

public class CountBomb : MonoBehaviour
{
    [Header("倒计时参数")]
    public float countdownTime = 4f;
    private float timer;
    public bool isActivated = false;

    [Header("引用")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] countdownSprites;
    public GameObject explosionPrefab;
    public AudioClip tickSound;              // 倒计时音效
    private AudioSource audioSource;

    public bool isFrozen = false;

    private int lastSpriteIndex = -1; // 缓存上一次显示的 Sprite 索引

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        timer = countdownTime;

        if (spriteRenderer != null && countdownSprites.Length > 0)
            spriteRenderer.sprite = countdownSprites[0];
    }

    private void Update()
    {
        if (isFrozen) return;
        if (!isActivated) return;

        timer -= Time.deltaTime;

        int currentIndex = GetSpriteIndex(timer);

        // Sprite 改变才播放音效
        if (currentIndex != lastSpriteIndex)
        {
            spriteRenderer.sprite = countdownSprites[currentIndex];
            if (tickSound != null && audioSource != null)
                audioSource.PlayOneShot(tickSound);

            lastSpriteIndex = currentIndex;
        }

        if (timer <= 0f)
        {
            Explode();
        }
    }

    private int GetSpriteIndex(float t)
    {
        if (t > 3f) return 1;
        else if (t > 2f) return 2;
        else if (t > 1f) return 3;
        else if (t > 0.5f) return 4;
        else if (t > 0.25f) return 5;
        else if (t > 0f) return 6;
        else return 6;
    }

    public void Explode()
    {
        // 播放爆炸特效
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // 隐形处理：禁用渲染、碰撞器和脚本
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        this.enabled = false; // 暂时禁用脚本
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActivated && other.CompareTag("Player"))
        {
            isActivated = true;
            timer = countdownTime;
            lastSpriteIndex = -1; // 确保激活时立即播放音效
        }
    }
    public void ResetBomb()
    {
        timer = countdownTime;
        isActivated = false;
        isFrozen = false;
        lastSpriteIndex = -1;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        this.enabled = true; // 重新启用脚本

        if (spriteRenderer != null && countdownSprites.Length > 0)
            spriteRenderer.sprite = countdownSprites[0]; // 显示未激活的“3”
    }




}
