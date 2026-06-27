using System;
using System.IO;
using UnityEngine;

namespace Summerjam.MainMenu
{
    /// <summary>
    /// JSON tabanlı kayıt/yükleme sistemi.
    /// Application.persistentDataPath altında save dosyası saklar.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "savegame.json";

        private static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        /// <summary>
        /// Kayıtlı oyun verisi var mı kontrol eder.
        /// </summary>
        public static bool HasSaveData()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Oyun verisini JSON olarak kaydeder.
        /// </summary>
        public static void Save(SaveData data)
        {
            try
            {
                data.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveSystem] Oyun kaydedildi: {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Kaydetme hatası: {e.Message}");
            }
        }

        /// <summary>
        /// Kaydedilmiş oyun verisini yükler.
        /// </summary>
        public static SaveData Load()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("[SaveSystem] Kayıt dosyası bulunamadı.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] Oyun yüklendi. Son kayıt: {data.lastSaveTime}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Yükleme hatası: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Kayıt dosyasını siler.
        /// </summary>
        public static void DeleteSave()
        {
            if (HasSaveData())
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveSystem] Kayıt dosyası silindi.");
            }
        }
    }

    /// <summary>
    /// Oyun kayıt verisi. İhtiyaca göre genişletilebilir.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string currentScene = "Scene 1";
        public string lastSaveTime;
        public float playerPosX;
        public float playerPosY;
        public float playerPosZ;
        public int currentChapter = 1;
        public float playTime; // saniye cinsinden toplam oynama süresi
    }
}
