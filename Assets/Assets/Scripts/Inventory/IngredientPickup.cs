using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class IngredientPickup : MonoBehaviour
{
    [SerializeField] private IngredientSO ingredient;
    [SerializeField] private PlayerInventory targetInventory;

    public IngredientSO GetIngredient() => ingredient;

  private void Awake()
  {
    if (targetInventory == null)
      targetInventory = FindObjectOfType<PlayerInventory>();
  }

  private void Reset()
  {
    Collider collider = GetComponent<Collider>();
    if (collider != null)
      collider.isTrigger = false;
  }

  private void OnMouseDown()
  {
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
      return;

    if (ingredient == null || targetInventory == null)
      return;

    if (!targetInventory.TryAdd(ingredient))
      return;

    Destroy(gameObject);
  }

  public void SetInventory(PlayerInventory inventory)
  {
    targetInventory = inventory;
  }

  public void SetIngredient(IngredientSO data)
  {
    ingredient = data;
  }
}
