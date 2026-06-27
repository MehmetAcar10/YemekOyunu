using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Kase : MonoBehaviour
{
    [Header("Tarifler")]
    [SerializeField] private List<RecipeSO> allRecipes = new List<RecipeSO>();

    [Header("Görsel")]
    [SerializeField] private Transform ingredientVisualParent;
    [SerializeField] private Transform resultSpawnPoint;

    [Header("Karıştırma")]
    [SerializeField] private Camera mixCamera;

    private readonly List<IngredientSO> ingredients = new List<IngredientSO>();
    private readonly List<GameObject> ingredientVisuals = new List<GameObject>();

    private RecipeSO matchedRecipe;
    private float totalAngleMoved;
    private float? lastAngle;
    private bool canMix;

    public IReadOnlyList<IngredientSO> Ingredients => ingredients;
    public RecipeSO MatchedRecipe => matchedRecipe;
    public float CurrentRotations => totalAngleMoved / 360f;
    public bool CanMix => canMix;

    private void Awake()
    {
        if (mixCamera == null)
            mixCamera = Camera.main;
    }

    public void MalzemeEkle(IngredientSO ingredient)
    {
        if (ingredient == null)
            return;

        ingredients.Add(ingredient);
        SpawnVisual(ingredient);
        TarifKontrolEt();
    }

    public void MalzemeCikar(IngredientSO ingredient)
    {
        if (ingredient == null || !ingredients.Contains(ingredient))
            return;

        int index = ingredients.IndexOf(ingredient);
        ingredients.RemoveAt(index);
        RemoveVisualAt(index);
        TarifKontrolEt();
    }

    public void SonEkleneniCikar()
    {
        if (ingredients.Count == 0)
            return;

        ingredients.RemoveAt(ingredients.Count - 1);
        RemoveVisualAt(ingredientVisuals.Count - 1);
        TarifKontrolEt();
    }

    public void KaseyiBosalt()
    {
        ingredients.Clear();
        ClearVisuals();
        ResetMixingState();
    }

    private void TarifKontrolEt()
    {
        matchedRecipe = allRecipes.FirstOrDefault(TarifEslesiyorMu);
        canMix = matchedRecipe != null;

        if (!canMix)
            ResetMixingState();
    }

    private bool TarifEslesiyorMu(RecipeSO recipe)
    {
        if (recipe == null || recipe.requiredIngredients == null)
            return false;

        return recipe.requiredIngredients.Count == ingredients.Count
            && recipe.requiredIngredients.All(ing => ingredients.Contains(ing))
            && ingredients.All(ing => recipe.requiredIngredients.Contains(ing));
    }

    private void Update()
    {
        if (!canMix || matchedRecipe == null)
            return;

        if (!Input.GetMouseButton(0))
        {
            lastAngle = null;
            return;
        }

        Vector3 screenPos = mixCamera.WorldToScreenPoint(transform.position);
        Vector2 bowlCenter = new Vector2(screenPos.x, screenPos.y);
        Vector2 mousePos = Input.mousePosition;

        float currentAngle = Mathf.Atan2(mousePos.y - bowlCenter.y, mousePos.x - bowlCenter.x) * Mathf.Rad2Deg;

        if (lastAngle.HasValue)
        {
            float delta = Mathf.DeltaAngle(lastAngle.Value, currentAngle);
            totalAngleMoved += Mathf.Abs(delta);
        }

        lastAngle = currentAngle;

        if (CurrentRotations >= matchedRecipe.requiredRotations)
            KarisimTamamlandi();
    }

    private void KarisimTamamlandi()
    {
        RecipeSO recipe = matchedRecipe;
        Transform spawn = resultSpawnPoint != null ? resultSpawnPoint : transform;

        if (recipe.resultPrefab != null)
            Instantiate(recipe.resultPrefab, spawn.position, spawn.rotation);

        KaseyiBosalt();
    }

    private void SpawnVisual(IngredientSO ingredient)
    {
        if (ingredient.visualPrefab == null)
            return;

        Transform parent = ingredientVisualParent != null ? ingredientVisualParent : transform;
        GameObject visual = Instantiate(ingredient.visualPrefab, parent);
        visual.transform.localPosition = new Vector3(0f, 0.1f * ingredientVisuals.Count, 0f);
        ingredientVisuals.Add(visual);
    }

    private void RemoveVisualAt(int index)
    {
        if (index < 0 || index >= ingredientVisuals.Count)
            return;

        Destroy(ingredientVisuals[index]);
        ingredientVisuals.RemoveAt(index);
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
        matchedRecipe = null;
        canMix = false;
        totalAngleMoved = 0f;
        lastAngle = null;
    }
}
