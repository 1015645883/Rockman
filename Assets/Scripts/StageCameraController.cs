using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class StageCameraController : MonoBehaviour
{
    [Header("Camera Zones")]
    [SerializeField]
    private List<StageCameraZone> cameraZones = new List<StageCameraZone>();

    [Header("Settings")]
    [SerializeField]
    private bool keepLastZoneWhenOutside = false;

    [Header("Camera Smooth")]
    [Tooltip("摄像机移动的平滑时间。数值越小越跟手，越大越平滑。")]
    [SerializeField]
    private float smoothTime = 0.15f;

    [Tooltip("摄像机最大移动速度。0表示不限制。")]
    [SerializeField]
    private float maxSpeed = 0f;

    private Camera cachedCamera;

    private Transform target;

    private StageCameraZone activeZone;

    private Vector2 shakeOffset;
    private Vector2 appliedShakeOffset;

    private float fixedZ = -10f;

    // SmoothDamp 使用的速度
    private Vector3 cameraVelocity = Vector3.zero;

    // 是否需要立即定位
    private bool snapNextFrame = false;

    private bool cameraPositionLocked = false;
    private Vector3 lockedCameraPosition;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();

        fixedZ = transform.position.z;

        // 摄像机不再作为玩家子物体存在
        transform.SetParent(null, true);
    }

    private void LateUpdate()
    {
        if (cameraPositionLocked)
        {
            transform.position = lockedCameraPosition;
            return;
        }

        if (target == null)
            return;

        // --------------------------------------------------
        // 1. 找当前玩家所在的 Zone
        // --------------------------------------------------

        StageCameraZone nextZone = FindZoneContaining(target);

        if (nextZone != null)
        {
            activeZone = nextZone;
        }
        else if (!keepLastZoneWhenOutside)
        {
            activeZone = null;
        }

        // --------------------------------------------------
        // 2. 计算摄像机“目标位置”
        // --------------------------------------------------

        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y,
            fixedZ
        );

        // Zone 限制只作用于“目标位置”
        // 而不是每帧强行限制摄像机当前位置
        if (activeZone != null)
        {
            desiredPosition = ClampToZone(
                desiredPosition,
                activeZone
            );
        }

        // --------------------------------------------------
        // 3. 需要立即定位时直接过去
        // --------------------------------------------------

        if (snapNextFrame)
        {
            transform.position = desiredPosition;

            cameraVelocity = Vector3.zero;

            snapNextFrame = false;
        }
        else
        {
            // --------------------------------------------------
            // 4. 平滑移动到目标位置
            // --------------------------------------------------

            float actualSmoothTime = Mathf.Max(
                0.01f,
                smoothTime
            );

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref cameraVelocity,
                actualSmoothTime,
                maxSpeed > 0f ? maxSpeed : Mathf.Infinity,
                Time.deltaTime
            );

            // 保证 Z 始终正确
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                fixedZ
            );
        }

        // --------------------------------------------------
        // 5. 最后再叠加震屏
        // --------------------------------------------------

        appliedShakeOffset = shakeOffset;

        transform.position += new Vector3(
            appliedShakeOffset.x,
            appliedShakeOffset.y,
            0f
        );
    }

    public void SetTarget(
        Transform newTarget,
        bool snapImmediately = true
    )
    {
        if (newTarget == null)
            return;

        target = newTarget;

        if (snapImmediately)
        {
            snapNextFrame = true;
        }
    }

    public void SnapToTarget()
    {
        snapNextFrame = true;
    }

    public Transform GetTarget()
    {
        return target;
    }

    public void SetShakeOffset(Vector2 offset)
    {
        shakeOffset = offset;
    }

    private StageCameraZone FindZoneContaining(
        Transform playerTransform
    )
    {
        if (playerTransform == null)
            return null;

        StageCameraZone bestZone = null;

        foreach (StageCameraZone zone in cameraZones)
        {
            if (zone == null ||
                !zone.isActiveAndEnabled ||
                !zone.Contains(playerTransform.position))
            {
                continue;
            }

            if (bestZone == null ||
                zone.Priority > bestZone.Priority)
            {
                bestZone = zone;
            }
        }

        return bestZone;
    }

    private Vector3 ClampToZone(
        Vector3 cameraPosition,
        StageCameraZone zone
    )
    {
        if (cachedCamera == null ||
            !cachedCamera.orthographic)
        {
            return cameraPosition;
        }

        Bounds bounds = zone.WorldBounds;

        float halfHeight =
            cachedCamera.orthographicSize;

        float halfWidth =
            halfHeight * cachedCamera.aspect;

        float minX =
            bounds.min.x + halfWidth;

        float maxX =
            bounds.max.x - halfWidth;

        float minY =
            bounds.min.y + halfHeight;

        float maxY =
            bounds.max.y - halfHeight;

        // X轴独立限制
        if (minX > maxX)
        {
            cameraPosition.x = bounds.center.x;
        }
        else
        {
            cameraPosition.x =
                Mathf.Clamp(
                    cameraPosition.x,
                    minX,
                    maxX
                );
        }

        // Y轴独立限制
        if (minY > maxY)
        {
            cameraPosition.y = bounds.center.y;
        }
        else
        {
            cameraPosition.y =
                Mathf.Clamp(
                    cameraPosition.y,
                    minY,
                    maxY
                );
        }

        return cameraPosition;
    }

    public void LockCameraPosition()
    {
        lockedCameraPosition = transform.position;
        cameraPositionLocked = true;
    }

    public void UnlockCameraPosition()
    {
        cameraPositionLocked = false;
    }
}