using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelClearManager : MonoBehaviour
{
    public AudioClip victoryClip;              // 胜利音效
    public GameObject player;                  // 主角 GameObject
    public string victoryAnimName = "Victory"; // 主角胜利动画名称
    public string levelSelectScene = "LevelSelect"; // 默认跳转场景
    public string endingScene = "Ending";      // 最终结局场景
    public AudioSource bgmAudioSource;         // 背景音乐的 AudioSource（可手动指定）

    [Header("转场黑幕")]
    public ScreenFader screenFader;            // Inspector 挂上 ScreenFader

    private AudioSource audioSource;
    private bool levelCleared = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        StartCoroutine(WaitForPlayer());
    }

    IEnumerator WaitForPlayer()
    {
        SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
        if (slm != null && slm.currentPlayer != null)
        {
            player = slm.currentPlayer;
        }

        // 如果没找到就用标签找
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null; 
        }

        Debug.Log("LevelClearManager 成功绑定玩家: " + player.name);
    }

    // 在 Boss 死亡时调用
    public void OnBossDefeated()
    {
        if (levelCleared) return;
        levelCleared = true;

        StartCoroutine(LevelClearSequence());
    }

    IEnumerator LevelClearSequence()
    {
        // 停止 BGM
        if (bgmAudioSource != null)
            bgmAudioSource.Stop();

        // 禁止玩家行动
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        // 播放胜利音效
        if (victoryClip != null && audioSource != null)
            audioSource.PlayOneShot(victoryClip);

        // 播放胜利动画
        var animator = player.GetComponent<Animator>();
        if (animator != null)
            animator.Play(victoryAnimName);

        yield return new WaitForSeconds(0.13f);

        // 传送效果（升天）
        float teleportDuration = 0.5f;
        float teleportHeight = 25f;
        Vector3 startPos = player.transform.position;
        Vector3 endPos = startPos + new Vector3(0, teleportHeight, 0);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.isKinematic = true;
        }
        float elapsed = 0f;
        while (elapsed < teleportDuration)
        {
            player.transform.position = Vector3.Lerp(startPos, endPos, elapsed / teleportDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        player.transform.position = endPos;

        // 等待动画播放完成
        yield return new WaitForSeconds(2f);

        // ✅ 黑幕淡入
        if (screenFader != null)
        {
            Debug.Log("进入黑屏");
            yield return StartCoroutine(screenFader.FadeInUnscaled());
        }

        // ✅ 根据场景决定跳转
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "WilyStage1")
        {
            SceneManager.LoadScene(endingScene);
        }
        else
        {
            SceneManager.LoadScene(levelSelectScene);
        }
    }
}
