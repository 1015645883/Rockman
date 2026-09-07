using UnityEngine;
using System.Collections;

public class FireSpecial : MonoBehaviour
{
    [Header("火焰人专属射击点")]
    public Transform headPoint;
    public GameObject fireStormPrefab;
    public float fireRate = 0.2f;
    private float lastFireTime;

    [Header("护盾配置")]
    public GameObject fireShieldPrefab;
    public float shieldDuration = 2f;
    public int shieldRotations = 3;

    [Header("熄火动画控制")]
    public AudioClip extinguishVoice;
    public AudioClip igniteVoice;
    public AudioSource voiceAudioSource;
    private RuntimeAnimatorController defaultController;
    public AnimatorOverrideController extinguishOverride;
    private Animator animator;

    private PlayerShooting playerShooting;
    private IPlayerMovement playerMovement;

    [Header("照明控制")]
    public LightController lightController; // ✅ 光源控制引用

    public bool isExtinguished = false; // 是否熄火状态

    void Start()
    {
        playerShooting = GetComponent<PlayerShooting>();
        playerMovement = GetComponent<IPlayerMovement>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("FireSpecial 需要 Animator 组件");
        }
        else
        {
            defaultController = animator.runtimeAnimatorController;
        }

        if (playerShooting != null)
            playerShooting.OnChargedShotFired += ActivateShield;

        // ✅ 获取 LightController
        if (lightController == null)
        {
            Debug.LogWarning("FireSpecial 未找到 LightController 脚本");
        }
    }

    void OnDestroy()
    {
        if (playerShooting != null)
            playerShooting.OnChargedShotFired -= ActivateShield;
    }

    /// <summary>
    /// 检测触碰水面
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isExtinguished && collision.CompareTag("Water"))
        {
            ExtinguishFire();
        }
    }

    /// <summary>
    /// 熄火处理
    /// </summary>
    private void ExtinguishFire()
    {
        if (isExtinguished) return;
        isExtinguished = true;

        // 禁止射击
        if (playerShooting != null)
            playerShooting.SetCanShoot(false);

        // 应用熄火覆盖器
        if (animator != null && extinguishOverride != null)
            animator.runtimeAnimatorController = extinguishOverride;

        // 播放熄火语音
        if (extinguishVoice != null && voiceAudioSource != null)
            voiceAudioSource.PlayOneShot(extinguishVoice);

        // ✅ 同步关闭火焰照明
        if (lightController != null)
        {
            lightController.allowLight = false;
        }
    }

    /// <summary>
    /// 恢复火焰状态
    /// </summary>
    public void IgniteFire()
    {
        if (!isExtinguished) return;
        isExtinguished = false;

        // 恢复动画控制器
        if (animator != null && defaultController != null)
            animator.runtimeAnimatorController = defaultController;

        // 允许射击
        if (playerShooting != null)
            playerShooting.SetCanShoot(true);

        // 播放复燃语音
        if (igniteVoice != null && voiceAudioSource != null)
            voiceAudioSource.PlayOneShot(igniteVoice);

        // ✅ 同步重新点亮火焰照明
        if (lightController != null)
        {
            lightController.allowLight = true;
        }
    }

    public void TryFireSpecial()
    {
        if (isExtinguished) return;
        if (Time.time - lastFireTime < fireRate) return;
        if (playerShooting == null || playerMovement == null) return;
        if (!playerShooting.CanShoot || playerMovement.isHurt) return;

        if (fireStormPrefab != null && headPoint != null)
        {
            GameObject bullet = Instantiate(fireStormPrefab, headPoint.position, Quaternion.identity);
            FireStorm fireScript = bullet.GetComponent<FireStorm>();
            if (fireScript != null)
            {
                fireScript.SetDirection(Vector2.up);
                fireScript.OnDestroyCallback = playerShooting.OnBulletDestroyed;
            }
            if (playerShooting.normalAttackEffect != null)
            {
                playerShooting.VoiceAudioSource.PlayOneShot(playerShooting.normalAttackEffect);
            }
            lastFireTime = Time.time;
        }
    }

    private void ActivateShield()
    {
        if (isExtinguished) return;
        if (fireShieldPrefab != null)
            StartCoroutine(SpawnShield());
    }

    private IEnumerator SpawnShield()
    {
        if (headPoint == null) yield break;

        Vector3 initialOffset = headPoint.position - transform.position + new Vector3(0f, 1f, 0f);
        GameObject shield = Instantiate(fireShieldPrefab, headPoint.position + new Vector3(0f, 1f, 0f), Quaternion.identity);

        float totalDegrees = shieldRotations * 360f;
        float rotationSpeed = totalDegrees / shieldDuration;

        float elapsed = 0f;
        while (elapsed < shieldDuration)
        {
            if (isExtinguished)
            {
                Destroy(shield);
                yield break;
            }

            elapsed += Time.deltaTime;
            float angle = rotationSpeed * elapsed;
            Vector3 rotatedOffset = Quaternion.Euler(0f, 0f, angle) * initialOffset;
            shield.transform.position = transform.position + rotatedOffset;
            yield return null;
        }

        Destroy(shield);
    }
}
