using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahneler arasi tasinan, envantere eklenmeyi bekleyen malzemeler.
/// IngredientSO bir asset oldugu icin static referanslar sahne yuklemesinde silinmez.
/// </summary>
public static class PendingPickups
{
    private static readonly List<IngredientSO> s_Pending = new List<IngredientSO>();
    private static bool s_SceneHookRegistered;

    public static int Count => s_Pending.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (s_SceneHookRegistered)
            return;

        s_SceneHookRegistered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
        if (inventory != null)
            Drain(inventory);
    }

    public static void Add(IngredientSO ingredient, int count = 1)
    {
        if (ingredient == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            s_Pending.Add(ingredient);
        }
    }

    /// <summary>
    /// Bekleyen malzemeleri envantere ekler. Sigmayanlar listede kalir.
    /// </summary>
    public static void Drain(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        IngredientSO lastAdded = null;

        for (int i = s_Pending.Count - 1; i >= 0; i--)
        {
            IngredientSO ingredient = s_Pending[i];
            if (inventory.TryAdd(ingredient))
            {
                lastAdded = ingredient;
                s_Pending.RemoveAt(i);
            }
        }

        if (lastAdded != null)
            inventory.SelectIngredient(lastAdded);
    }

    public static void Clear()
    {
        s_Pending.Clear();
    }
}
