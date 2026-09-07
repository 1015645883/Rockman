using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Light2D))]
public class LightController : MonoBehaviour
{
    [Header("外部控制变量")]
    public bool allowLight = true; // 外部控制光源是否允许开启

    private Light2D light2D;
    private SpecialLevelManager specialLevel;
    private bool permanentDisable = false;
    private bool initialized = false; // 延迟初始化标记
    private float initDelay = 0.02f;  // 延迟一帧后再判断

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        // 🚫 初始化前默认禁用，防止出现闪光
        light2D.enabled = false;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "WilyStage1")
        {
            specialLevel = FindObjectOfType<SpecialLevelManager>();
            Invoke(nameof(InitializeLight), initDelay); // 延迟一点点时间再执行初始判断
        }
    }

    void InitializeLight()
    {
        initialized = true;
        EvaluateLightImmediately();
    }

    void Update()
    {
        if (!initialized || specialLevel == null || light2D == null)
            return;

        // 🔒 汇合完成后 → 永久禁用
        if (IsAllCharactersReady())
        {
            if (!permanentDisable)
            {
                permanentDisable = true;
                light2D.enabled = false;
            }
            return;
        }

        // 🚫 外部主动关闭光时，不进行任何启用逻辑
        if (!allowLight)
        {
            light2D.enabled = false;
            return;
        }

        // ✅ 当前角色存在时判断角色名
        if (specialLevel.currentPlayer != null)
        {
            string name = specialLevel.currentPlayer.name;
            if (name.EndsWith("(Clone)"))
                name = name.Replace("(Clone)", "").Trim();

            // 🔥 仅炸弹人或火焰人启用光源
            light2D.enabled = (name == "PlayableBombman" || name == "PlayableFireman");
        }
    }

    // 🧠 立刻判断能否点亮（但只在初始化后调用）
    private void EvaluateLightImmediately()
    {
        if (!allowLight)
        {
            light2D.enabled = false;
            return;
        }

        if (specialLevel == null || specialLevel.currentPlayer == null)
        {
            light2D.enabled = allowLight;
            return;
        }

        string name = specialLevel.currentPlayer.name;
        if (name.EndsWith("(Clone)"))
            name = name.Replace("(Clone)", "").Trim();

        light2D.enabled = (name == "PlayableBombman" || name == "PlayableFireman");
    }

    // 检查是否所有角色都汇合完成
    private bool IsAllCharactersReady()
    {
        var field = typeof(SpecialLevelManager).GetField("allCharactersReady",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (bool)field.GetValue(specialLevel);
        }
        return false;
    }
}
