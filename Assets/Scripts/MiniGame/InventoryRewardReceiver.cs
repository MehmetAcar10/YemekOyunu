using UnityEngine;

/// <summary>
/// Anasahne'de GameManager uzerine takilir. Sahne acildiginda mini oyundan
/// gelen bekleyen malzemeleri (PendingPickups) envantere bosaltir.
/// </summary>
[DisallowMultipleComponent]
public class InventoryRewardReceiver : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    private void Start()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (inventory == null)
        {
            inventory = FindObjectOfType<PlayerInventory>();
        }

        PendingPickups.Drain(inventory);
    }
}
