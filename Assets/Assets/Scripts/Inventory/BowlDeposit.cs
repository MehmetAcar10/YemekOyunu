using UnityEngine;

public class BowlDeposit : MonoBehaviour
{
  [SerializeField] private Kase kase;
  [SerializeField] private PlayerInventory inventory;

  private void Awake()
  {
    if (inventory == null)
      inventory = FindObjectOfType<PlayerInventory>();

    if (kase == null)
      kase = GetComponent<Kase>();
  }

  public void TryDeposit()
  {
    if (kase == null || inventory == null)
      return;

    if (!inventory.TryRemoveSelected(out IngredientSO item))
      return;

    kase.MalzemeEkle(item);
  }

  public void TryWithdraw()
  {
    if (kase == null || inventory == null || kase.HasIngredients == false || inventory.IsFull)
      return;

    kase.SonMalzemeyiEnvantereGeriAl(inventory);
  }

  public void SetInventory(PlayerInventory value)
  {
    inventory = value;
  }

  public void SetKase(Kase value)
  {
    kase = value;
  }
}
