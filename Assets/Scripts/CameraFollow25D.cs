using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow25D : MonoBehaviour
{
    [SerializeField]
    private Transform m_Target;
    [SerializeField]
    private Vector3 m_Offset = new Vector3(-12f, 7.5f, -12f);
    [SerializeField]
    private Vector3 m_LookAtOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(0f)]
    private float m_FollowSmoothTime = 0.12f;
    [SerializeField]
    private bool m_SnapOnStart = true;
    [SerializeField]
    private bool m_LockRotation = true;
    [SerializeField]
    private bool m_UseOrthographic = true;
    [SerializeField, Min(0.1f)]
    private float m_OrthographicSize = 8.5f;
    [SerializeField, Min(0.01f)]
    private float m_NearClipPlane = 0.05f;

    [Header("Collision")]
    [SerializeField]
    private bool m_AvoidObstacles = true;
    [SerializeField]
    private LayerMask m_ObstacleLayers = ~0;
    [SerializeField, Min(0.01f)]
    private float m_CollisionRadius = 0.45f;
    [SerializeField, Min(0f)]
    private float m_CollisionPadding = 0.2f;
    [SerializeField, Min(0f)]
    private float m_CollisionSmoothTime = 0.04f;
    [SerializeField]
    private bool m_IgnoreTargetLayer = true;
    private Camera m_Camera;
    private Vector3 m_Velocity;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
        FindTargetIfMissing();
        ApplyLens();
    }

    private void Start()
    {
        if (m_SnapOnStart)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        FindTargetIfMissing();

        if (m_Target == null)
        {
            return;
        }

        ApplyLens();

        var lookPosition = GetLookPosition();
        var desiredPosition = ResolveCameraPosition(m_Target.position + m_Offset, lookPosition, out var collisionConstrained);
        var smoothTime = collisionConstrained ? Mathf.Min(m_FollowSmoothTime, m_CollisionSmoothTime) : m_FollowSmoothTime;
        MoveCamera(desiredPosition, smoothTime);

        // 25D: when rotation is locked we keep the orientation captured at Snap and never re-aim per frame.
        // Re-aiming each frame while position is SmoothDamp'd creates sub-pixel rotation drift -> visible jitter.
        if (!m_LockRotation)
        {
            LookAtPosition(lookPosition);
        }
    }

    private void OnValidate()
    {
        if (m_OrthographicSize < 0.1f)
        {
            m_OrthographicSize = 0.1f;
        }

        if (m_NearClipPlane < 0.01f)
        {
            m_NearClipPlane = 0.01f;
        }

        if (m_CollisionRadius < 0.01f)
        {
            m_CollisionRadius = 0.01f;
        }
    }

    public void SetTarget(Transform target)
    {
        m_Target = target;
        SnapToTarget();
    }

    private void SnapToTarget()
    {
        if (m_Target == null)
        {
            return;
        }

        m_Velocity = Vector3.zero;
        var lookPosition = GetLookPosition();
        transform.position = ResolveCameraPosition(m_Target.position + m_Offset, lookPosition, out _);

        if (m_LockRotation)
        {
            LookAtPosition(lookPosition);
        }
    }

    private void MoveCamera(Vector3 desiredPosition, float smoothTime)
    {
        if (smoothTime <= 0f)
        {
            m_Velocity = Vector3.zero;
            transform.position = desiredPosition;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref m_Velocity, smoothTime);
    }

    private Vector3 GetLookPosition()
    {
        return m_Target.position + m_LookAtOffset;
    }

    private void LookAtPosition(Vector3 lookPosition)
    {
        var direction = lookPosition - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private Vector3 ResolveCameraPosition(Vector3 desiredPosition, Vector3 lookPosition, out bool collisionConstrained)
    {
        collisionConstrained = false;

        if (!m_AvoidObstacles)
        {
            return desiredPosition;
        }

        var offset = desiredPosition - lookPosition;
        var distance = offset.magnitude;
        if (distance <= 0.001f)
        {
            return desiredPosition;
        }

        var direction = offset / distance;
        var obstacleMask = GetObstacleMask();

        // Start the cast a little along the ray so we don't self-trigger on colliders the sphere
        // is already overlapping at the look position (character feet, ground, etc.).
        var startSkip = Mathf.Min(m_CollisionRadius, distance * 0.5f);
        var rayOrigin = lookPosition + direction * startSkip;
        var rayDistance = distance - startSkip;

        if (rayDistance > 0f && Physics.SphereCast(rayOrigin, m_CollisionRadius, direction, out var hit, rayDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            collisionConstrained = true;
            var hitDistanceFromLook = startSkip + hit.distance;
            var safeDistance = Mathf.Max(0.05f, hitDistanceFromLook - m_CollisionPadding);
            return lookPosition + direction * Mathf.Min(safeDistance, distance);
        }

        if (Physics.CheckSphere(desiredPosition, m_CollisionRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            collisionConstrained = true;
            return lookPosition + direction * Mathf.Max(0.05f, distance - m_CollisionPadding);
        }

        return desiredPosition;
    }

    private int GetObstacleMask()
    {
        var mask = m_ObstacleLayers.value;
        if (m_IgnoreTargetLayer && m_Target != null)
        {
            mask &= ~(1 << m_Target.gameObject.layer);
        }

        return mask;
    }


    private void LookAtTarget()
    {
        if (m_Target == null)
        {
            return;
        }

        LookAtPosition(GetLookPosition());
    }

    private void ApplyLens()
    {
        if (m_Camera == null)
        {
            return;
        }

        m_Camera.nearClipPlane = m_NearClipPlane;
        m_Camera.orthographic = m_UseOrthographic;
        if (m_UseOrthographic)
        {
            m_Camera.orthographicSize = m_OrthographicSize;
        }
    }

    private void FindTargetIfMissing()
    {
        if (m_Target != null)
        {
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            m_Target = player.transform;
        }
    }
}
