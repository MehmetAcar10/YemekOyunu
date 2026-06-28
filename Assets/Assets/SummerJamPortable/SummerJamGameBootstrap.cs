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
    PendingPickups.Drain(inventory);
  }

  private static PlayerInventory EnsureGameManager(SummerJamMechanicsSettings settings)
  {
    PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
    GameObject gameManager = inventory != null ? inventory.gameObject : null;

    if (inventory == null)
    {
      gameManager = new GameObject(settings.gameManagerName);
      inventory = gameManager.AddComponent<PlayerInventory>();
      gameManager.AddComponent<InventoryUI>();
      gameManager.AddComponent<RecipeDiscovery>();
    }

    if (gameManager.GetComponent<InventoryRewardReceiver>() == null)
      gameManager.AddComponent<InventoryRewardReceiver>();

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
    if (Object.FindObjectOfType<InteractionInputHandler>() == null)
    {
      GameObject handlerObject = new GameObject(settings.inputHandlerName);
      handlerObject.AddComponent<InteractionInputHandler>();
    }

    if (Object.FindObjectOfType<IngredientPickupInput>() == null)
    {
      GameObject pickupInputObject = new GameObject("IngredientPickupInput");
      pickupInputObject.AddComponent<IngredientPickupInput>();
    }
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

    InteractZoneUtility.ConfigureChildZone(
      kase.transform,
      settings.interactZoneName,
      settings.bowlZoneCenter,
      settings.bowlZoneSize,
      settings.bowlZoneMinWorldSize);

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
    else if (trashCan.GetComponent<Collider>() == null)
    {
      BoxCollider bodyCollider = trashCan.AddComponent<BoxCollider>();
      bodyCollider.isTrigger = false;
    }

    InteractZoneUtility.ConfigureChildZone(
      trashCan.transform,
      settings.interactZoneName,
      settings.trashZoneCenter,
      settings.trashZoneSize,
      settings.trashZoneMinWorldSize);

    TrashBin trashBin = trashCan.GetComponent<TrashBin>();
    if (trashBin == null)
      trashBin = trashCan.AddComponent<TrashBin>();

    trashBin.SetInventory(inventory);
  }
}
