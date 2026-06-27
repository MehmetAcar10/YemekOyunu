using UnityEngine;

namespace Summerjam.MiniGame
{
    public class ItemSpawner : MonoBehaviour
    {
        public GameObject pepperPrefab;
        public GameObject bombPrefab;

        public float spawnInterval = 1f;
        public float spawnLimitX = 8f; // Sağ/Sol sınırı

        private float _timer;

        private void Start()
        {
            _timer = spawnInterval;
        }

        private void Update()
        {
            if (MiniGameManager.Instance != null && MiniGameManager.Instance.IsGameOver)
                return;

            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                SpawnItem();
                _timer = spawnInterval;
            }
        }

        private void SpawnItem()
        {
            if (pepperPrefab == null || bombPrefab == null) return;

            // Rastgele X pozisyonu
            float randomX = Random.Range(-spawnLimitX, spawnLimitX);
            Vector3 spawnPos = new Vector3(randomX, transform.position.y, 0f);

            // %50 biber, %50 bomba (önceki: %70 biber / %30 bomba)
            GameObject prefabToSpawn = Random.value > 0.5f ? pepperPrefab : bombPrefab;

            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }
    }
}
