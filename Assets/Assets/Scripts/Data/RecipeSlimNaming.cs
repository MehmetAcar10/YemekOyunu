using System.Text.RegularExpressions;
using UnityEngine;

public static class RecipeSlimNaming
{
    public static string GetSlimName(RecipeSO recipe)
    {
        string baseName = GetBaseName(recipe);
        return EnsureSlimPrefix(baseName);
    }

    private static string GetBaseName(RecipeSO recipe)
    {
        if (recipe?.resultInventoryItem != null
            && !string.IsNullOrWhiteSpace(recipe.resultInventoryItem.ingredientName))
        {
            return FormatBaseFromResult(recipe.resultInventoryItem.ingredientName);
        }

        if (recipe != null)
            return SplitWords(recipe.name);

        return "Recipe";
    }

    private static string FormatBaseFromResult(string name)
    {
        if (string.Equals(name, "Oat Milk", System.StringComparison.OrdinalIgnoreCase))
            return "Oat And Milk";

        return name;
    }

    private static string EnsureSlimPrefix(string baseName)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            return "Slim Recipe";

        if (baseName.StartsWith("Slim ", System.StringComparison.OrdinalIgnoreCase))
            return baseName;

        return $"Slim {baseName}";
    }

    private static string SplitWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Recipe";

        string spaced = Regex.Replace(value, "(?<!^)([A-Z])", " $1");
        spaced = Regex.Replace(spaced, "(_slim|_product)$", "", RegexOptions.IgnoreCase);
        return spaced.Trim();
    }
}
