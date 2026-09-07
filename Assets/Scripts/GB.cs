using UnityEngine;

[DisallowMultipleComponent]
public class GravityWatcher : MonoBehaviour
{
    public Rigidbody2D rb;

    private float lastGravity;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) lastGravity = rb.gravityScale;
    }

    private void Update()
    {
        if (rb != null && rb.gravityScale != lastGravity)
        {
            Debug.LogWarning($"[GravityWatcher] {gameObject.name} gravityScale changed from {lastGravity} ¡ú {rb.gravityScale}\n" +
                             $"StackTrace:\n{System.Environment.StackTrace}", this);
            lastGravity = rb.gravityScale;
        }

    }
}
