using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "SummerJam/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [Header("Ozel tarifler (ana + yan)")]
    [SerializeField] private List<RecipeSO> specificRecipes = new List<RecipeSO>();

    [Header("Varsayilan")]
    [SerializeField] private RecipeSO fallbackRecipe;
    [SerializeField] private float defaultMixRotations = 1f;

    public IReadOnlyList<RecipeSO> SpecificRecipes => specificRecipes;
    public RecipeSO FallbackRecipe => fallbackRecipe;

#if UNITY_EDITOR
    public void EditorSetFallbackRecipe(RecipeSO recipe)
    {
        fallbackRecipe = recipe;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorSetSpecificRecipes(List<RecipeSO> recipes)
    {
        specificRecipes = recipes ?? new List<RecipeSO>();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public RecipeMatchResult ResolveMatch(IReadOnlyList<IngredientSO> bowl)
    {
        if (bowl == null || bowl.Count == 0)
            return RecipeMatchResult.None;

        RecipeMatchResult bestExact = RecipeMatchResult.None;
        RecipeMatchResult bestSlim = RecipeMatchResult.None;

        foreach (RecipeSO recipe in specificRecipes)
        {
            if (recipe == null || recipe.isFallback || recipe.requiredIngredients == null)
                continue;

            if (recipe.requiredIngredients.Count == 0)
                continue;

            int difference = RecipeMatcher.HesaplaFark(recipe, bowl);
            if (difference == int.MaxValue)
                continue;

            if (difference == 0)
            {
                if (IsBetterMatch(recipe, difference, RecipeMatchQuality.Exact, bestExact))
                {
                    bestExact = new RecipeMatchResult
                    {
                        Recipe = recipe,
                        Quality = RecipeMatchQuality.Exact,
                        Difference = difference
                    };
                }

                continue;
            }

            if (!RecipeMatcher.SlimUyuyorMu(recipe, bowl))
                continue;

            if (IsBetterMatch(recipe, difference, RecipeMatchQuality.Slim, bestSlim))
            {
                bestSlim = new RecipeMatchResult
                {
                    Recipe = recipe,
                    Quality = RecipeMatchQuality.Slim,
                    Difference = difference
                };
            }
        }

        if (bestExact.IsValid)
            return bestExact;

        if (bestSlim.IsValid)
            return bestSlim;

        if (fallbackRecipe == null)
            Debug.LogError($"RecipeDatabase '{name}': Fallback Recipe atanmamis. Eslesmeyen karisimlar sonuc vermez.");

        return new RecipeMatchResult
        {
            Recipe = fallbackRecipe,
            Quality = RecipeMatchQuality.None,
            Difference = int.MaxValue
        };
    }

    public RecipeSO Resolve(IReadOnlyList<IngredientSO> bowl)
    {
        return ResolveMatch(bowl).Recipe;
    }

    public float GetMixRotations(RecipeSO recipe)
    {
        if (recipe == null)
            return defaultMixRotations;

        return recipe.requiredRotations > 0f ? recipe.requiredRotations : defaultMixRotations;
    }

    private static bool IsBetterMatch(
        RecipeSO candidate,
        int difference,
        RecipeMatchQuality quality,
        RecipeMatchResult currentBest)
    {
        if (!currentBest.IsValid)
            return true;

        if (difference < currentBest.Difference)
            return true;

        if (difference > currentBest.Difference)
            return false;

        int candidateScore = CalculateRecipeScore(candidate, quality);
        int currentScore = CalculateRecipeScore(currentBest.Recipe, currentBest.Quality);
        return candidateScore > currentScore;
    }

    private static int CalculateRecipeScore(RecipeSO recipe, RecipeMatchQuality quality)
    {
        if (recipe == null)
            return int.MinValue;

        int score = recipe.priority;

        if (quality == RecipeMatchQuality.Exact && recipe.isMainRecipe)
            score += 10000;

        if (recipe.requiredIngredients != null)
            score += recipe.requiredIngredients.Count * 100;

        return score;
    }
}
