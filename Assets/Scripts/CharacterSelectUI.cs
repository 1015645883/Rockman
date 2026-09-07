using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;  // 加载场景需要

public class CharacterSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class CharacterData
    {
        public string key;
        public string displayName;
        public string description;
        public Sprite bigSprite;
        public Sprite iconSprite;
        public Vector2 displaySize = new Vector2(400, 400);
        public AudioClip confirmVoice;
    }

    public CharacterData[] characters;

    [Header("UI引用")]
    public Image selectedCharacterImage;
    public TMP_Text characterNameText;
    public TMP_Text characterDescriptionText;
    public RectTransform contentPanel;
    public GameObject iconImagePrefab;
    public Button upButton;
    public Button downButton;
    public Button confirmButton;
    public Button backButton;  // 新增返回按钮
    public CanvasGroup flashCanvasGroup; // 给闪屏 Image 绑定 CanvasGroup

    [Header("效果引用")]
    public Image flashImage;
    public float flashDuration = 0.2f;
    public AudioSource audioSource;

    [Header("音效")]
    public AudioClip scrollSound;   // 上下选择音效
    public AudioClip confirmSound;  // 确认音效
    public AudioClip errorSound;
    [Header("头像显示设置")]
    public Vector2 normalIconSize = new Vector2(200, 200);
    public Vector2 selectedIconSize = new Vector2(250, 250);
    public float scrollDuration = 0.3f;
    public float slotSpacing = 300f; // 每个头像间距
    private Image[] lockIcons = new Image[5];
    private int selectedIndex = 0;
    private Image[] iconImages = new Image[5]; // 上上 / 上 / 中 / 下 / 下下
    private bool isScrolling = false;

    void Start()
    {
        InitIcons();
        UpdateUI();

        upButton.onClick.AddListener(() => ChangeSelection(-1));
        downButton.onClick.AddListener(() => ChangeSelection(1));
        confirmButton.onClick.AddListener(ConfirmSelection);
        backButton.onClick.AddListener(OnBackButton);  // 返回按钮监听

        if (flashImage != null)
        {
            var c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }
    }

    void InitIcons()
    {
        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        for (int i = 0; i < 5; i++)
        {
            GameObject go = Instantiate(iconImagePrefab, contentPanel);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, (2 - i) * slotSpacing);

            iconImages[i] = go.GetComponent<Image>();

            // ⭐ 获取子物体 LockIcon
            lockIcons[i] = go.transform.Find("Lock").GetComponent<Image>();
        }
    }

    bool IsCharacterUnlocked(string key)
    {
        // Rockman / Blues 永远可选
        if (key == "Rockman" || key == "Blues")
            return true;

        // Boss角色 → 必须击败
        return PlayerPrefs.GetInt(key + "Defeated", 0) == 1;
    }

    void ChangeSelection(int delta)
    {
        if (isScrolling || characters.Length == 0) return;

        selectedIndex = (selectedIndex + delta + characters.Length) % characters.Length;

        // 播放滚动音效
        if (audioSource != null && scrollSound != null)
        {
            audioSource.PlayOneShot(scrollSound);
        }

        StartCoroutine(AnimateIcons(delta));
    }

    IEnumerator AnimateIcons(int delta)
    {
        isScrolling = true;
        Vector2[] startLockSize = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            startLockSize[i] = lockIcons[i].rectTransform.sizeDelta;
        }
        // 起始状态
        Vector2[] startPos = new Vector2[5];
        Vector2[] startSize = new Vector2[5];
        Color[] startColor = new Color[5];
        for (int i = 0; i < 5; i++)
        {
            startPos[i] = iconImages[i].rectTransform.anchoredPosition;
            startSize[i] = iconImages[i].rectTransform.sizeDelta;
            startColor[i] = iconImages[i].color;
        }

        // 目标状态
        Vector2[] targetPos = new Vector2[5];
        Vector2[] targetSize = new Vector2[5];
        Color[] targetColor = new Color[5];

        float moveOffset = (delta > 0) ? slotSpacing : -slotSpacing;

        for (int i = 0; i < 5; i++)
        {
            targetPos[i] = startPos[i] + new Vector2(0, moveOffset);

            // 计算滚动后的新位置索引
            int newSlotIndex = i - delta;
            int charIndex = (selectedIndex + (newSlotIndex - 2) + characters.Length) % characters.Length;
            bool unlocked = IsCharacterUnlocked(characters[charIndex].key);
            if (newSlotIndex < 0) newSlotIndex += 5;
            if (newSlotIndex > 4) newSlotIndex -= 5;

            if (newSlotIndex == 2)
            {
                targetSize[i] = selectedIconSize;

                // 未解锁不变亮
                targetColor[i] = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }
            else
            {
                targetSize[i] = normalIconSize;

                targetColor[i] = unlocked ? Color.gray : new Color(0.4f, 0.4f, 0.4f, 1f);
            }
        }

        // 动画
        float t = 0f;
        while (t < scrollDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / scrollDuration);

            for (int i = 0; i < 5; i++)
            {
                iconImages[i].rectTransform.anchoredPosition = Vector2.Lerp(startPos[i], targetPos[i], progress);
                iconImages[i].rectTransform.sizeDelta = Vector2.Lerp(startSize[i], targetSize[i], progress);
                if (lockIcons[i] != null)
                {
                    RectTransform lockRT = lockIcons[i].rectTransform;

                    lockRT.sizeDelta = Vector2.Lerp(startLockSize[i], targetSize[i], progress);
                    lockRT.anchoredPosition = Vector2.zero;
                }
                iconImages[i].color = Color.Lerp(startColor[i], targetColor[i], progress);
            }
            yield return null;
        }

        // 位置循环 & 刷新头像
        if (delta > 0) // 向下滚
        {
            Image topIcon = iconImages[0];
            Image topLock = lockIcons[0];

            for (int i = 0; i < 4; i++)
            {
                iconImages[i] = iconImages[i + 1];
                lockIcons[i] = lockIcons[i + 1]; // ⭐ 同步！
            }

            iconImages[4] = topIcon;
            lockIcons[4] = topLock; // ⭐ 同步！
        }
        else // 向上滚
        {
            Image bottomIcon = iconImages[4];
            Image bottomLock = lockIcons[4];

            for (int i = 4; i > 0; i--)
            {
                iconImages[i] = iconImages[i - 1];
                lockIcons[i] = lockIcons[i - 1]; // ⭐ 同步！
            }

            iconImages[0] = bottomIcon;
            lockIcons[0] = bottomLock; // ⭐ 同步！
        }

        UpdateUI();
        isScrolling = false;
    }

    void UpdateUI()
    {
        int total = characters.Length;
        if (total == 0) return;

        for (int i = 0; i < 5; i++)
        {
            int charIndex = (selectedIndex + (i - 2) + total) % total;
            var data = characters[charIndex];

            iconImages[i].sprite = data.iconSprite;

            bool unlocked = IsCharacterUnlocked(data.key);

            // ⭐ 锁显示
            if (lockIcons[i] != null)
                lockIcons[i].gameObject.SetActive(!unlocked);

            iconImages[i].rectTransform.anchoredPosition = new Vector2(0, (2 - i) * slotSpacing);
            Vector2 targetSize = (i == 2) ? selectedIconSize : normalIconSize;

            // 头像尺寸
            iconImages[i].rectTransform.sizeDelta = targetSize;

            // ⭐⭐⭐ 关键：锁的尺寸 = 头像尺寸
            if (lockIcons[i] != null)
            {
                RectTransform lockRT = lockIcons[i].rectTransform;
                lockRT.sizeDelta = targetSize;

                // 确保锁在正中（防止偏移）
                lockRT.anchoredPosition = Vector2.zero;
            }

            // ⭐ 未解锁变暗
            if (!unlocked)
                iconImages[i].color = new Color(0.4f, 0.4f, 0.4f, 1f);
            else
                iconImages[i].color = (i == 2) ? Color.white : Color.gray;
        }

        var selectedData = characters[selectedIndex];
        bool unlockedSelected = IsCharacterUnlocked(selectedData.key);

        // 设置立绘
        selectedCharacterImage.sprite = selectedData.bigSprite;
        SetImageSize(selectedCharacterImage, selectedData.displaySize);

        // 立绘变暗
        if (!unlockedSelected)
        {
            selectedCharacterImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        else
        {
            selectedCharacterImage.color = Color.white;
        }

        //  名字
        characterNameText.text = selectedData.displayName;

        //  描述
        if (characterDescriptionText != null)
        {
            if (!unlockedSelected)
            {
                characterDescriptionText.text = "？？？";
                characterDescriptionText.alignment = TMPro.TextAlignmentOptions.Center;
            }
            else
            {
                characterDescriptionText.text = selectedData.description;
                characterDescriptionText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            }
        }
    }


    void SetImageSize(Image img, Vector2 maxSize)
    {
        if (img.sprite == null) return;
        float spriteWidth = img.sprite.rect.width;
        float spriteHeight = img.sprite.rect.height;
        float spriteRatio = spriteWidth / spriteHeight;
        float maxRatio = maxSize.x / maxSize.y;

        if (spriteRatio > maxRatio)
            img.rectTransform.sizeDelta = new Vector2(maxSize.x, maxSize.x / spriteRatio);
        else
            img.rectTransform.sizeDelta = new Vector2(maxSize.y * spriteRatio, maxSize.y);
    }

    void ConfirmSelection()
    {
        var data = characters[selectedIndex];

        bool unlocked = IsCharacterUnlocked(data.key);

        // ❌ 未解锁
        if (!unlocked)
        {
            Debug.Log("角色未解锁");

            if (audioSource != null && errorSound != null)
            {
                audioSource.PlayOneShot(errorSound); // 播放错误音效
            }

            return; // ⭐ 直接拦截！
        }

        // ✅ 正常选择
        PlayerPrefs.SetString("SelectedCharacter", data.key);
        PlayerPrefs.Save();

        Debug.Log("角色已选中：" + data.key);

        if (audioSource != null && data.confirmVoice != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(data.confirmVoice);
        }

        if (flashImage != null)
            StartCoroutine(FlashScreen());
    }


    IEnumerator FlashScreen()
    {
        flashCanvasGroup.blocksRaycasts = false; // 不阻挡事件

        float half = flashDuration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(0f, 1f, t / half));
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(1f, 0f, t / half));
            yield return null;
        }

        SetFlashAlpha(0f);
    }

    void SetFlashAlpha(float alpha)
    {
        if (flashImage != null)
        {
            var c = flashImage.color;
            c.a = alpha;
            flashImage.color = c;
        }
    }

    // 新增 返回关卡选择界面方法
    void OnBackButton()
    {

        // 请替换成你自己的场景名
        SceneManager.LoadScene("LevelSelect");
    }
}
