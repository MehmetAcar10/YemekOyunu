using UnityEngine;

public class InteractionInputHandler : MonoBehaviour
{
  [SerializeField] private KeyCode interactKey = KeyCode.E;
  [SerializeField] private KeyCode withdrawKey = KeyCode.Q;

  private void Awake()
  {
    SummerJamMechanicsSettings settings = SummerJamMechanicsSettings.Instance;
    interactKey = settings.interactKey;
    withdrawKey = settings.withdrawKey;
  }

  private void Update()
  {
    InteractionTriggerZone primaryZone = InteractionTriggerZone.GetPrimaryZone();

    if (Input.GetKeyDown(interactKey) && primaryZone != null)
    {
      BowlDeposit bowlDeposit = primaryZone.GetComponentInParent<BowlDeposit>();
      TrashBin trashBin = primaryZone.GetComponentInParent<TrashBin>();

      if (bowlDeposit != null)
        bowlDeposit.TryDeposit();
      else if (trashBin != null)
        trashBin.TryTrashSelected();
    }

    if (Input.GetKeyDown(withdrawKey))
    {
      BowlDeposit bowlUnderMouse = FindBowlDepositUnderMouse();
      if (bowlUnderMouse != null)
        bowlUnderMouse.TryWithdraw();
    }
  }

  private static BowlDeposit FindBowlDepositUnderMouse()
  {
    Kase kase = Object.FindObjectOfType<Kase>();
    if (kase == null || !kase.HasIngredients)
      return null;

    InteractionTriggerZone bowlZone = kase.GetComponentInChildren<InteractionTriggerZone>();
    if (bowlZone == null || !bowlZone.ContainsPoint(InteractionTriggerZone.GetInteractionPoint()))
      return null;

    return kase.GetComponent<BowlDeposit>();
  }
}
