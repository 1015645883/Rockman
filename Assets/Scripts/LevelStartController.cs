using System.Collections;
using UnityEngine;

public class LevelStartController : MonoBehaviour
{
    [Header("Screen Fader")]
    public ScreenFader screenFader;  // Inspector 挂上 ScreenFader

    [Header("Player")]
    private Animator playerAnimator;
    private AudioSource voiceSource;
    private MonoBehaviour playerMovementScript; // 玩家移动脚本（假设是MonoBehaviour）

    [System.Serializable]
    public class CharacterVoice
    {
        public string characterKey;
        public AudioClip spawnVoice;
    }
    public CharacterVoice[] characterVoices;

    [Header("BGM")]
    public AudioSource bgmSource;
    private AudioClip bgmClip;

    private void Start()
    {
        StartCoroutine(LevelStartSequence());
    }

    private IEnumerator LevelStartSequence()
    {
        if (screenFader != null)
            screenFader.SetAlphaInstant(1f);

        GameObject playerObj = null;
        while (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        playerAnimator = playerObj.GetComponent<Animator>();
        voiceSource = playerObj.GetComponent<AudioSource>();
        playerMovementScript = playerObj.GetComponent<PlayerMovement>();
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        playerRb.velocity = new Vector2(0f, playerRb.velocity.y); // 水平速度固定为0

        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "");
        AudioClip spawnVoice = null;
        foreach (var cv in characterVoices)
        {
            if (cv.characterKey == selectedCharacter)
            {
                spawnVoice = cv.spawnVoice;
                break;
            }
        }

        if (bgmSource != null)
            bgmClip = bgmSource.clip;

        yield return new WaitForSeconds(1f);

        // 播放出生动画
        if (playerAnimator != null)
        {
            // 屏蔽 ShootingLayer，假设它在索引 1
            int shootingLayerIndex = 1;
            playerAnimator.SetLayerWeight(shootingLayerIndex, 0f);

            playerAnimator.Play("Reborn", 0, 0f);

            if (screenFader != null)
                yield return StartCoroutine(screenFader.FadeOutUnscaled());

            // 获取 Reborn 动画长度
            float rebornLength = 0f;
            var clips = playerAnimator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == "Reborn")
                {
                    rebornLength = clip.length;
                    break;
                }
            }

            // 等待动画播放时间
            float timer = 0f;
            while (timer < rebornLength)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // 定格在最后一帧
            playerAnimator.speed = 0f;
            playerAnimator.Play("Reborn", 0, 1f); // 跳到末尾
        }


        yield return new WaitForSeconds(0.5f);

        // 播放语音
        if (voiceSource != null && spawnVoice != null)
        {
            voiceSource.PlayOneShot(spawnVoice);
        }

        // 等待语音播放完
        float voiceLength = spawnVoice != null ? spawnVoice.length : 0f;
        yield return new WaitForSecondsRealtime(voiceLength);

        // 播放站立动画，恢复动画播放速度
        if (playerAnimator != null)
        {
            playerAnimator.speed = 1f;
            playerAnimator.Play("Idle", 0, 0f);
        }

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }

     }
}
