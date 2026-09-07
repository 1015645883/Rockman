using UnityEngine;

public class SuperArm1 : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f; // 保底存在时间（秒）

    private void Start()
    {
        // 保底销毁
        Destroy(gameObject, lifetime);
    }

}
