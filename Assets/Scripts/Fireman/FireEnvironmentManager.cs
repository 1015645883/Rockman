using UnityEngine;
using TMPro;

public class FireEnvironmentManager : MonoBehaviour
{
    [Header("环境参数")]
    public float countdownTime = 15f;   // 倒计时时长
    private float currentTime;          // 当前倒计时
    private float zeroTimeCounter = 0f; // 玩家倒计时到0后计时3秒

    [Header("安全区设置")]
    public Collider2D[] safeZones;          // 普通安全区（可以多个）
    public Collider2D finalSafeZone;        // 最后一个安全区（UI隐藏用）
    private bool inSafeZone = false;        // 是否在普通安全区
    private bool inFinalSafeZone = false;   // 是否在最终安全区

    [Header("引用")]
    public GameObject fireIconPrefab;   // 火焰图标+数字的预制体（World Space Canvas）
    public Vector3 uiOffset = new Vector3(0, 2f, 0); // 在玩家头顶的偏移

    private Transform player;
    private GameObject fireIconInstance;
    private TextMeshProUGUI countdownText;
    private PlayerMovement playerMovement;

    private bool isUIActive = false; // 当前 UI 是否显示

    [Header("伤害参数")]
    public int damage = 2;

    private void Start()
    {
        // 根据玩家选择初始化倒计时
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Fireman"); // 默认 Fireman
        switch (selectedCharacter)
        {
            case "Fireman":
            case "Iceman":
                countdownTime = 20f;
                damage = 1;
                break;
            case "Bombman":
                countdownTime = 10f;
                damage = 3;
                break;
            default:
                countdownTime = 15f;
                break;
        }

        currentTime = countdownTime;
    }

    private void Update()
    {
        if (!player)
        {
            FindPlayerAndInitUI();
            return;
        }

        if (player == null || playerMovement == null) return;

        // 普通安全区检测
        inSafeZone = false;
        if (safeZones != null)
        {
            foreach (var zone in safeZones)
            {
                if (zone != null && zone.OverlapPoint(player.position))
                {
                    inSafeZone = true;
                    break;
                }
            }
        }

        // 最终安全区检测
        inFinalSafeZone = finalSafeZone != null && finalSafeZone.OverlapPoint(player.position);

        // UI显示控制
        if (fireIconInstance != null)
        {
            // 只有在最终安全区时隐藏 UI
            fireIconInstance.SetActive(!inFinalSafeZone);

            // 始终更新位置
            fireIconInstance.transform.position = player.position + uiOffset;
        }

        // 玩家死亡处理
        if (playerMovement.isDead)
        {
            if (isUIActive)
            {
                fireIconInstance.SetActive(false);
                isUIActive = false;
            }
            return;
        }
        else
        {
            if (!isUIActive)
            {
                fireIconInstance.SetActive(true);
                isUIActive = true;
                currentTime = countdownTime; // 复活重置倒计时
            }
        }

        HandleCountdown();
    }

    private void FindPlayerAndInitUI()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("Player 没有 PlayerMovement 组件！");
                return;
            }

            if (fireIconPrefab != null)
            {
                GameObject canvas = GameObject.Find("Canvas4");
                if (canvas == null)
                {
                    Debug.LogError("未找到 Canvas4，请确认场景中存在名为 Canvas4 的 Canvas");
                    return;
                }

                // 世界空间生成
                fireIconInstance = Instantiate(fireIconPrefab, player.position + uiOffset, Quaternion.identity);
                fireIconInstance.transform.SetParent(canvas.transform, worldPositionStays: true);

                // 获取数字组件
                countdownText = fireIconInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (countdownText == null)
                {
                    Debug.LogError("fireIconPrefab 内没有找到 TextMeshProUGUI 组件！");
                    return;
                }

                countdownText.text = Mathf.CeilToInt(currentTime).ToString();
                isUIActive = true;
            }
            else
            {
                Debug.LogError("未设置 fireIconPrefab！");
            }
        }
        else
        {
            Debug.LogWarning("未找到Tag=Player的对象！");
        }
    }

    private void HandleCountdown()
    {
        if (playerMovement.isDead) return;

        if (inSafeZone || inFinalSafeZone)
        {
            // 在任意安全区都恢复倒计时
            currentTime = Mathf.Lerp(currentTime, countdownTime, Time.deltaTime * 3f);
            zeroTimeCounter = 0f;
        }
        else
        {
            if (currentTime > 0f)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    zeroTimeCounter = 0f;
                    DamagePlayer();
                }
            }
            else
            {
                zeroTimeCounter += Time.deltaTime;
                if (zeroTimeCounter >= 3f)
                {
                    DamagePlayer();
                    zeroTimeCounter = 0f;
                }
            }
        }

        // 始终更新数字
        if (countdownText != null && isUIActive)
        {
            countdownText.text = Mathf.CeilToInt(currentTime).ToString();
            UpdateCountdownTextColor();
        }
    }

    private void UpdateCountdownTextColor()
    {
        if (countdownText == null) return;

        int t = Mathf.CeilToInt(currentTime);

        if (t >= 8) countdownText.color = Color.white;
        else if (t >= 6) countdownText.color = Color.yellow;
        else if (t >= 3) countdownText.color = new Color(1f, 0.65f, 0f);
        else countdownText.color = Color.red;
    }

    private void DamagePlayer()
    {
        if (player != null)
        {
            var health = player.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(damage);
        }
    }

    // 绘制安全区范围
    private void OnDrawGizmosSelected()
    {
        if (safeZones != null)
        {
            Gizmos.color = Color.green;
            foreach (var zone in safeZones)
            {
                if (zone == null) continue;
                if (zone is BoxCollider2D box)
                    Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
                else if (zone is CircleCollider2D circle)
                    Gizmos.DrawWireSphere(circle.bounds.center, circle.radius);
            }
        }

        if (finalSafeZone != null)
        {
            Gizmos.color = Color.blue;
            if (finalSafeZone is BoxCollider2D box)
                Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
            else if (finalSafeZone is CircleCollider2D circle)
                Gizmos.DrawWireSphere(circle.bounds.center, circle.radius);
        }
    }
}
