using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sahnedeki toplanabilir 3D malzeme. Bir IngredientSO atandiginda onun
/// visualPrefab modelini otomatik olarak "Visual" adli alt nesne olarak olusturur
/// ve BoxCollider'i modele gore boyutlandirir (editorde de calisir).
/// Toplama sirasinda nesne YOK EDILMEZ (sonsuz kaynak); cooldown ile sinirlanir.
/// Hem fareyle tiklama (OnMouseDown) hem yaklas+E (IngredientPickupByKey) ayni
/// TryCollect mantigini kullanir.
/// </summary>
[ExecuteAlways]
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

  // Halihazirda gorseli kurulu olan malzeme. Degisince model yeniden olusturulur.
  [SerializeField, HideInInspector] private IngredientSO builtIngredient;

  private float lastPickupTime = -999f;

  public IngredientSO GetIngredient() => ingredient;

  private void Awake()
  {
    if (!Application.isPlaying)
      return;

    if (targetInventory == null)
      targetInventory = FindObjectOfType<PlayerInventory>();

    if (autoBuildVisual)
      EnsureVisual();
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
    if (!autoBuildVisual || Application.isPlaying)
      return;

    // OnValidate sirasinda Instantiate/Destroy guvenli degil; bir frame ertele.
    UnityEditor.EditorApplication.delayCall += EditorDeferredBuild;
  }

  private void EditorDeferredBuild()
  {
    if (this == null || Application.isPlaying || !autoBuildVisual)
      return;

    EnsureVisual();
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
    if (ingredient == builtIngredient && FindVisualChild() != null)
      return;

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

    FitCollider(visual);
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
    return transform.Find(VisualChildName);
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

    Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
    if (renderers.Length == 0)
      return;

    Bounds worldBounds = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++)
      worldBounds.Encapsulate(renderers[i].bounds);

    Vector3 scale = transform.lossyScale;
    box.center = transform.InverseTransformPoint(worldBounds.center);
    box.size = new Vector3(
      Mathf.Abs(worldBounds.size.x / Mathf.Max(0.0001f, scale.x)),
      Mathf.Abs(worldBounds.size.y / Mathf.Max(0.0001f, scale.y)),
      Mathf.Abs(worldBounds.size.z / Mathf.Max(0.0001f, scale.z)));
    box.isTrigger = false;
  }
}
