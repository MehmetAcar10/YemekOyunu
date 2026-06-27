using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Summerjam.Utils
{
    /// <summary>
    /// Sahne geçişlerini fade efektiyle yöneten singleton sınıf.
    /// Her sahnede bir FadeOverlay (Image) gerektirir.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Fade Ayarları")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("Yükleme Ekranı")]
        [SerializeField] private GameObject loadingIndicator;

        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Main menü sahnesinde DontDestroyOnLoad kullanmıyoruz
            // çünkü her sahne kendi SceneLoader'ına sahip olabilir.

            if (fadeOverlay != null)
            {
                // Başlangıçta şeffaf
                Color c = fadeOverlay.color;
                c.a = 0f;
                fadeOverlay.color = c;
                fadeOverlay.raycastTarget = false;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Belirtilen sahneye fade geçişi ile geçer.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isTransitioning) return;
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        /// <summary>
        /// Belirtilen sahne indexine fade geçişi ile geçer.
        /// </summary>
        public void LoadScene(int sceneIndex)
        {
            if (_isTransitioning) return;
            StartCoroutine(LoadSceneCoroutine(sceneIndex));
        }

        /// <summary>
        /// Sahneye fade-in efekti uygular (sahne yüklendikten sonra çağrılır).
        /// </summary>
        public void FadeIn()
        {
            if (fadeOverlay != null)
            {
                StartCoroutine(FadeCoroutine(1f, 0f));
            }
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            _isTransitioning = true;

            // Fade out (karartma)
            yield return StartCoroutine(FadeCoroutine(0f, 1f));

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
            }

            // Async sahne yükleme
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            asyncLoad.allowSceneActivation = true;
            _isTransitioning = false;
        }

        private IEnumerator LoadSceneCoroutine(int sceneIndex)
        {
            _isTransitioning = true;

            yield return StartCoroutine(FadeCoroutine(0f, 1f));

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            asyncLoad.allowSceneActivation = true;
            _isTransitioning = false;
        }

        private IEnumerator FadeCoroutine(float startAlpha, float targetAlpha)
        {
            if (fadeOverlay == null) yield break;

            fadeOverlay.raycastTarget = true;
            Color color = fadeOverlay.color;
            float elapsed = 0f;

            color.a = startAlpha;
            fadeOverlay.color = color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                // Smooth easing
                t = t * t * (3f - 2f * t);
                color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            fadeOverlay.color = color;

            if (targetAlpha <= 0f)
            {
                fadeOverlay.raycastTarget = false;
            }
        }
    }
}
