using UnityEngine;

public class ItemController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float fallSpeed = 5f; // Düşme hızını Inspector'dan değiştirebilirsin

    void Update()
    {
        // Objenin saniyede fallSpeed kadar aşağı (Vector3.down) hareket etmesini sağlar
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }
}