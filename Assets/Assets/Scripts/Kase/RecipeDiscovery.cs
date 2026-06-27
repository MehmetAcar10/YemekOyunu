using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeDiscovery : MonoBehaviour
{
    public static RecipeDiscovery Instance { get; private set; }

    private readonly HashSet<RecipeSO> discoveredRecipes = new HashSet<RecipeSO>();

    public bool MainRecipeFound { get; private set; }

    public event Action<RecipeSO> OnRecipeDiscovered;
    public event Action<RecipeSO> OnMainRecipeDiscovered;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterCraft(RecipeSO recipe)
    {
        if (recipe == null || recipe.isFallback)
            return;

        if (!discoveredRecipes.Add(recipe))
            return;

        OnRecipeDiscovered?.Invoke(recipe);

        if (!recipe.isMainRecipe || MainRecipeFound)
            return;

        MainRecipeFound = true;
        OnMainRecipeDiscovered?.Invoke(recipe);
        Debug.Log($"Ana tarif kesfedildi: {recipe.name}");
    }

    public bool IsDiscovered(RecipeSO recipe)
    {
        return recipe != null && discoveredRecipes.Contains(recipe);
    }
}
