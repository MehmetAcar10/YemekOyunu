using UnityEngine;

public static class SummerJamGameBootstrap
{
  private static bool initialized;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  private static void AutoInitialize()
  {
    EnsurePlayModeSetup();
  }

  public static void EnsurePlayModeSetup()
  {
    if (initialized)
      return;

    initialized = true;
    SummerJamMechanicsSettings settings = SummerJamMechanicsSettings.Instance;

    PlayerInventory inventory = EnsureGameManager(settings);
    EnsureInputHandler(settings);
    EnsureInteractionCursor(settings);
    EnsureBowl(settings, inventory);
    EnsureTrashCan(settings, inventory);
    WireIngredientPickups(inventory);
  }

  private static PlayerInventory EnsureGameManager(SummerJamMechanicsSettings settings)
  {
    PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
    if (inventory != null)
      return inventory;

    GameObject gameManager = new GameObject(settings.gameManagerName);
    inventory = gameManager.AddComponent<PlayerInventory>();
    gameManager.AddComponent<InventoryUI>();
    gameManager.AddComponent<RecipeDiscovery>();
    return inventory;
  }

  private static void WireIngredientPickups(PlayerInventory inventory)
  {
    if (inventory == null)
      return;

    IngredientPickup[] pickups = Object.FindObjectsOfType<IngredientPickup>();
    for (int i = 0; i < pickups.Length; i++)
      pickups[i].SetInventory(inventory);
  }

  private static void EnsureInputHandler(SummerJamMechanicsSettings settings)
  {
    if (Object.FindObjectOfType<InteractionInputHandler>() != null)
      return;

    GameObject handlerObject = new GameObject(settings.inputHandlerName);
    handlerObject.AddComponent<InteractionInputHandler>();
  }

  private static void EnsureInteractionCursor(SummerJamMechanicsSettings settings)
  {
    GameObject cursor = GameObject.Find(settings.interactionCursorName);
    if (cursor == null)
      cursor = new GameObject(settings.interactionCursorName);

    SphereCollider sphereCollider = cursor.GetComponent<SphereCollider>();
    if (sphereCollider != null)
      Object.Destroy(sphereCollider);

    Rigidbody rigidbody = cursor.GetComponent<Rigidbody>();
    if (rigidbody != null)
      Object.Destroy(rigidbody);

    if (cursor.GetComponent<PlayerInteractionBody>() == null)
      cursor.AddComponent<PlayerInteractionBody>();

    if (cursor.transform.position == Vector3.zero)
      cursor.transform.position = new Vector3(0f, 0.5f, 0f);
  }

  private static void EnsureBowl(SummerJamMechanicsSettings settings, PlayerInventory inventory)
  {
    Kase kase = Object.FindObjectOfType<Kase>();
    if (kase == null)
      kase = CreateDefaultBowl();

    kase.AutoWireMissingReferences();

    EnsureInteractZoneChild(
      kase.transform,
      settings.interactZoneName,
      settings.bowlZoneCenter,
      settings.bowlZoneSize);

    BowlDeposit bowlDeposit = kase.GetComponent<BowlDeposit>();
    if (bowlDeposit == null)
      bowlDeposit = kase.gameObject.AddComponent<BowlDeposit>();

    bowlDeposit.SetKase(kase);
    bowlDeposit.SetInventory(inventory);
  }

  private static Kase CreateDefaultBowl()
  {
    GameObject bowlObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
    bowlObject.name = "Bowl";
    bowlObject.transform.position = new Vector3(0f, 0.25f, 0f);
    bowlObject.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);

    Collider bowlCollider = bowlObject.GetComponent<Collider>();
    if (bowlCollider != null)
      bowlCollider.isTrigger = false;

    return bowlObject.AddComponent<Kase>();
  }

  private static void EnsureTrashCan(SummerJamMechanicsSettings settings, PlayerInventory inventory)
  {
    GameObject trashCan = GameObject.Find(settings.trashCanName);
    if (trashCan == null)
    {
      trashCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
      trashCan.name = settings.trashCanName;
      Object.Destroy(trashCan.GetComponent<BoxCollider>());
      trashCan.transform.position = settings.defaultTrashCanPosition;
      trashCan.transform.localScale = settings.defaultTrashCanScale;
    }

    EnsureInteractZoneChild(
      trashCan.transform,
      settings.interactZoneName,
      settings.trashZoneCenter,
      settings.trashZoneSize);

    TrashBin trashBin = trashCan.GetComponent<TrashBin>();
    if (trashBin == null)
      trashBin = trashCan.AddComponent<TrashBin>();

    trashBin.SetInventory(inventory);
  }

  private static void EnsureInteractZoneChild(
    Transform parent,
    string zoneName,
    Vector3 localCenter,
    Vector3 size)
  {
    Transform existingZone = parent.Find(zoneName);
    bool createdZone = existingZone == null;
    GameObject zoneObject = createdZone
      ? new GameObject(zoneName)
      : existingZone.gameObject;

    if (createdZone)
      zoneObject.transform.SetParent(parent, false);

    if (createdZone)
    {
      zoneObject.transform.localPosition = localCenter;
      zoneObject.transform.localRotation = Quaternion.identity;
      zoneObject.transform.localScale = Vector3.one;
    }

    BoxCollider boxCollider = zoneObject.GetComponent<BoxCollider>();
    bool addedCollider = boxCollider == null;
    if (addedCollider)
      boxCollider = zoneObject.AddComponent<BoxCollider>();

    boxCollider.isTrigger = true;

    if (createdZone || addedCollider)
    {
      boxCollider.center = Vector3.zero;
      boxCollider.size = size;
    }

    if (zoneObject.GetComponent<InteractionTriggerZone>() == null)
      zoneObject.AddComponent<InteractionTriggerZone>();
  }
}
