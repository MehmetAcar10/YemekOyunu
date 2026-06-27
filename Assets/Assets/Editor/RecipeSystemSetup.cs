#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RecipeSystemSetup
{
    private const string RecipesFolder = "Assets/ingredients_recipes";
    private const string DatabasePath = "Assets/ingredients_recipes/recipe_database.asset";
    private const string FallbackPath = "Assets/ingredients_recipes/fallback_recipe.asset";
    private const string ResourcesDatabasePath = "Assets/Resources/recipe_database.asset";

    private static bool setupWarningLogged;

    static RecipeSystemSetup()
    {
        EditorApplication.delayCall += EnsureRecipeSystemConfigured;
    }

    [MenuItem("Tools/SummerJam/Fix Recipe System References")]
    [MenuItem("SummerJam/Fix Recipe System References")]
    public static void EnsureRecipeSystemConfigured()
    {
        RecipeSO fallback = AssetDatabase.LoadAssetAtPath<RecipeSO>(FallbackPath);
        RecipeDatabase database = LoadDatabase();

        if (database == null)
        {
            if (!setupWarningLogged)
            {
                Debug.LogWarning(
                    $"RecipeSystemSetup: Recipe Database bulunamadi. Beklenen: {DatabasePath} " +
                    $"(Script tipi RecipeDatabase olmali).");
                setupWarningLogged = true;
            }

            return;
        }

        setupWarningLogged = false;

        List<RecipeSO> allRecipes = LoadAllGameRecipes();
        EnsureSlimProducts(allRecipes);

        if (database.FallbackRecipe == null && fallback != null)
            database.EditorSetFallbackRecipe(fallback);

        database.EditorSetSpecificRecipes(allRecipes);
        AssetDatabase.SaveAssets();

        EnsureResourcesCopy(database);
        EnsureBowlReferences(database, fallback);

        Debug.Log($"RecipeSystemSetup: {allRecipes.Count} tarif database'e eklendi, slim urunler kontrol edildi.");
    }

    private static RecipeDatabase LoadDatabase()
    {
        RecipeDatabase database = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(DatabasePath);
        if (database != null)
            return database;

        return AssetDatabase.LoadAssetAtPath<RecipeDatabase>(ResourcesDatabasePath);
    }

    private static List<RecipeSO> LoadAllGameRecipes()
    {
        List<RecipeSO> recipes = new List<RecipeSO>();
        string[] guids = AssetDatabase.FindAssets("t:RecipeSO", new[] { RecipesFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RecipeSO recipe = AssetDatabase.LoadAssetAtPath<RecipeSO>(path);
            if (recipe == null || recipe.isFallback || recipe.name == "fallback_recipe")
                continue;

            recipes.Add(recipe);
        }

        recipes.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return recipes;
    }

    private static void EnsureSlimProducts(List<RecipeSO> recipes)
    {
        foreach (RecipeSO recipe in recipes)
        {
            if (recipe.resultInventoryItem == null && recipe.resultPrefab == null)
                continue;

            string slimPath = $"{RecipesFolder}/{recipe.name}_slim.asset";
            IngredientSO slim = AssetDatabase.LoadAssetAtPath<IngredientSO>(slimPath);

            if (slim == null)
            {
                slim = ScriptableObject.CreateInstance<IngredientSO>();
                AssetDatabase.CreateAsset(slim, slimPath);
            }

            slim.ingredientName = RecipeSlimNaming.GetSlimName(recipe);

            if (recipe.resultInventoryItem != null)
            {
                slim.visualPrefab = recipe.resultInventoryItem.visualPrefab != null
                    ? recipe.resultInventoryItem.visualPrefab
                    : recipe.resultPrefab;
            }
            else
            {
                slim.visualPrefab = recipe.resultPrefab;
            }

            EditorUtility.SetDirty(slim);

            SerializedObject recipeObject = new SerializedObject(recipe);
            SerializedProperty slimProperty = recipeObject.FindProperty("slimInventoryItem");
            if (slimProperty != null)
            {
                slimProperty.objectReferenceValue = slim;
                recipeObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(recipe);
        }
    }

    private static void EnsureResourcesCopy(RecipeDatabase database)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        RecipeDatabase resourcesCopy = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(ResourcesDatabasePath);
        if (resourcesCopy == null)
        {
            if (AssetDatabase.CopyAsset(DatabasePath, ResourcesDatabasePath))
                Debug.Log("RecipeSystemSetup: Resources/recipe_database kopyalandi.");
            return;
        }

        if (resourcesCopy.FallbackRecipe == null && database.FallbackRecipe != null)
        {
            resourcesCopy.EditorSetFallbackRecipe(database.FallbackRecipe);
            EditorUtility.SetDirty(resourcesCopy);
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureBowlReferences(RecipeDatabase database, RecipeSO fallback)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        bool sceneDirty = false;

        foreach (Kase kase in Object.FindObjectsOfType<Kase>(true))
        {
            SerializedObject kaseObject = new SerializedObject(kase);
            SerializedProperty dbProp = kaseObject.FindProperty("recipeDatabase");
            SerializedProperty fallbackProp = kaseObject.FindProperty("fallbackRecipe");
            bool kaseDirty = false;

            if (dbProp != null && dbProp.objectReferenceValue == null && database != null)
            {
                dbProp.objectReferenceValue = database;
                kaseDirty = true;
            }

            if (fallbackProp != null && fallbackProp.objectReferenceValue == null && fallback != null)
            {
                fallbackProp.objectReferenceValue = fallback;
                kaseDirty = true;
            }

            if (kaseDirty)
            {
                kaseObject.ApplyModifiedPropertiesWithoutUndo();
                sceneDirty = true;
            }
        }

        if (sceneDirty)
            EditorSceneManager.MarkSceneDirty(scene);
    }
}
#endif
