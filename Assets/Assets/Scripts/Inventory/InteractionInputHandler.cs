using UnityEngine;

public class InteractionInputHandler : MonoBehaviour
{
  [SerializeField] private KeyCode interactKey = KeyCode.E;
  [SerializeField] private KeyCode withdrawKey = KeyCode.Q;
  [SerializeField] private float interactRaycastDistance = 6f;

  private void Awake()
  {
    SummerJamMechanicsSettings settings = SummerJamMechanicsSettings.Instance;
    interactKey = settings.interactKey;
    withdrawKey = settings.withdrawKey;
    interactRaycastDistance = settings.pickupRaycastDistance;
  }

  private void Update()
  {
    if (Input.GetKeyDown(interactKey))
    {
      if (!TryInteractWithLookTarget())
        TryInteractWithProximityZone();
    }

    if (Input.GetKeyDown(withdrawKey))
    {
      BowlDeposit bowlUnderMouse = FindBowlDepositUnderMouse();
      if (bowlUnderMouse != null)
        bowlUnderMouse.TryWithdraw();
    }
  }

  private bool TryInteractWithLookTarget()
  {
    Camera camera = Camera.main;
    if (camera == null)
      return false;

    Vector3 screenPoint = Cursor.lockState == CursorLockMode.Locked
      ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
      : (Vector3)Input.mousePosition;

    Ray ray = camera.ScreenPointToRay(screenPoint);
    RaycastHit[] hits = Physics.RaycastAll(
      ray,
      interactRaycastDistance,
      Physics.DefaultRaycastLayers,
      QueryTriggerInteraction.Collide);

    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    for (int i = 0; i < hits.Length; i++)
    {
      InteractionTriggerZone zone = hits[i].collider.GetComponent<InteractionTriggerZone>();
      if (zone != null)
      {
        if (TryInteractWithZone(zone))
          return true;

        continue;
      }

      TrashBin trashBin = hits[i].collider.GetComponentInParent<TrashBin>();
      if (trashBin != null)
      {
        trashBin.TryTrashSelected();
        return true;
      }

      BowlDeposit bowlDeposit = hits[i].collider.GetComponentInParent<BowlDeposit>();
      if (bowlDeposit != null)
      {
        bowlDeposit.TryDeposit();
        return true;
      }
    }

    return false;
  }

  private void TryInteractWithProximityZone()
  {
    InteractionTriggerZone primaryZone = InteractionTriggerZone.GetPrimaryZone();
    if (primaryZone == null)
      return;

    TryInteractWithZone(primaryZone);
  }

  private static bool TryInteractWithZone(InteractionTriggerZone zone)
  {
    if (zone == null)
      return false;

    BowlDeposit bowlDeposit = zone.GetComponentInParent<BowlDeposit>();
    if (bowlDeposit != null)
    {
      bowlDeposit.TryDeposit();
      return true;
    }

    TrashBin trashBin = zone.GetComponentInParent<TrashBin>();
    if (trashBin != null)
    {
      trashBin.TryTrashSelected();
      return true;
    }

    return false;
  }

  private static BowlDeposit FindBowlDepositUnderMouse()
  {
    Kase kase = Object.FindObjectOfType<Kase>();
    if (kase == null || !kase.HasIngredients)
      return null;

    InteractionTriggerZone bowlZone = kase.GetComponentInChildren<InteractionTriggerZone>();
    if (bowlZone == null)
      return null;

    Vector3 interactionPoint = InteractionTriggerZone.GetInteractionPoint();
    if (!bowlZone.ContainsPoint(interactionPoint)
        && bowlZone.GetClosestSurfaceDistance(interactionPoint) > SummerJamMechanicsSettings.Instance.proximityInteractRadius)
      return null;

    return kase.GetComponent<BowlDeposit>();
  }
}
