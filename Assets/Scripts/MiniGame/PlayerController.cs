using UnityEngine;

namespace Summerjam.MiniGame
{
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 10f;
        public float limitX = 8f; // Sağ ve sol sınır

        private void Update()
        {
            if (MiniGameManager.Instance != null && MiniGameManager.Instance.IsGameOver)
                return;

            // Yatay hareket (A/D veya Sol/Sağ ok tuşları)
            float moveInput = Input.GetAxis("Horizontal");
            
            // Yeni pozisyonu hesapla
            Vector3 newPosition = transform.position + Vector3.right * moveInput * moveSpeed * Time.deltaTime;
            
            // X sınırlarını uygula
            newPosition.x = Mathf.Clamp(newPosition.x, -limitX, limitX);
            
            // Pozisyonu güncelle
            transform.position = newPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (MiniGameManager.Instance != null && MiniGameManager.Instance.IsGameOver)
                return;

            FallingItem item = other.GetComponentInParent<FallingItem>();
            if (item != null)
            {
                if (item.type == ItemType.Pepper)
                {
                    MiniGameManager.Instance.AddScore(10);
                    // TODO: Play positive sound/effect
                }
                else if (item.type == ItemType.Bomb)
                {
                    MiniGameManager.Instance.TakeDamage(1);
                    // TODO: Play explosion sound/effect
                }

                // Yakalanan objeyi yok et
                Destroy(item.gameObject);
            }
        }
    }
}
