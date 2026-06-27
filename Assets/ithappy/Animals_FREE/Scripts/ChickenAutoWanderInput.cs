using UnityEngine;

namespace Controller
{
    [RequireComponent(typeof(CreatureMover))]
    [DisallowMultipleComponent]
    public class ChickenAutoWanderInput : MonoBehaviour
    {
        [Header("Wander")]
        [SerializeField, Min(0.1f)]
        private float m_WanderRadius = 4f;
        [SerializeField, Min(0.05f)]
        private float m_TargetReachDistance = 0.35f;
        [SerializeField, Min(0f)]
        private float m_MinWaitTime = 0.75f;
        [SerializeField, Min(0f)]
        private float m_MaxWaitTime = 2.25f;
        [SerializeField, Min(0f)]
        private float m_RandomStartDelay = 1.5f;
        [SerializeField]
        private bool m_Run;
        [SerializeField]
        private LayerMask m_GroundLayers = ~0;
        [SerializeField, Range(1, 30)]
        private int m_TargetSearchAttempts = 12;
        [SerializeField, Min(0.5f)]
        private float m_GroundProbeHeight = 4f;
        [SerializeField, Min(0.5f)]
        private float m_GroundProbeDistance = 12f;
        [SerializeField, Range(0f, 1f)]
        private float m_MinGroundNormalY = 0.55f;
        [SerializeField, Min(0.5f)]
        private float m_FallRecoveryDistance = 3f;
        private CreatureMover m_Mover;
        private CharacterController m_Controller;
        private Vector3 m_SpawnPosition;
        private Vector3 m_LastSafePosition;
        private Vector3 m_TargetPosition;
        private float m_WaitTimer;

        private void Awake()
        {
            m_Mover = GetComponent<CreatureMover>();
            m_Controller = GetComponent<CharacterController>();
            m_SpawnPosition = transform.position;
            m_LastSafePosition = m_SpawnPosition;
        }

        private void OnEnable()
        {
            m_WaitTimer = Random.Range(0f, m_RandomStartDelay);
            PickNewTarget();
        }

        private void OnDisable()
        {
            StopMoving();
        }

        private void OnValidate()
        {
            if (m_MaxWaitTime < m_MinWaitTime)
            {
                m_MaxWaitTime = m_MinWaitTime;
            }
        }

        private void Update()
        {
            if (transform.position.y < m_SpawnPosition.y - m_FallRecoveryDistance)
            {
                RecoverToLastSafePosition();
                return;
            }

            if (HasGroundBelow(transform.position))
            {
                m_LastSafePosition = transform.position;
            }

            if (m_WaitTimer > 0f)
            {
                m_WaitTimer -= Time.deltaTime;
                StopMoving();
                return;
            }

            var currentPosition = transform.position;
            var flatOffset = new Vector2(
                m_TargetPosition.x - currentPosition.x,
                m_TargetPosition.z - currentPosition.z);

            if (flatOffset.sqrMagnitude <= m_TargetReachDistance * m_TargetReachDistance)
            {
                m_WaitTimer = Random.Range(m_MinWaitTime, m_MaxWaitTime);
                PickNewTarget();
                StopMoving();
                return;
            }

            m_Mover.SetInput(Vector2.up, m_TargetPosition, m_Run, false);
        }

        private void PickNewTarget()
        {
            for (var i = 0; i < m_TargetSearchAttempts; i++)
            {
                var offset = Random.insideUnitCircle * m_WanderRadius;
                var candidate = new Vector3(
                    m_SpawnPosition.x + offset.x,
                    m_SpawnPosition.y,
                    m_SpawnPosition.z + offset.y);

                if (TryProjectToGround(candidate, out var groundedTarget))
                {
                    m_TargetPosition = groundedTarget;
                    return;
                }
            }

            m_TargetPosition = m_LastSafePosition;
        }

        private void StopMoving()
        {
            if (m_Mover == null)
            {
                return;
            }

            m_Mover.SetInput(Vector2.zero, transform.position + transform.forward, false, false);
        }

        private bool HasGroundBelow(Vector3 position)
        {
            return TryProjectToGround(position, out _);
        }

        private bool TryProjectToGround(Vector3 position, out Vector3 groundedPosition)
        {
            var rayOrigin = new Vector3(
                position.x,
                m_SpawnPosition.y + m_GroundProbeHeight,
                position.z);

            var rayDistance = m_GroundProbeHeight + m_GroundProbeDistance;
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, rayDistance, m_GroundLayers, QueryTriggerInteraction.Ignore)
                && hit.normal.y >= m_MinGroundNormalY)
            {
                groundedPosition = hit.point;
                return true;
            }

            groundedPosition = position;
            return false;
        }

        private void RecoverToLastSafePosition()
        {
            StopMoving();

            if (m_Controller != null)
            {
                m_Controller.enabled = false;
            }

            transform.position = m_LastSafePosition;

            if (m_Controller != null)
            {
                m_Controller.enabled = true;
            }

            m_WaitTimer = Random.Range(m_MinWaitTime, m_MaxWaitTime);
            PickNewTarget();
        }
    }
}
