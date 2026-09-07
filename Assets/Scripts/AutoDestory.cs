using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifeTime = 0.5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
