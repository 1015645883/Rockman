using UnityEngine;

public class EffectAutoDestroy : MonoBehaviour
{

    // 如果你还想提供手动销毁接口
    public void DestroySelf()
    {
        if (Application.isPlaying)
            Destroy(gameObject);
    }
}
