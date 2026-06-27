#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SummerJamPortableInstaller
{
    private const string SettingsAssetPath = "Assets/SummerJamPortable/SummerJamMechanicsSettings.asset";
    private const string ResourcesSettingsPath = "Assets/Resources/SummerJamMechanicsSettings.asset";
    private const string CopyManifestPath = "Assets/SummerJamPortable/KOPYALANACAK_DOSYALAR.txt";

    private static readonly string[] PortableFolders =
    {
        "Assets/Scripts/Data",
        "Assets/Scripts/Inventory",
        "Assets/Scripts/Kase/Kase.cs",
        "Assets/Scripts/Kase/RecipeDiscovery.cs",
        "Assets/SummerJamPortable",
        "Assets/ingredients_recipes",
        "Assets/Editor/RecipeSystemSetup.cs",
        "Assets/Editor/InteractionSceneSetup.cs"
    };

    [MenuItem("Tools/SummerJam/Install All Mechanics (Portable)")]
    [MenuItem("SummerJam/Install All Mechanics (Portable)")]
    public static void InstallAllMechanics()
    {
        SummerJamMechanicsSettings settings = EnsureSettingsAsset();
        EnsureResourcesSettingsCopy(settings);
        EnsureGameManager(settings);
        RecipeSystemSetup.EnsureRecipeSystemConfigured();
        InteractionSceneSetup.SetupInteractionZones();
        WriteCopyManifest();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        AssetDatabase.SaveAssets();
        Debug.Log(
            "SummerJamPortableInstaller: Tum mekanikler kuruldu. " +
            "Baska projeye tasimak icin: Assets/SummerJamPortable/KOPYALANACAK_DOSYALAR.txt");
    }

    [MenuItem("Tools/SummerJam/Write Portable Copy Manifest")]
    public static void WriteCopyManifestMenu()
    {
        WriteCopyManifest();
        AssetDatabase.Refresh();
        Debug.Log($"SummerJamPortableInstaller: Liste yazildi -> {CopyManifestPath}");
    }

    private static SummerJamMechanicsSettings EnsureSettingsAsset()
    {
        SummerJamMechanicsSettings settings = AssetDatabase.LoadAssetAtPath<SummerJamMechanicsSettings>(SettingsAssetPath);
        if (settings != null)
            return settings;

        if (!AssetDatabase.IsValidFolder("Assets/SummerJamPortable"))
            AssetDatabase.CreateFolder("Assets", "SummerJamPortable");

        settings = ScriptableObject.CreateInstance<SummerJamMechanicsSettings>();
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        return settings;
    }

    private static void EnsureResourcesSettingsCopy(SummerJamMechanicsSettings settings)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        SummerJamMechanicsSettings resourcesCopy =
            AssetDatabase.LoadAssetAtPath<SummerJamMechanicsSettings>(ResourcesSettingsPath);

        if (resourcesCopy == null)
        {
            AssetDatabase.CopyAsset(SettingsAssetPath, ResourcesSettingsPath);
            return;
        }

        EditorUtility.SetDirty(resourcesCopy);
    }

    private static void EnsureGameManager(SummerJamMechanicsSettings settings)
    {
        GameObject gameManager = GameObject.Find(settings.gameManagerName);
        if (gameManager == null)
        {
            gameManager = new GameObject(settings.gameManagerName);
            Undo.RegisterCreatedObjectUndo(gameManager, "Create GameManager");
        }

        if (gameManager.GetComponent<PlayerInventory>() == null)
            Undo.AddComponent<PlayerInventory>(gameManager);

        if (gameManager.GetComponent<InventoryUI>() == null)
            Undo.AddComponent<InventoryUI>(gameManager);

        if (gameManager.GetComponent<RecipeDiscovery>() == null)
            Undo.AddComponent<RecipeDiscovery>(gameManager);
    }

    private static void WriteCopyManifest()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("SUMMERJAM MEKANIKLERI - BASKA PROJEYE TASIMA LISTESI");
        builder.AppendLine("======================================================");
        builder.AppendLine();
        builder.AppendLine("1) Asagidaki klasor ve dosyalari yeni Unity projesine kopyala:");
        builder.AppendLine();

        foreach (string path in PortableFolders)
            builder.AppendLine($"   - {path}");

        builder.AppendLine();
        builder.AppendLine("2) Opsiyonel (varsa kopyala):");
        builder.AppendLine("   - Assets/Resources/recipe_database.asset");
        builder.AppendLine("   - Assets/Resources/SummerJamMechanicsSettings.asset");
        builder.AppendLine("   - Assets/Prefabs/");
        builder.AppendLine("   - Assets/Scenes/SampleScene.unity (ornek sahne)");
        builder.AppendLine();
        builder.AppendLine("3) Yeni projede Unity acildiktan sonra calistir:");
        builder.AppendLine("   Tools -> SummerJam -> Install All Mechanics (Portable)");
        builder.AppendLine();
        builder.AppendLine("4) Kontroller:");
        builder.AppendLine("   1-4      : Envanter slot sec");
        builder.AppendLine("   E        : Kaseye koy / Cop (trigger icinde)");
        builder.AppendLine("   Q        : Kaseden al (trigger icinde)");
        builder.AppendLine("   Sol tik  : Malzeme topla");
        builder.AppendLine("   Sol tik+don: Karistir (min 2 malzeme)");
        builder.AppendLine();
        builder.AppendLine("5) Mekanik ozeti:");
        builder.AppendLine("   - 4 slotlu envanter, secili slot vurgusu");
        builder.AppendLine("   - Slim / exact tarif eslesmesi, failed sludge fallback");
        builder.AppendLine("   - Trigger tabanli kase ve cop etkilesimi");
        builder.AppendLine("   - Fare konumuna gore etkilesim bolgesi secimi");
        builder.AppendLine();
        builder.AppendLine($"Olusturulma: {System.DateTime.Now}");

        File.WriteAllText(CopyManifestPath, builder.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(CopyManifestPath);
    }
}
#endif
