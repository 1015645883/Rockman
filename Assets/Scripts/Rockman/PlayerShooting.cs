using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class ShapeAttackConfig
{
    public string shapeKey; // 如 "R", "C", "G", ...
    public GameObject bulletPrefab;
    public GameObject chargedBulletPrefab; // 新增：蓄力子弹
    public int maxBullets = 3;
    public AudioClip[] attackVoices; // 每个形态特有的攻击音效数组
}

[System.Serializable]
public class SingleAttackConfig
{
    public GameObject bulletPrefab;
    public GameObject chargedBulletPrefab;
    public int maxBullets = 3;
    public AudioClip[] attackVoices;
}

public class PlayerShooting : MonoBehaviour
{
    public List<ShapeAttackConfig> shapeConfigs;
    public SingleAttackConfig singleCharacterConfig; // 新增：单形态角色专用
    public GameObject currentBulletPrefab;
    [Header("G形态特殊攻击")]
    public Transform GFirePoint;     // G形态中心点
    public GameObject GStonePrefab;  // G形态石头预制体
    private int currentMaxBullets;
    public Transform firePoint;
    public float fireRate = 0.2f;
    private float lastFireTime = 0f;
    public int currentBulletCount = 0;
    public string currentShapeKey = "R";
    private float chargeStartTime;
    public bool isCharging = false;
    private GameObject chargedBulletPrefab;
    public bool isMultiShapeCharacter = false; // 是否多形态角色（Rockman/Blues）
    public Animator animator;

    [Header("能量系统")]
    public CEnergyManager cEnergyBar;
    public GEnergyManager gEnergyBar;
    public IEnergyManager iEnergyBar;
    public BEnergyManager bEnergyBar;
    public FEnergyManager fEnergyBar;
    public EEnergyManager eEnergyBar;
    public EEnergyBarUI eEnergyUI;

    [Header("蓄力Shader控制")]
    private bool canCharge = false; // 当前角色是否能蓄力
    public Renderer playerRenderer; // 玩家的Renderer组件
    private Material playerMaterial; // 用于控制Shader的材质实例
    public float maxChargeTime = 2.0f; // 最大蓄力时间
    private bool chargeEffectStarted = false;
    public GameObject groundCutterPrefab;
    private BluesSpecial bluesSpecial;
    public BombSpecial bombSpecial;
    public delegate void ChargedShotHandler();
    public event ChargedShotHandler OnChargedShotFired;


    [Header("音效设置")]
    [Range(0, 1)] public float attackVoiceProbability = 0.09f;
    public AudioSource VoiceAudioSource;
    public AudioClip normalAttackEffect;    // 普通攻击音效
    public AudioClip chargedAttackEffect;   // 蓄力攻击音效
    public AudioClip GAttackSound; // ✅ 在 Inspector 里指定 G 形态攻击音效
    public AudioClip chargeSound;
    public AudioSource chargeAudioSource; // 独立音效源

    [SerializeField] private MonoBehaviour playerMovementBehaviour;
    private IPlayerMovement playerMovement;
    private PlayerMovement playerMovementScript;
    private string SelectedChar => PlayerPrefs.GetString("SelectedCharacter", "");


    void Awake()
    {
        playerMovementScript = playerMovementBehaviour as PlayerMovement;
        if (playerMovementScript == null)
        {
            Debug.LogError("绑定的脚本不是 PlayerMovement 类型，无法访问 IsClimbing");
        }
    }

    public bool canShootOverride = true;
    public bool CanShoot => canShootOverride && currentBulletCount < currentMaxBullets && !playerMovement.isHurt;

    public void SetCanShoot(bool value)
    {
        canShootOverride = value;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        bluesSpecial = GetComponent<BluesSpecial>();
        bombSpecial = GetComponent<BombSpecial>();
        // 自动查找场景中带有标签 "C能量" 的 GameObject 并获取 CEnergyManager 组件
        // ----------------- C 能量 -----------------
        if (cEnergyBar == null)
        {
            GameObject cEnergyObj = GameObject.FindGameObjectWithTag("CEnergy");
            if (cEnergyObj != null)
            {
                cEnergyBar = cEnergyObj.GetComponent<CEnergyManager>();
                if (cEnergyBar == null)
                    Debug.LogWarning("对象带标签 CEnergy，但无 CEnergyManager 组件");
            }
            else
            {
                Debug.LogWarning("场景中无带标签 CEnergy 的对象");
            }
        }

        // ----------------- G 能量 -----------------
        if (gEnergyBar == null)
        {
            GameObject gEnergyObj = GameObject.FindGameObjectWithTag("GEnergy");
            if (gEnergyObj != null)
            {
                gEnergyBar = gEnergyObj.GetComponent<GEnergyManager>();
                if (gEnergyBar == null)
                    Debug.LogWarning("对象带标签 GEnergy，但无 GEnergyManager 组件");
            }
            else
            {
                Debug.LogWarning("场景中无带标签 GEnergy 的对象");
            }
        }

        // ----------------- I 能量 -----------------
        if (iEnergyBar == null)
        {
            GameObject iEnergyObj = GameObject.FindGameObjectWithTag("IEnergy");
            if (iEnergyObj != null)
            {
                iEnergyBar = iEnergyObj.GetComponent<IEnergyManager>();
                if (iEnergyBar == null)
                    Debug.LogWarning("对象带标签 IEnergy，但无 IEnergyManager 组件");
            }
            else
            {
                Debug.LogWarning("场景中无带标签 IEnergy 的对象");
            }
        }

        // ----------------- B 能量 -----------------
        if (bEnergyBar == null)
        {
            GameObject bEnergyObj = GameObject.FindGameObjectWithTag("BEnergy");
            if (bEnergyObj != null)
            {
                bEnergyBar = bEnergyObj.GetComponent<BEnergyManager>();
                if (bEnergyBar == null)
                    Debug.LogWarning("对象带标签 BEnergy，但无 BEnergyManager 组件");
            }
            else
            {
                Debug.LogWarning("场景中无带标签 BEnergy 的对象");
            }
        }

        // ----------------- F 能量 -----------------
        if (fEnergyBar == null)
        {
            GameObject fEnergyObj = GameObject.FindGameObjectWithTag("FEnergy");
            if (fEnergyObj != null)
            {
                fEnergyBar = fEnergyObj.GetComponent<FEnergyManager>();
                if (fEnergyBar == null)
                    Debug.LogWarning("对象带标签 FEnergy，但无 FEnergyManager 组件");
            }
            else
            {
                Debug.LogWarning("场景中无带标签 FEnergy 的对象");
            }
        }

        // ----------------- E 能量 -----------------
        if (eEnergyBar == null)
        {
            eEnergyBar = GetComponent<EEnergyManager>();
            if (eEnergyBar == null)
            {
                Debug.LogWarning($"{gameObject.name} 身上没有挂 EEnergyManager 组件");
            }
        }

        // 🔹 在场景中找到标签为 EEnergy 的对象
        GameObject eEnergyObj = GameObject.FindGameObjectWithTag("EEnergy");
        if (eEnergyObj != null)
        {
            var energyUI = eEnergyObj.GetComponent<EEnergyBarUI>();
            if (energyUI != null)
            {
                // 🔹 绑定 UI 到当前角色的 EEnergyManager
                energyUI.Bind(eEnergyBar);
            }
            else
            {
                Debug.LogWarning("EEnergy 对象没有挂 EEnergyBarUI 脚本");
            }
        }
        else
        {
            Debug.LogWarning("场景中没有带标签 EEnergy 的对象");
        }



        if (playerMovementBehaviour == null)
        {
            Debug.LogError("请在 Inspector 手动绑定实现了 IPlayerMovement 的脚本");
            return;
        }
        playerMovement = playerMovementBehaviour as IPlayerMovement;
        if (playerMovement == null)
        {
            Debug.LogError("绑定的脚本没有实现 IPlayerMovement 接口");
        }
        if (playerRenderer != null)
        {
            playerMaterial = playerRenderer.material;
        }
        else
        {
            Debug.LogWarning("Player Renderer未分配，无法控制蓄力Shader效果");
        }
        isMultiShapeCharacter = (SelectedChar == "Rockman" || SelectedChar == "Blues");

        // 新增：蓄力权限控制
        if (isMultiShapeCharacter)
        {
            canCharge = true; // Rockman、Blues 永远能蓄力
        }
        else
        {
            // 只有 Fireman 和 Cutman 可以蓄力
            canCharge = (SelectedChar == "Fireman" || SelectedChar == "Bombman");
        }

        if (isMultiShapeCharacter)
        {
            // 多形态角色初始形态
            SwitchShape("R");
        }
        else
        {
            // 单形态角色配置
            currentBulletPrefab = singleCharacterConfig.bulletPrefab;
            chargedBulletPrefab = singleCharacterConfig.chargedBulletPrefab;
            currentMaxBullets = singleCharacterConfig.maxBullets;
        }
    }
    // 新增：重新初始化攻击配置（用于角色切换时）
    public void ReinitializeForCharacter(string characterKey = null)
    {
        string currentSelectedChar = characterKey ?? PlayerPrefs.GetString("SelectedCharacter", "");

        // 判断多形态角色
        isMultiShapeCharacter = (currentSelectedChar == "Rockman" || currentSelectedChar == "Blues");

        // 设置蓄力权限
        canCharge = isMultiShapeCharacter ? true : (currentSelectedChar == "PlayableFireman" || currentSelectedChar == "PlayableBombman");

        // 初始化攻击配置
        if (!isMultiShapeCharacter)
        {
            currentBulletPrefab = singleCharacterConfig.bulletPrefab;
            chargedBulletPrefab = singleCharacterConfig.chargedBulletPrefab;
            currentMaxBullets = singleCharacterConfig.maxBullets;
            currentBulletCount = 0;
        }

        Debug.Log($"攻击配置已重新初始化: {currentSelectedChar}, 多形态: {isMultiShapeCharacter}, 可蓄力: {canCharge}, 当前形态: {currentShapeKey}");
    }



    void Update()
    {
        if (!canCharge) return; // 非蓄力角色直接跳过

        if (isCharging && playerMaterial != null)
        {
            float baseChargeTime = Time.time - chargeStartTime;

            // 如果有BluesSpecial组件，取倍率，否则默认1
            float multiplier = bluesSpecial != null ? bluesSpecial.chargeSpeedMultiplier : 1f;
            float chargeTime = baseChargeTime * multiplier;

            if (chargeTime <= 0.25f)
            {
                playerMaterial.SetFloat("_ChargeLevel", 0);
                if (chargeAudioSource.isPlaying && chargeAudioSource.clip == chargeSound)
                {
                    chargeAudioSource.Stop();
                    chargeAudioSource.clip = null;
                }
                chargeEffectStarted = false;
                return;
            }

            if (chargeTime >= 0.25f && chargeTime < maxChargeTime)
            {
                playerMaterial.SetFloat("_ChargeLevel", 1.0f);
                if (!chargeEffectStarted)
                {
                    chargeEffectStarted = true;
                    if (chargeFadeCoroutine != null) StopCoroutine(chargeFadeCoroutine);
                    chargeFadeCoroutine = StartCoroutine(PlayChargeSoundWithFade());
                }
            }
            else
            {
                playerMaterial.SetFloat("_ChargeLevel", 2.0f);
            }
        }
    }

    public void SwitchShape(string shapeKey)
    {
        if (!isMultiShapeCharacter)
            return; // 非多形态角色直接禁止切换

        currentShapeKey = shapeKey;
        ShapeAttackConfig config = shapeConfigs.Find(c => c.shapeKey == shapeKey);
        if (config != null)
        {
            currentBulletPrefab = config.bulletPrefab;
            chargedBulletPrefab = config.chargedBulletPrefab;
            currentMaxBullets = config.maxBullets;
            currentBulletCount = 0;
        }
        else
        {
            Debug.LogWarning("No config found for shape: " + shapeKey);
        }
    }


    public bool TryShoot()
    {
        if (!CanShoot || Time.time - lastFireTime < fireRate)
            return false;

        // 形态能量检查
        switch (currentShapeKey)
        {
            case "C":
                if (cEnergyBar == null || !cEnergyBar.TryUseEnergy(1))
                {
                    Debug.Log("C形态能量不足");
                    return false;
                }
                break;

            case "G":
                if (gEnergyBar == null || !gEnergyBar.TryUseEnergy(1))
                {
                    Debug.Log("G形态能量不足");
                    return false;
                }
                break;

            case "I":
                if (iEnergyBar == null || !iEnergyBar.TryUseEnergy(1))
                {
                    Debug.Log("I形态能量不足");
                    return false;
                }
                break;

            case "B":
                if (bEnergyBar == null || !bEnergyBar.TryUseEnergy(3))
                {
                    Debug.Log("B形态能量不足");
                    return false;
                }
                break;

            case "F":
                if (fEnergyBar == null || !fEnergyBar.TryUseEnergy(2))
                {
                    Debug.Log("F形态能量不足");
                    return false;
                }
                break;

            case "E":
                if (eEnergyBar == null || !eEnergyBar.TryUseEnergy(1))
                {
                    Debug.Log("E形态能量不足");
                    return false;
                }
                break;
        }


        bool bulletFired = Shoot(); // 调用 Shoot() 并获取返回值
        if (bulletFired)
        {
            lastFireTime = Time.time;
        }
        return bulletFired; // 返回是否成功发射
    }

    public bool Shoot(bool useCharged = false)
    {
        if (SelectedChar == "Elecman" && eEnergyBar != null && !eEnergyBar.TryUseEnergy(2))
        {
            Debug.Log("电力不足，无法攻击");
            return false;
        }
        if ((SelectedChar == "Gutsman" || SelectedChar == "Bombman") && playerMovementScript != null && playerMovementScript.isClimbing)
        {
            Debug.Log("爬梯时不能射击");
            return false;
        }
        if (!CanShoot || Time.time - lastFireTime < fireRate)
            return false;
        float direction = playerMovement != null && playerMovement.IsFacingRight() ? 1f : -1f;
        Debug.Log("调用一次Shoot");
        animator.SetBool("IsShooting", true);
        // ✅ G形态特殊攻击逻辑
        if (currentShapeKey == "G" && GFirePoint != null && GStonePrefab != null)
        {

            // 以 GFirePoint 为中心点，在 X 轴偏移 生成石头
            float[] offsets = new float[] { -3f, -1f, 1f, 3f };

            foreach (float offset in offsets)
            {
                Vector3 spawnPos = GFirePoint.position + new Vector3(offset * direction, 0f, 0f);
                GameObject stone = Instantiate(GStonePrefab, spawnPos, Quaternion.identity);

                if (stone.TryGetComponent<Rigidbody2D>(out var rb))
                    rb.velocity = new Vector2(0, 0); 

                // 注册销毁回调（如果 GStone 有生命期）
                GStone gstoneScript = stone.GetComponent<GStone>();
                if (gstoneScript != null)
                    gstoneScript.OnDestroyCallback = OnBulletDestroyed;
            }

            // 播放动画、音效
            if (GAttackSound != null && VoiceAudioSource != null)
            {
                VoiceAudioSource.PlayOneShot(GAttackSound);
            }
            PlayAttackVoice();

            currentBulletCount++;
            lastFireTime = Time.time;

            if (playerMovement != null)
            {
                playerMovement.SetShootingState(true);
                Invoke(nameof(StopShootingAnimation), fireRate);
            }
            return true; // ✅ 直接返回，跳过普通子弹逻辑
        }

        AudioClip clipToPlay = useCharged ? chargedAttackEffect : normalAttackEffect;
        if (clipToPlay != null)
        {
            VoiceAudioSource.PlayOneShot(clipToPlay);
        }

        PlayAttackVoice();

        GameObject prefabToUse = useCharged ? chargedBulletPrefab : currentBulletPrefab;
        if (prefabToUse == null)
        {
            //Debug.LogWarning("预制件为空，无法发射");
            return false;
        }

        if (SelectedChar == "Cutman")
        {
            CutSpecial cutSpecial = GetComponent<CutSpecial>();
            if (cutSpecial != null && cutSpecial.IsWallClinging())
                direction *= -1f;

            float verticalInput = Input.GetAxisRaw("Vertical");

            GameObject cutter = null;

            if (verticalInput > 0)
            {
                // ⬆️ 上攻击 = 圆周模式
                cutter = Instantiate(currentBulletPrefab, firePoint.position, Quaternion.identity);
                RollingCutter rc = cutter.GetComponent<RollingCutter>();
                if (rc != null)
                    rc.Initialize(new Vector2(direction, 0f), transform, circularMode: true, groundMode: false);
            }
            else if (verticalInput < 0)
            {
                // ⬇️ 下攻击 = 滚地剪刀
                Vector2 spawnPos = (Vector2)transform.position + Vector2.right * direction * 0.2f; // 向玩家面向方向偏移0.2单位
                cutter = Instantiate(groundCutterPrefab, spawnPos, Quaternion.identity);

                RollingCutter rc = cutter.GetComponent<RollingCutter>();
                if (rc != null)
                    rc.Initialize(new Vector2(direction, 0f), transform, circularMode: false, groundMode: true);
            }

            else
            {
                // ➡️ 普通攻击 = 半圆模式
                cutter = Instantiate(currentBulletPrefab, firePoint.position, Quaternion.identity);
                RollingCutter rc = cutter.GetComponent<RollingCutter>();
                if (rc != null)
                    rc.Initialize(new Vector2(direction, 0f), transform, circularMode: false, groundMode: false);
            }

            currentBulletCount++;
            lastFireTime = Time.time;

            if (playerMovement != null)
            {
                playerMovement.SetShootingState(true);
                Invoke(nameof(StopShootingAnimation), fireRate);
            }

            return true;
        }




        // ✅ 判断是不是 HyperBomb
        if (prefabToUse.GetComponent<HyperBomb>() != null)
        {
            GameObject bomb = Instantiate(prefabToUse, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = bomb.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                string SelectedChar = PlayerPrefs.GetString("SelectedCharacter", "");

                if (SelectedChar == "Bombman")
                {
                    float verticalInput = Input.GetAxisRaw("Vertical");

                    if (verticalInput > 0) // ⬆️ 向上
                    {
                        rb.velocity = new Vector2(0f, 20f); // 垂直向上
                    }
                    else if (verticalInput < 0) // ⬇️ 脚下
                    {
                        rb.velocity = Vector2.zero;
                        bomb.transform.position = new Vector2(transform.position.x, transform.position.y - 0.5f);
                    }
                    else // ⬅️➡️ 正常斜抛
                    {
                        rb.velocity = new Vector2(12f * direction, 6f);
                    }
                }
                else
                {
                    // ❌ 非 Bombman，一律走默认斜抛
                    rb.velocity = new Vector2(12f * direction, 6f);
                }
            }

            // ✅ 给 HyperBomb 设置回调
            HyperBomb hyperBombScript = bomb.GetComponent<HyperBomb>();
            if (hyperBombScript != null)
            {
                hyperBombScript.OnDestroyCallback = OnBulletDestroyed;
            }

            currentBulletCount++;
            lastFireTime = Time.time;

            if (playerMovement != null)
            {
                playerMovement.SetShootingState(true);
                Invoke(nameof(StopShootingAnimation), fireRate);
            }

            return true;
        }




        // ✅ 普通子弹逻辑
        GameObject bullet = Instantiate(prefabToUse, firePoint.position, Quaternion.identity);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        RollingCutter rollingScript = bullet.GetComponent<RollingCutter>();
        RIceArrow iceScript = bullet.GetComponent<RIceArrow>();
        FireStorm fireScript = bullet.GetComponent<FireStorm>();
        HyperBombExplosion explosionPrefab = bullet.GetComponent<HyperBombExplosion>();
        ThunderBeam thunderScript = bullet.GetComponent<ThunderBeam>();

        if (bulletScript != null)
        {
            bulletScript.SetDirection(new Vector2(direction, 0));
            bulletScript.OnDestroyCallback = OnBulletDestroyed;
        }
        else if (rollingScript != null)
        {
            rollingScript.Initialize(new Vector2(direction, 0), this.transform);
        }
        else if (iceScript != null)
        {
            iceScript.SetDirection(new Vector2(direction, 0));
            iceScript.OnDestroyCallback = OnBulletDestroyed;
        }
        else if (fireScript != null) // ✅ 处理 FireStorm
        {
            fireScript.SetDirection(new Vector2(direction, 0));
            fireScript.OnDestroyCallback = OnBulletDestroyed;
        }
        else if (explosionPrefab != null)
        {
            explosionPrefab.OnDestroyCallback = OnBulletDestroyed;
        }
        else if (thunderScript != null)
        {
            thunderScript.SetDirection(new Vector2(direction, 0));
            thunderScript.OnDestroyCallback = OnBulletDestroyed;
        }
        currentBulletCount++;
        lastFireTime = Time.time;

        if (playerMovement != null)
        {
            playerMovement.SetShootingState(true);
            Invoke(nameof(StopShootingAnimation), fireRate);
        }

        return true;
    }


    private void PlayAttackVoice()
    {
        if (isMultiShapeCharacter)
        {
            ShapeAttackConfig config = shapeConfigs.Find(c => c.shapeKey == currentShapeKey);
            if (config != null && config.attackVoices.Length > 0 && Random.value <= attackVoiceProbability)
            {
                AudioClip clip = config.attackVoices[Random.Range(0, config.attackVoices.Length)];
                VoiceAudioSource.PlayOneShot(clip);
            }
        }
        else
        {
            if (singleCharacterConfig.attackVoices.Length > 0 && Random.value <= attackVoiceProbability)
            {
                AudioClip clip = singleCharacterConfig.attackVoices[Random.Range(0, singleCharacterConfig.attackVoices.Length)];
                VoiceAudioSource.PlayOneShot(clip);
            }
        }
    }

    public void StartCharge()
    {
        if (currentShapeKey == "R")
        {
            isCharging = true;
            chargeEffectStarted = false;
            chargeStartTime = Time.time;

        }
    }


    public void ReleaseCharge()
    {
        if (!canCharge) { TryShoot(); return; } // 非蓄力角色直接普通射击

        if (isCharging)
        {
            float baseChargeTime = Time.time - chargeStartTime;
            float multiplier = bluesSpecial != null ? bluesSpecial.chargeSpeedMultiplier : 1f;
            float chargeTime = baseChargeTime * multiplier;

            if (chargeFadeCoroutine != null)
            {
                StopCoroutine(chargeFadeCoroutine);
                chargeAudioSource.Stop();
            }

            playerMaterial.SetFloat("_ChargeLevel", 0);
            isCharging = false;
            chargeEffectStarted = false;

            if (chargeTime >= 2.0f)
            {
                if (PlayerPrefs.GetString("SelectedCharacter") == "Bombman" && bombSpecial != null)
                {
                    Debug.Log("是炸弹人");
                    bombSpecial.ReleaseBombCharge(); // 触发 BombSpecial 的 ReleaseCharge
                    return;
                }
                Shoot(true);
                OnChargedShotFired?.Invoke();
            }
            else if (chargeTime >= 0.25f)
            {
                Shoot(false);
            }
            else
            {
                TryShoot();
            }
        }
        else
        {
            TryShoot();
        }
    }

    public void CancelCharge()
    {
        if (isCharging)
        {
            isCharging = false;
            chargeEffectStarted = false;

            if (playerMaterial != null)
            {
                playerMaterial.SetFloat("_ChargeLevel", 0);
            }

            StopChargeSound();
        }
    }


    private Coroutine chargeFadeCoroutine;

    private IEnumerator PlayChargeSoundWithFade()
    {
        if (chargeSound == null) yield break;
        Debug.Log("StopChargeSound 被调用");
        chargeAudioSource.clip = chargeSound;
        chargeAudioSource.volume = 1f;
        chargeAudioSource.Play();

        // 正常播放 2 秒
        yield return new WaitForSeconds(2f);

        // 在 1 秒内音量淡出
        float fadeDuration = 1f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            chargeAudioSource.volume = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        chargeAudioSource.Stop();
        chargeAudioSource.volume = 1f; // 重置音量
    }

    private void StopChargeSound()
    {
        if (chargeFadeCoroutine != null)
        {
            StopCoroutine(chargeFadeCoroutine);
            chargeFadeCoroutine = null;
        }

        // ⭐只停止“蓄力音效”
        if (chargeAudioSource.isPlaying && chargeAudioSource.clip == chargeSound)
        {
            chargeAudioSource.Stop();
            chargeAudioSource.clip = null;
            chargeAudioSource.volume = 1f;
        }
    }

    public void OnBulletDestroyed()
    {
        if (currentBulletCount > 0)
        {
            currentBulletCount--;
            Debug.Log("Bullet destroyed, currentBulletCount: " + currentBulletCount);
        }
    }

    void StopShootingAnimation()
    {
        if (playerMovement != null)
        {
            playerMovement.SetShootingState(false);
        }
    }
}