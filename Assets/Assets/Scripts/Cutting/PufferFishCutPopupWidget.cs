using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PufferFishCutPopupWidget : MonoBehaviour
{
  private static PufferFishCutPopupWidget instance;

  private PufferFishCutSettings settings;
  private readonly RectContourTracker contourTracker = new RectContourTracker();

  private Canvas rootCanvas;
  private GameObject popupRoot;
  private RectTransform overlayRect;
  private RectTransform fishImageRect;
  private Image fishImage;
  private RectContourGraphic contourGraphic;
  private Text progressText;
  private Text statusText;
  private Text instructionText;

  private bool isDragging;
  private bool sessionFinished;
  private bool waitForOpeningClickRelease;

  private IngredientSO pendingIngredient;
  private PlayerInventory pendingInventory;
  private IngredientPickup pendingPickup;
  private Action<bool> pendingCallback;

  private static readonly List<Vector2> PhysicsPathBuffer = new List<Vector2>();

  public static PufferFishCutPopupWidget Instance
  {
    get
    {
      if (instance != null)
        return instance;

      instance = FindObjectOfType<PufferFishCutPopupWidget>();
      if (instance != null)
        return instance;

      GameObject host = new GameObject("PufferFishCutPopupWidget");
      instance = host.AddComponent<PufferFishCutPopupWidget>();
      return instance;
    }
  }

  public static bool TryStartCut(
    IngredientSO ingredient,
    PlayerInventory inventory,
    IngredientPickup pickup,
    Action<bool> onComplete)
  {
    if (ingredient == null)
      return false;

    Instance.BeginSession(ingredient, inventory, pickup, onComplete);
    return true;
  }

  private void Awake()
  {
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }

    instance = this;
    DontDestroyOnLoad(gameObject);
    settings = PufferFishCutSettings.Instance;
    BuildUiIfNeeded();
    HidePopup();
  }

  private void OnDestroy()
  {
    if (instance == this)
      instance = null;
  }

  private void Update()
  {
    if (!ContourCutSession.IsActive || popupRoot == null || !popupRoot.activeSelf || sessionFinished)
      return;

    if (waitForOpeningClickRelease)
    {
      if (Input.GetMouseButton(0))
        return;

      waitForOpeningClickRelease = false;
    }

    if (Input.GetKeyDown(KeyCode.Escape))
    {
      CancelSession();
      return;
    }

    if (Input.GetMouseButtonDown(0) && IsPointerInsideFishDisplay())
      isDragging = true;

    if (Input.GetMouseButton(0) && isDragging)
      TraceContour();

    if (Input.GetMouseButtonUp(0) && isDragging)
    {
      isDragging = false;

      if (contourTracker.IsSuccess)
        CompleteSuccess();
      else
        ShowFailAndAllowRetry();
    }

    RefreshProgressUi();
  }

  private void BeginSession(
    IngredientSO ingredient,
    PlayerInventory inventory,
    IngredientPickup pickup,
    Action<bool> onComplete)
  {
    settings = PufferFishCutSettings.Instance;
    pendingIngredient = ingredient;
    pendingInventory = inventory;
    pendingPickup = pickup;
    pendingCallback = onComplete;
    sessionFinished = false;
    isDragging = false;
    waitForOpeningClickRelease = true;

    CancelInvoke(nameof(CloseAfterDelay));

    BuildUiIfNeeded();
    ApplyFishSprite();
    ShowPopup();
    ContourCutSession.SetActive(true);
    RefreshProgressUi();
  }

  private void ApplyFishSprite()
  {
    Sprite sprite = settings.ResolveFishSprite();
    if (fishImage == null)
      return;

    fishImage.sprite = sprite;
    fishImage.enabled = sprite != null;

    if (sprite == null)
    {
      statusText.text = "PufferFishCutSettings icinde fishSprite atanmadi.";
      statusText.color = new Color(0.95f, 0.55f, 0.35f);
      return;
    }

    AspectRatioFitter fitter = fishImageRect.GetComponent<AspectRatioFitter>();
    if (fitter != null)
      fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(fishImageRect);
    SetupContourFromFishImage();
  }

  private void CompleteSuccess()
  {
    if (sessionFinished)
      return;

    sessionFinished = true;
    statusText.text = settings.successMessage;
    statusText.color = new Color(0.4f, 0.95f, 0.5f);

    if (pendingInventory != null && pendingIngredient != null)
      pendingInventory.TryAdd(pendingIngredient);

    if (pendingPickup != null)
      Destroy(pendingPickup.gameObject);

    pendingCallback?.Invoke(true);
    pendingCallback = null;

    Invoke(nameof(CloseAfterDelay), settings.successCloseDelay);
  }

  private void ShowFailAndAllowRetry()
  {
    statusText.text = settings.failMessage;
    statusText.color = new Color(0.95f, 0.45f, 0.4f);
    contourTracker.Reset();
    contourGraphic?.Refresh();
    RefreshProgressUi();
  }

  private void CancelSession()
  {
    if (sessionFinished)
      return;

    sessionFinished = true;
    pendingCallback?.Invoke(false);
    pendingCallback = null;
    CloseAfterDelay();
  }

  private void CloseAfterDelay()
  {
    CancelInvoke(nameof(CloseAfterDelay));
    HidePopup();
    ContourCutSession.SetActive(false);

    pendingIngredient = null;
    pendingInventory = null;
    pendingPickup = null;
    sessionFinished = false;
    waitForOpeningClickRelease = false;
  }

  private void TraceContour()
  {
    if (overlayRect == null)
      return;

    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
          overlayRect,
          Input.mousePosition,
          rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
          out Vector2 local))
      return;

    contourTracker.TryMarkPoint(local);
    contourGraphic?.Refresh();
  }

  private bool IsPointerInsideFishDisplay()
  {
    if (fishImageRect == null)
      return false;

    return RectTransformUtility.RectangleContainsScreenPoint(
      fishImageRect,
      Input.mousePosition,
      rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera);
  }

  private void RefreshProgressUi()
  {
    if (progressText == null)
      return;

    float progressPercent = contourTracker.Progress * 100f;
    float requiredPercent = settings.successThreshold * 100f;
    progressText.text = $"Ilerleme: {progressPercent:0}% / {requiredPercent:0}% gerekli";
  }

  private void SetupContourFromFishImage()
  {
    if (overlayRect == null)
      return;

    if (!TryGetSpriteContentRect(out float contentWidth, out float contentHeight, out Vector2 contentCenter))
    {
      Rect fallback = overlayRect.rect;
      contentWidth = fallback.width * 0.72f;
      contentHeight = fallback.height * 0.72f;
      contentCenter = Vector2.zero;
    }

    float targetArea = contentWidth * contentHeight * settings.areaFraction;
    float aspect = Mathf.Max(0.1f, settings.rectAspectRatio);
    float rectWidth = Mathf.Sqrt(targetArea * aspect);
    float rectHeight = Mathf.Sqrt(targetArea / aspect);

    float maxRectWidth = contentWidth * settings.maxRectInsideFish;
    float maxRectHeight = contentHeight * settings.maxRectInsideFish;
    if (rectWidth > maxRectWidth || rectHeight > maxRectHeight)
    {
      float scale = Mathf.Min(maxRectWidth / rectWidth, maxRectHeight / rectHeight, 1f);
      rectWidth *= scale;
      rectHeight *= scale;
    }

    float tolerance = Mathf.Min(contentWidth, contentHeight) * settings.toleranceNormalized;

    contourTracker.Setup(
      rectWidth,
      rectHeight,
      settings.contourBins,
      tolerance,
      settings.successThreshold,
      contentCenter);
    contourTracker.Reset();

    contourGraphic?.Configure(contourTracker);
    contourGraphic?.Refresh();
  }

  private bool TryGetSpriteContentRect(out float width, out float height, out Vector2 center)
  {
    width = 0f;
    height = 0f;
    center = Vector2.zero;

    if (fishImage == null || fishImage.sprite == null || overlayRect == null)
      return false;

    Sprite sprite = fishImage.sprite;
    Rect container = overlayRect.rect;
    GetPreserveAspectDisplaySize(
      container.width,
      container.height,
      sprite.rect.width / sprite.rect.height,
      out float displayWidth,
      out float displayHeight);

    float spriteUnitWidth = sprite.rect.width / sprite.pixelsPerUnit;
    float spriteUnitHeight = sprite.rect.height / sprite.pixelsPerUnit;
    float uniformScale = displayWidth / spriteUnitWidth;
    float contentInset = settings.spriteContentInset;

    if (!TryGetSpritePhysicsBounds(sprite, out Rect physicsBounds))
    {
      width = displayWidth * (1f - contentInset * 2f);
      height = displayHeight * (1f - contentInset * 2f);
      return width > 1f && height > 1f;
    }

    width = physicsBounds.width * uniformScale;
    height = physicsBounds.height * uniformScale;
    center = new Vector2(physicsBounds.center.x * uniformScale, physicsBounds.center.y * uniformScale);

    width *= 1f - contentInset * 2f;
    height *= 1f - contentInset * 2f;

    return width > 1f && height > 1f;
  }

  private static void GetPreserveAspectDisplaySize(
    float containerWidth,
    float containerHeight,
    float spriteAspect,
    out float displayWidth,
    out float displayHeight)
  {
    float containerAspect = containerWidth / containerHeight;
    if (spriteAspect > containerAspect)
    {
      displayWidth = containerWidth;
      displayHeight = containerWidth / spriteAspect;
      return;
    }

    displayHeight = containerHeight;
    displayWidth = containerHeight * spriteAspect;
  }

  private static bool TryGetSpritePhysicsBounds(Sprite sprite, out Rect bounds)
  {
    bounds = default;
    int shapeCount = sprite.GetPhysicsShapeCount();
    if (shapeCount == 0)
      return false;

    float minX = float.MaxValue;
    float maxX = float.MinValue;
    float minY = float.MaxValue;
    float maxY = float.MinValue;

    for (int i = 0; i < shapeCount; i++)
    {
      PhysicsPathBuffer.Clear();
      sprite.GetPhysicsShape(i, PhysicsPathBuffer);
      for (int p = 0; p < PhysicsPathBuffer.Count; p++)
      {
        Vector2 point = PhysicsPathBuffer[p];
        minX = Mathf.Min(minX, point.x);
        maxX = Mathf.Max(maxX, point.x);
        minY = Mathf.Min(minY, point.y);
        maxY = Mathf.Max(maxY, point.y);
      }
    }

    if (minX > maxX || minY > maxY)
      return false;

    bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
    return bounds.width > 0.001f && bounds.height > 0.001f;
  }

  private void ShowPopup()
  {
    if (popupRoot != null)
      popupRoot.SetActive(true);

    if (statusText != null)
    {
      statusText.text = string.Empty;
      statusText.color = Color.white;
    }

    if (instructionText != null)
      instructionText.text = settings.instruction + " (Esc = iptal)";
  }

  private void HidePopup()
  {
    if (popupRoot != null)
      popupRoot.SetActive(false);
  }

  private void BuildUiIfNeeded()
  {
    if (popupRoot != null)
      return;

    settings = PufferFishCutSettings.Instance;
    Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    rootCanvas = FindObjectOfType<Canvas>();
    if (rootCanvas == null)
    {
      GameObject canvasObject = new GameObject("CutPopupCanvas");
      rootCanvas = canvasObject.AddComponent<Canvas>();
      rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
      rootCanvas.sortingOrder = 200;

      CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920f, 1080f);
      canvasObject.AddComponent<GraphicRaycaster>();
    }

    if (FindObjectOfType<EventSystem>() == null)
    {
      GameObject eventSystem = new GameObject("EventSystem");
      eventSystem.AddComponent<EventSystem>();
      eventSystem.AddComponent<StandaloneInputModule>();
    }

    popupRoot = new GameObject("PufferFishCutPopup");
    popupRoot.transform.SetParent(rootCanvas.transform, false);

    RectTransform popupRect = popupRoot.AddComponent<RectTransform>();
    popupRect.anchorMin = Vector2.zero;
    popupRect.anchorMax = Vector2.one;
    popupRect.offsetMin = Vector2.zero;
    popupRect.offsetMax = Vector2.zero;

    Image dimBackground = popupRoot.AddComponent<Image>();
    dimBackground.color = new Color(0f, 0f, 0f, 0.72f);

    GameObject panelObject = new GameObject("Panel");
    panelObject.transform.SetParent(popupRoot.transform, false);
    RectTransform panelRect = panelObject.AddComponent<RectTransform>();
    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
    panelRect.pivot = new Vector2(0.5f, 0.5f);
    panelRect.sizeDelta = new Vector2(640f, 760f);

    Image panelImage = panelObject.AddComponent<Image>();
    panelImage.color = new Color(0.14f, 0.17f, 0.22f, 0.98f);

    GameObject titleObject = CreateText(panelObject.transform, "Title", font, 28, FontStyle.Bold, TextAnchor.UpperCenter);
    RectTransform titleRect = titleObject.GetComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0f, 1f);
    titleRect.anchorMax = new Vector2(1f, 1f);
    titleRect.pivot = new Vector2(0.5f, 1f);
    titleRect.anchoredPosition = new Vector2(0f, -18f);
    titleRect.sizeDelta = new Vector2(-40f, 40f);
    titleObject.GetComponent<Text>().text = settings.title;

    GameObject fishFrameObject = new GameObject("FishFrame");
    fishFrameObject.transform.SetParent(panelObject.transform, false);
    RectTransform fishFrameRect = fishFrameObject.AddComponent<RectTransform>();
    fishFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
    fishFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
    fishFrameRect.pivot = new Vector2(0.5f, 0.5f);
    fishFrameRect.anchoredPosition = new Vector2(0f, 20f);
    fishFrameRect.sizeDelta = new Vector2(520f, 520f);

    Image fishFrameImage = fishFrameObject.AddComponent<Image>();
    fishFrameImage.color = new Color(0.08f, 0.1f, 0.12f, 1f);

    GameObject fishImageObject = new GameObject("FishImage");
    fishImageObject.transform.SetParent(fishFrameObject.transform, false);
    fishImageRect = fishImageObject.AddComponent<RectTransform>();
    fishImageRect.anchorMin = Vector2.zero;
    fishImageRect.anchorMax = Vector2.one;
    fishImageRect.offsetMin = new Vector2(12f, 12f);
    fishImageRect.offsetMax = new Vector2(-12f, -12f);

    AspectRatioFitter fitter = fishImageObject.AddComponent<AspectRatioFitter>();
    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
    fitter.aspectRatio = 1f;

    fishImage = fishImageObject.AddComponent<Image>();
    fishImage.preserveAspect = true;
    fishImage.raycastTarget = true;

    GameObject overlayObject = new GameObject("ContourOverlay");
    overlayObject.transform.SetParent(fishImageObject.transform, false);
    overlayRect = overlayObject.AddComponent<RectTransform>();
    overlayRect.anchorMin = Vector2.zero;
    overlayRect.anchorMax = Vector2.one;
    overlayRect.offsetMin = Vector2.zero;
    overlayRect.offsetMax = Vector2.zero;

    contourGraphic = overlayObject.AddComponent<RectContourGraphic>();
    contourGraphic.raycastTarget = false;

    GameObject instructionObject = CreateText(panelObject.transform, "Instruction", font, 18, FontStyle.Normal, TextAnchor.UpperCenter);
    RectTransform instructionRect = instructionObject.GetComponent<RectTransform>();
    instructionRect.anchorMin = new Vector2(0f, 0f);
    instructionRect.anchorMax = new Vector2(1f, 0f);
    instructionRect.pivot = new Vector2(0.5f, 0f);
    instructionRect.anchoredPosition = new Vector2(0f, 118f);
    instructionRect.sizeDelta = new Vector2(-40f, 48f);
    instructionText = instructionObject.GetComponent<Text>();
    instructionText.text = settings.instruction;

    GameObject progressObject = CreateText(panelObject.transform, "Progress", font, 22, FontStyle.Bold, TextAnchor.UpperCenter);
    RectTransform progressRect = progressObject.GetComponent<RectTransform>();
    progressRect.anchorMin = new Vector2(0f, 0f);
    progressRect.anchorMax = new Vector2(1f, 0f);
    progressRect.pivot = new Vector2(0.5f, 0f);
    progressRect.anchoredPosition = new Vector2(0f, 78f);
    progressRect.sizeDelta = new Vector2(-40f, 32f);
    progressText = progressObject.GetComponent<Text>();

    GameObject statusObject = CreateText(panelObject.transform, "Status", font, 20, FontStyle.Italic, TextAnchor.UpperCenter);
    RectTransform statusRect = statusObject.GetComponent<RectTransform>();
    statusRect.anchorMin = new Vector2(0f, 0f);
    statusRect.anchorMax = new Vector2(1f, 0f);
    statusRect.pivot = new Vector2(0.5f, 0f);
    statusRect.anchoredPosition = new Vector2(0f, 36f);
    statusRect.sizeDelta = new Vector2(-40f, 32f);
    statusText = statusObject.GetComponent<Text>();
  }

  private static GameObject CreateText(
    Transform parent,
    string name,
    Font font,
    int fontSize,
    FontStyle fontStyle,
    TextAnchor alignment)
  {
    GameObject textObject = new GameObject(name);
    textObject.transform.SetParent(parent, false);
    Text text = textObject.AddComponent<Text>();
    text.font = font;
    text.fontSize = fontSize;
    text.fontStyle = fontStyle;
    text.alignment = alignment;
    text.color = Color.white;
    text.horizontalOverflow = HorizontalWrapMode.Wrap;
    text.verticalOverflow = VerticalWrapMode.Overflow;
    return textObject;
  }
}
