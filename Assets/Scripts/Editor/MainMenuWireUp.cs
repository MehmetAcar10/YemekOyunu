using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Summerjam.Editor
{
    /// <summary>
    /// MainMenu sahnesindeki Manager objelerine script bileşenlerini ve
    /// referansları otomatik olarak bağlayan Editor scripti.
    /// Unity menüsünden: Tools > Summerjam > Wire Main Menu References
    /// </summary>
    public class MainMenuWireUp
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Summerjam/Wire Main Menu References")]
        public static void WireReferences()
        {
            // Doğru sahnede miyiz kontrol et
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "MainMenu")
            {
                EditorUtility.DisplayDialog("Hata", 
                    "Lütfen önce MainMenu sahnesini açın!\n(Assets/Scenes/MainMenu.unity)", "Tamam");
                return;
            }

            // ========== SCENE LOADER ==========
            GameObject sceneLoaderObj = GameObject.Find("SceneLoader");
            if (sceneLoaderObj != null)
            {
                var sceneLoader = sceneLoaderObj.GetComponent<Utils.SceneLoader>();
                if (sceneLoader == null)
                    sceneLoader = sceneLoaderObj.AddComponent<Utils.SceneLoader>();

                // FadeOverlay referansı
                Transform fadeOverlay = FindDeep("FadeOverlay");
                if (fadeOverlay != null)
                {
                    Image fadeImage = fadeOverlay.GetComponent<Image>();
                    SerializedObject so = new SerializedObject(sceneLoader);
                    so.FindProperty("fadeOverlay").objectReferenceValue = fadeImage;
                    so.ApplyModifiedProperties();
                }
                Debug.Log("✅ SceneLoader bağlandı.");
            }

            // ========== SETTINGS MANAGER ==========
            GameObject settingsManagerObj = GameObject.Find("SettingsManager");
            if (settingsManagerObj != null)
            {
                var settingsManager = settingsManagerObj.GetComponent<MainMenu.SettingsManager>();
                if (settingsManager == null)
                    settingsManager = settingsManagerObj.AddComponent<MainMenu.SettingsManager>();

                SerializedObject smSO = new SerializedObject(settingsManager);

                // Slider'ları bağla
                AssignSlider(smSO, "masterVolumeSlider", "MasterVolumeSlider");
                AssignSlider(smSO, "musicVolumeSlider", "MusicVolumeSlider");
                AssignSlider(smSO, "sfxVolumeSlider", "SFXVolumeSlider");

                // Dropdown'ları bağla
                AssignDropdown(smSO, "resolutionDropdown", "ResolutionDropdown");

                // Toggle bağla
                AssignToggle(smSO, "fullscreenToggle", "FullscreenToggle");

                smSO.ApplyModifiedProperties();
                Debug.Log("✅ SettingsManager bağlandı.");
            }

            // ========== MAIN MENU MANAGER ==========
            GameObject menuManagerObj = GameObject.Find("MainMenuManager");
            if (menuManagerObj != null)
            {
                var menuManager = menuManagerObj.GetComponent<MainMenu.MainMenuManager>();
                if (menuManager == null)
                    menuManager = menuManagerObj.AddComponent<MainMenu.MainMenuManager>();

                SerializedObject mmSO = new SerializedObject(menuManager);

                // Paneller
                AssignCanvasGroup(mmSO, "mainPanel", "MainPanel");
                AssignCanvasGroup(mmSO, "settingsPanel", "SettingsPanel");
                AssignCanvasGroup(mmSO, "confirmDialog", "ConfirmDialog");

                // Ana butonlar
                AssignButton(mmSO, "continueButton", "ContinueButton");
                AssignButton(mmSO, "newGameButton", "NewGameButton");
                AssignButton(mmSO, "settingsButton", "SettingsButton");
                AssignButton(mmSO, "quitButton", "QuitButton");

                // Onay butonları
                AssignButton(mmSO, "confirmYesButton", "ConfirmYesButton");
                AssignButton(mmSO, "confirmNoButton", "ConfirmNoButton");

                // Onay mesajı
                Transform confirmMsg = FindDeep("ConfirmMessage");
                if (confirmMsg != null)
                {
                    mmSO.FindProperty("confirmMessage").objectReferenceValue = 
                        confirmMsg.GetComponent<TMP_Text>();
                }

                // Settings buttons
                AssignButton(mmSO, "settingsApplyButton", "SettingsApplyButton");
                AssignButton(mmSO, "settingsBackButton", "SettingsBackButton");

                // Başlık
                Transform titleText = FindDeep("TitleText");
                if (titleText != null)
                {
                    mmSO.FindProperty("titleText").objectReferenceValue = 
                        titleText.GetComponent<TMP_Text>();
                }

                // Kamera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mmSO.FindProperty("mainCamera").objectReferenceValue = mainCam.transform;
                }

                mmSO.ApplyModifiedProperties();
                Debug.Log("✅ MainMenuManager bağlandı.");
            }

            // Sahneyi kaydet
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            EditorUtility.DisplayDialog("Summerjam - Wire Up Complete",
                "Tüm referanslar başarıyla bağlandı!\n\n" +
                "Artık Play tuşuna basarak menüyü test edebilirsiniz.", "Harika!");
        }

        // ========== YARDIMCI METODLAR ==========

        private static Transform FindDeep(string name)
        {
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name == name) return go.transform;
            }
            return null;
        }

        private static void AssignButton(SerializedObject so, string property, string objectName)
        {
            Transform t = FindDeep(objectName);
            if (t != null)
            {
                Button btn = t.GetComponent<Button>();
                if (btn != null)
                    so.FindProperty(property).objectReferenceValue = btn;
            }
        }

        private static void AssignCanvasGroup(SerializedObject so, string property, string objectName)
        {
            Transform t = FindDeep(objectName);
            if (t != null)
            {
                CanvasGroup cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                    so.FindProperty(property).objectReferenceValue = cg;
            }
        }

        private static void AssignSlider(SerializedObject so, string property, string objectName)
        {
            Transform t = FindDeep(objectName);
            if (t != null)
            {
                Slider slider = t.GetComponent<Slider>();
                if (slider != null)
                    so.FindProperty(property).objectReferenceValue = slider;
            }
        }

        private static void AssignDropdown(SerializedObject so, string property, string objectName)
        {
            Transform t = FindDeep(objectName);
            if (t != null)
            {
                TMP_Dropdown dropdown = t.GetComponent<TMP_Dropdown>();
                if (dropdown != null)
                    so.FindProperty(property).objectReferenceValue = dropdown;
            }
        }

        private static void AssignToggle(SerializedObject so, string property, string objectName)
        {
            Transform t = FindDeep(objectName);
            if (t != null)
            {
                Toggle toggle = t.GetComponent<Toggle>();
                if (toggle != null)
                    so.FindProperty(property).objectReferenceValue = toggle;
            }
        }
#endif
    }
}
