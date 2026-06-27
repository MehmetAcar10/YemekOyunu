using UnityEngine;

public class TrashBin : MonoBehaviour
{
  [SerializeField] private PlayerInventory inventory;

  private void Awake()
  {
    if (inventory == null)
      inventory = FindObjectOfType<PlayerInventory>();
  }

  public void TryTrashSelected()
  {
    if (inventory == null)
      return;

    inventory.TryRemoveSelected(out _);
  }

  public void SetInventory(PlayerInventory value)
  {
    inventory = value;
  }
}
