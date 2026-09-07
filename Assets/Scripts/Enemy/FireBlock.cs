using System.Collections;
using UnityEngine;

public class FireBlock : MonoBehaviour
{
    [Header("火柱参数")]
    public float extendHeight = 3f;
    public float extendDuration = 1f;
    public float stayDuration = 1f;
    public float retractDuration = 1f;
    public int damage = 2;
    public float damageCooldown = 1.5f;

    [Header("冰冻参数")]
    public AnimatorOverrideController frozenOverride; // 冰冻动画覆盖器
    public float frozenDuration = 5f;

    private Vector3 bottomPosition;
    private Vector3 topPosition;
    private float lastDamageTime;
    private Collider2D col;
    private Animator animator;
    private RuntimeAnimatorController defaultController;

    private bool disableForFireman = false;
    private bool isFrozen = false;
    private Coroutine freezeCoroutine;

    private string lastSelectedCharacter = "";

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        if (animator != null)
            defaultController = animator.runtimeAnimatorController;

        bottomPosition = transform.position;
        topPosition = bottomPosition + new Vector3(0f, extendHeight, 0f);
        transform.position = bottomPosition;

        CheckPlayerPreference(true); // ✅ 初始化时执行一次
    }

    private void Update()
    {
        CheckPlayerPreference(false); // ✅ 每帧动态检测玩家偏好
    }

    /// <summary>
    /// 根据当前玩家选择动态调整火柱行为
    /// </summary>
    private void CheckPlayerPreference(bool force)
    {
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Default");

        // 如果角色没有变化且不是强制刷新，则不执行
        if (!force && selectedCharacter == lastSelectedCharacter)
            return;

        lastSelectedCharacter = selectedCharacter;

        if (selectedCharacter == "Fireman")
        {
            // 火焰人 → 火柱无效
            disableForFireman = true;
            gameObject.tag = "Untagged";

            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (var c in colliders)
            {
                if (!c.isTrigger)
                    c.enabled = false;
            }
        }
        else
        {
            // 其他角色 → 恢复
            disableForFireman = false;
            gameObject.tag = "Enemy";

            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (var c in colliders)
            {
                if (!c.isTrigger)
                    c.enabled = true;
            }
        }

        // 冰人 → 受到伤害减弱
        if (selectedCharacter == "Iceman")
        {
            damage = 1;
        }
        else
        {
            damage = 2;
        }
    }

    private void Start()
    {
        StartCoroutine(FireCycle());
    }

    private IEnumerator FireCycle()
    {
        while (true)
        {
            float timer = 0f;
            while (timer < extendDuration)
            {
                if (!isFrozen)
                {
                    transform.position = Vector3.Lerp(bottomPosition, topPosition, timer / extendDuration);
                    timer += Time.deltaTime;
                }
                yield return null;
            }
            transform.position = topPosition;

            float stayTimer = 0f;
            while (stayTimer < stayDuration)
            {
                if (!isFrozen)
                    stayTimer += Time.deltaTime;
                yield return null;
            }

            timer = 0f;
            while (timer < retractDuration)
            {
                if (!isFrozen)
                {
                    transform.position = Vector3.Lerp(topPosition, bottomPosition, timer / retractDuration);
                    timer += Time.deltaTime;
                }
                yield return null;
            }
            transform.position = bottomPosition;

            float restTimer = 0f;
            while (restTimer < 1.5f)
            {
                if (!isFrozen)
                    restTimer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FireSpecial fireSpecial = collision.GetComponent<FireSpecial>();
            if (fireSpecial != null && fireSpecial.isExtinguished)
            {
                fireSpecial.IgniteFire();
                return;
            }

            if (disableForFireman) return;

            if (!isFrozen && Time.time > lastDamageTime + damageCooldown)
            {
                HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    lastDamageTime = Time.time;
                }
            }
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            iceArrow.DestroyBullet();
            ApplyFreeze();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FireSpecial fireSpecial = collision.GetComponent<FireSpecial>();
            if (fireSpecial != null && fireSpecial.isExtinguished)
            {
                fireSpecial.IgniteFire();
                return;
            }

            if (disableForFireman) return;

            if (!isFrozen && Time.time > lastDamageTime + damageCooldown)
            {
                HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    lastDamageTime = Time.time;
                }
            }
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            iceArrow.DestroyBullet();
            ApplyFreeze();
        }
    }

    private void ApplyFreeze()
    {
        if (freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);

        freezeCoroutine = StartCoroutine(FreezeCoroutine());
    }

    private IEnumerator FreezeCoroutine()
    {
        isFrozen = true;
        gameObject.tag = "Ground";
        gameObject.layer = LayerMask.NameToLayer("Ground");

        if (animator != null && frozenOverride != null)
            animator.runtimeAnimatorController = frozenOverride;

        animator.speed = 0f;

        yield return new WaitForSeconds(frozenDuration);

        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Default");
        if (animator != null)
        {
            animator.runtimeAnimatorController = defaultController;
            animator.speed = 1f;
        }

        isFrozen = false;
    }
}
