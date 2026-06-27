using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
  public const int SlotCount = 5;
  public const int MaxStack = 5;

  [SerializeField] private IngredientSO[] slots = new IngredientSO[SlotCount];
  [SerializeField] private int[] counts = new int[SlotCount];

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

    if (counts == null || counts.Length != SlotCount)
    {
      int[] resizedCounts = new int[SlotCount];
      if (counts != null)
      {
        int copyCount = Mathf.Min(counts.Length, SlotCount);
        for (int i = 0; i < copyCount; i++)
          resizedCounts[i] = counts[i];
      }

      counts = resizedCounts;
    }

    // Inspector'da malzeme atanmis ama adet 0 kalmissa en az 1 yap
    for (int i = 0; i < SlotCount; i++)
    {
      if (slots[i] != null && counts[i] <= 0)
        counts[i] = 1;
      else if (slots[i] == null)
        counts[i] = 0;
    }

    SelectSlot(0);
  }

  public int GetCount(int index)
  {
    if (index < 0 || index >= SlotCount)
      return 0;

    return slots[index] != null ? counts[index] : 0;
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
    if (ingredient == null)
      return false;

    bool wasEmpty = IsEmpty;

    // 1) Ayni malzemenin oldugu, dolmamis bir stack'e ekle
    for (int i = 0; i < SlotCount; i++)
    {
      if (slots[i] == ingredient && counts[i] < MaxStack)
      {
        counts[i]++;
        OnChanged?.Invoke();
        return true;
      }
    }

    // 2) Bos slota yeni stack olarak ekle
    for (int i = 0; i < SlotCount; i++)
    {
      if (slots[i] != null)
        continue;

      slots[i] = ingredient;
      counts[i] = 1;
      OnChanged?.Invoke();

      if (wasEmpty)
        SelectSlot(i);

      return true;
    }

    return false;
  }

  /// <summary>Bu malzemeden bir tane daha eklenebilir mi (bos slot ya da uygun stack var mi).</summary>
  public bool CanAccept(IngredientSO ingredient)
  {
    if (ingredient == null)
      return false;

    for (int i = 0; i < SlotCount; i++)
    {
      if (slots[i] == null)
        return true;
      if (slots[i] == ingredient && counts[i] < MaxStack)
        return true;
    }

    return false;
  }

  public bool TryRemove(int slotIndex)
  {
    if (slotIndex < 0 || slotIndex >= SlotCount || slots[slotIndex] == null)
      return false;

    counts[slotIndex]--;
    if (counts[slotIndex] <= 0)
    {
      counts[slotIndex] = 0;
      slots[slotIndex] = null;
    }

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
    {
      slots[i] = null;
      counts[i] = 0;
    }

    OnChanged?.Invoke();
  }
}
