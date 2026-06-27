using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
  public const int SlotCount = 4;

  [SerializeField] private IngredientSO[] slots = new IngredientSO[SlotCount];

  public event Action OnChanged;
  public event Action OnSelectionChanged;

  public int SelectedSlotIndex { get; private set; }

  private void Awake()
  {
    if (slots == null || slots.Length != SlotCount)
    {
      IngredientSO[] resized = new IngredientSO[SlotCount];
      if (slots != null)
      {
        int copyCount = Mathf.Min(slots.Length, SlotCount);
        for (int i = 0; i < copyCount; i++)
          resized[i] = slots[i];
      }

      slots = resized;
    }

    SelectSlot(0);
  }

  private void Update()
  {
    for (int i = 0; i < SlotCount; i++)
    {
      KeyCode alphaKey = KeyCode.Alpha1 + i;
      KeyCode keypadKey = KeyCode.Keypad1 + i;

      if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
        SelectSlot(i);
    }
  }

  public int FilledCount
  {
    get
    {
      int count = 0;
      for (int i = 0; i < SlotCount; i++)
      {
        if (slots[i] != null)
          count++;
      }

      return count;
    }
  }

  public bool IsFull => FilledCount >= SlotCount;
  public bool IsEmpty => FilledCount == 0;
  public int FreeSlotCount => SlotCount - FilledCount;

  public bool CanFit(int itemCount)
  {
    return itemCount > 0 && itemCount <= FreeSlotCount;
  }

  public void SelectSlot(int index)
  {
    if (index < 0 || index >= SlotCount)
      return;

    SelectedSlotIndex = index;

    if (GetSelectedItem() == null)
      AdvanceToNextFilledSlot();

    OnSelectionChanged?.Invoke();
  }

  private void AdvanceToNextFilledSlot()
  {
    int start = SelectedSlotIndex;

    for (int offset = 1; offset <= SlotCount; offset++)
    {
      int index = (start + offset) % SlotCount;
      if (slots[index] != null)
      {
        SelectedSlotIndex = index;
        return;
      }
    }
  }

  public IngredientSO GetSlot(int index)
  {
    if (index < 0 || index >= SlotCount)
      return null;

    return slots[index];
  }

  public IngredientSO GetSelectedItem()
  {
    return GetSlot(SelectedSlotIndex);
  }

  public bool TryAdd(IngredientSO ingredient)
  {
    if (ingredient == null || IsFull)
      return false;

    bool wasEmpty = IsEmpty;

    for (int i = 0; i < SlotCount; i++)
    {
      if (slots[i] != null)
        continue;

      slots[i] = ingredient;
      OnChanged?.Invoke();

      if (wasEmpty)
        SelectSlot(i);

      return true;
    }

    return false;
  }

  public bool TryRemove(int slotIndex)
  {
    if (slotIndex < 0 || slotIndex >= SlotCount || slots[slotIndex] == null)
      return false;

    slots[slotIndex] = null;
    OnChanged?.Invoke();

    if (slotIndex == SelectedSlotIndex && GetSelectedItem() == null)
    {
      AdvanceToNextFilledSlot();
      OnSelectionChanged?.Invoke();
    }

    return true;
  }

  public bool TryRemoveSelected(out IngredientSO item)
  {
    item = GetSelectedItem();
    if (item == null)
      return false;

    return TryRemove(SelectedSlotIndex);
  }

  public void Clear()
  {
    for (int i = 0; i < SlotCount; i++)
      slots[i] = null;

    OnChanged?.Invoke();
  }
}
