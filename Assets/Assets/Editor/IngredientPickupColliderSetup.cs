#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IngredientPickupColliderSetup
{
    [MenuItem("Tools/SummerJam/Refit All Scene Colliders")]
    [MenuItem("SummerJam/Refit All Scene Colliders")]
    public static void RefitAllSceneColliders()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("IngredientPickupColliderSetup: Aktif sahne bulunamadi.");
            return;
        }

        int pickupCount = RefitAllPickups();
        int zoneCount = RefitInteractionZones();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"IngredientPickupColliderSetup: {pickupCount} pickup ve {zoneCount} etkilesim zone collider'i guncellendi.");
    }

    [MenuItem("Tools/SummerJam/Refit Pickup Colliders")]
    [MenuItem("SummerJam/Refit Pickup Colliders")]
    public static void RefitPickupCollidersOnly()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("IngredientPickupColliderSetup: Aktif sahne bulunamadi.");
            return;
        }

        int pickupCount = RefitAllPickups();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"IngredientPickupColliderSetup: {pickupCount} pickup collider'i guncellendi.");
    }

    private static int RefitAllPickups()
    {
        IngredientPickup[] pickups = Object.FindObjectsOfType<IngredientPickup>(true);
        for (int i = 0; i < pickups.Length; i++)
            pickups[i].RefitColliderFromVisual();

        return pickups.Length;
    }

    private static int RefitInteractionZones()
    {
        SummerJamMechanicsSettings settings = SummerJamMechanicsSettings.Instance;
        int count = 0;

        Kase kase = Object.FindObjectOfType<Kase>();
        if (kase != null)
        {
            InteractZoneUtility.ConfigureChildZone(
                kase.transform,
                settings.interactZoneName,
                settings.bowlZoneCenter,
                settings.bowlZoneSize,
                settings.bowlZoneMinWorldSize);
            count++;
        }

        GameObject trashCan = GameObject.Find(settings.trashCanName);
        if (trashCan != null)
        {
            InteractZoneUtility.ConfigureChildZone(
                trashCan.transform,
                settings.interactZoneName,
                settings.trashZoneCenter,
                settings.trashZoneSize,
                settings.trashZoneMinWorldSize);
            count++;
        }

        return count;
    }
}
#endif
