using UnityEngine;

[DisallowMultipleComponent]
public class IngredientPickupByKey : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float m_PickupRadius = 1.75f;
    [SerializeField]
    private KeyCode m_PickupKey = KeyCode.E;

    private PlayerInventory m_Inventory;
    private static readonly Collider[] s_Buffer = new Collider[32];

    private void Awake()
    {
        m_Inventory = FindObjectOfType<PlayerInventory>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(m_PickupKey))
        {
            return;
        }

        if (m_Inventory == null)
        {
            m_Inventory = FindObjectOfType<PlayerInventory>();
            if (m_Inventory == null)
            {
                return;
            }
        }

        if (InteractionTriggerZone.GetPrimaryZone() != null)
        {
            return;
        }

        var pickup = FindClosestPickup();
        if (pickup == null)
        {
            return;
        }

        if (!m_Inventory.TryAdd(pickup.GetIngredient()))
        {
            return;
        }

        Destroy(pickup.gameObject);
    }

    private IngredientPickup FindClosestPickup()
    {
        var origin = transform.position;
        int count = Physics.OverlapSphereNonAlloc(origin, m_PickupRadius, s_Buffer, ~0, QueryTriggerInteraction.Collide);

        IngredientPickup best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var p = s_Buffer[i].GetComponentInParent<IngredientPickup>();
            if (p == null)
            {
                continue;
            }

            var sq = (p.transform.position - origin).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = p;
            }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, m_PickupRadius);
    }
}
