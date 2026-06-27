using System.Collections.Generic;
using UnityEngine;

public static class RecipeMatcher
{
    public static bool EslesiyorMu(RecipeSO recipe, IReadOnlyList<IngredientSO> bowl)
    {
        return HesaplaFark(recipe, bowl) == 0;
    }

    public static int HesaplaFark(RecipeSO recipe, IReadOnlyList<IngredientSO> bowl)
    {
        if (recipe == null || recipe.isFallback || recipe.requiredIngredients == null || bowl == null)
            return int.MaxValue;

        if (recipe.requiredIngredients.Count == 0)
            return int.MaxValue;

        int matched = CountMatchedIngredients(recipe.requiredIngredients, bowl);
        return recipe.requiredIngredients.Count + bowl.Count - 2 * matched;
    }

    public static int GetSlimTolerance(int requiredIngredientCount)
    {
        if (requiredIngredientCount <= 0)
            return 0;

        if (requiredIngredientCount == 3)
            return 1;

        if (requiredIngredientCount == 5 || requiredIngredientCount == 6)
            return 2;

        if (requiredIngredientCount > 6)
            return 3;

        if (requiredIngredientCount <= 2)
            return 1;

        if (requiredIngredientCount == 4)
            return 2;

        return 1;
    }

    public static bool SlimUyuyorMu(RecipeSO recipe, IReadOnlyList<IngredientSO> bowl)
    {
        if (recipe == null || recipe.isFallback || recipe.requiredIngredients == null || bowl == null)
            return false;

        int difference = HesaplaFark(recipe, bowl);
        if (difference <= 0)
            return false;

        return difference <= GetSlimTolerance(recipe.requiredIngredients.Count);
    }

    private static int CountMatchedIngredients(
        IReadOnlyList<IngredientSO> required,
        IReadOnlyList<IngredientSO> bowl)
    {
        Dictionary<IngredientSO, int> requiredCounts = CountIngredients(required);
        Dictionary<IngredientSO, int> bowlCounts = CountIngredients(bowl);

        int matched = 0;
        foreach (KeyValuePair<IngredientSO, int> entry in requiredCounts)
        {
            if (bowlCounts.TryGetValue(entry.Key, out int bowlCount))
                matched += Mathf.Min(entry.Value, bowlCount);
        }

        return matched;
    }

    private static Dictionary<IngredientSO, int> CountIngredients(IReadOnlyList<IngredientSO> ingredients)
    {
        Dictionary<IngredientSO, int> counts = new Dictionary<IngredientSO, int>();

        foreach (IngredientSO ingredient in ingredients)
        {
            if (ingredient == null)
                continue;

            counts.TryGetValue(ingredient, out int count);
            counts[ingredient] = count + 1;
        }

        return counts;
    }
}
