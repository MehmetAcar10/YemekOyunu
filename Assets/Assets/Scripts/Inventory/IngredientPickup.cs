using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sahnedeki toplanabilir 3D malzeme. Bir IngredientSO atandiginda onun
/// visualPrefab modelini otomatik olarak "Visual" adli alt nesne olarak olusturur
/// ve BoxCollider'i modele gore boyutlandirir (yalnizca editorde, gerektiginde).
/// Toplama sirasinda nesne YOK EDILMEZ (sonsuz kaynak); cooldown ile sinirlanir.
/// Toplama yalnizca fare sol tik ile yapilir (OnMouseDown).
/// Toplama sirasinda nesne YOK EDILMEZ (sonsuz kaynak); cooldown ile sinirlanir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IngredientPickup : MonoBehaviour
{
  private const string VisualChildName = "Visual";

  [SerializeField] private IngredientSO ingredient;
  [SerializeField] private PlayerInventory targetInventory;

  [Tooltip("Ardisik alimlar arasindaki minimum bekleme suresi (saniye).")]
  [SerializeField] private float pickupCooldown = 0.3f;

  [Tooltip("Ingredient atandiginda 3D modeli (visualPrefab) otomatik olustur ve collider'i ona gore boyutlandir.")]
  [SerializeField] private bool autoBuildVisual = true;

  [Tooltip("Collider'in gorsel sinirlardan ne kadar buyuk olacagi (world birim).")]
  [SerializeField] private float colliderPadding = 0.2f;

  [Tooltip("Collider'in minimum world boyutu (kucuk modeller icin).")]
  [SerializeField] private float minColliderWorldSize = 0.4f;

  // Halihazirda gorseli kurulu olan malzeme. Degisince model yeniden olusturulur.
  [SerializeField, HideInInspector] private IngredientSO builtIngredient;

  private float lastPickupTime = -999f;

#if UNITY_EDITOR
  private static bool suppressOnValidate;
  private static readonly HashSet<int> scheduledEditorBuilds = new HashSet<int>();
  private static readonly HashSet<int> scheduledEditorColliderRefits = new HashSet<int>();
  private IngredientSO editorPreviousIngredient;
#endif

  public IngredientSO GetIngredient() => ingredient;

  /// <summary>
  /// BoxCollider'i mevcut Visual modelinin sinirlarina gore yeniden boyutlandirir.
  /// </summary>
  public void RefitColliderFromVisual()
  {
    Physics.SyncTransforms();
    RefreshColliderFromVisual();
  }

  private void Awake()
  {
    if (!Application.isPlaying)
      return;

    if (targetInventory == null)
      targetInventory = FindObjectOfType<PlayerInventory>();

    if (autoBuildVisual)
      EnsureVisual();
  }

  private void Start()
  {
    if (!Application.isPlaying)
      return;

    RefreshColliderFromVisual();
  }

  private void Reset()
  {
    Collider collider = GetComponent<Collider>();
    if (collider != null)
      collider.isTrigger = false;
  }

#if UNITY_EDITOR
  private void OnValidate()
  {
    if (suppressOnValidate || !autoBuildVisual || Application.isPlaying)
      return;

    bool ingredientChangedInEditor = editorPreviousIngredient != null
      && editorPreviousIngredient != ingredient;
    editorPreviousIngredient = ingredient;

    Transform visual = FindVisualChild();
    if (visual != null && builtIngredient == null && ingredient != null && !ingredientChangedInEditor)
    {
      builtIngredient = ingredient;
      UnityEditor.EditorUtility.SetDirty(this);
      ScheduleEditorColliderRefit();
      return;
    }

    if (!NeedsVisualRebuild())
    {
      ScheduleEditorColliderRefit();
      return;
    }

    ScheduleEditorBuild();
  }

  private void ScheduleEditorBuild()
  {
    int instanceId = GetInstanceID();
    if (!scheduledEditorBuilds.Add(instanceId))
      return;

    UnityEditor.EditorApplication.delayCall += () =>
    {
      scheduledEditorBuilds.Remove(instanceId);
      if (this == null)
        return;

      EditorDeferredBuild();
    };
  }

  private void EditorDeferredBuild()
  {
    if (this == null || Application.isPlaying || !autoBuildVisual)
      return;

    EnsureVisual();
  }

  private void ScheduleEditorColliderRefit()
  {
    int instanceId = GetInstanceID();
    if (!scheduledEditorColliderRefits.Add(instanceId))
      return;

    UnityEditor.EditorApplication.delayCall += () =>
    {
      scheduledEditorColliderRefits.Remove(instanceId);
      if (this == null || Application.isPlaying)
        return;

      RefitColliderFromVisual();
      UnityEditor.EditorUtility.SetDirty(this);
    };
  }
#endif

  private void OnMouseDown()
  {
    if (!Application.isPlaying)
      return;

    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
      return;

    TryCollect(targetInventory);
  }

  private void RefreshColliderFromVisual()
  {
    Transform visual = FindVisualChild();
    if (visual == null)
      return;

    SceneCameraBootstrap.DisableEmbeddedCameras(visual.gameObject);
    DisableExtraneousColliders();
    FitCollider(visual.gameObject);
  }

  private void DisableExtraneousColliders()
  {
    Collider ownCollider = GetComponent<Collider>();
    Collider[] colliders = GetComponentsInChildren<Collider>(true);
    for (int i = 0; i < colliders.Length; i++)
    {
      Collider collider = colliders[i];
      if (collider != null && collider != ownCollider)
        collider.enabled = false;
    }
  }

  /// <summary>
  /// Malzemeyi envantere eklemeye calisir. Basariyla alinsa bile nesne yok
  /// edilmez (sonsuz kaynak), cooldown ile ardisik hizli alimlar sinirlanir.
  /// </summary>
  public bool TryCollect(PlayerInventory inventory)
  {
    if (ingredient == null || inventory == null)
      return false;

    if (Time.time - lastPickupTime < pickupCooldown)
      return false;

    if (!inventory.TryAdd(ingredient))
      return false;

    lastPickupTime = Time.time;
    return true;
  }

  public void SetInventory(PlayerInventory inventory)
  {
    targetInventory = inventory;
  }

  public void SetIngredient(IngredientSO data)
  {
    ingredient = data;
    if (autoBuildVisual)
      EnsureVisual();
  }

  /// <summary>
  /// Atanmis malzemenin visualPrefab modelini "Visual" alt nesnesi olarak kurar
  /// ve BoxCollider'i model sinirlarina gore ayarlar. Malzeme degismediyse hicbir
  /// sey yapmaz (gereksiz yeniden olusturmayi onler).
  /// </summary>
  public void EnsureVisual()
  {
    if (!NeedsVisualRebuild())
    {
      AdoptExistingVisualIfNeeded();
#if UNITY_EDITOR
      if (!Application.isPlaying)
        ScheduleEditorColliderRefit();
      else
#endif
        RefitColliderFromVisual();
      return;
    }

    RemoveExistingVisual();
    builtIngredient = ingredient;

    if (ingredient == null || ingredient.visualPrefab == null)
      return;

    GameObject visual = InstantiateVisual(ingredient.visualPrefab);
    if (visual == null)
      return;

    visual.name = VisualChildName;
    visual.transform.SetParent(transform, false);
    visual.transform.localPosition = Vector3.zero;
    visual.transform.localRotation = Quaternion.identity;

    SceneCameraBootstrap.DisableEmbeddedCameras(visual);
    DisableExtraneousColliders();
    FitCollider(visual);
  }

  private bool NeedsVisualRebuild()
  {
    Transform visual = FindVisualChild();

    if (ingredient == null)
      return visual != null;

    if (visual == null)
      return true;

    if (builtIngredient == ingredient)
      return false;

    // Prefab/sahne instance'larinda builtIngredient kaydedilmemis olabilir;
    // mevcut Visual varsa gereksiz yikim/onarim dongusune girmeyelim.
    if (builtIngredient == null)
      return false;

    return builtIngredient != ingredient;
  }

  private void AdoptExistingVisualIfNeeded()
  {
    if (ingredient == null)
    {
      builtIngredient = null;
      return;
    }

    if (FindVisualChild() == null || builtIngredient == ingredient)
      return;

    builtIngredient = ingredient;

#if UNITY_EDITOR
    if (!Application.isPlaying)
      UnityEditor.EditorUtility.SetDirty(this);
#endif

    Transform visual = FindVisualChild();
    if (visual != null)
      SceneCameraBootstrap.DisableEmbeddedCameras(visual.gameObject);
  }

  private GameObject InstantiateVisual(GameObject prefab)
  {
#if UNITY_EDITOR
    if (!Application.isPlaying)
    {
      GameObject instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
      if (instance != null)
        return instance;
    }
#endif
    return Instantiate(prefab);
  }

  private Transform FindVisualChild()
  {
    Transform visual = transform.Find(VisualChildName);
    if (visual != null)
      return visual;

    for (int i = 0; i < transform.childCount; i++)
    {
      Transform child = transform.GetChild(i);
      if (child.name == InteractZoneUtility.InteractZoneObjectName)
        continue;

      if (child.GetComponentInChildren<Renderer>(true) != null)
        return child;
    }

    return null;
  }

  private void RemoveExistingVisual()
  {
    Transform child = FindVisualChild();
    while (child != null)
    {
      if (Application.isPlaying)
        Destroy(child.gameObject);
      else
        DestroyImmediate(child.gameObject);

      child = FindVisualChild();
    }
  }

  private void FitCollider(GameObject visual)
  {
    BoxCollider box = GetComponent<Collider>() as BoxCollider;
    if (box == null)
      return;

    Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
    if (renderers.Length == 0)
      renderers = GetComponentsInChildren<Renderer>(true);

    FitColliderToRenderers(renderers);
  }

  private void FitColliderToRenderers(Renderer[] renderers)
  {
    BoxCollider box = GetComponent<Collider>() as BoxCollider;
    if (box == null || renderers == null || renderers.Length == 0)
      return;

    bool hasBounds = false;
    Bounds worldBounds = default;
    for (int i = 0; i < renderers.Length; i++)
    {
      Renderer renderer = renderers[i];
      if (renderer == null || !renderer.enabled || renderer.gameObject == gameObject)
        continue;

      if (!hasBounds)
      {
        worldBounds = renderer.bounds;
        hasBounds = true;
      }
      else
      {
        worldBounds.Encapsulate(renderer.bounds);
      }
    }

    if (!hasBounds)
      return;

    worldBounds.Expand(colliderPadding);

    Vector3 scale = transform.lossyScale;
    Vector3 newCenter = transform.InverseTransformPoint(worldBounds.center);
    Vector3 newSize = new Vector3(
      Mathf.Abs(worldBounds.size.x / Mathf.Max(0.0001f, scale.x)),
      Mathf.Abs(worldBounds.size.y / Mathf.Max(0.0001f, scale.y)),
      Mathf.Abs(worldBounds.size.z / Mathf.Max(0.0001f, scale.z)));

    float minLocalSize = minColliderWorldSize / Mathf.Max(0.0001f, Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)));
    newSize.x = Mathf.Max(newSize.x, minLocalSize);
    newSize.y = Mathf.Max(newSize.y, minLocalSize);
    newSize.z = Mathf.Max(newSize.z, minLocalSize);

    if (Approximately(box.center, newCenter) && Approximately(box.size, newSize))
      return;

#if UNITY_EDITOR
    suppressOnValidate = true;
    try
    {
      UnityEditor.Undo.RecordObject(box, "Fit Ingredient Collider");
      box.center = newCenter;
      box.size = newSize;
      box.isTrigger = false;
      UnityEditor.EditorUtility.SetDirty(box);
    }
    finally
    {
      suppressOnValidate = false;
    }
#else
    box.center = newCenter;
    box.size = newSize;
    box.isTrigger = false;
#endif
  }

  private static bool Approximately(Vector3 a, Vector3 b)
  {
    return (a - b).sqrMagnitude < 0.000001f;
  }
}
