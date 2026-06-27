using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
  [SerializeField] private PlayerInventory inventory;
  [SerializeField] private Image[] slotImages = new Image[PlayerInventory.SlotCount];
  [SerializeField] private Text[] slotLabels = new Text[PlayerInventory.SlotCount];
  [SerializeField] private Text[] slotNumberLabels = new Text[PlayerInventory.SlotCount];
  [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
  [SerializeField] private Color filledColor = Color.white;
  [SerializeField] private Color emptySelectedColor = new Color(0.45f, 0.55f, 0.75f, 0.85f);
  [SerializeField] private Color filledSelectedColor = new Color(0.75f, 0.9f, 1f, 1f);
  [SerializeField] private bool buildRuntimeUiIfMissing = true;

  private void Awake()
  {
    if (inventory == null)
      inventory = FindObjectOfType<PlayerInventory>();

    if (buildRuntimeUiIfMissing && !HasAssignedSlots())
      BuildRuntimeUi();
  }

  private void OnEnable()
  {
    if (inventory == null)
      return;

    inventory.OnChanged += Refresh;
    inventory.OnSelectionChanged += Refresh;
  }

  private void OnDisable()
  {
    if (inventory == null)
      return;

    inventory.OnChanged -= Refresh;
    inventory.OnSelectionChanged -= Refresh;
  }

  private void Start()
  {
    Refresh();
  }

  private bool HasAssignedSlots()
  {
    for (int i = 0; i < PlayerInventory.SlotCount; i++)
    {
      if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
        return true;
    }

    return false;
  }

  private void BuildRuntimeUi()
  {
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null)
    {
      GameObject canvasObject = new GameObject("InventoryCanvas");
      canvas = canvasObject.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      canvasObject.AddComponent<GraphicRaycaster>();
    }

    if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
    {
      GameObject eventSystem = new GameObject("EventSystem");
      eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
      eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    slotImages = new Image[PlayerInventory.SlotCount];
    slotLabels = new Text[PlayerInventory.SlotCount];
    slotNumberLabels = new Text[PlayerInventory.SlotCount];

    for (int i = 0; i < PlayerInventory.SlotCount; i++)
    {
      GameObject slotObject = new GameObject($"Slot{i}");
      slotObject.transform.SetParent(canvas.transform, false);

      RectTransform slotRect = slotObject.AddComponent<RectTransform>();
      slotRect.anchorMin = new Vector2(0f, 1f);
      slotRect.anchorMax = new Vector2(0f, 1f);
      slotRect.pivot = new Vector2(0f, 1f);
      slotRect.sizeDelta = new Vector2(90f, 90f);
      slotRect.anchoredPosition = new Vector2(20f + i * 100f, -20f);

      Image image = slotObject.AddComponent<Image>();
      image.color = emptyColor;
      slotImages[i] = image;

      GameObject numberObject = new GameObject("Number");
      numberObject.transform.SetParent(slotObject.transform, false);

      RectTransform numberRect = numberObject.AddComponent<RectTransform>();
      numberRect.anchorMin = new Vector2(0f, 1f);
      numberRect.anchorMax = new Vector2(0f, 1f);
      numberRect.pivot = new Vector2(0f, 1f);
      numberRect.anchoredPosition = new Vector2(6f, -4f);
      numberRect.sizeDelta = new Vector2(24f, 24f);

      Text numberLabel = numberObject.AddComponent<Text>();
      numberLabel.font = font;
      numberLabel.fontSize = 16;
      numberLabel.fontStyle = FontStyle.Bold;
      numberLabel.alignment = TextAnchor.UpperLeft;
      numberLabel.color = Color.white;
      numberLabel.text = (i + 1).ToString();
      slotNumberLabels[i] = numberLabel;

      GameObject labelObject = new GameObject("Label");
      labelObject.transform.SetParent(slotObject.transform, false);

      RectTransform labelRect = labelObject.AddComponent<RectTransform>();
      labelRect.anchorMin = Vector2.zero;
      labelRect.anchorMax = Vector2.one;
      labelRect.offsetMin = new Vector2(4f, 4f);
      labelRect.offsetMax = new Vector2(-4f, -20f);

      Text label = labelObject.AddComponent<Text>();
      label.font = font;
      label.fontSize = 12;
      label.alignment = TextAnchor.MiddleCenter;
      label.color = Color.black;
      label.text = "-";
      slotLabels[i] = label;
    }
  }

  private void Refresh()
  {
    if (inventory == null)
      return;

    for (int i = 0; i < PlayerInventory.SlotCount; i++)
    {
      IngredientSO item = inventory.GetSlot(i);
      bool hasItem = item != null;
      bool selected = inventory.SelectedSlotIndex == i;

      if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
      {
        if (selected && hasItem)
          slotImages[i].color = filledSelectedColor;
        else if (selected)
          slotImages[i].color = emptySelectedColor;
        else if (hasItem)
          slotImages[i].color = filledColor;
        else
          slotImages[i].color = emptyColor;
      }

      if (slotNumberLabels != null && i < slotNumberLabels.Length && slotNumberLabels[i] != null)
        slotNumberLabels[i].text = (i + 1).ToString();

      if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
      {
        if (hasItem)
        {
          int count = inventory.GetCount(i);
          slotLabels[i].text = count > 1 ? $"{item.ingredientName} x{count}" : item.ingredientName;
        }
        else
        {
          slotLabels[i].text = "-";
        }
      }
    }
  }
}
