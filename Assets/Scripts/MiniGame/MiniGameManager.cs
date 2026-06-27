using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using Summerjam.Utils;

namespace Summerjam.MiniGame
{
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("UI Elements")]
        public TextMeshProUGUI healthText;
        public GameObject gameOverPanel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI finalScoreText;

        [Header("Game Settings")]
        public int maxHealth = 3;
        public int peppersToWin = 5;

        [Header("Odul / Geri Donus")]
        [Tooltip("Kazaninca envantere eklenecek malzeme (Pepper).")]
        public IngredientSO rewardIngredient;
        [Tooltip("Kazaninca donulecek sahne.")]
        public string returnSceneName = "Anasahne";
        [Tooltip("Kazanma panelini gosterip sahneye donmeden once beklenecek sure.")]
        public float returnDelay = 1.5f;

        private int _currentHealth;
        private int _peppersCollected;
        private bool _isGameOver;

        public bool IsGameOver => _isGameOver;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _currentHealth = maxHealth;
            _peppersCollected = 0;
            _isGameOver = false;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            UpdateUI();
        }

        public void AddScore(int points)
        {
            if (_isGameOver) return;

            // Arka planda biber sayısını takip ediyoruz (UI'da gösterilmez)
            _peppersCollected++;
            UpdateUI();

            if (_peppersCollected >= peppersToWin) // hedef biber toplayınca
            {
                WinGame();
            }
        }

        public void TakeDamage(int damage)
        {
            if (_isGameOver) return;

            _currentHealth -= damage;
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                GameOver();
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (healthText != null)
                healthText.text = $"HEALTH: {_currentHealth}";
        }

        private void GameOver()
        {
            _isGameOver = true;
            Debug.Log("[MiniGame] Game Over!");

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                
                if (titleText != null)
                {
                    titleText.text = "GAME OVER";
                    titleText.color = new Color(1f, 0.2f, 0.2f);
                }

                if (finalScoreText != null)
                    finalScoreText.text = "YOU FAILED";
            }
        }

        private void WinGame()
        {
            _isGameOver = true;
            Debug.Log("[MiniGame] You Win!");

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                if (titleText != null)
                {
                    titleText.text = "YOU WIN!";
                    titleText.color = new Color(0.2f, 1f, 0.2f);
                }

                if (finalScoreText != null)
                    finalScoreText.text = $"YOU COLLECTED\n{peppersToWin} PEPPERS!";
            }

            // Toplanan biberi envantere gonderilmek uzere kuyruga al
            if (rewardIngredient != null)
                PendingPickups.Add(rewardIngredient, 1);

            // Kisa bir gecikmeyle Anasahne'ye don
            StartCoroutine(ReturnToSceneAfterDelay());
        }

        private IEnumerator ReturnToSceneAfterDelay()
        {
            yield return new WaitForSecondsRealtime(returnDelay);

            if (string.IsNullOrEmpty(returnSceneName))
                yield break;

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(returnSceneName);
            else
                SceneManager.LoadScene(returnSceneName);
        }

        public void RetryGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
