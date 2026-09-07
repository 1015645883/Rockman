using UnityEngine;
using System;

public class HyperBombExplosion : MonoBehaviour
{
    public Action OnDestroyCallback;  // 子弹销毁时的回调
    private void Start()
    {
        DestroyBullet();
    }
    public void DestroyBullet()
    {
        OnDestroyCallback?.Invoke();  // 通知 PlayerShooting 子弹销毁
        Destroy(gameObject, 0.5f);
    }
}
