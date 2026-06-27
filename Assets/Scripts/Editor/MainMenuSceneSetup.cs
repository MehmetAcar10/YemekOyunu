using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Summerjam.Editor
{
    /// <summary>
    /// MainMenu sahnesini otomatik olarak kuran Editor scripti.
    /// Unity menüsünden: Tools > Summerjam > Setup Main Menu Scene
    /// </summary>
    public class MainMenuSceneSetup
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Summerjam/Setup Main Menu Scene")]
        public static void SetupMainMenuScene()
        {
            // Yeni sahne oluştur
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // ========== KAMERA AYARLARI ==========
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.05f, 0.02f, 0.1f, 1f); // Koyu mor
                mainCam.transform.position = new Vector3(0, 1, -10);
                mainCam.transform.rotation = Quaternion.identity;
                mainCam.gameObject.name = "Main Camera";
            }

            // ========== AYDINLATMA ==========
            // Varsayılan Directional Light'ı ayarla
            Light[] lights = Object.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(0.6f, 0.5f, 0.8f, 1f); // Mor tonlu ışık
                    light.intensity = 0.3f;
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                    light.gameObject.name = "Ambient Light";
                }
            }

            // Sürrealist accent ışık
            GameObject accentLightObj = new GameObject("Accent Light");
            Light accentLight = accentLightObj.AddComponent<Light>();
            accentLight.type = LightType.Point;
            accentLight.color = new Color(1f, 0.7f, 0.3f, 1f); // Amber
            accentLight.intensity = 2f;
            accentLight.range = 20f;
            accentLightObj.transform.position = new Vector3(3, 5, -5);

            // ========== PARçACIK EFEKTİ ==========
            GameObject particleObj = new GameObject("Surreal Particles");
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.4f, 0.6f),  // Altın
                new Color(0.7f, 0.5f, 1f, 0.4f)     // Mor
            );
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f; // Hafif yukarı süzülme

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20, 10, 5);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0.8f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Parçacık renderer ayarları
            ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            psRenderer.material.SetColor("_Color", new Color(1f, 0.9f, 0.6f, 0.5f));

            particleObj.transform.position = new Vector3(0, 2, 0);

            // ========== CANVAS ==========
            GameObject canvasObj = new GameObject("MenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // ========== MAIN PANEL ==========
            GameObject mainPanelObj = CreatePanel(canvasObj.transform, "MainPanel");
            CanvasGroup mainPanelGroup = mainPanelObj.AddComponent<CanvasGroup>();

            // Arka plan (koyu gradient overlay)
            GameObject bgOverlay = CreateUIElement(mainPanelObj.transform, "BackgroundOverlay");
            RectTransform bgRect = bgOverlay.GetComponent<RectTransform>();
            StretchFull(bgRect);
            Image bgImage = bgOverlay.AddComponent<Image>();
            bgImage.color = new Color(0.03f, 0.01f, 0.08f, 0.7f);

            // Başlık
            GameObject titleObj = CreateUIElement(mainPanelObj.transform, "TitleText");
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -100);
            titleRect.sizeDelta = new Vector2(800, 120);
            
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SUMMERJAM";
            titleTMP.fontSize = 72;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.85f, 0.4f, 1f); // Altın rengi

            // Okunabilirliği artırmak için gölge eklendi
            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            titleShadow.effectDistance = new Vector2(3f, -3f);

            // Alt başlık
            GameObject subtitleObj = CreateUIElement(mainPanelObj.transform, "SubtitleText");
            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0, -220);
            subtitleRect.sizeDelta = new Vector2(600, 40);
            
            TextMeshProUGUI subtitleTMP = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleTMP.text = "A  S U R R E A L  J O U R N E Y";
            subtitleTMP.fontSize = 18;
            subtitleTMP.alignment = TextAlignmentOptions.Center;
            subtitleTMP.color = new Color(0.8f, 0.7f, 0.9f, 0.7f);
            subtitleTMP.characterSpacing = 8f;

            Shadow subShadow = subtitleObj.AddComponent<Shadow>();
            subShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            subShadow.effectDistance = new Vector2(2f, -2f);

            // Buton konteyneri
            GameObject buttonContainer = CreateUIElement(mainPanelObj.transform, "ButtonContainer");
            RectTransform btnContRect = buttonContainer.GetComponent<RectTransform>();
            btnContRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnContRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnContRect.pivot = new Vector2(0.5f, 0.5f);
            btnContRect.anchoredPosition = new Vector2(0, -40);
            btnContRect.sizeDelta = new Vector2(400, 320);
            
            VerticalLayoutGroup vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Butonlar
            CreateMenuButton(buttonContainer.transform, "ContinueButton", "CONTINUE", 55);
            CreateMenuButton(buttonContainer.transform, "NewGameButton", "NEW GAME", 55);
            CreateMenuButton(buttonContainer.transform, "SettingsButton", "SETTINGS", 55);
            
            // Spacer
            GameObject spacer = CreateUIElement(buttonContainer.transform, "Spacer");
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.preferredHeight = 10;
            
            CreateMenuButton(buttonContainer.transform, "QuitButton", "QUIT", 55);

            // Versiyon text
            GameObject versionObj = CreateUIElement(mainPanelObj.transform, "VersionText");
            RectTransform versionRect = versionObj.GetComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(1f, 0f);
            versionRect.anchorMax = new Vector2(1f, 0f);
            versionRect.pivot = new Vector2(1f, 0f);
            versionRect.anchoredPosition = new Vector2(-30, 20);
            versionRect.sizeDelta = new Vector2(200, 30);
            
            TextMeshProUGUI versionTMP = versionObj.AddComponent<TextMeshProUGUI>();
            versionTMP.text = "v0.1.0";
            versionTMP.fontSize = 14;
            versionTMP.alignment = TextAlignmentOptions.BottomRight;
            versionTMP.color = new Color(1f, 1f, 1f, 0.3f);

            // ========== SETTINGS PANEL ==========
            GameObject settingsPanelObj = CreatePanel(canvasObj.transform, "SettingsPanel");
            CanvasGroup settingsPanelGroup = settingsPanelObj.AddComponent<CanvasGroup>();
            settingsPanelGroup.alpha = 0f;
            settingsPanelGroup.interactable = false;
            settingsPanelGroup.blocksRaycasts = false;

            // Settings arka plan
            GameObject settingsBg = CreateUIElement(settingsPanelObj.transform, "SettingsBackground");
            RectTransform settingsBgRect = settingsBg.GetComponent<RectTransform>();
            settingsBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsBgRect.sizeDelta = new Vector2(850, 750);
            Image settingsBgImg = settingsBg.AddComponent<Image>();
            settingsBgImg.color = new Color(0.08f, 0.05f, 0.15f, 0.95f);

            // Settings başlık
            GameObject settingsTitleObj = CreateUIElement(settingsBg.transform, "SettingsTitle");
            RectTransform stRect = settingsTitleObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.5f, 1f);
            stRect.anchorMax = new Vector2(0.5f, 1f);
            stRect.pivot = new Vector2(0.5f, 1f);
            stRect.anchoredPosition = new Vector2(0, -40);
            stRect.sizeDelta = new Vector2(400, 60);
            TextMeshProUGUI stTMP = settingsTitleObj.AddComponent<TextMeshProUGUI>();
            stTMP.text = "SETTINGS";
            stTMP.fontSize = 42;
            stTMP.fontStyle = FontStyles.Bold;
            stTMP.alignment = TextAlignmentOptions.Center;
            stTMP.color = new Color(1f, 0.85f, 0.4f, 1f);

            // Ses bölümü başlık
            CreateSettingsLabel(settingsBg.transform, "AudioHeader", "AUDIO", 
                new Vector2(0, -110), 32, true);

            // Master Volume slider
            CreateSettingsSlider(settingsBg.transform, "MasterVolumeSlider", "Master Volume", 
                new Vector2(0, -165));
            
            // Music Volume slider
            CreateSettingsSlider(settingsBg.transform, "MusicVolumeSlider", "Music", 
                new Vector2(0, -220));
            
            // SFX Volume slider
            CreateSettingsSlider(settingsBg.transform, "SFXVolumeSlider", "SFX", 
                new Vector2(0, -275));

            // Grafik bölümü başlık
            CreateSettingsLabel(settingsBg.transform, "GraphicsHeader", "GRAPHICS", 
                new Vector2(0, -350), 32, true);

            // Resolution dropdown
            CreateSettingsDropdown(settingsBg.transform, "ResolutionDropdown", "Resolution",
                new Vector2(0, -405));

            // Fullscreen toggle
            CreateSettingsToggle(settingsBg.transform, "FullscreenToggle", "Fullscreen",
                new Vector2(0, -460));

            // Apply butonu
            GameObject applyBtnObj = CreateUIElement(settingsBg.transform, "SettingsApplyButton");
            RectTransform applyRect = applyBtnObj.GetComponent<RectTransform>();
            applyRect.anchorMin = new Vector2(0.5f, 0f);
            applyRect.anchorMax = new Vector2(0.5f, 0f);
            applyRect.pivot = new Vector2(0.5f, 0f);
            applyRect.anchoredPosition = new Vector2(-130, 50);
            applyRect.sizeDelta = new Vector2(220, 60);
            
            Image applyImg = applyBtnObj.AddComponent<Image>();
            applyImg.color = new Color(0.25f, 0.15f, 0.4f, 0.9f);
            Button applyBtn = applyBtnObj.AddComponent<Button>();
            
            GameObject applyTextObj = CreateUIElement(applyBtnObj.transform, "Text");
            TextMeshProUGUI applyTMP = applyTextObj.AddComponent<TextMeshProUGUI>();
            applyTMP.text = "APPLY";
            applyTMP.fontSize = 24;
            applyTMP.fontStyle = FontStyles.Bold;
            applyTMP.alignment = TextAlignmentOptions.Center;
            applyTMP.color = Color.white;
            StretchFull(applyTextObj.GetComponent<RectTransform>());

            // Back butonu
            GameObject backBtnObj = CreateUIElement(settingsBg.transform, "SettingsBackButton");
            RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(130, 50);
            backRect.sizeDelta = new Vector2(220, 60);
            
            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.15f, 0.1f, 0.25f, 0.8f);
            Button backBtn = backBtnObj.AddComponent<Button>();
            
            GameObject backTextObj = CreateUIElement(backBtnObj.transform, "Text");
            TextMeshProUGUI backTMP = backTextObj.AddComponent<TextMeshProUGUI>();
            backTMP.text = "BACK";
            backTMP.fontSize = 24;
            backTMP.fontStyle = FontStyles.Bold;
            backTMP.alignment = TextAlignmentOptions.Center;
            backTMP.color = Color.white;
            RectTransform backTxtRect = backTextObj.GetComponent<RectTransform>();
            StretchFull(backTxtRect);

            // ========== CONFIRM DIALOG ==========
            GameObject confirmObj = CreatePanel(canvasObj.transform, "ConfirmDialog");
            CanvasGroup confirmGroup = confirmObj.AddComponent<CanvasGroup>();
            confirmGroup.alpha = 0f;
            confirmGroup.interactable = false;
            confirmGroup.blocksRaycasts = false;

            // Confirm arka plan overlay
            GameObject confirmOverlay = CreateUIElement(confirmObj.transform, "Overlay");
            StretchFull(confirmOverlay.GetComponent<RectTransform>());
            Image confirmOverlayImg = confirmOverlay.AddComponent<Image>();
            confirmOverlayImg.color = new Color(0, 0, 0, 0.7f);

            // Confirm kutu
            GameObject confirmBox = CreateUIElement(confirmObj.transform, "ConfirmBox");
            RectTransform confirmBoxRect = confirmBox.GetComponent<RectTransform>();
            confirmBoxRect.sizeDelta = new Vector2(500, 250);
            Image confirmBoxImg = confirmBox.AddComponent<Image>();
            confirmBoxImg.color = new Color(0.1f, 0.06f, 0.18f, 0.95f);

            // Confirm mesaj
            GameObject confirmMsgObj = CreateUIElement(confirmBox.transform, "ConfirmMessage");
            RectTransform cmRect = confirmMsgObj.GetComponent<RectTransform>();
            cmRect.anchorMin = new Vector2(0.1f, 0.4f);
            cmRect.anchorMax = new Vector2(0.9f, 0.9f);
            cmRect.offsetMin = Vector2.zero;
            cmRect.offsetMax = Vector2.zero;
            TextMeshProUGUI cmTMP = confirmMsgObj.AddComponent<TextMeshProUGUI>();
            cmTMP.text = "Mevcut kayıt silinecek.\nDevam etmek istiyor musun?";
            cmTMP.fontSize = 22;
            cmTMP.alignment = TextAlignmentOptions.Center;
            cmTMP.color = new Color(0.9f, 0.85f, 0.75f, 1f);

            // Yes butonu
            GameObject yesBtnObj = CreateUIElement(confirmBox.transform, "ConfirmYesButton");
            RectTransform yesRect = yesBtnObj.GetComponent<RectTransform>();
            yesRect.anchorMin = new Vector2(0.1f, 0.1f);
            yesRect.anchorMax = new Vector2(0.45f, 0.35f);
            yesRect.offsetMin = Vector2.zero;
            yesRect.offsetMax = Vector2.zero;
            Image yesImg = yesBtnObj.AddComponent<Image>();
            yesImg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            yesBtnObj.AddComponent<Button>();
            
            GameObject yesTextObj = CreateUIElement(yesBtnObj.transform, "Text");
            StretchFull(yesTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = "YES";
            yesTMP.fontSize = 22;
            yesTMP.alignment = TextAlignmentOptions.Center;
            yesTMP.color = Color.white;

            // No butonu
            GameObject noBtnObj = CreateUIElement(confirmBox.transform, "ConfirmNoButton");
            RectTransform noRect = noBtnObj.GetComponent<RectTransform>();
            noRect.anchorMin = new Vector2(0.55f, 0.1f);
            noRect.anchorMax = new Vector2(0.9f, 0.35f);
            noRect.offsetMin = Vector2.zero;
            noRect.offsetMax = Vector2.zero;
            Image noImg = noBtnObj.AddComponent<Image>();
            noImg.color = new Color(0.15f, 0.1f, 0.25f, 0.8f);
            noBtnObj.AddComponent<Button>();
            
            GameObject noTextObj = CreateUIElement(noBtnObj.transform, "Text");
            StretchFull(noTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = "NO";
            noTMP.fontSize = 22;
            noTMP.alignment = TextAlignmentOptions.Center;
            noTMP.color = Color.white;

            // ========== FADE OVERLAY ==========
            GameObject fadeObj = CreateUIElement(canvasObj.transform, "FadeOverlay");
            RectTransform fadeRect = fadeObj.GetComponent<RectTransform>();
            StretchFull(fadeRect);
            Image fadeImg = fadeObj.AddComponent<Image>();
            fadeImg.color = new Color(0, 0, 0, 0);
            fadeImg.raycastTarget = false;

            // ========== EVENT SYSTEM ==========
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ========== MANAGER GAME OBJECTS ==========
            // SceneLoader
            GameObject sceneLoaderObj = new GameObject("SceneLoader");
            // Script bileşenleri elle atanacak (compile sonrası)

            // MainMenuManager
            GameObject menuManagerObj = new GameObject("MainMenuManager");
            // Script bileşenleri elle atanacak (compile sonrası)

            // SettingsManager
            GameObject settingsManagerObj = new GameObject("SettingsManager");
            // Script bileşenleri elle atanacak (compile sonrası)

            // ========== SAHNEYI KAYDET ==========
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            
            // Build Settings'e ekle
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            
            // MainMenu zaten var mı kontrol et
            bool mainMenuExists = false;
            foreach (var s in buildScenes)
            {
                if (s.path == scenePath)
                {
                    mainMenuExists = true;
                    break;
                }
            }

            if (!mainMenuExists)
            {
                // MainMenu'yü index 0'a ekle
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log("✅ [Summerjam] MainMenu sahnesi başarıyla oluşturuldu ve Build Settings'e eklendi!");
            Debug.Log("ℹ️ [Summerjam] Scriptler compile edildikten sonra, Manager objelerine script bileşenlerini atayın.");
            
            EditorUtility.DisplayDialog("Summerjam - Main Menu Setup",
                "Main Menu sahnesi başarıyla oluşturuldu!\n\n" +
                "Sonraki adımlar:\n" +
                "1. Scriptlerin compile edilmesini bekleyin\n" +
                "2. MainMenuManager objesine MainMenuManager.cs ekleyin\n" +
                "3. SceneLoader objesine SceneLoader.cs ekleyin\n" +
                "4. SettingsManager objesine SettingsManager.cs ekleyin\n" +
                "5. Inspector'dan referansları bağlayın",
                "Tamam");
        }

        // ========== YARDIMCI METODLAR ==========

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            StretchFull(rect);
            return obj;
        }

        private static GameObject CreateUIElement(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateMenuButton(Transform parent, string name, string text, float height)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(350, height);

            // Buton arka planı - tamamen transparan (kahverengi dikdörtgen kalktı)
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0f, 0f, 0f, 0f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0f, 0f, 0f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f); // Sadece çok hafif bir beyazlık
            colors.pressedColor = new Color(1f, 1f, 1f, 0.1f);
            colors.selectedColor = new Color(0f, 0f, 0f, 0f);
            colors.fadeDuration = 0.15f;
            btn.colors = colors;

            // Layout element
            LayoutElement layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;

            // Buton metni
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            StretchFull(textRect);
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.82f, 0.65f, 1f); // Altın tonu
            
            // Okunabilirliği artırmak için net gölge eklendi
            Shadow textShadow = textObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textShadow.effectDistance = new Vector2(2f, -2f);
        }

        private static void CreateSettingsLabel(Transform parent, string name, string text, 
            Vector2 position, int fontSize, bool isBold)
        {
            GameObject obj = CreateUIElement(parent, name);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(650, 45);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = new Color(1f, 0.85f, 0.4f, 0.9f);

            Shadow labelShadow = obj.AddComponent<Shadow>();
            labelShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            labelShadow.effectDistance = new Vector2(2f, -2f);
        }

        private static void CreateSettingsSlider(Transform parent, string name, string label,
            Vector2 position)
        {
            // Container
            GameObject container = CreateUIElement(parent, name + "Container");
            RectTransform contRect = container.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 1f);
            contRect.anchorMax = new Vector2(0.5f, 1f);
            contRect.pivot = new Vector2(0.5f, 1f);
            contRect.anchoredPosition = position;
            contRect.sizeDelta = new Vector2(650, 45);

            // Label
            GameObject labelObj = CreateUIElement(container.transform, "Label");
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.35f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 24;
            labelTMP.fontStyle = FontStyles.Normal;
            labelTMP.alignment = TextAlignmentOptions.Left;
            labelTMP.color = Color.white;

            // Slider
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.4f, 0.2f);
            sliderRect.anchorMax = new Vector2(1f, 0.8f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 1f;
            slider.maxValue = 10f;
            slider.value = 7f;

            // Background
            GameObject bgObj = CreateUIElement(sliderObj.transform, "Background");
            RectTransform bgR = bgObj.GetComponent<RectTransform>();
            StretchFull(bgR);
            Image bgI = bgObj.AddComponent<Image>();
            bgI.color = new Color(0.1f, 0.06f, 0.18f, 0.8f);

            // Fill area
            GameObject fillArea = CreateUIElement(sliderObj.transform, "Fill Area");
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            StretchFull(fillAreaRect);

            GameObject fill = CreateUIElement(fillArea.transform, "Fill");
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            StretchFull(fillRect);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.75f, 0.3f, 0.7f);

            // Handle
            GameObject handleArea = CreateUIElement(sliderObj.transform, "Handle Slide Area");
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            StretchFull(handleAreaRect);

            GameObject handle = CreateUIElement(handleArea.transform, "Handle");
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(1f, 0.85f, 0.4f, 1f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
        }

        private static void CreateSettingsDropdown(Transform parent, string name, string label,
            Vector2 position)
        {
            GameObject container = CreateUIElement(parent, name + "Container");
            RectTransform contRect = container.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 1f);
            contRect.anchorMax = new Vector2(0.5f, 1f);
            contRect.pivot = new Vector2(0.5f, 1f);
            contRect.anchoredPosition = position;
            contRect.sizeDelta = new Vector2(650, 45);

            // Label
            GameObject labelObj = CreateUIElement(container.transform, "Label");
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.35f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 24;
            labelTMP.fontStyle = FontStyles.Normal;
            labelTMP.alignment = TextAlignmentOptions.Left;
            labelTMP.color = Color.white;

            // Dropdown (TMP_Dropdown oluşturmak yerine placeholder — Inspector'dan yapılacak)
            GameObject ddObj = CreateUIElement(container.transform, name);
            RectTransform ddRect = ddObj.GetComponent<RectTransform>();
            ddRect.anchorMin = new Vector2(0.4f, 0f);
            ddRect.anchorMax = new Vector2(1f, 1f);
            ddRect.offsetMin = Vector2.zero;
            ddRect.offsetMax = Vector2.zero;
            Image ddImg = ddObj.AddComponent<Image>();
            ddImg.color = new Color(0.12f, 0.08f, 0.2f, 0.8f);
            
            // TMP_Dropdown bileşeni
            TMP_Dropdown dropdown = ddObj.AddComponent<TMP_Dropdown>();
            
            // Caption
            GameObject captionObj = CreateUIElement(ddObj.transform, "Label");
            RectTransform capRect = captionObj.GetComponent<RectTransform>();
            StretchFull(capRect);
            capRect.offsetMin = new Vector2(10, 0);
            capRect.offsetMax = new Vector2(-25, 0);
            TextMeshProUGUI capTMP = captionObj.AddComponent<TextMeshProUGUI>();
            capTMP.text = "Select...";
            capTMP.fontSize = 22;
            capTMP.fontStyle = FontStyles.Normal;
            capTMP.color = Color.white;
            capTMP.alignment = TextAlignmentOptions.Left;
            dropdown.captionText = capTMP;

            // Template (minimal - runtime'da çalışacak)
            GameObject templateObj = CreateUIElement(ddObj.transform, "Template");
            RectTransform tempRect = templateObj.GetComponent<RectTransform>();
            tempRect.anchorMin = new Vector2(0, 0);
            tempRect.anchorMax = new Vector2(1, 0);
            tempRect.pivot = new Vector2(0.5f, 1f);
            tempRect.sizeDelta = new Vector2(0, 150);
            Image tempImg = templateObj.AddComponent<Image>();
            tempImg.color = new Color(0.1f, 0.06f, 0.18f, 0.95f);
            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();

            GameObject viewportObj = CreateUIElement(templateObj.transform, "Viewport");
            RectTransform vpRect = viewportObj.GetComponent<RectTransform>();
            StretchFull(vpRect);
            viewportObj.AddComponent<Image>().color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollRect.viewport = vpRect;

            GameObject contentObj = CreateUIElement(viewportObj.transform, "Content");
            RectTransform contRectT = contentObj.GetComponent<RectTransform>();
            contRectT.anchorMin = new Vector2(0, 1);
            contRectT.anchorMax = new Vector2(1, 1);
            contRectT.pivot = new Vector2(0.5f, 1);
            contRectT.sizeDelta = new Vector2(0, 28);
            scrollRect.content = contRectT;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject itemObj = CreateUIElement(contentObj.transform, "Item");
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 35);
            Toggle itemToggle = itemObj.AddComponent<Toggle>();

            // Item Background
            GameObject itemBgObj = CreateUIElement(itemObj.transform, "Item Background");
            RectTransform ibgRect = itemBgObj.GetComponent<RectTransform>();
            StretchFull(ibgRect);
            Image itemBgImg = itemBgObj.AddComponent<Image>();
            itemBgImg.color = new Color(0.15f, 0.1f, 0.25f, 1f);

            // Item Checkmark
            GameObject itemChkObj = CreateUIElement(itemObj.transform, "Item Checkmark");
            RectTransform ichkRect = itemChkObj.GetComponent<RectTransform>();
            ichkRect.anchorMin = new Vector2(0, 0.5f);
            ichkRect.anchorMax = new Vector2(0, 0.5f);
            ichkRect.sizeDelta = new Vector2(20, 20);
            ichkRect.anchoredPosition = new Vector2(15, 0);
            Image itemChkImg = itemChkObj.AddComponent<Image>();
            itemChkImg.color = new Color(1f, 0.85f, 0.4f, 1f);

            GameObject itemLabelObj = CreateUIElement(itemObj.transform, "Item Label");
            RectTransform ilRect = itemLabelObj.GetComponent<RectTransform>();
            StretchFull(ilRect);
            ilRect.offsetMin = new Vector2(35, 0);
            TextMeshProUGUI ilTMP = itemLabelObj.AddComponent<TextMeshProUGUI>();
            ilTMP.text = "Option";
            ilTMP.fontSize = 22;
            ilTMP.fontStyle = FontStyles.Normal;
            ilTMP.color = Color.white;
            ilTMP.alignment = TextAlignmentOptions.Left;

            itemToggle.targetGraphic = itemBgImg;
            itemToggle.graphic = itemChkImg;

            dropdown.template = tempRect;
            dropdown.itemText = ilTMP;
            templateObj.SetActive(false);
        }

        private static void CreateSettingsToggle(Transform parent, string name, string label,
            Vector2 position)
        {
            GameObject container = CreateUIElement(parent, name + "Container");
            RectTransform contRect = container.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 1f);
            contRect.anchorMax = new Vector2(0.5f, 1f);
            contRect.pivot = new Vector2(0.5f, 1f);
            contRect.anchoredPosition = position;
            contRect.sizeDelta = new Vector2(650, 45);

            // Label
            GameObject labelObj = CreateUIElement(container.transform, "Label");
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.35f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 24;
            labelTMP.fontStyle = FontStyles.Normal;
            labelTMP.alignment = TextAlignmentOptions.Left;
            labelTMP.color = Color.white;

            // Toggle
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(container.transform, false);
            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.4f, 0.1f);
            toggleRect.anchorMax = new Vector2(0.4f, 0.9f);
            toggleRect.sizeDelta = new Vector2(40, 0);
            toggleRect.anchoredPosition = new Vector2(0, 0);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;

            // Background
            GameObject toggleBg = CreateUIElement(toggleObj.transform, "Background");
            RectTransform tbgRect = toggleBg.GetComponent<RectTransform>();
            StretchFull(tbgRect);
            Image tbgImg = toggleBg.AddComponent<Image>();
            tbgImg.color = new Color(0.1f, 0.06f, 0.18f, 0.8f);

            // Checkmark
            GameObject checkmark = CreateUIElement(toggleBg.transform, "Checkmark");
            RectTransform cmkRect = checkmark.GetComponent<RectTransform>();
            StretchFull(cmkRect);
            cmkRect.offsetMin = new Vector2(4, 4);
            cmkRect.offsetMax = new Vector2(-4, -4);
            Image cmkImg = checkmark.AddComponent<Image>();
            cmkImg.color = new Color(1f, 0.85f, 0.4f, 1f);

            toggle.targetGraphic = tbgImg;
            toggle.graphic = cmkImg;
        }
#endif
    }
}
