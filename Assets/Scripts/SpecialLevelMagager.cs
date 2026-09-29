using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SpecialLevelManager : MonoBehaviour
{
    [Header("角色预制体顺序")]
    public List<GameObject> characterPrefabs;

    [Header("对应出生点顺序")]
    public List<Transform> spawnPoints;

    [Header("对应停留点顺序")]
    public List<Transform> stayPoints;

    private Dictionary<string, Vector3> currentRespawnPoints = new Dictionary<string, Vector3>();
    [Header("汇合提示文本")]
    public TextMeshPro meetingText;
    [Header("主摄像机")]
    public Camera mainCamera;

    [Header("区域摄像机控制器")]
    public StageCameraController stageCameraController;

    [Header("BGM控制")]
    public AudioSource bgmSource;
    [Header("角色传送音效")]
    [SerializeField] private AudioClip teleportStartSound; 
    [SerializeField] private AudioClip teleportFinishSound;
    [Header("屏幕淡入淡出")]
    public ScreenFader screenFader;

    public GamePauseManager gamePauseManager;
    [Header("生命条UI")]
    public HealthBarUI healthBarUI;
    [Header("E 能量条 UI")]
    public EEnergyBarUI eEnergyUI;

    [System.Serializable]
    public class CharacterVoice
    {
        public string characterKey;
        public AudioClip spawnVoice;
    }
    public CharacterVoice[] characterVoices;

    [Header("关卡解锁元素")]
    public SpriteRenderer[] lockRenderers;     // 4个锁的SpriteRenderer
    public Sprite unlockedSprite;              // 解锁时替换的Sprite
    public GameObject doorObject;              // 门对象（销毁用）
    public AudioClip unlockSound;              // 解锁音效
    public AudioSource sfxSource;              // 播放解锁音效的音源
    [Header("可切换区域")]
    public Collider2D switchAreaCollider; // 放置可切换角色的区域 Collider

    private int currentIndex = -1; // 出生顺序索引（最后一个已生成索引）
    public GameObject currentPlayer;
    private List<GameObject> spawnedPlayers = new List<GameObject>();

    private bool allCharactersReady = false; // 所有角色是否已登场
    private int controlledIndex = 0;         // 当前受控角色索引

    private readonly string[] characterNames =
    {
        "Rockman", "Cutman", "Gutsman", "Iceman", "Bombman", "Fireman", "Elecman"
    };

    private void Awake()
    {
        // 强制 SelectedCharacter 为 Rockman
        PlayerPrefs.SetString("SelectedCharacter", "Rockman");
        PlayerPrefs.Save();

        // 初始隐藏
        if (meetingText != null)
        {
            meetingText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        SpawnNextCharacter();
    }

    private void Update()
    {
        // 当所有角色聚齐且按下 W
        if (allCharactersReady && Input.GetKeyDown(KeyCode.F))
        {
            // 检查主控角色是否在可切换区域内
            if (currentPlayer != null && switchAreaCollider != null)
            {
                if (switchAreaCollider.bounds.Contains(currentPlayer.transform.position))
                {
                    SwitchToNextCharacter();
                }
                else
                {
                    Debug.Log("主控角色不在可切换区域内，无法切换");
                }
            }
            else
            {
                // 如果没指定区域，默认允许切换
                SwitchToNextCharacter();
            }
        }
    }

    private void UpdateEEnergyUI()
    {
        if (eEnergyUI == null)
        {
            Debug.LogWarning("EEnergyUI 未分配！");
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogWarning("当前玩家为null，隐藏E能量UI");
            eEnergyUI.gameObject.SetActive(false);
            return;
        }

        var eEnergyManager = currentPlayer.GetComponent<EEnergyManager>();
        if (eEnergyManager != null)
        {
            Debug.Log($"绑定E能量UI到 {currentPlayer.name}");
            eEnergyUI.Bind(eEnergyManager);
            eEnergyUI.gameObject.SetActive(true);
            eEnergyUI.Refresh(); // 强制刷新显示
        }
        else
        {
            Debug.Log($"{currentPlayer.name} 没有EEnergyManager，隐藏UI");
            eEnergyUI.gameObject.SetActive(false);
        }
    }


    public void SpawnNextCharacter()
    {
        currentIndex++;
        if (currentIndex >= characterPrefabs.Count) return;

        GameObject prefab = characterPrefabs[currentIndex];
        Transform spawnPoint = (spawnPoints != null && currentIndex < spawnPoints.Count) ? spawnPoints[currentIndex] : null;
        if (prefab != null && spawnPoint != null)
        {
            currentPlayer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            spawnedPlayers.Add(currentPlayer);

            // 每个角色出生后，都将区域摄像机切换到当前出生角色
            if (stageCameraController != null &&
                currentPlayer != null)
            {
                stageCameraController.SetTarget(
                    currentPlayer.transform,
                    true
                );
            }

            // 设置初始复活点为出生点
            string charKey = StripCloneSuffix(prefab.name);
            if (!currentRespawnPoints.ContainsKey(charKey))
            {
                currentRespawnPoints[charKey] = spawnPoint.position;
            }

            // 绑定当前角色的 UI
            if (healthBarUI != null) healthBarUI.BindToNewPlayer(currentPlayer);
            UpdateEEnergyUI();

            // 更新 BossTrigger 的 player 引用
            BossTrigger bossTrigger = FindObjectOfType<BossTrigger>();
            if (bossTrigger != null)
            {
                bossTrigger.player = currentPlayer;
                bossTrigger.gutsmanMelee = currentPlayer.GetComponent<GutsmanMelee>();
            }

            LevelClearManager levelClearManager = FindObjectOfType<LevelClearManager>();
            if (levelClearManager != null)
            {
                levelClearManager.player = currentPlayer;
            }

            // 更新 PlayerPrefs 对应名字
            if (currentIndex < characterNames.Length)
            {
                PlayerPrefs.SetString("SelectedCharacter", characterNames[currentIndex]);
                PlayerPrefs.Save();
            }

            if (!NameMatches(currentPlayer, "Rockman"))
            {
                StartCoroutine(PlayCharacterSpawnSequence(currentPlayer));
            }

            // ✅ 如果登场角色是火焰人（PlayableFireman），点亮所有光源
            string prefabKey = StripCloneSuffix(prefab.name); // 使用 prefab 名判断更可靠
            if (prefabKey.Equals("Fireman", System.StringComparison.OrdinalIgnoreCase)
                || prefabKey.Equals("PlayableFireman", System.StringComparison.OrdinalIgnoreCase))
            {
                LightController[] lights = FindObjectsOfType<LightController>();
                foreach (var light in lights)
                {
                    light.allowLight = true;
                }

                Debug.Log("🔥 PlayableFireman 登场，重新点亮所有 LightController 光源");
            }
        }
        else
        {
            Debug.LogWarning("角色或出生点未设置！ index=" + currentIndex);
        }
    }



    private void OnAllCharactersSpawned()
    {
        allCharactersReady = true;
        Debug.Log("所有角色已登场完成！");

        int rockIndex = spawnedPlayers.FindIndex(p => NameMatches(p, "Rockman"));
        controlledIndex = rockIndex >= 0 ? rockIndex : 0;
        PlayerPrefs.SetString("SelectedCharacter", "Rockman");
        PlayerPrefs.Save();

        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            var p = spawnedPlayers[i];
            if (p == null) continue;

            bool shouldControl = (i == controlledIndex);

            if (shouldControl)
            {
                // ✅ 启用所有控制相关的组件
                var movement = p.GetComponent<PlayerMovement>();
                var shooting = p.GetComponent<PlayerShooting>(); // 获取攻击组件
                var rb = p.GetComponent<Rigidbody2D>();
                var col = p.GetComponent<Collider2D>();
                var anim = p.GetComponent<Animator>();
                var sr = p.GetComponent<SpriteRenderer>();

                if (movement != null)
                {
                    movement.enabled = true;
                    movement.canMove = true;
                }

                // ✅ 关键修复：重新初始化攻击配置
                if (shooting != null)
                {
                    shooting.ReinitializeForCharacter();
                    shooting.enabled = true;
                }

                if (rb != null)
                {
                    rb.simulated = true;
                    rb.isKinematic = false;
                    rb.velocity = Vector2.zero;
                }
                if (col != null) col.enabled = true;

                if (anim != null)
                {
                    anim.speed = 1f;
                    anim.Play("Idle", 0, 0f);
                }
                if (sr != null) sr.flipX = false;

                Debug.Log("控制权交还给 Rockman！已重新初始化攻击配置");
            }
            else
            {
                SetPlayerControlState(p, false);
            }
        }

        currentPlayer = spawnedPlayers[controlledIndex];

        // 将当前受控角色交给区域摄像机
        if (stageCameraController != null && currentPlayer != null)
        {
            stageCameraController.SetTarget(
                currentPlayer.transform,
                true
            );
        }

        if (healthBarUI != null)
            healthBarUI.BindToNewPlayer(currentPlayer);
        UpdateEEnergyUI();
        // 更新 BossTrigger 的 player 引用
        BossTrigger bossTrigger = FindObjectOfType<BossTrigger>();
        if (bossTrigger != null)
        {
            bossTrigger.player = currentPlayer;
            bossTrigger.gutsmanMelee = currentPlayer.GetComponent<GutsmanMelee>();
        }
        LevelClearManager levelClearManager = FindObjectOfType<LevelClearManager>();
        if (levelClearManager != null)
        {
            levelClearManager.player = currentPlayer;
        }

        Debug.Log("所有角色到齐，当前受控角色 → " + currentPlayer.name);
        //  触发解锁流程
        StartCoroutine(UnlockRoutine());
        //  显示汇合文本
        if (meetingText != null)
        {
            meetingText.gameObject.SetActive(true);
        }
    }

    // 🔹 解锁协程
    private IEnumerator UnlockRoutine()
    {
        // 替换锁的Sprite
        if (lockRenderers != null && unlockedSprite != null)
        {
            foreach (var lr in lockRenderers)
            {
                if (lr != null)
                {
                    lr.sprite = unlockedSprite;
                }
            }
        }

        // 播放音效
        if (sfxSource != null && unlockSound != null)
        {
            sfxSource.PlayOneShot(unlockSound);
        }

        // 给点延迟，让音效/动画先表现
        yield return new WaitForSeconds(1f);

        // 销毁门
        if (doorObject != null)
        {
            Destroy(doorObject);
        }

        Debug.Log("✅ 所有角色汇合 → 锁解开 + 门销毁完成");
    }

    // 在切换时统一处理所有角色的启停，保证只有一个能动
    private void SwitchToNextCharacter()
    {
        if (spawnedPlayers.Count == 0) return;

        int previousIndex = controlledIndex;
        controlledIndex = (controlledIndex + 1) % spawnedPlayers.Count;

        // ✅ 如果之前有控制角色，则清除其X轴速度
        if (previousIndex >= 0 && previousIndex < spawnedPlayers.Count)
        {
            var prevPlayer = spawnedPlayers[previousIndex];
            if (prevPlayer != null)
            {
                Rigidbody2D rb = prevPlayer.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y); // 清除水平速度
                }

                // 如果角色动画有移动状态，可以在这里重置动画
                var animator = prevPlayer.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f); // 可选：重置移动动画参数
                }
            }
        }

        // 执行切换逻辑
        for (int i = 0; i < spawnedPlayers.Count; i++)
        {
            var p = spawnedPlayers[i];
            if (p == null) continue;

            bool isActive = (i == controlledIndex);
            SetPlayerControlState(p, isActive);

            if (isActive)
            {
                var shooting = p.GetComponent<PlayerShooting>();
                if (shooting != null)
                {
                    string charKey = StripCloneSuffix(p.name); // 当前角色 Key
                    shooting.ReinitializeForCharacter(charKey);
                    if (shooting.isMultiShapeCharacter)
                    {
                        shooting.SwitchShape(shooting.currentShapeKey);
                    }
                }
            }
        }

        currentPlayer = spawnedPlayers[controlledIndex];

        // 更新区域摄像机目标
        if (stageCameraController != null && currentPlayer != null)
        {
            stageCameraController.SetTarget(
                currentPlayer.transform,
                true
            );
        }

        // 刷新 UI
        if (healthBarUI != null) healthBarUI.BindToNewPlayer(currentPlayer);
        UpdateEEnergyUI();

        // 更新 BossTrigger
        BossTrigger bossTrigger = FindObjectOfType<BossTrigger>();
        if (bossTrigger != null)
        {
            bossTrigger.player = currentPlayer;
            bossTrigger.gutsmanMelee = currentPlayer.GetComponent<GutsmanMelee>();
        }

        LevelClearManager levelClearManager = FindObjectOfType<LevelClearManager>();
        if (levelClearManager != null)
        {
            levelClearManager.player = currentPlayer;
        }

        // 更新 PlayerPrefs
        if (controlledIndex < characterNames.Length)
        {
            PlayerPrefs.SetString("SelectedCharacter", characterNames[controlledIndex]);
            PlayerPrefs.Save();
        }

        Debug.Log($"切换控制角色 → {currentPlayer.name}");
    }




    // 控制角色的启用/禁用：active==true => 允许移动、启用物理、打开 collider、播放 Idle
    // active==false => 禁止移动（禁用 movement 脚本），禁用物理/碰撞，保持 Idle
    private void SetPlayerControlState(GameObject player, bool active)
    {
        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement>();
        var shooting = player.GetComponent<PlayerShooting>();
        var rb = player.GetComponent<Rigidbody2D>();
        var col = player.GetComponent<Collider2D>();
        var anim = player.GetComponent<Animator>();
        var sr = player.GetComponent<SpriteRenderer>();

        // ✅ 定义几个常用层
        int playerLayer = LayerMask.NameToLayer("Player");
        int hideLayer = LayerMask.NameToLayer("Hide");


        if (active)
        {
            // 启用可控状态
            if (movement != null)
            {
                movement.enabled = true;
                movement.canMove = true;
            }
            if (shooting != null)
            {
                shooting.enabled = true;
            }
            if (rb != null)
            {
                rb.simulated = true;
                rb.isKinematic = false;
                rb.velocity = Vector2.zero;
            }
            if (col != null) col.enabled = true;
            if (anim != null) anim.Play("Idle", 0, 0f);

            // ✅ 仅切换角色本体 Layer
            player.layer = playerLayer;
            // ✅ 可控角色的 Sprite 排序置前
            if (sr != null)
                sr.sortingOrder = 1;
        }
        else
        {
            // 禁止玩家操作并冻结物理
            if (movement != null)
            {
                movement.canMove = false;
            }
            if (shooting != null)
            {
                shooting.enabled = false;
                shooting.CancelCharge();
            }
            if (!allCharactersReady)
            {
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.isKinematic = true;
                    rb.simulated = false;
                }
            }
            if (anim != null)
            {
                // 🔹 先重置所有 bool 参数
                foreach (var param in anim.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        anim.SetBool(param.name, false);
                    }
                }
            }

            player.layer = hideLayer;
            if (sr != null)
                sr.sortingOrder = 0;
        }
    }



    // ----------------- 出生演出 -----------------
    private IEnumerator PlayCharacterSpawnSequence(GameObject player)
    {
        if (player == null) yield break;

        // ✅ 新增：在演出开始前清除蓄力状态
        var shooting = player.GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.CancelCharge();
        }

        if (screenFader != null)
            screenFader.SetAlphaInstant(1f);

        var anim = player.GetComponent<Animator>();
        var voiceSource = player.GetComponent<AudioSource>();
        var movement = player.GetComponent<PlayerMovement>();

        // 临时禁用控制脚本
        if (movement != null)
            movement.enabled = false;

        string selectedCharacter = StripCloneSuffix(player.name);
        AudioClip spawnVoice = null;
        foreach (var cv in characterVoices)
        {
            if (cv.characterKey == selectedCharacter)
            {
                spawnVoice = cv.spawnVoice;
                break;
            }
        }

        yield return new WaitForSeconds(0.2f);

        if (anim != null)
        {
            int shootingLayerIndex = 1;
            anim.SetLayerWeight(shootingLayerIndex, 0f);

            anim.Play("Reborn", 0, 0f);

            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeOutUnscaled());

            float rebornLength = 0f;
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                if (clip.name == "Reborn") rebornLength = clip.length;

            float timer = 0f;
            while (timer < rebornLength)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            anim.speed = 0f;
            anim.Play("Reborn", 0, 1f);
        }

        // 播放语音
        if (voiceSource != null && spawnVoice != null)
            voiceSource.PlayOneShot(spawnVoice);

        float voiceLength = spawnVoice != null ? spawnVoice.length : 0f;
        yield return new WaitForSecondsRealtime(voiceLength);

        // 恢复 Idle，允许脚本再次运行
        if (anim != null)
        {
            anim.speed = 1f;
            anim.Play("Idle", 0, 0f);
        }

        if (movement != null)
            movement.enabled = true;
        
        // 非 Rockman 的出生演出结束后恢复 BGM
        if (!selectedCharacter.Contains("Rockman") && bgmSource != null)
        {
            bgmSource.Play();
        }
        yield return new WaitForSeconds(0.5f);

    }

    // ----------------- 汇合点处理 -----------------
    public void OnCharacterReachedMeetingPoint()
    {
        // 如果已经到齐，就不再触发汇合演出
        if (allCharactersReady) return;

        if (currentPlayer != null)
        {
            StartCoroutine(CharacterMeetingRoutine(currentPlayer));
        }
    }

    private IEnumerator CharacterMeetingRoutine(GameObject player)
    {
        if (player == null) yield break;

        // 汇合演出开始前清除蓄力状态
        var shooting = player.GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.CancelCharge();
        }

        // 判断是不是最后一个角色
        bool isLastCharacter = (currentIndex >= characterPrefabs.Count - 1);

        // 如果不是最后一个角色，则停止 BGM
        if (!isLastCharacter && bgmSource != null)
            bgmSource.Stop();

        var movement = player.GetComponent<PlayerMovement>();
        var rb = player.GetComponent<Rigidbody2D>();
        var collider = player.GetComponent<Collider2D>();
        var sr = player.GetComponent<SpriteRenderer>();
        var anim = player.GetComponent<Animator>();

        // 保存 Animator 原始速度
        float originalAnimSpeed = 1f;
        if (anim != null)
        {
            originalAnimSpeed = anim.speed;
        }

        // 先禁止即时输入
        if (movement != null)
            movement.canMove = false;

        // 清空 Animator 的 bool 参数，防止奇怪状态继续播放
        if (anim != null)
        {
            foreach (var param in anim.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    anim.SetBool(param.name, false);
                }
            }
        }

        // 重置所有动画图层为默认值
        if (anim != null)
        {
            for (int i = 0; i < anim.layerCount; i++)
            {
                anim.SetLayerWeight(i, i == 0 ? 1f : 0f);
            }
        }

        // 找到该 player 的 spawnIndex（用于找到对应的 stayPoint）
        int playerIdx = spawnedPlayers.IndexOf(player);

        bool isRockman = NameMatches(player, "Rockman");

        // =========================================================
        // 洛克人：保持原来的自然进入汇合区域流程
        // =========================================================
        if (isRockman)
        {
            // 洛克人自然下落（物理）
            if (rb != null)
            {
                rb.simulated = true;
                rb.isKinematic = false;
                rb.gravityScale = 2.7f;
            }

            // 等待落地
            while (movement != null && !movement.isGrounded)
            {
                if (rb != null)
                    rb.gravityScale = 2.7f;

                yield return null;
            }

            // 落地后短暂停顿
            yield return new WaitForSeconds(1f);

            // 落地演出：左右看两次
            if (sr != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    sr.flipX = !sr.flipX;
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 瞬移到停留点后，更新复活点
            if (playerIdx >= 0 &&
                playerIdx < stayPoints.Count &&
                stayPoints[playerIdx] != null)
            {
                player.transform.position = stayPoints[playerIdx].position;

                string charKey = StripCloneSuffix(player.name);
                currentRespawnPoints[charKey] = stayPoints[playerIdx].position;
            }

            // 固定位置
            if (collider != null)
                collider.enabled = false;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
                rb.simulated = false;
            }

            if (movement != null)
                movement.enabled = false;
        }

        // =========================================================
        // 后六个角色：传送器 → Victory → 瞬移 → 等待 → 镜头切换
        // =========================================================
        else
        {
            // -----------------------------------------------------
            // 1. 传送前：彻底停止角色
            // -----------------------------------------------------

            if (movement != null)
                movement.enabled = false;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
                rb.simulated = false;
            }

            if (collider != null)
                collider.enabled = false;

            // -----------------------------------------------------
            // 2. 播放 Victory 正向动画
            // -----------------------------------------------------

            if (anim != null)
            {
                anim.speed = 1f;

                // 从 Victory 第一帧开始播放
                anim.Play("Victory", 0, 0f);
                // ★ 传送瞬间音效
                if (sfxSource != null && teleportStartSound != null)
                {
                    sfxSource.PlayOneShot(teleportStartSound);
                }
                // 等待 Victory 播放完成
                yield return new WaitForSeconds(0.2f);


                // 暂停 Animator
                anim.speed = 0f;
            }

            // -----------------------------------------------------
            // 3. Victory 播放完成
            //    锁定摄像机当前位置
            // -----------------------------------------------------

            if (stageCameraController != null)
            {
                stageCameraController.LockCameraPosition();
            }

            // -----------------------------------------------------
            // 4. 角色瞬间传送到 stayPoint
            //
            // 注意：摄像机此时已经被锁住，
            // 所以不会跟着角色一起移动。
            // -----------------------------------------------------

            if (playerIdx >= 0 &&
                playerIdx < stayPoints.Count &&
                stayPoints[playerIdx] != null)
            {
                player.transform.position = stayPoints[playerIdx].position;

                string charKey = StripCloneSuffix(player.name);
                currentRespawnPoints[charKey] = stayPoints[playerIdx].position;
            }

            //
            // Victory 最后一帧保持 1.5 秒
            // -----------------------------------------------------

            yield return new WaitForSeconds(1.5f);

            // -----------------------------------------------------
            // 5. 1 秒结束
            //    摄像机瞬间切到 stayPoint
            // -----------------------------------------------------

            if (stageCameraController != null)
            {
                stageCameraController.UnlockCameraPosition();

                stageCameraController.SetTarget(
                    player.transform,
                    true
                );

                // ★ 传送完成音效
                if (sfxSource != null && teleportFinishSound != null)
                {
                    sfxSource.PlayOneShot(teleportFinishSound);
                }
            }

            // -----------------------------------------------------
            // 6. Victory 倒放
            //
            // Unity Animator 不支持普通模式下 speed = -1。
            // 所以手动把 normalizedTime 从 1 推回 0。
            // -----------------------------------------------------

            if (anim != null)
            {
                const float victoryReverseDuration = 0.1f;

                float elapsed = 0f;

                while (elapsed < victoryReverseDuration)
                {
                    elapsed += Time.deltaTime;

                    float t = Mathf.Clamp01(
                        1f - elapsed / victoryReverseDuration
                    );

                    // 强制播放 Victory 的指定时间点
                    anim.Play("Victory", 0, t);

                    // 防止 Animator 自己继续向前播放
                    anim.speed = 0f;

                    yield return null;
                }

                // 确保最后停在 Victory 第一帧
                anim.Play("Victory", 0, 0f);
                anim.Update(0f);

                // 恢复 Animator
                anim.speed = 1f;

                // 回到 Idle
                anim.Play("Idle", 0, 0f);
            }

            // -----------------------------------------------------
            // 7. 原来的汇合演出：左右看两次
            // -----------------------------------------------------
            yield return new WaitForSeconds(1f);
            if (sr != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    sr.flipX = !sr.flipX;
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // -----------------------------------------------------
            // 8. 确保最终位置正确
            // -----------------------------------------------------

            if (playerIdx >= 0 &&
                playerIdx < stayPoints.Count &&
                stayPoints[playerIdx] != null)
            {
                player.transform.position = stayPoints[playerIdx].position;
            }

            // -----------------------------------------------------
            // 9. 最终保持禁用物理与脚本
            // -----------------------------------------------------

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
                rb.simulated = false;
            }

            if (sr != null)
                sr.sortingOrder = 0;
        }

        // =========================================================
        // 当前角色已经完成汇合 → 生成下一个角色
        // =========================================================

        if (currentIndex < characterPrefabs.Count - 1)
        {
            // 不是最后一个 → 生成下一个
            SpawnNextCharacter();
        }
        else
        {
            // 最后一个 → 所有角色到齐
            OnAllCharactersSpawned();
        }

        // 小延迟后恢复 BGM
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator CharacterEntranceRoutine(GameObject player)
    {
        // 可扩展：如果你要对每个出生也播放单独演出，可以在这里做（目前用 PlayCharacterSpawnSequence）
        yield return null;
    }

    // 辅助：判断名字（处理 Unity 自动后缀 "(Clone)"）
    private static bool NameMatches(GameObject g, string key)
    {
        if (g == null) return false;
        string name = StripCloneSuffix(g.name);
        return name.Equals(key, System.StringComparison.OrdinalIgnoreCase) || name.StartsWith(key, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string StripCloneSuffix(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// 获取某个玩家当前的复活点
    /// </summary>
    public bool TryGetRespawnPoint(GameObject player, out Vector3 respawnPos)
    {
        string key = StripCloneSuffix(player.name);
        if (currentRespawnPoints.TryGetValue(key, out respawnPos))
        {
            return true;
        }
        respawnPos = Vector3.zero;
        return false;
    }

}