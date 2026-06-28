using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Her sahneye dönüşte farklı bir bölümün cutscene'ini oynatır.
/// Cutscene sırasında oyuncu kontrolü kapatılır; bitince FPS imleci ve hareket geri yüklenir.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    [Serializable]
    public class Slide
    {
        public enum SlideType { Image, Video }

        public SlideType type = SlideType.Image;
        public Texture2D image;
        public VideoClip videoClip;

        [TextArea(2, 5)]
        public string subtitle = "";
    }

    [Serializable]
    public class Chapter
    {
        public string chapterName = "Bölüm";
        public List<Slide> slides = new List<Slide>();

        [Header("Müzik")]
        public AudioClip music;
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;
        public bool loopMusic = true;
        public bool isFinal = false;
    }

    [Header("══ Bölümler ══")]
    [SerializeField] private List<Chapter> chapters = new List<Chapter>();

    [Header("══ Geçiş Ayarları ══")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float blackScreenDuration = 0.5f;

    [Header("══ Altyazı Ayarları ══")]
    [SerializeField] private int subtitleFontSize = 32;
    [SerializeField] private Color subtitleColor = Color.white;

    [Header("══ Kayıt Ayarları ══")]
    [SerializeField] private string saveKey = "CutsceneChapter";
    [SerializeField] private bool resetProgressOnStart = false;

    [Header("══ Video ══")]
    [Tooltip("Video oynatılamazsa bu süre sonunda otomatik geçilir.")]
    [SerializeField] private float videoFallbackSeconds = 12f;

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
    private RenderTexture _renderTexture;

    private Chapter _currentChapter;
    private int _currentSlideIndex = -1;
    private bool _isTransitioning;
    private bool _isFinished;
    private bool _inputReady;
    private bool _waitingForVideoEnd;
    private Coroutine _videoSlideCoroutine;

    private readonly List<Behaviour> _disabledDuringCutscene = new List<Behaviour>();
#if ENABLE_INPUT_SYSTEM
    private readonly List<PlayerInput> _deactivatedPlayerInputs = new List<PlayerInput>();
#endif

    private void Awake()
    {
        CreateUI();
        HookVideoEvents();
    }

    private void Start()
    {
        SetGameplayInputEnabled(false);

        if (resetProgressOnStart)
            PlayerPrefs.SetInt(saveKey, 0);

        int chapterIndex = PlayerPrefs.GetInt(saveKey, 0);
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

        int nextChapter = chapterIndex + 1;
        if (_currentChapter.isFinal)
            nextChapter = 0;
        PlayerPrefs.SetInt(saveKey, nextChapter);
        PlayerPrefs.Save();

        StartMusic(_currentChapter);

        if (_currentChapter.slides.Count > 0)
            StartCoroutine(ShowFirstSlide());
        else
            FinishCutscene(_currentChapter);
    }

    private IEnumerator ShowFirstSlide()
    {
        _isTransitioning = true;
        _inputReady = false;
        _contentGroup.alpha = 0f;

        ShowChapterTitle(_currentChapter.chapterName);
        yield return new WaitForSeconds(1.5f);
        HideChapterTitle();
        yield return new WaitForSeconds(0.3f);

        ShowSlide(0);
        yield return StartCoroutine(FadeContent(0f, 1f));
        yield return new WaitForSeconds(0.3f);

        _isTransitioning = false;
        _inputReady = true;
    }

    private void Update()
    {
        if (_isFinished)
            return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (!_inputReady || _isTransitioning)
            return;

        if (IsSkipCutscenePressed())
        {
            StartCoroutine(SkipEntireCutscene());
            return;
        }

        if (_waitingForVideoEnd)
            return;

        if (IsAdvanceSlidePressed())
            NextSlide();
    }

    private static bool IsAdvanceSlidePressed()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    private static bool IsSkipCutscenePressed()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
            _videoPlayer.errorReceived -= OnVideoError;

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        if (_canvas != null)
            Destroy(_canvas.gameObject);

        if (!_isFinished)
            SetGameplayInputEnabled(true);
    }

    private void HookVideoEvents()
    {
        if (_videoPlayer == null)
            return;

        _videoPlayer.errorReceived += OnVideoError;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"[CutscenePlayer] Video oynatma hatası, slayt atlanıyor: {message}");
        StopVideoSlideRoutine();
        if (!_isFinished && !_isTransitioning && _inputReady)
            NextSlide();
    }

    private void StartMusic(Chapter chapter)
    {
        if (chapter.music == null)
            return;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.clip = chapter.music;
        _musicSource.volume = chapter.musicVolume;
        _musicSource.loop = chapter.loopMusic;
        _musicSource.playOnAwake = false;
        _musicSource.Play();
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        if (_musicSource == null || !_musicSource.isPlaying)
            yield break;

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

    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("CutsceneCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _background = bgObj.AddComponent<Image>();
        _background.color = Color.black;
        SetFullStretch(bgObj.GetComponent<RectTransform>());

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(canvasObj.transform, false);
        SetFullStretch(contentObj.AddComponent<RectTransform>());
        _contentGroup = contentObj.AddComponent<CanvasGroup>();
        _contentGroup.alpha = 0f;

        GameObject imgObj = new GameObject("ImageDisplay");
        imgObj.transform.SetParent(contentObj.transform, false);
        _imageDisplay = imgObj.AddComponent<RawImage>();
        _imageDisplay.color = Color.white;
        SetFullStretch(imgObj.GetComponent<RectTransform>());
        imgObj.SetActive(false);

        GameObject vidObj = new GameObject("VideoDisplay");
        vidObj.transform.SetParent(contentObj.transform, false);
        _videoDisplay = vidObj.AddComponent<RawImage>();
        _videoDisplay.color = Color.white;
        SetFullStretch(vidObj.GetComponent<RectTransform>());
        vidObj.SetActive(false);

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

        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = false;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        _renderTexture = new RenderTexture(1920, 1080, 0);
        _videoPlayer.targetTexture = _renderTexture;
        _videoDisplay.texture = _renderTexture;

        GameObject hintObj = new GameObject("ClickHint");
        hintObj.transform.SetParent(canvasObj.transform, false);
        _hintText = hintObj.AddComponent<Text>();
        _hintText.text = "Devam: tıkla / Space  |  Atla: Esc";
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

    private void ShowChapterTitle(string title)
    {
        if (_chapterTitle == null || string.IsNullOrEmpty(title))
            return;

        _chapterTitle.text = title;
        _chapterTitle.gameObject.SetActive(true);
    }

    private void HideChapterTitle()
    {
        if (_chapterTitle != null)
            _chapterTitle.gameObject.SetActive(false);
    }

    private void ShowSlide(int index)
    {
        StopVideoSlideRoutine();
        _currentSlideIndex = index;
        Slide slide = _currentChapter.slides[index];

        if (slide.type == Slide.SlideType.Image)
        {
            if (_videoPlayer != null)
                _videoPlayer.Stop();

            _videoDisplay.gameObject.SetActive(false);
            _imageDisplay.texture = slide.image;
            _imageDisplay.gameObject.SetActive(true);
            _waitingForVideoEnd = false;
        }
        else
        {
            _imageDisplay.gameObject.SetActive(false);
            _videoDisplay.gameObject.SetActive(true);

            if (slide.videoClip != null)
            {
                _waitingForVideoEnd = true;
                _videoSlideCoroutine = StartCoroutine(PlayVideoSlide(slide.videoClip));
            }
            else
            {
                _waitingForVideoEnd = false;
            }
        }

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

    private IEnumerator PlayVideoSlide(VideoClip clip)
    {
        _videoPlayer.Stop();
        _videoPlayer.clip = clip;
        _videoPlayer.Prepare();

        float prepareTimeout = 3f;
        float prepareElapsed = 0f;
        while (!_videoPlayer.isPrepared && prepareElapsed < prepareTimeout)
        {
            prepareElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_videoPlayer.isPrepared)
        {
            Debug.LogWarning($"[CutscenePlayer] Video hazırlanamadı, slayt atlanıyor: {clip.name}");
            _waitingForVideoEnd = false;
            if (!_isFinished && !_isTransitioning && _inputReady)
                NextSlide();
            yield break;
        }

        _videoPlayer.Play();

        float duration = clip.length > 0.1f ? (float)clip.length + 1f : videoFallbackSeconds;
        float timeout = Mathf.Clamp(duration, 2f, Mathf.Max(videoFallbackSeconds, duration));
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (!_videoPlayer.isPlaying && elapsed > 0.15f)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_waitingForVideoEnd && !_isFinished && !_isTransitioning && _inputReady)
        {
            _waitingForVideoEnd = false;
            NextSlide();
        }
    }

    private void StopVideoSlideRoutine()
    {
        _waitingForVideoEnd = false;

        if (_videoSlideCoroutine != null)
        {
            StopCoroutine(_videoSlideCoroutine);
            _videoSlideCoroutine = null;
        }

        if (_videoPlayer != null && _videoPlayer.isPlaying)
            _videoPlayer.Stop();
    }

    private void NextSlide()
    {
        int nextIndex = _currentSlideIndex + 1;

        if (nextIndex < _currentChapter.slides.Count)
            StartCoroutine(TransitionToSlide(nextIndex));
        else
            StartCoroutine(EndCutsceneCoroutine());
    }

    private IEnumerator TransitionToSlide(int index)
    {
        _isTransitioning = true;
        StopVideoSlideRoutine();

        yield return StartCoroutine(FadeContent(1f, 0f));
        yield return new WaitForSeconds(blackScreenDuration);

        ShowSlide(index);
        yield return StartCoroutine(FadeContent(0f, 1f));

        _isTransitioning = false;
    }

    private IEnumerator SkipEntireCutscene()
    {
        if (_isFinished || _isTransitioning)
            yield break;

        _isTransitioning = true;
        StopVideoSlideRoutine();
        yield return StartCoroutine(EndCutsceneCoroutine());
    }

    private IEnumerator EndCutsceneCoroutine()
    {
        _isTransitioning = true;
        _isFinished = true;
        StopVideoSlideRoutine();

        StartCoroutine(FadeOutMusic(fadeDuration + blackScreenDuration));
        yield return StartCoroutine(FadeContent(1f, 0f));
        yield return new WaitForSeconds(blackScreenDuration);

        if (_currentChapter != null && _currentChapter.isFinal)
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
        if (_contentGroup == null)
            yield break;

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

    private void FinishCutscene(Chapter chapter)
    {
        _isFinished = true;
        StopVideoSlideRoutine();

        if (_canvas != null)
            _canvas.gameObject.SetActive(false);

        SetGameplayInputEnabled(true);
        StartCoroutine(EnsureGameplayInputRestored());
        Debug.Log("[CutscenePlayer] Cutscene bitti, oyuncu kontrolü geri yüklendi.");
    }

    private IEnumerator EnsureGameplayInputRestored()
    {
        yield return null;
        SetGameplayInputEnabled(true);
    }

    private void SetGameplayInputEnabled(bool enabled)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (!enabled)
        {
            _disabledDuringCutscene.Clear();
#if ENABLE_INPUT_SYSTEM
            _deactivatedPlayerInputs.Clear();
#endif

            if (player == null)
                return;

            ClearPlayerInputState(player);
            DisableIfEnabled<FirstPersonController>(player);
            DisableIfEnabled<ThirdPersonController>(player);
#if ENABLE_INPUT_SYSTEM
            DeactivatePlayerInputs(player);
#endif
            return;
        }

        for (int i = 0; i < _disabledDuringCutscene.Count; i++)
        {
            if (_disabledDuringCutscene[i] != null)
                _disabledDuringCutscene[i].enabled = true;
        }

        _disabledDuringCutscene.Clear();

        if (player != null)
            ClearPlayerInputState(player);

#if ENABLE_INPUT_SYSTEM
        ActivatePlayerInputs(player);
#endif

        RestoreGameplayCursor();
        SceneCameraBootstrap.ConfigureSceneCameras();
    }

    private static void ClearPlayerInputState(GameObject player)
    {
        StarterAssetsInputs inputs = player.GetComponentInChildren<StarterAssetsInputs>(true);
        if (inputs == null)
            return;

        inputs.move = Vector2.zero;
        inputs.look = Vector2.zero;
        inputs.jump = false;
        inputs.sprint = false;
    }

#if ENABLE_INPUT_SYSTEM
    private void DeactivatePlayerInputs(GameObject player)
    {
        PlayerInput[] playerInputs = player.GetComponentsInChildren<PlayerInput>(true);
        for (int i = 0; i < playerInputs.Length; i++)
        {
            PlayerInput playerInput = playerInputs[i];
            if (!playerInput.inputIsActive)
                continue;

            playerInput.DeactivateInput();
            _deactivatedPlayerInputs.Add(playerInput);
        }
    }

    private void ActivatePlayerInputs(GameObject player)
    {
        if (player != null)
        {
            PlayerInput[] playerInputs = player.GetComponentsInChildren<PlayerInput>(true);
            for (int i = 0; i < playerInputs.Length; i++)
                ActivatePlayerInput(playerInputs[i]);
        }

        for (int i = 0; i < _deactivatedPlayerInputs.Count; i++)
        {
            if (_deactivatedPlayerInputs[i] != null)
                ActivatePlayerInput(_deactivatedPlayerInputs[i]);
        }

        _deactivatedPlayerInputs.Clear();
    }

    private static void ActivatePlayerInput(PlayerInput playerInput)
    {
        if (playerInput == null)
            return;

        playerInput.enabled = true;
        if (!playerInput.inputIsActive)
            playerInput.ActivateInput();
    }
#endif

    private void DisableIfEnabled<T>(GameObject root) where T : Behaviour
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (!components[i].enabled)
                continue;

            components[i].enabled = false;
            _disabledDuringCutscene.Add(components[i]);
        }
    }

    private static void RestoreGameplayCursor()
    {
        SceneCursorState cursorState = FindObjectOfType<SceneCursorState>();
        if (cursorState != null)
            cursorState.ApplyCursorState();
        else
            ApplyFallbackCursorLock();
    }

    private static void ApplyFallbackCursorLock()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        StarterAssetsInputs inputs = player.GetComponentInChildren<StarterAssetsInputs>(true);
        if (inputs != null)
        {
            inputs.cursorLocked = true;
            inputs.cursorInputForLook = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    [ContextMenu("İlerlemeyi Sıfırla")]
    public void ResetProgress()
    {
        PlayerPrefs.SetInt(saveKey, 0);
        PlayerPrefs.Save();
        Debug.Log($"[CutscenePlayer] İlerleme sıfırlandı (Anahtar: {saveKey})");
    }
}
