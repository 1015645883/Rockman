using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingManager : MonoBehaviour
{
    [Header("引用")]
    public CanvasGroup blackScreen;       // 黑幕淡入淡出
    public Animator rockmanAnimator;      // 洛克人动画机
    public Image bluesImage;              // Blues影子Image
    public Transform background1;         // 背景1
    public Transform background2;         // 背景2

    [Header("参数")]
    public float fadeDuration = 2f;       // 黑幕淡入时长
    public float bluesFadeDuration = 2f;  // Blues渐入/渐出时长
    public float bluesStayDuration = 3f;  // Blues停留总时间
    public float imageSwitchInterval = 0.5f; // 图片切换间隔
    public Sprite[] bluesSprites;         // Blues循环的三张图片

    public float bg1Speed = 0.5f;         // 背景1移动速度
    public float bg2Speed = 1f;           // 背景2移动速度
    public Vector3 bg2TargetPos;          // 背景2目标点

    private bool isMoving = false;
    [Header("下半画面")]
    public CanvasGroup[] pages;      // 10页内容，每页挂一个CanvasGroup
    public float pageFadeDuration = 1f; // 每页淡入淡出时间
    public float pageStayDuration = 2f; // 每页停留时间


    void Start()
    {
        blackScreen.alpha = 1f;
        bluesImage.color = new Color(1, 1, 1, 0);

        // 确保所有页面初始不可见
        if (pages != null)
        {
            foreach (var page in pages)
            {
                page.alpha = 0f;
            }
        }

        StartCoroutine(PlayEndingSequence());
    }


    private IEnumerator PlayEndingSequence()
    {
        // 黑幕淡入
        yield return StartCoroutine(FadeCanvas(blackScreen, 1f, 0f, fadeDuration)); 
        // 洛克人 Special 动画
        rockmanAnimator.Play("Special"); 
        // Blues 渐入
        yield return StartCoroutine(FadeImage(bluesImage, 0f, 1f, bluesFadeDuration)); 
        // Blues 停留并循环切换图片
        yield return StartCoroutine(BluesStayAndLoopImages(bluesStayDuration)); 
        // Blues 渐出
        yield return StartCoroutine(FadeImage(bluesImage, 1f, 0f, bluesFadeDuration)); 
        // 洛克人切换 LoopWalk
        rockmanAnimator.Play("Idle"); 
        rockmanAnimator.SetBool("IsWalking", true); 
        // 翻转 sprite 朝左
        var sr = rockmanAnimator.GetComponent<SpriteRenderer>(); 
        if (sr != null) sr.flipX = false;
        // 开始移动背景
        isMoving = true;

        // 并行播放下半画面
        StartCoroutine(PlayPagesSequence());

        // 等待背景2到达目标点
        yield return new WaitUntil(() => background2.position.x >= bg2TargetPos.x);

        // 背景停止
        isMoving = false;


        // 洛克人移动到 (-2, 1.63)
        yield return StartCoroutine(MoveCharacter(rockmanAnimator.transform, new Vector2(-2f, 1.63f), 2f));

        // 走完后，设置跳跃
        rockmanAnimator.SetBool("IsJumping", true);

        // 洛克人跳到 (-2, 3)
        yield return StartCoroutine(MoveCharacter(rockmanAnimator.transform, new Vector2(-2f, 3f), 1.5f));

    }


    // 工具：平滑移动角色到目标点
    IEnumerator MoveCharacter(Transform target, Vector2 destination, float speed)
    {
        while ((Vector2)target.position != destination)
        {
            target.position = Vector2.MoveTowards(
                target.position,
                destination,
                speed * Time.deltaTime
            );
            yield return null;
        }
    }


    void Update()
    {
        if (isMoving)
        {
            // 背景1：持续匀速移动（不需要严格停点）
            background1.Translate(Vector3.right * bg1Speed * Time.deltaTime);

            // 背景2：严格移动到目标点
            background2.position = Vector3.MoveTowards(
                background2.position,
                bg2TargetPos,
                bg2Speed * Time.deltaTime
            );

            // 如果到达目标点，停止移动
            if (background2.position == bg2TargetPos)
            {
                isMoving = false;
            }
        }
    }


    // 工具：淡入淡出Canvas
    IEnumerator FadeCanvas(CanvasGroup canvas, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvas.alpha = to;
    }

    // 工具：淡入淡出Image
    IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        float t = 0f;
        Color c = image.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            image.color = c;
            yield return null;
        }
        c.a = to;
        image.color = c;
    }

    // Blues停留并循环切换图片
    IEnumerator BluesStayAndLoopImages(float totalDuration)
    {
        if (bluesSprites == null || bluesSprites.Length == 0) yield break;

        float elapsed = 0f;
        int index = 0;

        while (elapsed < totalDuration)
        {
            bluesImage.sprite = bluesSprites[index];
            index = (index + 1) % bluesSprites.Length;

            float waitTime = Mathf.Min(imageSwitchInterval, totalDuration - elapsed);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
        }
    }
    IEnumerator PlayPagesSequence()
    {
        if (pages == null || pages.Length == 0) yield break;

        foreach (var page in pages)
        {
            // 先初始化透明
            page.alpha = 0f;

            // 淡入
            yield return StartCoroutine(FadeCanvas(page, 0f, 1f, pageFadeDuration));

            // 停留
            yield return new WaitForSeconds(pageStayDuration);

            // 淡出
            yield return StartCoroutine(FadeCanvas(page, 1f, 0f, pageFadeDuration));
        }
    }

}
