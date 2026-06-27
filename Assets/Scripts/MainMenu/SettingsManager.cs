using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

namespace Summerjam.MainMenu
{
    /// <summary>
    /// Ayarlar paneli yöneticisi.
    /// Ses (AudioMixer), grafik kalitesi, çözünürlük ve tam ekran ayarlarını kontrol eder.
    /// Ayarlar PlayerPrefs ile kalıcı olarak saklanır.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("Ses Ayarları")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
        private const string KEY_RESOLUTION = "Settings_Resolution";
        private const string KEY_FULLSCREEN = "Settings_Fullscreen";

        // AudioMixer parametreleri
        private const string MIXER_MASTER = "MasterVolume";
        private const string MIXER_MUSIC = "MusicVolume";
        private const string MIXER_SFX = "SFXVolume";

        private Resolution[] _resolutions;

        private static Resolution CreateResolution(int width, int height, int refreshRateHz)
        {
            Resolution resolution = new Resolution { width = width, height = height };
            resolution.refreshRateRatio = new RefreshRate
            {
                numerator = (uint)refreshRateHz,
                denominator = 1
            };
            return resolution;
        }

        private static int GetRefreshRateHz(Resolution resolution)
        {
            return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
        }

        private void Start()
        {
            InitializeResolutions();
            LoadSettings();
            SetupListeners();
        }

        /// <summary>
        /// Mevcut çözünürlükleri dropdown'a yükler.
        /// </summary>
        private void InitializeResolutions()
        {
            if (resolutionDropdown == null) return;

            // Kullanıcının isteği üzerine çözünürlük menüsü sadece 1920x1080'e sabitlendi.
            _resolutions = new Resolution[]
            {
                CreateResolution(1920, 1080, 60),
                CreateResolution(1920, 1080, 144),
                CreateResolution(1920, 1080, 165)
            };
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = $"{_resolutions[i].width} x {_resolutions[i].height} @ {GetRefreshRateHz(_resolutions[i])}Hz";
                options.Add(option);

                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt(KEY_RESOLUTION, currentResIndex);
            resolutionDropdown.RefreshShownValue();
        }



        private void LoadSettings()
        {
            // Ses ayarları (1-10 arası varsayılan 7)
            float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 7f);
            float musicVol = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 7f);
            float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 7f);

            if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
            if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;

            SetMasterVolume(masterVol);
            SetMusicVolume(musicVol);
            SetSFXVolume(sfxVol);

            // Tam ekran
            bool fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
            if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }

        private void SetupListeners()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(SetResolution);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        public void SetMasterVolume(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(MIXER_MASTER, VolumeToDecibel(value));
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, value);
        }

        public void SetMusicVolume(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(MIXER_MUSIC, VolumeToDecibel(value));
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, value);
        }

        public void SetSFXVolume(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(MIXER_SFX, VolumeToDecibel(value));
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, value);
        }



        public void SetResolution(int resolutionIndex)
        {
            if (_resolutions != null && resolutionIndex < _resolutions.Length)
            {
                Resolution res = _resolutions[resolutionIndex];
                Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
                PlayerPrefs.SetInt(KEY_RESOLUTION, resolutionIndex);
            }
        }

        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullscreen ? 1 : 0);
        }

        /// <summary>
        /// Tüm ayarları kaydeder.
        /// </summary>
        public void SaveAllSettings()
        {
            PlayerPrefs.Save();
            Debug.Log("[SettingsManager] Ayarlar kaydedildi.");
        }

        /// <summary>
        /// Slider değerini (1-10) desibel değerine çevirir.
        /// </summary>
        private float VolumeToDecibel(float volume)
        {
            // volume 1 ile 10 arasında olacak. Bunu 0.0001 - 1 aralığına çevirelim.
            // Eğer volume 1 ise, -80dB (sessiz) yapalım.
            if (volume <= 1.0f) return -80f;
            
            float normalizedVolume = volume / 10f;
            return Mathf.Log10(normalizedVolume) * 20f;
        }
    }
}
