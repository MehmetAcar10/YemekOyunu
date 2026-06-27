using UnityEngine;

namespace Summerjam.MiniGame
{
    public enum ItemType
    {
        Pepper,
        Bomb
    }

    public class FallingItem : MonoBehaviour
    {
        public ItemType type;
        public float fallSpeed = 5f;
        public float rotationSpeed = 100f;
        
        // Zemin sınırını geçtiğinde objeyi silmek için
        public float destroyY = -6f;

        private bool _resolved;

        private void Update()
        {
            if (MiniGameManager.Instance != null && MiniGameManager.Instance.IsGameOver)
                return;

            // Aşağı düşme
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

            // Dönme efekti
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right * (rotationSpeed * 0.5f) * Time.deltaTime, Space.World);

            // Oyuncu kaçırırsa ceza uygula ve objeyi sil
            if (!_resolved && transform.position.y < destroyY)
            {
                _resolved = true;

                if (type == ItemType.Pepper && MiniGameManager.Instance != null)
                    MiniGameManager.Instance.TakeDamage(1);

                Destroy(gameObject);
            }
        }
    }
}
