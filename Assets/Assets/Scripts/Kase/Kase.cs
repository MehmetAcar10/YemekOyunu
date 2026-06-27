using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Kase : MonoBehaviour
{
    [Header("Tarifler")]
    [SerializeField] private RecipeDatabase recipeDatabase;
    [SerializeField] private RecipeSO fallbackRecipe;

    [Header("Gorsel")]
    [SerializeField] private Transform ingredientVisualParent;
    [SerializeField] private Transform resultSpawnPoint;
    [SerializeField] private float ingredientSpreadRadius = 0.28f;
    [SerializeField] private float ingredientHeight = 0.2f;

    [Header("Karistirma")]
    [SerializeField] private Camera mixCamera;

    private const int MinMixIngredients = 2;

    private readonly List<IngredientSO> ingredients = new List<IngredientSO>();
    private readonly List<GameObject> ingredientVisuals = new List<GameObject>();

    private RecipeSO resolvedRecipe;
    private RecipeMatchQuality resolvedMatchQuality;
    private float totalAngleMoved;
    private float? lastAngle;
    private bool canMix;

    public IReadOnlyList<IngredientSO> Ingredients => ingredients;
    public RecipeSO ResolvedRecipe => resolvedRecipe;
    public float CurrentRotations => totalAngleMoved / 360f;
    public bool CanMix => canMix;
    public bool HasIngredients => ingredients.Count > 0;

    private void Awake()
    {
        AutoWireMissingReferences();
    }

    public void AutoWireMissingReferences()
    {
        if (mixCamera == null)
            mixCamera = Camera.main;

        if (recipeDatabase == null)
            recipeDatabase = Resources.Load<RecipeDatabase>("recipe_database");

        if (fallbackRecipe == null && recipeDatabase != null)
            fallbackRecipe = recipeDatabase.FallbackRecipe;

        if (ingredientVisualParent == null)
        {
            Transform existingParent = transform.Find("IngredientVisualParent");
            if (existingParent == null)
            {
                GameObject parentObject = new GameObject("IngredientVisualParent");
                parentObject.transform.SetParent(transform, false);
                parentObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                existingParent = parentObject.transform;
            }

            ingredientVisualParent = existingParent;
        }
    }

    public void MalzemeEkle(IngredientSO ingredient)
    {
        if (ingredient == null)
            return;

        ingredients.Add(ingredient);
        SpawnVisual(ingredient);
        GuncelleKaristirmaDurumu();
    }

    public void MalzemeCikar(IngredientSO ingredient)
    {
        if (ingredient == null || !ingredients.Contains(ingredient))
            return;

        int index = ingredients.IndexOf(ingredient);
        ingredients.RemoveAt(index);
        RemoveVisualAt(index);
        GuncelleKaristirmaDurumu();
    }

    public void SonEkleneniCikar()
    {
        if (ingredients.Count == 0)
            return;

        int lastIndex = ingredients.Count - 1;
        ingredients.RemoveAt(lastIndex);
        RemoveVisualAt(lastIndex);
        GuncelleKaristirmaDurumu();
    }

    public bool SonMalzemeyiEnvantereGeriAl(PlayerInventory inventory)
    {
        if (ingredients.Count == 0 || inventory == null || inventory.IsFull)
            return false;

        int lastIndex = ingredients.Count - 1;
        IngredientSO ingredient = ingredients[lastIndex];

        if (ingredient == null || !inventory.TryAdd(ingredient))
            return false;

        ingredients.RemoveAt(lastIndex);
        RemoveVisualAt(lastIndex);
        GuncelleKaristirmaDurumu();
        return true;
    }

    public bool TryPickUpToInventory(PlayerInventory inventory)
    {
        if (canMix || !HasIngredients || inventory == null || inventory.IsFull)
            return false;

        return SonMalzemeyiEnvantereGeriAl(inventory);
    }

    public bool TumMalzemeleriEnvantereGeriAl(PlayerInventory inventory)
    {
        if (inventory == null)
            return false;

        bool movedAny = false;

        while (SonMalzemeyiEnvantereGeriAl(inventory))
            movedAny = true;

        return movedAny;
    }

    public void KaseyiBosalt()
    {
        ingredients.Clear();
        ClearVisuals();
        ResetMixingState();
    }

    private void GuncelleKaristirmaDurumu()
    {
        canMix = ingredients.Count >= MinMixIngredients;

        if (!canMix)
        {
            resolvedRecipe = null;
            ResetMixingState();
            return;
        }

        RecipeMatchResult match = CozumleTarif();
        RecipeSO yeniTarif = match.Recipe;

        if (yeniTarif != resolvedRecipe || resolvedMatchQuality != match.Quality)
        {
            resolvedRecipe = yeniTarif;
            resolvedMatchQuality = match.Quality;
            totalAngleMoved = 0f;
            lastAngle = null;
        }
    }

    private void Update()
    {
        if (!canMix && HasIngredients && Input.GetMouseButtonDown(0))
            TryClickPickUp();

        if (!canMix || mixCamera == null)
            return;

        if (!Input.GetMouseButton(0))
        {
            lastAngle = null;
            return;
        }

        if (IsPointerOverUi())
            return;

        Vector3 screenPos = mixCamera.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0f)
            return;

        Vector2 bowlCenter = new Vector2(screenPos.x, screenPos.y);
        Vector2 mousePos = Input.mousePosition;

        float currentAngle = Mathf.Atan2(mousePos.y - bowlCenter.y, mousePos.x - bowlCenter.x) * Mathf.Rad2Deg;

        if (lastAngle.HasValue)
        {
            float delta = Mathf.DeltaAngle(lastAngle.Value, currentAngle);
            totalAngleMoved += Mathf.Abs(delta);
        }

        lastAngle = currentAngle;

        float hedefTur = recipeDatabase != null
            ? recipeDatabase.GetMixRotations(resolvedRecipe)
            : 1f;

        if (CurrentRotations >= hedefTur)
            KarisimTamamlandi();
    }

    private RecipeMatchResult CozumleTarif()
    {
        if (ingredients.Count < MinMixIngredients)
            return RecipeMatchResult.None;

        if (recipeDatabase != null)
        {
            RecipeMatchResult fromDatabase = recipeDatabase.ResolveMatch(ingredients);
            if (fromDatabase.IsValid)
                return fromDatabase;
        }

        if (fallbackRecipe == null)
            return RecipeMatchResult.None;

        return new RecipeMatchResult
        {
            Recipe = fallbackRecipe,
            Quality = RecipeMatchQuality.None,
            Difference = int.MaxValue
        };
    }

    private void KarisimTamamlandi()
    {
        RecipeMatchResult match = CozumleTarif();
        RecipeSO recipe = match.Recipe;

        if (!SonucUretilebilirMi(recipe, match.Quality))
        {
            if (recipe == null)
                Debug.LogWarning("Karışım tamamlandı ancak tarif bulunamadı. Recipe Database → Fallback Recipe alanını kontrol edin.");
            else
                Debug.LogWarning($"Tarif '{recipe.name}' için sonuç tanımlı değil (Result Inventory Item veya Result Prefab).");

            totalAngleMoved = 0f;
            lastAngle = null;
            return;
        }

        ingredients.Clear();
        ClearVisuals();
        totalAngleMoved = 0f;
        lastAngle = null;

        UretimSonucuYerlestir(recipe, match.Quality);

        if (RecipeDiscovery.Instance != null && match.Quality == RecipeMatchQuality.Exact)
            RecipeDiscovery.Instance.RegisterCraft(recipe);
    }

    private static bool SonucUretilebilirMi(RecipeSO recipe, RecipeMatchQuality quality)
    {
        if (recipe == null)
            return false;

        if (quality == RecipeMatchQuality.Slim)
            return recipe.slimInventoryItem != null
                || recipe.resultInventoryItem != null
                || recipe.resultPrefab != null;

        return recipe.resultInventoryItem != null || recipe.resultPrefab != null;
    }

    private void UretimSonucuYerlestir(RecipeSO recipe, RecipeMatchQuality quality)
    {
        IngredientSO product = null;

        if (quality == RecipeMatchQuality.Slim)
        {
            if (recipe.slimInventoryItem != null)
                product = recipe.slimInventoryItem;
            else
                Debug.LogWarning($"Tarif '{recipe.name}' icin Slim Inventory Item eksik. Tools/SummerJam/Fix Recipe System References calistirin.");
        }
        else
        {
            product = recipe.resultInventoryItem;
        }

        if (product == null && quality == RecipeMatchQuality.Slim)
            product = recipe.resultInventoryItem;

        if (product != null)
        {
            ingredients.Add(product);
            SpawnVisual(product);
            GuncelleKaristirmaDurumu();
            return;
        }

        if (recipe.resultInventoryItem != null)
        {
            ingredients.Add(recipe.resultInventoryItem);
            SpawnVisual(recipe.resultInventoryItem);
            GuncelleKaristirmaDurumu();
            return;
        }

        if (recipe.resultPrefab == null)
            return;

        Transform parent = ingredientVisualParent != null ? ingredientVisualParent : transform;
        GameObject visual = Instantiate(recipe.resultPrefab, parent);
        visual.transform.localPosition = GetIngredientLocalPosition(0, 1);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = recipe.resultPrefab.transform.localScale;
        ingredientVisuals.Add(visual);
        Debug.LogWarning($"Tarif '{recipe.name}' icin Result Inventory Item eksik; urun envantere alinamaz.");
        GuncelleKaristirmaDurumu();
    }

    private void TryClickPickUp()
    {
        if (IsPointerOverUi() || mixCamera == null)
            return;

        Ray ray = mixCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            return;

        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (inventory != null)
            TryPickUpToInventory(inventory);
    }

    private Vector3 GetIngredientLocalPosition(int index, int totalCount)
    {
        if (totalCount <= 1)
            return new Vector3(0f, ingredientHeight, 0f);

        float angleStep = 360f / totalCount;
        float angleRad = (angleStep * index) * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(angleRad) * ingredientSpreadRadius,
            ingredientHeight,
            Mathf.Sin(angleRad) * ingredientSpreadRadius);
    }

    private void YenidenYerlestirGorseller()
    {
        int total = ingredientVisuals.Count;
        for (int i = 0; i < total; i++)
        {
            if (ingredientVisuals[i] == null)
                continue;

            ingredientVisuals[i].transform.localPosition = GetIngredientLocalPosition(i, total);
        }
    }

    private void SpawnVisual(IngredientSO ingredient)
    {
        Transform parent = ingredientVisualParent != null ? ingredientVisualParent : transform;

        if (ingredient.visualPrefab == null)
        {
            ingredientVisuals.Add(null);
            return;
        }

        int index = ingredientVisuals.Count;
        int totalAfterAdd = index + 1;
        GameObject visual = Instantiate(ingredient.visualPrefab, parent);
        visual.transform.localPosition = GetIngredientLocalPosition(index, totalAfterAdd);
        visual.transform.localRotation = Quaternion.identity;
        ingredientVisuals.Add(visual);
    }

    private void RemoveVisualAt(int index)
    {
        if (index < 0 || index >= ingredientVisuals.Count)
            return;

        if (ingredientVisuals[index] != null)
            Destroy(ingredientVisuals[index]);

        ingredientVisuals.RemoveAt(index);
        YenidenYerlestirGorseller();
    }

    private void ClearVisuals()
    {
        foreach (GameObject visual in ingredientVisuals)
        {
            if (visual != null)
                Destroy(visual);
        }

        ingredientVisuals.Clear();
    }

    private void ResetMixingState()
    {
        resolvedRecipe = null;
        resolvedMatchQuality = RecipeMatchQuality.None;
        canMix = false;
        totalAngleMoved = 0f;
        lastAngle = null;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
