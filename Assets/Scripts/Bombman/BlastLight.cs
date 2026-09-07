using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class BlastLight : MonoBehaviour
{
    [Header("光源设置")]
    public Light2D blastLight;
    public float duration = 0.3f;
    public float maxIntensity = 2.5f;

    private float timer = 0f;
    private SpecialLevelManager specialLevel;

    void Awake()
    {
        if (blastLight == null)
            blastLight = GetComponent<Light2D>();

        // 🚫 默认关闭（防闪）
        if (blastLight != null)
            blastLight.enabled = false;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "WilyStage1")
        {
            Destroy(gameObject);
            return;
        }

        specialLevel = FindObjectOfType<SpecialLevelManager>();

        // ❗初始化时就判断能不能存在
        if (!CanUseLight())
        {
            Destroy(gameObject);
            return;
        }

        // ✅ 真正允许才开启
        blastLight.enabled = true;
        blastLight.intensity = maxIntensity;
    }

    void Update()
    {
        if (blastLight == null || specialLevel == null)
            return;

        // 🔒 汇合完成 → 直接消失
        if (IsAllCharactersReady())
        {
            Destroy(gameObject);
            return;
        }

        // 🚫 中途切角色 → 直接消失（避免乱闪）
        if (!CanUseLight())
        {
            Destroy(gameObject);
            return;
        }

        // ✅ 正常衰减
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        blastLight.intensity = Mathf.Lerp(maxIntensity, 0f, t);

        if (timer >= duration)
            Destroy(gameObject);
    }

    // ⭐ 判断当前是否允许光照
    private bool CanUseLight()
    {
        if (specialLevel == null || specialLevel.currentPlayer == null)
            return false;

        string name = specialLevel.currentPlayer.name;

        if (name.EndsWith("(Clone)"))
            name = name.Replace("(Clone)", "").Trim();

        return name == "PlayableBombman" || name == "PlayableFireman";
    }

    // 检查是否所有角色都汇合完成
    private bool IsAllCharactersReady()
    {
        var field = typeof(SpecialLevelManager).GetField(
            "allCharactersReady",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            return (bool)field.GetValue(specialLevel);
        }

        return false;
    }
}