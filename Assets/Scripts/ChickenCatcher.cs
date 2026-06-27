using System;
using Controller;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ChickenCatcher : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float m_CatchRadius = 2.5f;
    [SerializeField]
    private Key m_CatchKey = Key.E;

    public static int CaughtCount { get; private set; }
    public static event Action<int> CaughtCountChanged;

    private static readonly Collider[] s_Buffer = new Collider[32];

    private void OnEnable()
    {
        CaughtCountChanged?.Invoke(CaughtCount);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[m_CatchKey].wasPressedThisFrame)
        {
            return;
        }

        var nearest = FindNearestChicken();
        if (nearest == null)
        {
            return;
        }

        Destroy(nearest.gameObject);
        CaughtCount++;
        CaughtCountChanged?.Invoke(CaughtCount);
    }

    private ChickenAutoWanderInput FindNearestChicken()
    {
        var origin = transform.position;
        var count = Physics.OverlapSphereNonAlloc(origin, m_CatchRadius, s_Buffer, ~0, QueryTriggerInteraction.Ignore);

        ChickenAutoWanderInput best = null;
        float bestSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var chick = s_Buffer[i].GetComponentInParent<ChickenAutoWanderInput>();
            if (chick == null || chick.gameObject == gameObject)
            {
                continue;
            }

            var sq = (chick.transform.position - origin).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = chick;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, m_CatchRadius);
    }
}
