using System.Collections.Generic;

/// <summary>
/// Sahneler arasi tasinan, envantere eklenmeyi bekleyen malzemeler.
/// IngredientSO bir asset oldugu icin static referanslar sahne yuklemesinde silinmez.
/// </summary>
public static class PendingPickups
{
    private static readonly List<IngredientSO> s_Pending = new List<IngredientSO>();

    public static int Count => s_Pending.Count;

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

        for (int i = s_Pending.Count - 1; i >= 0; i--)
        {
            if (inventory.TryAdd(s_Pending[i]))
            {
                s_Pending.RemoveAt(i);
            }
        }
    }

    public static void Clear()
    {
        s_Pending.Clear();
    }
}
