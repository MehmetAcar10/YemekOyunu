using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Her sahneye dönüşte farklı bir bölümün cutscene'ini oynatır.
/// Her bölümün kendi slaytları, altyazıları ve müziği vardır.
/// Bölüm ilerlemesi PlayerPrefs ile kaydedilir.
///
/// Kurulum:
///   1. Sahneye boş bir GameObject ekleyin ve bu scripti ekleyin.
///   2. Inspector'dan chapters listesine 6 bölüm ekleyin.
///   3. Her bölüme kendi slaytlarını, müziğini ve sonraki sahne ayarını girin.
///   Script gerekli UI bileşenlerini otomatik oluşturur.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Veri Yapıları
    // ─────────────────────────────────────────

    [Serializable]
    public class Slide
    {
        public enum SlideType { Image, Video }

        [Tooltip("Bu slayt görsel mi yoksa video mu?")]
        public SlideType type = SlideType.Image;

        [Tooltip("Görsel slayt için texture (JPEG/PNG) atayın")]
        public Texture2D image;

        [Tooltip("Video slayt için VideoClip atayın")]
        public VideoClip videoClip;

        [Tooltip("Slaytın altında gösterilecek altyazı (boş bırakılabilir)")]
        [TextArea(2, 5)]
        public string subtitle = "";
    }

    [Serializable]
    public class Chapter
    {
        [Tooltip("Bölüm adı (Inspector'da kolaylık için)")]
        public string chapterName = "Bölüm";

        [Tooltip("Bu bölümün slaytları")]
        public List<Slide> slides = new List<Slide>();

        [Header("Müzik")]
        [Tooltip("Bu bölümde çalacak müzik (MP3/WAV/OGG)")]
        public AudioClip music;

        [Tooltip("Müzik ses seviyesi (0-1)")]
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;

        [Tooltip("Müzik döngü olarak çalsın mı?")]
        public bool loopMusic = true;

        [Tooltip("Bu bölüm final mi? Final ise ilerleme sıfırlanır.")]
        public bool isFinal = false;
    }

    // ─────────────────────────────────────────
    //  Inspector Alanları
    // ─────────────────────────────────────────

    [Header("══ Bölümler ══")]
    [Tooltip("Her sahneye dönüşte sırayla oynatılacak bölümler (6 adet)")]
    [SerializeField] private List<Chapter> chapters = new List<Chapter>();

    [Header("══ Geçiş Ayarları ══")]
    [Tooltip("Slayt fade süresi (saniye)")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Tooltip("Slaytlar arası siyah ekran bekleme süresi (saniye)")]
    [SerializeField] private float blackScreenDuration = 0.5f;

    [Header("══ Altyazı Ayarları ══")]
    [Tooltip("Altyazı yazı boyutu")]
    [SerializeField] private int subtitleFontSize = 32;

    [Tooltip("Altyazı rengi")]
    [SerializeField] private Color subtitleColor = Color.white;

    [Header("══ Kayıt Ayarları ══")]
    [Tooltip("PlayerPrefs anahtarı — birden fazla cutscene varsa farklı isim verin")]
    [SerializeField] private string saveKey = "CutsceneChapter";

    [Tooltip("İşaretlenirse her oynatmada bölüm 0'dan başlar (test için)")]
    [SerializeField] private bool resetProgressOnStart = false;

    // ─────────────────────────────────────────
    //  Dahili Değişkenler
    // ─────────────────────────────────────────

    // Otomatik oluşturulan UI
    private Canvas _canvas;
    private CanvasGroup _contentGroup;
    private RawImage _imageDisplay;
    private RawImage _videoDisplay;
    private VideoPlayer _videoPlayer;
    private Image _background;
    private Text _subtitleText;
    private Image _subtitleBg;
    private Text _hintText;
    private Text _chapterTitle;
    private AudioSource _musicSource;

    // Durum
    private Chapter _currentChapter;
    private int _currentSlideIndex = -1;
    private bool _isTransitioning;
    private bool _isFinished;
    private bool _inputReady; // İlk slayt gösterilene kadar input kabul etme
    private RenderTexture _renderTexture;

    // ═══════════════════════════════════════════
    //  Yaşam Döngüsü
    // ═══════════════════════════════════════════

    private void Awake()
    {
        CreateUI();
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (resetProgressOnStart)
            PlayerPrefs.SetInt(saveKey, 0);

        int chapterIndex = PlayerPrefs.GetInt(saveKey, 0);

        // Tüm bölümler bittiyse ilk bölüme dön veya sahneyi atla
        if (chapterIndex >= chapters.Count)
        {
            chapterIndex = 0;
            PlayerPrefs.SetInt(saveKey, 0);
        }

        if (chapters.Count == 0)
        {
            Debug.LogWarning("[CutscenePlayer] Hiç bölüm eklenmemiş!");
            FinishCutscene(null);
            return;
        }

        _currentChapter = chapters[chapterIndex];
        Debug.Log($"[CutscenePlayer] Bölüm {chapterIndex + 1}/{chapters.Count}: {_currentChapter.chapterName}");

        // Sonraki sefer için bölümü ilerlet
        int nextChapter = chapterIndex + 1;
        if (_currentChapter.isFinal)
            nextChapter = 0; // Final ise sıfırla
        PlayerPrefs.SetInt(saveKey, nextChapter);
        PlayerPrefs.Save();

        // Müzik başlat
        StartMusic(_currentChapter);

        if (_currentChapter.slides.Count > 0)
        {
            StartCoroutine(ShowFirstSlide());
        }
        else
        {
            FinishCutscene(_currentChapter);
        }
    }

    private IEnumerator ShowFirstSlide()
    {
        _isTransitioning = true;
        _inputReady = false;
        _contentGroup.alpha = 0f;

        // Bölüm başlığını göster
        ShowChapterTitle(_currentChapter.chapterName);
        yield return new WaitForSeconds(1.5f);
        HideChapterTitle();
        yield return new WaitForSeconds(0.3f);

        ShowSlide(0);
        yield return StartCoroutine(FadeContent(0f, 1f));

        // Kısa bekleme — önceki sahneden kalan input'u yoksay
        yield return new WaitForSeconds(0.3f);

        _isTransitioning = false;
        _inputReady = true;
    }

    private void Update()
    {
        if (_isFinished) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Input hazır değilse veya geçiş sırasındaysa tıklamayı yoksay
        if (!_inputReady || _isTransitioning) return;

        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            NextSlide();
        }
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
        }
    }

    // ═══════════════════════════════════════════
    //  Müzik
    // ═══════════════════════════════════════════

    private void StartMusic(Chapter chapter)
    {
        if (chapter.music == null) return;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.clip = chapter.music;
        _musicSource.volume = chapter.musicVolume;
        _musicSource.loop = chapter.loopMusic;
        _musicSource.playOnAwake = false;
        _musicSource.Play();
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        if (_musicSource == null || !_musicSource.isPlaying) yield break;

        float startVol = _musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        _musicSource.Stop();
        _musicSource.volume = 0f;
    }

    // ═══════════════════════════════════════════
    //  UI Oluşturma
    // ═══════════════════════════════════════════

    private void CreateUI()
    {
        // Canvas — root seviyede
        GameObject canvasObj = new GameObject("CutsceneCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Siyah arka plan ──
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _background = bgObj.AddComponent<Image>();
        _background.color = Color.black;
        SetFullStretch(bgObj.GetComponent<RectTransform>());

        // ── İçerik grubu (fade edilecek) ──
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(canvasObj.transform, false);
        SetFullStretch(contentObj.AddComponent<RectTransform>());
        _contentGroup = contentObj.AddComponent<CanvasGroup>();
        _contentGroup.alpha = 0f;

        // Görsel gösterici
        GameObject imgObj = new GameObject("ImageDisplay");
        imgObj.transform.SetParent(contentObj.transform, false);
        _imageDisplay = imgObj.AddComponent<RawImage>();
        _imageDisplay.color = Color.white;
        SetFullStretch(imgObj.GetComponent<RectTransform>());
        imgObj.SetActive(false);

        // Video gösterici
        GameObject vidObj = new GameObject("VideoDisplay");
        vidObj.transform.SetParent(contentObj.transform, false);
        _videoDisplay = vidObj.AddComponent<RawImage>();
        _videoDisplay.color = Color.white;
        SetFullStretch(vidObj.GetComponent<RectTransform>());
        vidObj.SetActive(false);

        // ── Altyazı arka planı ──
        GameObject subBgObj = new GameObject("SubtitleBackground");
        subBgObj.transform.SetParent(contentObj.transform, false);
        _subtitleBg = subBgObj.AddComponent<Image>();
        _subtitleBg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform subBgRect = subBgObj.GetComponent<RectTransform>();
        subBgRect.anchorMin = new Vector2(0f, 0f);
        subBgRect.anchorMax = new Vector2(1f, 0f);
        subBgRect.pivot = new Vector2(0.5f, 0f);
        subBgRect.offsetMin = Vector2.zero;
        subBgRect.offsetMax = Vector2.zero;
        subBgRect.sizeDelta = new Vector2(0, 120);
        subBgObj.SetActive(false);

        // ── Altyazı metni ──
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(subBgObj.transform, false);
        _subtitleText = subObj.AddComponent<Text>();
        _subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _subtitleText.fontSize = subtitleFontSize;
        _subtitleText.color = subtitleColor;
        _subtitleText.alignment = TextAnchor.MiddleCenter;
        _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleText.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = subObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(2, -2);

        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = Vector2.zero;
        subRect.anchorMax = Vector2.one;
        subRect.offsetMin = new Vector2(40, 10);
        subRect.offsetMax = new Vector2(-40, -10);

        // ── Bölüm başlığı (ortada büyük yazı) ──
        GameObject titleObj = new GameObject("ChapterTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        _chapterTitle = titleObj.AddComponent<Text>();
        _chapterTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _chapterTitle.fontSize = 56;
        _chapterTitle.color = Color.white;
        _chapterTitle.alignment = TextAnchor.MiddleCenter;
        _chapterTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetFullStretch(titleObj.GetComponent<RectTransform>());

        Shadow titleShadow = titleObj.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0, 0, 0.9f);
        titleShadow.effectDistance = new Vector2(3, -3);

        titleObj.SetActive(false);

        // ── VideoPlayer ──
        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = false;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        _renderTexture = new RenderTexture(1920, 1080, 0);
        _videoPlayer.targetTexture = _renderTexture;
        _videoDisplay.texture = _renderTexture;

        // ── İpucu yazısı ──
        GameObject hintObj = new GameObject("ClickHint");
        hintObj.transform.SetParent(canvasObj.transform, false);
        _hintText = hintObj.AddComponent<Text>();
        _hintText.text = "Devam etmek için tıklayın...";
        _hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hintText.fontSize = 22;
        _hintText.color = new Color(1f, 1f, 1f, 0.5f);
        _hintText.alignment = TextAnchor.UpperRight;
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        SetFullStretch(hintRect);
        hintRect.offsetMax = new Vector2(-30, -20);
    }

    private void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ═══════════════════════════════════════════
    //  Bölüm Başlığı
    // ═══════════════════════════════════════════

    private void ShowChapterTitle(string title)
    {
        if (_chapterTitle == null || string.IsNullOrEmpty(title)) return;
        _chapterTitle.text = title;
        _chapterTitle.gameObject.SetActive(true);
    }

    private void HideChapterTitle()
    {
        if (_chapterTitle != null)
            _chapterTitle.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  Slayt Yönetimi
    // ═══════════════════════════════════════════

    private void ShowSlide(int index)
    {
        _currentSlideIndex = index;
        Slide slide = _currentChapter.slides[index];

        if (slide.type == Slide.SlideType.Image)
        {
            if (_videoPlayer != null) _videoPlayer.Stop();
            _videoDisplay.gameObject.SetActive(false);

            _imageDisplay.texture = slide.image;
            _imageDisplay.gameObject.SetActive(true);
        }
        else // Video
        {
            _imageDisplay.gameObject.SetActive(false);

            _videoDisplay.gameObject.SetActive(true);
            if (slide.videoClip != null)
            {
                _videoPlayer.clip = slide.videoClip;
                _videoPlayer.Play();
            }
        }

        // Altyazı
        if (!string.IsNullOrEmpty(slide.subtitle))
        {
            _subtitleText.text = slide.subtitle;
            _subtitleBg.gameObject.SetActive(true);
        }
        else
        {
            _subtitleBg.gameObject.SetActive(false);
        }
    }

    private void NextSlide()
    {
        int nextIndex = _currentSlideIndex + 1;

        if (nextIndex < _currentChapter.slides.Count)
        {
            StartCoroutine(TransitionToSlide(nextIndex));
        }
        else
        {
            StartCoroutine(EndCutsceneCoroutine());
        }
    }

    // ═══════════════════════════════════════════
    //  Geçiş Animasyonları
    // ═══════════════════════════════════════════

    private IEnumerator TransitionToSlide(int index)
    {
        _isTransitioning = true;

        // 1) Fade out → siyah ekran
        yield return StartCoroutine(FadeContent(1f, 0f));

        // 2) Siyah ekranda bekle
        yield return new WaitForSeconds(blackScreenDuration);

        // 3) Yeni slaytı yükle ve fade in
        ShowSlide(index);
        yield return StartCoroutine(FadeContent(0f, 1f));

        _isTransitioning = false;
    }

    private IEnumerator EndCutsceneCoroutine()
    {
        _isTransitioning = true;
        _isFinished = true;

        // Müzik ve içerik fade out
        StartCoroutine(FadeOutMusic(fadeDuration + blackScreenDuration));
        yield return StartCoroutine(FadeContent(1f, 0f));
        yield return new WaitForSeconds(blackScreenDuration);

        if (_videoPlayer != null) _videoPlayer.Stop();

        // Final bölümüyse özel mesaj göster
        if (_currentChapter.isFinal)
        {
            ShowChapterTitle("FIN");
            yield return new WaitForSeconds(2f);
            HideChapterTitle();
            yield return new WaitForSeconds(0.5f);
        }

        FinishCutscene(_currentChapter);
    }

    private IEnumerator FadeContent(float from, float to)
    {
        if (_contentGroup == null) yield break;

        float elapsed = 0f;
        _contentGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
            _contentGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        _contentGroup.alpha = to;
    }

    // ═══════════════════════════════════════════
    //  Bitiş
    // ═══════════════════════════════════════════

    private void FinishCutscene(Chapter chapter)
    {
        _isFinished = true;

        // Sahne değiştirme — sadece cutscene panelini kapat, aynı sahnede kal
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  Debug — İlerlemeyi Sıfırla (Inspector'dan çağrılabilir)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Bölüm ilerlemesini sıfırlar. Test için kullanışlıdır.
    /// </summary>
    [ContextMenu("İlerlemeyi Sıfırla")]
    public void ResetProgress()
    {
        PlayerPrefs.SetInt(saveKey, 0);
        PlayerPrefs.Save();
        Debug.Log($"[CutscenePlayer] İlerleme sıfırlandı (Anahtar: {saveKey})");
    }
}
