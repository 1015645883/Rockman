using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GamePauseManager : MonoBehaviour
{
    public GameObject statusPanel;
    public ScreenFader screenFader;
    public GameObject player;
    public Material playerMaterial;
    public AudioSource audioSource;
    public AudioClip fadeInSound;
    public AudioClip shapeSwitchSound;

    private string[] shapes = { "R", "C", "G", "I", "B", "F", "E" };
    public Image[] energyIcons; // 6 个图标的 Image，顺序跟 energyBars 里的顺序一致
                                // 当前选中的行列
    private int currentRow = 0;
    private int currentCol = 0;

    // UI布局（-1表示没有按钮）
    private int[,] grid = new int[4, 2]
    {
    { 0, -1 },   // R
    { 1, 4 },    // C B
    { 2, 5 },    // G F
    { 3, 6 }     // I E
    };
    Dictionary<string, string> shapeToBoss = new Dictionary<string, string>()
{
    { "C", "Cutman" },
    { "G", "Gutsman" },
    { "I", "Iceman" },
    { "B", "Bombman" },
    { "F", "Fireman" },
    { "E", "Elecman" }
};
    private Sprite defaultRIconSprite;
    private int currentShapeIndex = 0;
    public bool isPaused = false;

    public Button[] shapeButtons;
    public GameObject energyBarC, energyBarG, energyBarI, energyBarB, energyBarF, energyBarE;

    private Dictionary<string, GameObject> energyBars;
    [Header("Etank道具")]
    public Button etankButton;       // UI 按钮
    public Text etankCountText;      // 显示数量的文字
    private int etankCount = 0;      // 玩家持有的Etank数量
    [Header("Etank音效")]
    public AudioClip etankUseSound;     // 使用E罐的音效
    public AudioClip etankFailSound;    // 满血时点击的提示音效

    public string SelectedChar

    {
        get
        {
            return PlayerPrefs.GetString("SelectedCharacter", "");
        }
    }

    void Start()
    {
        statusPanel.SetActive(false);
        // 加载玩家Etank数量
        etankCount = PlayerPrefs.GetInt("PlayerEtankCount", 0);
        foreach (var btn in shapeButtons)
        {
            btn.transition = Selectable.Transition.None; // ❗关键
        }
        UpdateEtankUI();

        if (etankButton != null)
            etankButton.onClick.AddListener(UseEtank);
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        if (energyIcons != null && energyIcons.Length > 0)
            defaultRIconSprite = energyIcons[0].sprite;
        energyBars = new Dictionary<string, GameObject>
        {
            { "C", energyBarC },
            { "G", energyBarG },
            { "I", energyBarI },
            { "B", energyBarB },
            { "F", energyBarF },
            { "E", energyBarE }
        };

        // 不禁用能量条，初始全透明
        foreach (var bar in energyBars.Values)
        {
            SetAlphaRecursive(bar, 0f);
        }

        StartCoroutine(WaitForPlayer());

    }

    IEnumerator WaitForPlayer()
    {
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        var renderer = player.GetComponent<Renderer>();
        if (renderer != null)
            playerMaterial = renderer.material;

        for (int i = 0; i < shapeButtons.Length; i++)
        {
            int index = i;
            shapeButtons[i].onClick.AddListener(() => SwitchToShape(index));
        }

        Debug.Log("GamePauseManager 成功绑定玩家: " + player.name);

        // 🔹 动态更新能量条显示
        UpdateEnergyBarsByCurrentPlayer();
    }

    public void UpdateEnergyBarsByCurrentPlayer()
    {
        // 获取当前选中角色
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");

        for (int i = 0; i < energyIcons.Length; i++)
        {
            string shapeKey = shapes[i];
            bool visible = false;

            if (i == 0) // 血条 R 总是显示
            {
                visible = true;
            }
            else if (shapeKey == "E")
            {
                // Elecman 永久显示 E 条
                if (selectedChar.Equals("Elecman", System.StringComparison.OrdinalIgnoreCase))
                {
                    visible = true;
                }
                else
                {
                    // Rockman/Blues 在切换到 E 形态时显示
                    visible = (selectedChar.Equals("Rockman", System.StringComparison.OrdinalIgnoreCase)
                               || selectedChar.Equals("Blues", System.StringComparison.OrdinalIgnoreCase))
                              && currentShapeIndex == i;
                }
            }
            else if (selectedChar.Equals("Rockman", System.StringComparison.OrdinalIgnoreCase)
                     || selectedChar.Equals("Blues", System.StringComparison.OrdinalIgnoreCase))
            {
                // 其他形态条只显示当前选中形态
                visible = currentShapeIndex == i;
            }
            else
            {
                // 其他角色不显示能量条
                visible = false;
            }

            if (energyBars.ContainsKey(shapeKey))
                SetEnergyBarVisible(energyBars[shapeKey], visible);
        }

    }

    public void UpdateIconsByCurrentPlayer()
    {
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");

        for (int i = 0; i < energyIcons.Length; i++)
        {
            string shape = shapes[i];

            bool visible = false;

            if (i == 0)
            {
                visible = true; // R永远显示
            }
            else if (selectedChar == "Rockman" || selectedChar == "Blues")
            {
                // 👉 只有解锁的才显示
                visible = IsShapeUnlocked(shape);
            }
            else
            {
                visible = false;
            }

            if (energyIcons[i] != null)
                energyIcons[i].enabled = visible;
        }
    }


    void Update()
    {
        UpdateEnergyBarsByCurrentPlayer();

        // 暂停面板开关
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isPaused)
                StartCoroutine(CloseStatusPanel());
            else
                StartCoroutine(OpenStatusPanel());
        }
        string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
        if (isPaused)
        {
            if (currentChar == "Rockman" || currentChar == "Blues")
            {
                HandleCursorInput(); // ✅ 只有这两个能操作
            }
        }
        // 🔹 如果当前角色是 Rockman 或 Blues 且未暂停，则允许用 U / I 键切换形态
        if (!isPaused && (currentChar == "Rockman" || currentChar == "Blues"))
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                currentShapeIndex = GetNextUnlockedShape(currentShapeIndex, +1);
                SwitchToShape(currentShapeIndex);
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                currentShapeIndex = GetNextUnlockedShape(currentShapeIndex, -1);
                SwitchToShape(currentShapeIndex);
            }
        }
    }


    IEnumerator OpenStatusPanel()
    {
        foreach (var btn in shapeButtons)
        {
            btn.interactable = false;
        }
        if (fadeInSound != null)
            audioSource.PlayOneShot(fadeInSound);

        statusPanel.SetActive(true);

        string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
        bool canSwitchShape = currentChar == "Rockman" || currentChar == "Blues";

        // 替换第一个图标
        UpdateRIconByPlayer(currentChar);
        etankCount = PlayerPrefs.GetInt("PlayerEtankCount", 0);
        UpdateEtankUI();
        UpdateIconsByCurrentPlayer();
        // 控制形态按钮（只有洛克人和布鲁斯能用）
        for (int i = 0; i < shapeButtons.Length; i++)
        {
            string shape = shapes[i];

            bool visible = false;

            if (i == 0)
            {
                visible = true; // R永远显示
            }
            else if (canSwitchShape)
            {
                // ✅ 关键：只显示已解锁
                visible = IsShapeUnlocked(shape);
            }

            shapeButtons[i].gameObject.SetActive(visible);
        }

        // 控制能量条和图标
        for (int i = 0; i < energyIcons.Length; i++)
        {
            string shape = shapes[i];

            bool visible = false;

            if (i == 0)
            {
                visible = true; // R永远显示
            }
            else if (canSwitchShape)
            {
                // ✅ 关键：只有解锁的才显示
                visible = IsShapeUnlocked(shape);
            }

            if (energyIcons[i] != null)
                energyIcons[i].enabled = visible;

            if (energyBars.ContainsKey(shape))
                energyBars[shape].SetActive(visible);
        }

        // 🔹 单独处理 Elecman E 能量条
        if (currentChar == "Elecman" && energyBarE != null)
        {
            energyBarE.SetActive(true);
            SetAlphaRecursive(energyBarE, 1f);
        }

        Time.timeScale = 0f;
        isPaused = true;
        SetCursorToCurrentShape();
        UpdateButtonVisual();
        yield return StartCoroutine(screenFader.FadeInUnscaled());
    }




    IEnumerator CloseStatusPanel()
    {
        // 按下关闭时，先暂停游戏时间
        Time.timeScale = 0f;

        // 播放不受时间缩放影响的淡出动画
        yield return StartCoroutine(screenFader.FadeOutUnscaled());

        // 淡出动画完毕后，恢复时间，隐藏面板
        Time.timeScale = 1f;
        statusPanel.SetActive(false);
        isPaused = false;
        // 🔹 修复能量条消失的问题
        string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
        if (currentChar == "Elecman" && energyBarE != null)
        {
            SetEnergyBarVisible(energyBarE, true);
        }
    }

    private void UpdateRIconByPlayer(string playerName)
    {
        if (energyIcons == null || energyIcons.Length < 7) return;

        // 角色名与能量图标索引的对应关系（根据你说的第2到第7个是对应角色）
        Dictionary<string, int> playerToIconIndex = new Dictionary<string, int>()
    {
        { "Cutman", 1 },
        { "Gutsman", 2 },
        { "Iceman", 3 },
        { "Bombman", 4 },
        { "Fireman", 5 },
        { "Elecman", 6 }
    };

        if (playerName == "Rockman" || playerName == "Blues")
        {
            // 恢复默认 R 图标
            energyIcons[0].sprite = defaultRIconSprite;
        }
        else if (playerToIconIndex.ContainsKey(playerName))
        {
            int iconIndex = playerToIconIndex[playerName];
            energyIcons[0].sprite = energyIcons[iconIndex].sprite;
        }
        else
        {
            // 如果是其他角色，恢复默认图标
            energyIcons[0].sprite = defaultRIconSprite;
        }
    }

    void SetCursorToCurrentShape()
    {
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 2; c++)
            {
                if (grid[r, c] == currentShapeIndex)
                {
                    currentRow = r;
                    currentCol = c;
                    return;
                }
            }
        }
    }

    void HandleCursorInput()
    {
        int currentIndex = grid[currentRow, currentCol];
        int newIndex = currentIndex;

        if (Input.GetKeyDown(KeyCode.W))
            newIndex = FindNextValidIndex(currentIndex, Vector2.up);

        if (Input.GetKeyDown(KeyCode.S))
            newIndex = FindNextValidIndex(currentIndex, Vector2.down);

        if (Input.GetKeyDown(KeyCode.A))
            newIndex = FindNextValidIndex(currentIndex, Vector2.left);

        if (Input.GetKeyDown(KeyCode.D))
            newIndex = FindNextValidIndex(currentIndex, Vector2.right);

        // 更新光标位置
        SetCursorByIndex(newIndex);

        UpdateButtonVisual();

        if (Input.GetKeyDown(KeyCode.J))
        {
            SwitchToShape(newIndex);
        }
    }

    void SetCursorByIndex(int index)
    {
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 2; c++)
            {
                if (grid[r, c] == index)
                {
                    currentRow = r;
                    currentCol = c;
                    return;
                }
            }
        }
    }

    void UpdateButtonVisual()
    {
        int cursorIndex = grid[currentRow, currentCol];

        for (int i = 0; i < shapeButtons.Length; i++)
        {
            Image img = shapeButtons[i].image;
            Color color = img.color;

            if (i == currentShapeIndex)
            {
                // ⭐⭐⭐ 当前形态（完全不透明）
                color.a = 1f;   // 255
            }
            else if (i == cursorIndex)
            {
                // ⭐⭐ 光标选中（中等透明）
                color.a = 0.6f;
            }
            else
            {
                // ⭐ 普通（半透明）
                color.a = 0.2f;
            }

            img.color = color;
        }
    }

    // 🔹 在 SwitchToShape 切换形态后也调用，保证实时更新
    public void SwitchToShape(int index)
    {
        if (index < 0 || index >= shapes.Length) return;

        currentShapeIndex = index;
        string shape = shapes[index];
        Debug.Log("Switched to " + shape);

        if (shapeSwitchSound != null)
            audioSource.PlayOneShot(shapeSwitchSound);

        switch (shape)
        {
            case "R": SwitchToR(); break;
            case "C": SwitchToC(); break;
            case "G": SwitchToG(); break;
            case "I": SwitchToI(); break;
            case "B": SwitchToB(); break;
            case "F": SwitchToF(); break;
            case "E": SwitchToE(); break;
        }

        UpdateEnergyBar(shape);
        UpdateEnergyBarsByCurrentPlayer(); // 🔹 保证实时刷新

        // 切换形态后取消蓄力
        var playerShooting = player.GetComponent<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.CancelCharge();
        }
    }

    private void SetAlphaRecursive(GameObject obj, float alpha)
    {
        if (obj == null) return;

        var images = obj.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    private void SetEnergyBarVisible(GameObject bar, bool visible)
    {
        float alpha = visible ? 1f : 0f;
        SetAlphaRecursive(bar, alpha);

        var canvasGroup = bar.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = visible;
    }

    private void UpdateEnergyBar(string shape)
    {
        foreach (var kv in energyBars)
        {
            SetEnergyBarVisible(kv.Value, kv.Key == shape && shape != "R");
        }
    }


    // ----------------- 各形态切换 -----------------

    public void SwitchToR()
    {
        Debug.Log("Switched to R");
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 0);
            player.GetComponent<PlayerShooting>().SwitchShape("R");
            player.GetComponent<PlayerMovement>().SwitchShape("R");
        }
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && movement.animator != null)
            movement.animator.runtimeAnimatorController = movement.defaultController;
    }

    public void SwitchToC()
    {
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 1);
            player.GetComponent<PlayerShooting>().SwitchShape("C");
            player.GetComponent<PlayerMovement>().SwitchShape("C");
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null && movement.animator != null)
                movement.animator.runtimeAnimatorController = movement.cShapeOverrideController;
        }
    }

    public void SwitchToG()
    {
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 2);
            player.GetComponent<PlayerShooting>().SwitchShape("G");
            player.GetComponent<PlayerMovement>().SwitchShape("G");
        }
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && movement.animator != null)
            movement.animator.runtimeAnimatorController = movement.gShapeOverrideController;
    }

    public void SwitchToI()
    {
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 3);
            player.GetComponent<PlayerShooting>().SwitchShape("I");
            player.GetComponent<PlayerMovement>().SwitchShape("I");
        }
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && movement.animator != null)
            movement.animator.runtimeAnimatorController = movement.iShapeOverrideController;
    }

    public void SwitchToB()
    {
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 4);
            player.GetComponent<PlayerShooting>().SwitchShape("B");
            player.GetComponent<PlayerMovement>().SwitchShape("B");
        }
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && movement.animator != null)
            movement.animator.runtimeAnimatorController = movement.bShapeOverrideController;
    }

    public void SwitchToF()
    {
        Debug.Log("Switched to F");
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 5);
            player.GetComponent<PlayerShooting>().SwitchShape("F");
            player.GetComponent<PlayerMovement>().SwitchShape("F");
        }
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && movement.animator != null)
            movement.animator.runtimeAnimatorController = movement.fShapeOverrideController;
    }

    public void SwitchToE()
    {
        if (playerMaterial != null)
        {
            playerMaterial.SetInt("_ColorReplaceMode", 6);
            player.GetComponent<PlayerShooting>().SwitchShape("E");
            player.GetComponent<PlayerMovement>().SwitchShape("E");
        }
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null && movement.animator != null)
            movement.animator.runtimeAnimatorController = movement.eShapeOverrideController;
    }

    bool IsShapeUnlocked(string shape)
    {
        if (shape == "R") return true; // R 永远可用

        if (!shapeToBoss.ContainsKey(shape)) return false;

        string bossName = shapeToBoss[shape];

        // 👉 关键：读取你存的击败数据
        return PlayerPrefs.GetInt(bossName + "Defeated", 0) == 1;
    }

    int FindNextValidIndex(int currentIndex, Vector2 direction)
    {
        Vector2 currentPos = GetGridPosition(currentIndex);

        List<int> valid = new List<int>();

        // 收集所有已解锁
        for (int i = 0; i < shapes.Length; i++)
        {
            if (!IsShapeUnlocked(shapes[i])) continue;
            if (i == currentIndex) continue;

            valid.Add(i);
        }

        if (valid.Count == 0)
            return currentIndex;

        // =========================
        // ⭐ 方向轴限制（你要的关键）
        // =========================

        bool isHorizontal = Mathf.Abs(direction.x) > 0;
        bool isVertical = Mathf.Abs(direction.y) > 0;

        int sameRowCount = 0;
        int sameColCount = 0;

        foreach (var i in valid)
        {
            Vector2 pos = GetGridPosition(i);

            if (Mathf.Approximately(pos.y, currentPos.y))
                sameRowCount++;

            if (Mathf.Approximately(pos.x, currentPos.x))
                sameColCount++;
        }

        // =========================
        // ⭐ 第一轮：严格方向
        // =========================

        int best = -1;
        float bestScore = float.MinValue;

        foreach (var i in valid)
        {
            Vector2 targetPos = GetGridPosition(i);
            Vector2 delta = (targetPos - currentPos).normalized;

            float dot = Vector2.Dot(delta, direction);

            if (dot > 0.7f) // 严格方向
            {
                float dist = Vector2.Distance(currentPos, targetPos);
                float score = dot * 10f - dist;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
        }

        if (best != -1)
            return best;

        // =========================
        // ⭐ 第二轮：宽松方向（允许斜）
        // =========================

        bestScore = float.MinValue;

        foreach (var i in valid)
        {
            Vector2 targetPos = GetGridPosition(i);
            Vector2 delta = (targetPos - currentPos).normalized;

            float dot = Vector2.Dot(delta, direction);

            if (dot > 0.2f) // 放宽
            {
                float dist = Vector2.Distance(currentPos, targetPos);
                float score = dot * 5f - dist;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
        }

        if (best != -1)
            return best;

        return currentIndex;
    }

    Vector2 GetGridPosition(int index)
    {
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 2; c++)
            {
                if (grid[r, c] == index)
                {
                    return new Vector2(c, -r); // 注意Y取反更直观
                }
            }
        }
        return Vector2.zero;
    }

    int GetNextUnlockedShape(int startIndex, int direction)
    {
        int count = shapes.Length;

        for (int i = 1; i <= count; i++)
        {
            int index = (startIndex + i * direction + count) % count;

            string shape = shapes[index];

            if (IsShapeUnlocked(shape))
            {
                return index;
            }
        }

        return startIndex; // 理论不会发生（因为R永远解锁）
    }

    private void UpdateEtankUI()
    {
        if (etankCountText != null)
            etankCountText.text = "×" + etankCount.ToString();

        if (etankButton != null)
            etankButton.interactable = etankCount > 0;
    }
    public void AddEtank(int amount)
    {
        etankCount += amount;
        PlayerPrefs.SetInt("PlayerEtankCount", etankCount);
        UpdateEtankUI();
    }

    public void UseEtank()
    {
        if (etankCount <= 0 || player == null) return;

        HealthSystem hs = player.GetComponent<HealthSystem>();
        if (hs != null)
        {
            if (hs.CurrentHealth < hs.MaxHealth)
            {
                // 使用E罐
                hs.Heal(hs.MaxHealth);
                etankCount--;
                PlayerPrefs.SetInt("PlayerEtankCount", etankCount);
                UpdateEtankUI();

                if (etankUseSound != null)
                    audioSource.PlayOneShot(etankUseSound);
            }
            else
            {
                // 满血时点击 → 播放提示音效，不消耗E罐
                if (etankFailSound != null)
                    audioSource.PlayOneShot(etankFailSound);
            }
        }
    }




    public void ExitToStageSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect");
    }
}


