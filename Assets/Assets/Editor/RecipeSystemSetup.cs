#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RecipeSystemSetup
{
    private const string PrimaryRecipesFolder = "Assets/Assets/ingredients_recipes";
    private const string LegacyRecipesFolder = "Assets/ingredients_recipes";
    private const string PrimaryDatabasePath = "Assets/Assets/ingredients_recipes/recipe_database.asset";
    private const string LegacyDatabasePath = "Assets/ingredients_recipes/recipe_database.asset";
    private const string PrimaryFallbackPath = "Assets/Assets/ingredients_recipes/fallback_recipe.asset";
    private const string LegacyFallbackPath = "Assets/ingredients_recipes/fallback_recipe.asset";
    private const string PrimaryResourcesDatabasePath = "Assets/Assets/Resources/recipe_database.asset";
    private const string LegacyResourcesDatabasePath = "Assets/Resources/recipe_database.asset";

    private static bool setupWarningLogged;

    static RecipeSystemSetup()
    {
        EditorApplication.delayCall += EnsureRecipeSystemConfigured;
    }

    [MenuItem("Tools/SummerJam/Fix Recipe System References")]
    [MenuItem("SummerJam/Fix Recipe System References")]
    public static void EnsureRecipeSystemConfigured()
    {
        RecipeSO fallback = LoadAssetAtAnyPath<RecipeSO>(PrimaryFallbackPath, LegacyFallbackPath);
        RecipeDatabase database = LoadDatabase();

        if (database == null)
        {
            if (!setupWarningLogged)
            {
                Debug.LogWarning(
                    "RecipeSystemSetup: Recipe Database bulunamadi. Beklenen konumlardan biri: " +
                    $"{PrimaryDatabasePath} veya {LegacyDatabasePath} (Script tipi RecipeDatabase olmali).");
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
        RecipeDatabase database = LoadAssetAtAnyPath<RecipeDatabase>(
            PrimaryDatabasePath,
            LegacyDatabasePath,
            PrimaryResourcesDatabasePath,
            LegacyResourcesDatabasePath);
        if (database != null)
            return database;

        string[] guids = AssetDatabase.FindAssets("t:RecipeDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            database = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
            if (database != null)
                return database;
        }

        return null;
    }

    private static string GetRecipesFolder()
    {
        if (AssetDatabase.IsValidFolder(PrimaryRecipesFolder))
            return PrimaryRecipesFolder;

        return LegacyRecipesFolder;
    }

    private static string GetResourcesDatabasePath()
    {
        if (AssetDatabase.LoadAssetAtPath<RecipeDatabase>(PrimaryResourcesDatabasePath) != null
            || AssetDatabase.IsValidFolder("Assets/Assets/Resources"))
            return PrimaryResourcesDatabasePath;

        return LegacyResourcesDatabasePath;
    }

    private static T LoadAssetAtAnyPath<T>(params string[] paths) where T : Object
    {
        foreach (string path in paths)
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
        }

        return null;
    }

    private static List<RecipeSO> LoadAllGameRecipes()
    {
        List<RecipeSO> recipes = new List<RecipeSO>();
        string recipesFolder = GetRecipesFolder();
        string[] guids = AssetDatabase.FindAssets("t:RecipeSO", new[] { recipesFolder });

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

            string slimPath = $"{GetRecipesFolder()}/{recipe.name}_slim.asset";
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
        string resourcesDatabasePath = GetResourcesDatabasePath();
        string resourcesFolder = System.IO.Path.GetDirectoryName(resourcesDatabasePath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(resourcesFolder))
            return;

        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            string parentFolder = System.IO.Path.GetDirectoryName(resourcesFolder)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(resourcesFolder);
            if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(folderName))
                AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        RecipeDatabase resourcesCopy = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(resourcesDatabasePath);
        if (resourcesCopy == null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(database);
            if (!string.IsNullOrEmpty(sourcePath) && AssetDatabase.CopyAsset(sourcePath, resourcesDatabasePath))
                Debug.Log($"RecipeSystemSetup: {resourcesDatabasePath} kopyalandi.");
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
