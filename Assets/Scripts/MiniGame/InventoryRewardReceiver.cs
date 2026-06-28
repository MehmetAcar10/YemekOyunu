using System.Collections;
using UnityEngine;

/// <summary>
/// Anasahne'de GameManager uzerine takilir. Sahne acildiginda mini oyundan
/// gelen bekleyen malzemeleri (PendingPickups) envantere bosaltir.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class InventoryRewardReceiver : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    private void OnEnable()
    {
        StartCoroutine(DrainWhenReady());
    }

    private IEnumerator DrainWhenReady()
    {
        yield return null;

        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();

        if (inventory != null)
            PendingPickups.Drain(inventory);
    }
}
