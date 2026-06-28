using UnityEngine;

[CreateAssetMenu(fileName = "SummerJamMechanicsSettings", menuName = "SummerJam/Mechanics Settings")]
public class SummerJamMechanicsSettings : ScriptableObject
{
  [Header("Tarif Verileri")]
  public string recipesFolder = "Assets/Assets/ingredients_recipes";
  public string recipeDatabasePath = "Assets/Assets/ingredients_recipes/recipe_database.asset";
  public string fallbackRecipePath = "Assets/Assets/ingredients_recipes/fallback_recipe.asset";
  public string resourcesDatabasePath = "Assets/Assets/Resources/recipe_database.asset";

  [Header("Sahne Nesneleri")]
  public string gameManagerName = "GameManager";
  public string interactionCursorName = "InteractionCursor";
  public string trashCanName = "TrashCan";
  public string interactZoneName = "InteractZone";
  public string inputHandlerName = "InteractionInputHandler";

  [Header("Kontroller")]
  public KeyCode interactKey = KeyCode.E;
  public KeyCode withdrawKey = KeyCode.Q;

  [Header("Trigger Alanlari")]
  public Vector3 bowlZoneCenter = new Vector3(0f, 0.35f, 0f);
  public Vector3 bowlZoneSize = new Vector3(2.5f, 1.5f, 2.5f);
  public float bowlZoneMinWorldSize = 2f;
  public float proximityInteractRadius = 2.75f;

  [Header("Malzeme Toplama")]
  public float pickupRaycastDistance = 6f;
  public Vector3 trashZoneCenter = new Vector3(0f, 0.35f, 0f);
  public Vector3 trashZoneSize = new Vector3(2.5f, 1.5f, 2.5f);
  public float trashZoneMinWorldSize = 2f;
  public Vector3 defaultTrashCanPosition = new Vector3(2f, 0.35f, 0f);
  public Vector3 defaultTrashCanScale = new Vector3(0.6f, 0.7f, 0.6f);

  private static SummerJamMechanicsSettings instance;

  public static SummerJamMechanicsSettings Instance
  {
    get
    {
      if (instance != null)
        return instance;

      instance = Resources.Load<SummerJamMechanicsSettings>("SummerJamMechanicsSettings");
      if (instance != null)
        return instance;

      instance = CreateInstance<SummerJamMechanicsSettings>();
      return instance;
    }
  }
}
