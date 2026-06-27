#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Summerjam.MiniGame;

namespace Summerjam.Editor
{
    public static class Scene1Setup
    {
        [MenuItem("Tools/Summerjam/Setup Scene 1 (Mini-Game)")]
        public static void SetupMiniGameScene()
        {
            // Yeni bir sahne oluştur (Eğer isteniyorsa kaydedilebilir, biz mevcut sahneyi değiştiriyoruz)
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                Debug.LogWarning("Lütfen önce sahneyi kaydedin (File -> Save As).");
            }

            // Temizleme: Eski objeleri sil
            var rootObjects = activeScene.GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                if (obj.name != "Main Camera" && obj.name != "Directional Light")
                {
                    GameObject.DestroyImmediate(obj);
                }
            }

            // 1. Kamera Ayarı
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.6f, 0.85f, 0.95f); // Açık Mavi
            mainCam.orthographic = true;
            mainCam.orthographicSize = 7f;

            // 2. Işık
            Light dirLight = GameObject.FindObjectOfType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 3. Oyun Yöneticisi (Manager)
            GameObject managerObj = new GameObject("MiniGameManager");
            MiniGameManager manager = managerObj.AddComponent<MiniGameManager>();

            // 4. Oyuncu (Küp/Capsule)
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.transform.position = new Vector3(0, -5f, 0);
            player.transform.localScale = new Vector3(2f, 1f, 1f); // Geniş bir platform gibi
            
            // Player Material & Physics
            Renderer pRenderer = player.GetComponent<Renderer>();
            Material pMat = new Material(Shader.Find("Standard"));
            pMat.color = Color.blue;
            pRenderer.sharedMaterial = pMat;

            Rigidbody pRb = player.AddComponent<Rigidbody>();
            pRb.useGravity = false;
            pRb.isKinematic = true; // Sadece kodla hareket etsin

            // PlayerController
            PlayerController pController = player.AddComponent<PlayerController>();
            pController.moveSpeed = 12f;
            pController.limitX = 8.5f;

            // 5. Duvarlar (Sınırlar) görünmez olabilir
            CreateWall("LeftWall", new Vector3(-10f, 0, 0));
            CreateWall("RightWall", new Vector3(10f, 0, 0));

            // Zemin (Düşen objelerin kaçması durumunda silinmesi FallingItem scriptinden sağlanacak)

            // 6. Prefablar (Geçici)
            // Pepper Prefab (3D model veya fallback küre)
            GameObject pepperModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pepper.fbx");
            GameObject pepperPrefab;
            if (pepperModel != null)
            {
                pepperPrefab = (GameObject)PrefabUtility.InstantiatePrefab(pepperModel);
                pepperPrefab.name = "PepperPrefab";
                pepperPrefab.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                pepperPrefab.transform.localScale = PepperPrefabSetup.CalculateScale(pepperPrefab, 0.85f);

                foreach (Collider existing in pepperPrefab.GetComponents<Collider>())
                    GameObject.DestroyImmediate(existing);

                MeshFilter meshFilter = pepperPrefab.GetComponent<MeshFilter>();
                Bounds meshBounds = meshFilter != null && meshFilter.sharedMesh != null
                    ? meshFilter.sharedMesh.bounds
                    : new Bounds(Vector3.zero, Vector3.one * 0.5f);

                SphereCollider pepperCollider = pepperPrefab.AddComponent<SphereCollider>();
                pepperCollider.isTrigger = true;
                pepperCollider.center = meshBounds.center;
                pepperCollider.radius = Mathf.Max(meshBounds.extents.x, meshBounds.extents.y, meshBounds.extents.z);
            }
            else
            {
                pepperPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pepperPrefab.name = "PepperPrefab";
                pepperPrefab.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                pepperPrefab.GetComponent<Renderer>().sharedMaterial = CreateMaterial("PepperMaterial", Color.red);
                pepperPrefab.GetComponent<Collider>().isTrigger = true;
            }
            
            FallingItem pItem = pepperPrefab.AddComponent<FallingItem>();
            pItem.type = ItemType.Pepper;
            pItem.fallSpeed = 3f; // Yavaşlatıldı (Eskiden 6f)
            pepperPrefab.AddComponent<Rigidbody>().isKinematic = true;

            // Bomb Prefab (Siyah Küre)
            GameObject bombPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bombPrefab.name = "BombPrefab";
            bombPrefab.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            bombPrefab.GetComponent<Renderer>().sharedMaterial = CreateMaterial("BombMaterial", Color.black);
            bombPrefab.GetComponent<Collider>().isTrigger = true;
            FallingItem bItem = bombPrefab.AddComponent<FallingItem>();
            bItem.type = ItemType.Bomb;
            bItem.fallSpeed = 4f; // Yavaşlatıldı (Eskiden 8f)
            bombPrefab.AddComponent<Rigidbody>().isKinematic = true;

            // Prefabları kaydet
            if (!System.IO.Directory.Exists("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            
            GameObject savedPepper = PrefabUtility.SaveAsPrefabAsset(pepperPrefab, "Assets/Prefabs/PepperPrefab.prefab");
            GameObject savedBomb = PrefabUtility.SaveAsPrefabAsset(bombPrefab, "Assets/Prefabs/BombPrefab.prefab");
            
            // Sahnedeki prefab kalıntılarını sil
            GameObject.DestroyImmediate(pepperPrefab);
            GameObject.DestroyImmediate(bombPrefab);

            // 7. Spawner
            GameObject spawnerObj = new GameObject("ItemSpawner");
            spawnerObj.transform.position = new Vector3(0, 8f, 0);
            ItemSpawner spawner = spawnerObj.AddComponent<ItemSpawner>();
            spawner.pepperPrefab = savedPepper;
            spawner.bombPrefab = savedBomb;
            spawner.spawnInterval = 2f; // Obje düşme sıklığı yavaşlatıldı (Eskiden 0.8 idi)

            // 8. UI Kurulumu
            SetupUI(manager);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Summerjam] Scene 1 (Mini-Game) başarıyla kuruldu!");
        }

        private static void CreateWall(string name, Vector3 pos)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = new Vector3(1f, 20f, 1f);
            wall.GetComponent<Renderer>().enabled = false; // Görünmez duvar
        }

        private static Material CreateMaterial(string matName, Color color)
        {
            if (!System.IO.Directory.Exists("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            string path = $"Assets/Materials/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void SetupUI(MiniGameManager manager)
        {
            // EventSystem kontrolü
            if (GameObject.FindObjectOfType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            // Canvas
            GameObject canvasObj = new GameObject("MiniGameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // (Skor UI kaldırıldı)

            // Health Text
            GameObject healthObj = new GameObject("HealthText");
            healthObj.transform.SetParent(canvasObj.transform, false);
            RectTransform healthRect = healthObj.AddComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(1, 1);
            healthRect.anchorMax = new Vector2(1, 1);
            healthRect.pivot = new Vector2(1, 1);
            healthRect.anchoredPosition = new Vector2(-50, -50);
            healthRect.sizeDelta = new Vector2(300, 50);
            TextMeshProUGUI healthTmp = healthObj.AddComponent<TextMeshProUGUI>();
            healthTmp.text = "HEALTH: 3";
            healthTmp.fontSize = 40;
            healthTmp.fontStyle = FontStyles.Bold;
            healthTmp.color = new Color(1f, 0.3f, 0.3f);
            healthTmp.alignment = TextAlignmentOptions.TopRight;
            manager.healthText = healthTmp;

            // Game Over Panel
            GameObject goPanelObj = new GameObject("GameOverPanel");
            goPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform goRect = goPanelObj.AddComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;
            Image goImg = goPanelObj.AddComponent<Image>();
            goImg.color = new Color(0, 0, 0, 0.85f);
            manager.gameOverPanel = goPanelObj;

            // Game Over Title
            GameObject goTitleObj = new GameObject("Title");
            goTitleObj.transform.SetParent(goPanelObj.transform, false);
            RectTransform titleRect = goTitleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);
            titleRect.sizeDelta = new Vector2(600, 100);
            TextMeshProUGUI titleTmp = goTitleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "GAME OVER";
            titleTmp.fontSize = 80;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.2f, 0.2f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            manager.titleText = titleTmp;

            // Final Score
            GameObject fScoreObj = new GameObject("FinalScore");
            fScoreObj.transform.SetParent(goPanelObj.transform, false);
            RectTransform fScoreRect = fScoreObj.AddComponent<RectTransform>();
            fScoreRect.anchoredPosition = new Vector2(0, 0);
            fScoreRect.sizeDelta = new Vector2(600, 100);
            TextMeshProUGUI fScoreTmp = fScoreObj.AddComponent<TextMeshProUGUI>();
            fScoreTmp.text = "FINAL SCORE\n0";
            fScoreTmp.fontSize = 50;
            fScoreTmp.color = Color.white;
            fScoreTmp.alignment = TextAlignmentOptions.Center;
            manager.finalScoreText = fScoreTmp;

            // Retry Button
            GameObject btnObj = new GameObject("RetryButton");
            btnObj.transform.SetParent(goPanelObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -150);
            btnRect.sizeDelta = new Vector2(300, 80);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.2f);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(manager.RetryGame);

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRect = btnTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;
            TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "RETRY";
            btnTmp.fontSize = 40;
            btnTmp.color = Color.white;
            btnTmp.alignment = TextAlignmentOptions.Center;

            goPanelObj.SetActive(false);
        }
    }
}
#endif
