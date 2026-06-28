using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// FPS modunda sol tik ile malzeme toplama. Ekran merkezinden (veya serbest imleçte
/// fare konumundan) RaycastAll yaparak önündeki masa/zemin collider'larını atlar.
/// </summary>
public class IngredientPickupInput : MonoBehaviour
{
  [SerializeField] private float maxPickupDistance = 6f;
  [SerializeField] private PlayerInventory inventory;
  [SerializeField] private Camera pickupCamera;

  private void Awake()
  {
    SummerJamMechanicsSettings settings = SummerJamMechanicsSettings.Instance;
    maxPickupDistance = settings.pickupRaycastDistance;

    if (inventory == null)
      inventory = FindObjectOfType<PlayerInventory>();

    if (pickupCamera == null)
      pickupCamera = Camera.main;
  }

  private void Update()
  {
    if (!Input.GetMouseButtonDown(0))
      return;

    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
      return;

    if (pickupCamera == null)
      pickupCamera = Camera.main;

    if (pickupCamera == null || inventory == null)
      return;

    Vector3 screenPoint = Cursor.lockState == CursorLockMode.Locked
      ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
      : (Vector3)Input.mousePosition;

    Ray ray = pickupCamera.ScreenPointToRay(screenPoint);
    RaycastHit[] hits = Physics.RaycastAll(ray, maxPickupDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    for (int i = 0; i < hits.Length; i++)
    {
      IngredientPickup pickup = hits[i].collider.GetComponentInParent<IngredientPickup>();
      if (pickup == null)
        continue;

      if (pickup.TryCollect(inventory))
        return;
    }
  }
}
