using UnityEngine;
using System;
using System.Collections;

public class RIceArrow : MonoBehaviour
{
    [Header("子弹参数")]
    public float speed = 10f;
    public float maxDistance = 10f;
    private Vector3 startPosition;
    private Vector2 moveDirection;

    [Header("音效")]
    private AudioSource audioSource;
    public AudioClip Dink;

    [Header("冰冻参数")]
    public GameObject iceBlockPrefab;
    public GameObject IceBlockPiece1;
    public GameObject IceBlockPiece2;
    public float freezeDuration = 3f;

    [Header("状态")]
    private bool isDeflected = false;
    public Action OnDestroyCallback;

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = direction.x > 0;
    }

    void Start() => startPosition = transform.position;

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            DestroyBullet();
    }

    void FixedUpdate()
    {
        GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    public void Deflect()
    {
        if (isDeflected) return;
        isDeflected = true;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Dink;
            audioSource.playOnAwake = false;
        }
        audioSource.Play();

        moveDirection = Quaternion.Euler(0, 0, 150f) * moveDirection;
        Destroy(GetComponent<Collider2D>());

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
            rb.gravityScale = 0f;
        }

        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
        Destroy(gameObject, 2f);
    }

    public void DestroyBullet()
    {
        OnDestroyCallback?.Invoke();
        Destroy(gameObject);
    }

    public void Freeze(MonoBehaviour enemy)
    {
        var isFrozenField = enemy.GetType().GetField("isFrozen");
        if (isFrozenField == null) return;

        bool frozen = (bool)isFrozenField.GetValue(enemy);
        if (frozen) return;

        isFrozenField.SetValue(enemy, true);

        // ===== 暂停敌人 =====
        enemy.StopAllCoroutines();

        Animator anim = enemy.GetComponent<Animator>();
        if (anim != null) anim.speed = 0f;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }
        // ===== 暂停完成 =====

        // ===== 生成冰块 =====
        if (iceBlockPrefab == null) return;

        Vector2 icePos;
        Vector2 iceSize;

        // 如果是 MetalPickelman，特殊处理
        MetalPickelman metal = enemy.GetComponent<MetalPickelman>();
        if (metal != null)
        {
            Collider2D[] cols = new Collider2D[] { metal.bodyCollider, metal.headCollider };
            Bounds bounds = cols[0].bounds;
            foreach (var c in cols)
            {
                bounds.Encapsulate(c.bounds); // 合并头和身体
            }

            icePos = bounds.center;
            iceSize = bounds.size * 1.2f; // 放大一点
        }
        else
        {
            Collider2D col = enemy.GetComponent<Collider2D>();
            if (col == null) return;
            icePos = col.bounds.center;
            iceSize = col.bounds.size * 1.2f;
        }

        GameObject ice = Instantiate(iceBlockPrefab, icePos, Quaternion.identity);
        ice.name = "IceBlock_" + enemy.name;

        // 调整冰块大小
        SpriteRenderer sr = ice.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = iceSize;
            Color c = sr.color;
            c.a = 0.8f;
            sr.color = c;
        }

        BoxCollider2D box = ice.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = iceSize;
            box.offset = Vector2.zero;
        }

        // 添加监控敌人死亡的脚本
        IceBlockWatcher watcher = ice.AddComponent<IceBlockWatcher>();
        watcher.enemyName = enemy.name;
        watcher.onEnemyDeadCallback = () => BreakIce(ice);

        enemy.StartCoroutine(Unfreeze(enemy, isFrozenField, ice));
    }


    private IEnumerator Unfreeze(MonoBehaviour enemy, System.Reflection.FieldInfo field, GameObject ice)
    {
        if (ice == null) yield break;
        SpriteRenderer sr = ice.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float elapsed = 0f;
        float startAlpha = 0.8f;
        float endAlpha = 0.3f;

        while (elapsed < freezeDuration)
        {
            if (enemy == null) yield break; // 敌人死亡直接交由 IceBlockWatcher 处理

            elapsed += Time.deltaTime;
            float t = elapsed / freezeDuration;
            Color c = sr.color;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            sr.color = c;
            yield return null;
        }

        if (enemy != null)
        {
            // 恢复冻结状态
            field.SetValue(enemy, false);

            // 恢复动画
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.speed = 1f;

            // 恢复物理
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = false;  // 让它重新受物理系统控制
            }

            // 恢复协程（重新启动主要行为协程）
            // 检查并重新启动 DelayedAttackCycle
            var delayedAttack = enemy.GetType().GetMethod("DelayedAttackCycle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (delayedAttack != null)
                enemy.StartCoroutine((IEnumerator)delayedAttack.Invoke(enemy, null));

            // 检查并重新启动 DiveAttack
            var diveAttack = enemy.GetType().GetMethod("DiveAttack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (diveAttack != null)
                enemy.StartCoroutine((IEnumerator)diveAttack.Invoke(enemy, null));
            // 如果敌人在 DiveAttack 中
            if (enemy.GetType().GetField("diveInterrupted") != null)
            {
                var diveInterruptedField = enemy.GetType().GetField("diveInterrupted");
                diveInterruptedField.SetValue(enemy, true);  // 解除冰冻时中断俯冲
            }

            // 检查并重新启动 MoveToBasePosition
            var moveToBase = enemy.GetType().GetMethod("MoveToBasePosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (moveToBase != null)
                enemy.StartCoroutine((IEnumerator)moveToBase.Invoke(enemy, null));

            var jumpUpRoutine = enemy.GetType().GetMethod("JumpUpRoutine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (jumpUpRoutine != null)
            {
                enemy.StartCoroutine((IEnumerator)jumpUpRoutine.Invoke(enemy, null));
            }

            // 针对 ChangKey 的 FallRoutine
            var fallRoutine = enemy.GetType().GetMethod("FallRoutine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (fallRoutine != null)
            {
                enemy.StartCoroutine((IEnumerator)fallRoutine.Invoke(enemy, null));
            }
        }

        // 冰块解冻完成，生成碎片
        BreakIce(ice);
    }


    private void BreakIce(GameObject ice)
    {
        if (ice == null) return;

        Vector3 pos = ice.transform.position;

        if (IceBlockPiece1 != null)
        {
            GameObject p1 = Instantiate(IceBlockPiece1, pos, Quaternion.identity);
            Rigidbody2D rb1 = p1.AddComponent<Rigidbody2D>();
            rb1.gravityScale = 1f;
            rb1.velocity = new Vector2(UnityEngine.Random.Range(-2f, -1f), UnityEngine.Random.Range(1f, 2f));
            Destroy(p1, 3f);
        }

        if (IceBlockPiece2 != null)
        {
            GameObject p2 = Instantiate(IceBlockPiece2, pos, Quaternion.identity);
            Rigidbody2D rb2 = p2.AddComponent<Rigidbody2D>();
            rb2.gravityScale = 1f;
            rb2.velocity = new Vector2(UnityEngine.Random.Range(1f, 2f), UnityEngine.Random.Range(1f, 2f));
            Destroy(p2, 3f);
        }

        Destroy(ice);
    }
}
