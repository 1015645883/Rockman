using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class BossTrigger : MonoBehaviour
{
    [Header("玩家相关")]
    public Transform playerSpawnPoint;
    public Transform gutsmanSpawnPoint;
    public GameObject player;
    public GutsmanMelee gutsmanMelee;

    [Header("Boss 相关")]
    public GameObject boss;
    public GameObject bossDoor; // Tilemap对象
    public GameObject bossHealthContainer;

    private Vector3 bossInitialPosition;
    private Quaternion bossInitialRotation;
    private Vector3 bossInitialScale;
    private bool bossInitialFlipX;
    private SpriteRenderer bossSpriteRenderer;

    [Header("摄像机相关")]
    public Camera mainCamera;
    public Camera bossCamera;
    private Vector3 mainCameraInitialPosition;
    private Quaternion mainCameraInitialRotation;
    [Header("音乐相关")]
    public AudioSource bgmSource;
    private AudioClip defaultBgmClip;
    public AudioClip bossBgm;

    [Header("对话系统")]
    public BossDialogueManager dialogueManager;
    public GameObject dialoguePanel;

    [System.Serializable]
    public class BossDialogueSet
    {
        public string playerName;
        public string playerKey;
        public BossDialogueManager.DialogueLine[] dialogueLines;
    }

    [Header("对话数组")]
    public BossDialogueSet[] dialogues = new BossDialogueSet[8];

    [Header("演出相关")]
    public AudioClip fillHealthSfx;
    private AudioSource fillAudioSource;

    // ✅ 新增：门音效
    [Header("门音效")]
    public AudioClip doorSfx;
    private AudioSource doorAudioSource;

    public static BossTrigger instance;
    private bool triggered = false;
    private AudioSource originalBgm;

    // ===== Tilemap门数据 =====
    private Tilemap doorTilemap;
    private Vector3Int[] doorTilesPos;
    private TileBase[] originalTiles;

    void Awake()
    {
        if (boss != null)
        {
            bossInitialPosition = boss.transform.position;
            bossInitialRotation = boss.transform.rotation;
            bossInitialScale = boss.transform.localScale;

            bossSpriteRenderer = boss.GetComponentInChildren<SpriteRenderer>();
            if (bossSpriteRenderer != null)
                bossInitialFlipX = bossSpriteRenderer.flipX;

        }
    }

    IEnumerator Start()
    {
        instance = this;

        yield return new WaitForSeconds(0.1f);

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            gutsmanMelee = player.GetComponent<GutsmanMelee>();
        }

        // 自动获取摄像机
        if (mainCamera == null)
        {
            if (player != null)
                mainCamera = player.GetComponentInChildren<Camera>();

            if (mainCamera == null)
                mainCamera = Camera.main;
            mainCameraInitialPosition = mainCamera.transform.position;
            mainCameraInitialRotation = mainCamera.transform.rotation;
        }
        if (bgmSource != null)
            defaultBgmClip = bgmSource.clip;

        if (bossCamera != null) bossCamera.enabled = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // ✅ 初始化门音效
        if (doorAudioSource == null)
        {
            doorAudioSource = gameObject.AddComponent<AudioSource>();
            doorAudioSource.playOnAwake = false;
        }

        // ===== Tilemap门初始化 =====
        if (bossDoor != null)
        {
            doorTilemap = bossDoor.GetComponent<Tilemap>();

            BoundsInt bounds = doorTilemap.cellBounds;

            var tempList = new System.Collections.Generic.List<Vector3Int>();

            foreach (var pos in bounds.allPositionsWithin)
            {
                if (doorTilemap.GetTile(pos) != null)
                {
                    tempList.Add(pos);
                    if (tempList.Count >= 4) break;
                }
            }

            doorTilesPos = tempList.ToArray();
            originalTiles = new TileBase[doorTilesPos.Length];

            for (int i = 0; i < doorTilesPos.Length; i++)
            {
                originalTiles[i] = doorTilemap.GetTile(doorTilesPos[i]);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(HandlePreBossSequence());
        }
    }

    IEnumerator MovePlayerToPoint(Vector3 targetPos, float speed)
    {
        var animator = player.GetComponent<Animator>();
        var rb = player.GetComponent<Rigidbody2D>();
        var sr = player.GetComponentInChildren<SpriteRenderer>();

        Vector3 lastPos = player.transform.position;
        float stuckTimer = 0f;
        float stuckThreshold = 0.2f; // 👉 0.2秒没动就判定卡住

        while (Vector2.Distance(player.transform.position, targetPos) > 0.05f)
        {
            Vector2 delta = targetPos - player.transform.position;

            float moveDirX = Mathf.Sign(delta.x);

            if (Mathf.Abs(delta.x) < 0.05f)
                moveDirX = 0;

            // ✅ 移动
            rb.velocity = new Vector2(moveDirX * speed, rb.velocity.y);

            // ✅ 动画
            if (animator != null)
                animator.SetBool("IsWalking", moveDirX != 0);

            // ✅ 朝向
            if (sr != null && moveDirX != 0)
                sr.flipX = moveDirX > 0;

            // ===== ✅ 防卡死检测 =====
            float movedDistance = Vector2.Distance(player.transform.position, lastPos);

            if (movedDistance < 0.001f)
            {
                stuckTimer += Time.fixedDeltaTime;

                if (stuckTimer >= stuckThreshold)
                {
                    Debug.Log("检测到玩家卡住，强制结束移动");
                    break;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            lastPos = player.transform.position;

            yield return new WaitForFixedUpdate();
        }

        // ✅ 强制对齐到目标点（防止差一点点）
        rb.position = new Vector2(targetPos.x, rb.position.y);

        // ✅ 停止移动
        rb.velocity = new Vector2(0, rb.velocity.y);

        // ✅ 朝向恢复（你要求右=true）
        if (sr != null)
            sr.flipX = true;

        // ✅ 动画复位
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsShooting", false);
            animator.SetBool("IsPunching", false);
            animator.SetBool("Dash", false);
            animator.SetBool("StartMoving", false);

            animator.Play("Idle", 0, 0f);
        }
    }

    // ===== 门打开 =====
    IEnumerator OpenDoorTiles()
    {
        if (doorTilemap == null) yield break;

        // ✅ 播放一次音效
        if (doorSfx != null && doorAudioSource != null)
            doorAudioSource.PlayOneShot(doorSfx);

        for (int i = 0; i < doorTilesPos.Length; i++)
        {
            doorTilemap.SetTile(doorTilesPos[i], null);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ===== 门关闭 =====
    IEnumerator CloseDoorTiles()
    {
        if (doorTilemap == null) yield break;

        // ✅ 播放一次音效
        if (doorSfx != null && doorAudioSource != null)
            doorAudioSource.PlayOneShot(doorSfx);

        for (int i = doorTilesPos.Length - 1; i >= 0; i--)
        {
            doorTilemap.SetTile(doorTilesPos[i], originalTiles[i]);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ===== 摄像机平滑 =====
    IEnumerator SmoothCameraTransition()
    {
        if (mainCamera == null || bossCamera == null) yield break;

        float duration = 1f;
        float timer = 0f;

        Transform cam = mainCamera.transform;

        Vector3 start = cam.position;
        Vector3 target = bossCamera.transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            cam.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        mainCamera.enabled = false;
        bossCamera.enabled = true;

        var mainListener = mainCamera.GetComponent<AudioListener>();
        if (mainListener) mainListener.enabled = false;

        var bossListener = bossCamera.GetComponent<AudioListener>();
        if (bossListener == null)
            bossListener = bossCamera.gameObject.AddComponent<AudioListener>();
        bossListener.enabled = true;
    }

    IEnumerator FillBossHealthBar()
    {
        if (boss == null) yield break;

        BossHealthSystem bossHealth = boss.GetComponent<BossHealthSystem>();
        if (bossHealth == null) yield break;

        bossHealth.SetHealthInstant(0);

        if (fillHealthSfx != null)
        {
            if (fillAudioSource == null)
            {
                fillAudioSource = gameObject.AddComponent<AudioSource>();
                fillAudioSource.loop = true;
                fillAudioSource.playOnAwake = false;
            }

            fillAudioSource.clip = fillHealthSfx;
            fillAudioSource.Play();
        }

        float fillDuration = 2f;
        float timer = 0f;

        while (timer < fillDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fillDuration);

            int newHealth = Mathf.RoundToInt(t * bossHealth.MaxHealth);
            bossHealth.SetHealthInstant(newHealth);

            yield return null;
        }

        bossHealth.SetHealthInstant(bossHealth.MaxHealth);

        if (fillAudioSource != null && fillAudioSource.isPlaying)
            fillAudioSource.Stop();
    }

    private IEnumerator HandlePreBossSequence()
    {
        IBossController[] bossControllers = boss.GetComponentsInChildren<IBossController>(true);

        foreach (var controller in bossControllers)
            controller.SetDialogueState(true);

        if (bgmSource != null)
        {
            originalBgm = bgmSource;
            bgmSource.Stop();
        }

        if (gutsmanMelee != null) gutsmanMelee.dialogueActive = true;

        var playerController = player.GetComponent<PlayerMovement>();
        var playerRb = player.GetComponent<Rigidbody2D>();

        if (playerController != null)
        {
            playerController.canMove = false; // ✅ 只禁止输入，不禁用脚本
        }

        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        Transform spawnPoint = playerSpawnPoint;
        string key = PlayerPrefs.GetString("SelectedCharacter", "");

        if (gutsmanSpawnPoint != null &&
            (key == "Gutsman" || key == "Cutman"))
        {
            spawnPoint = gutsmanSpawnPoint;
        }

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(OpenDoorTiles());

        Coroutine move = StartCoroutine(MovePlayerToPoint(spawnPoint.position, 3f));
        Coroutine cam = StartCoroutine(SmoothCameraTransition());

        yield return move;
        yield return cam;

        yield return new WaitForSeconds(0.2f);

        

        yield return StartCoroutine(CloseDoorTiles());

        yield return new WaitForSeconds(0.5f);

        if (boss != null) boss.SetActive(true);

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        bool dialogueFinished = false;

        BossDialogueManager.DialogueLine[] lines = dialogues[0].dialogueLines;

        foreach (var d in dialogues)
        {
            if (d.playerKey == key)
            {
                lines = d.dialogueLines;
                break;
            }
        }

        dialogueManager.StartDialogue(lines, () =>
        {
            dialogueFinished = true;
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        });

        yield return new WaitUntil(() => dialogueFinished);

        if (bossHealthContainer != null)
            bossHealthContainer.SetActive(true);

        yield return StartCoroutine(FillBossHealthBar());

        yield return new WaitForSeconds(1f);

        if (gutsmanMelee != null) gutsmanMelee.dialogueActive = false;

        if (playerController != null)
        {
            playerController.canMove = true;
        }

        foreach (var controller in bossControllers)
        {
            if (controller is MonoBehaviour mb)
                mb.enabled = true;
        }

        foreach (var controller in bossControllers)
        {
            controller.SetDialogueState(false);
            (controller as MonoBehaviour)?.StartCoroutine(controller.ChooseAction());
        }

        if (bgmSource != null && bossBgm != null)
        {
            bgmSource.clip = bossBgm;
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        Debug.Log("Boss战开始！");
    }

    public static void StopBGM()
    {
        if (instance != null && instance.bgmSource != null && instance.bgmSource.isPlaying)
        {
            instance.bgmSource.Stop();
        }
    }

    public void ResetBossToInitialState()
    {
        if (boss != null)
        {
            // ===== 1. 还原Transform =====
            boss.transform.position = bossInitialPosition;
            boss.transform.rotation = bossInitialRotation;
            boss.transform.localScale = bossInitialScale;

            if (bossSpriteRenderer != null)
                bossSpriteRenderer.flipX = bossInitialFlipX;

            // ===== 2. 还原血量 =====
            var bossHealth = boss.GetComponent<BossHealthSystem>();
            if (bossHealth != null)
            {
                bossHealth.SetHealthInstant(bossHealth.MaxHealth);
            }

            // ===== 3. 恢复物理 & 碰撞 =====
            var rb = boss.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = false;
                rb.simulated = true;
            }

            var col = boss.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = true;
            }

            // ===== 4. 恢复动画 =====
            var animator = boss.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1f;
                animator.Rebind();   // ✅ 强制重置动画状态机（关键！）
                animator.Update(0f);
            }

            // ===== 5. 重置 BossController =====
            foreach (var controller in boss.GetComponentsInChildren<IBossController>(true))
            {
                controller.SetDialogueState(true);

                if (controller is MonoBehaviour mono)
                {
                    // ❗停止所有旧协程
                    mono.StopAllCoroutines();

                    // ❗重新启用脚本
                    mono.enabled = true;

                    // ❗反射重置 isDead（你现在是这么设计的）
                    var type = mono.GetType();
                    var isDeadField = type.GetField("isDead");
                    if (isDeadField != null)
                    {
                        isDeadField.SetValue(mono, false);
                    }
                }
            }

            // ===== 6. 清理死亡残留（子物体可能被关掉）=====
            foreach (Transform child in boss.transform)
            {
                child.gameObject.SetActive(true);
            }

            // ===== 7. 重置Boss显示层（防止overlay错乱）=====
            var sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            var overlay = boss.GetComponent<BossHealthSystem>()?.overlayRenderer;
            if (overlay != null) overlay.enabled = false;

            // ===== 8. 最关键：重新激活Boss（但先关再开，确保状态刷新）=====
            boss.SetActive(false);
            boss.SetActive(true);
        }

        // ===== 摄像机恢复 =====
        if (mainCamera != null)
        {
            mainCamera.enabled = true;

            if (player != null)
            {
                Vector3 pos = mainCamera.transform.position;

                // ✅ 跟随玩家当前位置（关键）
                pos.x = player.transform.position.x;
                pos.y = player.transform.position.y;

                mainCamera.transform.position = pos;
            }

            var mainListener = mainCamera.GetComponent<AudioListener>();
            if (mainListener) mainListener.enabled = true;
        }

        if (bossCamera != null)
        {
            bossCamera.enabled = false;

            var bossListener = bossCamera.GetComponent<AudioListener>();
            if (bossListener) bossListener.enabled = false;
        }

        // ===== UI恢复 =====
        if (bossHealthContainer != null) bossHealthContainer.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // ===== 音乐恢复 =====
        if (bgmSource != null && defaultBgmClip != null)
        {
            bgmSource.clip = defaultBgmClip;
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        // ===== 门状态（防止卡门）=====
        if (doorTilemap != null && originalTiles != null)
        {
            for (int i = 0; i < doorTilesPos.Length; i++)
            {
                doorTilemap.SetTile(doorTilesPos[i], originalTiles[i]);
            }
        }

        // ===== 重置触发器 =====
        triggered = false;

        Debug.Log("Boss已完全重置（用于玩家死亡后重进）");
    }
}