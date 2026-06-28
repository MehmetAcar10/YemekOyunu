using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Summerjam.Utils;

namespace Summerjam.MainMenu
{
    /// <summary>
    /// Ana menü kontrolcüsü. Menü navigasyonu, buton etkileşimleri,
    /// panel geçişleri ve sürrealist kamera efektini yönetir.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Paneller")]
        [SerializeField] private CanvasGroup mainPanel;
        [SerializeField] private CanvasGroup settingsPanel;
        [SerializeField] private CanvasGroup confirmDialog;

        [Header("Butonlar")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Onay Dialogu")]
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private TMP_Text confirmMessage;

        [Header("Settings Panel")]
        [SerializeField] private Button settingsApplyButton;
        [SerializeField] private Button settingsBackButton;

        [Header("Başlık")]
        [SerializeField] private TMP_Text titleText;

        [Header("Kamera Efekti")]
        [SerializeField] private Transform mainCamera;
        [SerializeField] private float cameraDriftSpeed = 0.3f;
        [SerializeField] private float cameraDriftAmount = 2f;

        [Header("Animasyon Ayarları")]
        [SerializeField] private float panelTransitionDuration = 0.4f;

        [Header("Sahne Ayarları")]
        [SerializeField] private string gameSceneName = "Scene 1";

        // Buton referansları (animasyon için)
        private Button[] _menuButtons;
        private Vector3 _cameraStartPos;
        private Vector3 _cameraStartRot;

        private void Start()
        {
            // Ana menüde mouse her zaman görünür ve serbest olmalı
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            InitializeMenu();
            SetupButtonListeners();
            StartCoroutine(AnimateMenuEntrance());

            // Sahne geçişinden geliyorsak fade-in uygula
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.FadeIn();
            }
        }

        private void Update()
        {
            AnimateCameraDrift();

            // Her frame cursor durumunu zorla (diğer scriptler kilitleyebilir)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Menü başlangıç durumunu ayarlar.
        /// </summary>
        private void InitializeMenu()
        {
            // Kamera başlangıç pozisyonu
            if (mainCamera != null)
            {
                _cameraStartPos = mainCamera.position;
                _cameraStartRot = mainCamera.eulerAngles;
            }

            // Continue butonunu kayıt durumuna göre ayarla
            if (continueButton != null)
            {
                continueButton.interactable = SaveSystem.HasSaveData();

                // Kayıt yoksa butonu soluk göster
                CanvasGroup continueGroup = continueButton.GetComponent<CanvasGroup>();
                if (continueGroup == null)
                    continueGroup = continueButton.gameObject.AddComponent<CanvasGroup>();
                continueGroup.alpha = SaveSystem.HasSaveData() ? 1f : 0.4f;
            }

            // Panelleri başlangıç durumuna getir
            SetPanelState(settingsPanel, false);
            SetPanelState(confirmDialog, false);

            // Başlık animasyonu için başlangıçta gizle
            if (mainPanel != null)
            {
                mainPanel.alpha = 0f;
            }

            _menuButtons = new Button[] { continueButton, newGameButton, settingsButton, quitButton };
        }

        /// <summary>
        /// Butonlara listener ekler.
        /// </summary>
        private void SetupButtonListeners()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);

            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);

            if (settingsApplyButton != null)
                settingsApplyButton.onClick.AddListener(OnSettingsApply);

            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(OnSettingsBack);

            // Butonlara hover efekti ekle
            foreach (Button btn in new[] { continueButton, newGameButton, settingsButton, quitButton })
            {
                if (btn != null)
                {
                    MenuButtonEffect effect = btn.gameObject.GetComponent<MenuButtonEffect>();
                    if (effect == null)
                        btn.gameObject.AddComponent<MenuButtonEffect>();
                }
            }
        }

        #region Buton Eylemleri

        private void OnContinueClicked()
        {
            SaveData saveData = SaveSystem.Load();
            if (saveData != null)
            {
                Debug.Log($"[MainMenu] Devam ediliyor — Sahne: {saveData.currentScene}, Bölüm: {saveData.currentChapter}");
                if (SceneLoader.Instance != null)
                    SceneLoader.Instance.LoadScene(saveData.currentScene);
            }
        }

        private void OnNewGameClicked()
        {
            if (SaveSystem.HasSaveData())
            {
                // Mevcut kayıt var — onay dialogu göster
                if (confirmMessage != null)
                    confirmMessage.text = "Mevcut kayıt silinecek.\nDevam etmek istiyor musun?";
                ShowPanel(confirmDialog);
            }
            else
            {
                StartNewGame();
            }
        }

        private void OnSettingsClicked()
        {
            StartCoroutine(TransitionPanels(mainPanel, settingsPanel));
        }

        private void OnSettingsApply()
        {
            // Ayarları kaydet
            SettingsManager settings = FindObjectOfType<SettingsManager>();
            if (settings != null) settings.SaveAllSettings();
            
            // Ana menüye dön
            StartCoroutine(TransitionPanels(settingsPanel, mainPanel));
        }

        private void OnSettingsBack()
        {
            StartCoroutine(TransitionPanels(settingsPanel, mainPanel));
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Oyundan çıkılıyor...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnConfirmYes()
        {
            SaveSystem.DeleteSave();
            HidePanel(confirmDialog);
            StartNewGame();
        }

        private void OnConfirmNo()
        {
            HidePanel(confirmDialog);
        }

        private void StartNewGame()
        {
            Debug.Log("[MainMenu] Yeni oyun başlatılıyor...");

            // Cutscene bölüm ilerlemesini sıfırla
            PlayerPrefs.SetInt("CutsceneChapter", 0);
            PlayerPrefs.Save();

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(1);
        }

        #endregion

        #region Panel Geçişleri

        private void SetPanelState(CanvasGroup panel, bool active)
        {
            if (panel == null) return;
            panel.alpha = active ? 1f : 0f;
            panel.interactable = active;
            panel.blocksRaycasts = active;
        }

        private void ShowPanel(CanvasGroup panel)
        {
            StartCoroutine(FadePanel(panel, 0f, 1f, true));
        }

        private void HidePanel(CanvasGroup panel)
        {
            StartCoroutine(FadePanel(panel, 1f, 0f, false));
        }

        private IEnumerator FadePanel(CanvasGroup panel, float from, float to, bool interactableAfter)
        {
            if (panel == null) yield break;

            float elapsed = 0f;
            panel.alpha = from;

            if (interactableAfter)
            {
                panel.blocksRaycasts = true;
            }

            while (elapsed < panelTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / panelTransitionDuration);
                panel.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            panel.alpha = to;
            panel.interactable = interactableAfter;
            panel.blocksRaycasts = interactableAfter;
        }

        private IEnumerator TransitionPanels(CanvasGroup fromPanel, CanvasGroup toPanel)
        {
            yield return StartCoroutine(FadePanel(fromPanel, 1f, 0f, false));
            yield return StartCoroutine(FadePanel(toPanel, 0f, 1f, true));
        }

        #endregion

        #region Animasyonlar

        /// <summary>
        /// Menü açılış animasyonu — başlık ve butonlar sırayla fade-in yapar.
        /// </summary>
        private IEnumerator AnimateMenuEntrance()
        {
            yield return new WaitForSeconds(0.3f);

            // Ana panel fade-in
            float elapsed = 0f;
            float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (mainPanel != null)
                    mainPanel.alpha = t;
                yield return null;
            }

            if (mainPanel != null)
            {
                mainPanel.alpha = 1f;
                mainPanel.interactable = true;
                mainPanel.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Sürrealist kamera drift efekti — yavaş, rüyamsı hareket.
        /// </summary>
        private void AnimateCameraDrift()
        {
            if (mainCamera == null) return;

            float time = Time.time * cameraDriftSpeed;

            // Perlin noise tabanlı organik hareket
            float offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * cameraDriftAmount;
            float offsetY = (Mathf.PerlinNoise(0f, time * 0.7f) - 0.5f) * cameraDriftAmount * 0.5f;

            mainCamera.position = _cameraStartPos + new Vector3(offsetX, offsetY, 0f);

            // Hafif rotasyon
            float rotX = (Mathf.PerlinNoise(time * 0.5f, 1f) - 0.5f) * 2f;
            float rotY = (Mathf.PerlinNoise(1f, time * 0.5f) - 0.5f) * 2f;

            mainCamera.eulerAngles = _cameraStartRot + new Vector3(rotX, rotY, 0f);
        }

        #endregion
    }

    /// <summary>
    /// Menü butonlarına hover efekti ekleyen yardımcı bileşen.
    /// Hover'da scale büyütme ve glow efekti uygular.
    /// </summary>
    public class MenuButtonEffect : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        private Vector3 _originalScale;
        private Coroutine _scaleCoroutine;

        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float animDuration = 0.2f;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            AnimateScale(hoverScale);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            AnimateScale(1f);
        }

        private void AnimateScale(float targetMultiplier)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleCoroutine(_originalScale * targetMultiplier));
        }

        private IEnumerator ScaleCoroutine(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }
    }
}
