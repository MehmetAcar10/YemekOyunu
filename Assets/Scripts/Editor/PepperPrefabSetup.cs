#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Summerjam.MiniGame;

namespace Summerjam.Editor
{
    public static class PepperPrefabSetup
    {
        private const string ModelPath = "Assets/Prefabs/Pepper.fbx";
        private const string PrefabPath = "Assets/Prefabs/PepperPrefab.prefab";
        private const string MaterialPath = "Assets/Materials/PepperMaterial.mat";
        private const float TargetWorldSize = 0.85f;

        [MenuItem("Tools/Summerjam/Setup Pepper Prefab from Model")]
        public static void SetupPepperPrefabFromModel()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"[Summerjam] Biber modeli bulunamadı: {ModelPath}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "PepperPrefab";
            // Model ince kenarı kameraya bakıyordu; Y ekseninde çevirerek geniş yüzü göster.
            instance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            instance.transform.localScale = CalculateScale(instance, TargetWorldSize);

            foreach (Collider existing in instance.GetComponents<Collider>())
                Object.DestroyImmediate(existing);

            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
            Bounds meshBounds = meshFilter != null && meshFilter.sharedMesh != null
                ? meshFilter.sharedMesh.bounds
                : new Bounds(Vector3.zero, Vector3.one * 0.5f);

            SphereCollider trigger = instance.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = meshBounds.center;
            trigger.radius = Mathf.Max(meshBounds.extents.x, meshBounds.extents.y, meshBounds.extents.z);

            FallingItem fallingItem = instance.GetComponent<FallingItem>();
            if (fallingItem == null)
                fallingItem = instance.AddComponent<FallingItem>();
            fallingItem.type = ItemType.Pepper;
            fallingItem.fallSpeed = 3f;
            fallingItem.rotationSpeed = 100f;
            fallingItem.destroyY = -6f;

            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb == null)
                rb = instance.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Renderer renderer = instance.GetComponentInChildren<Renderer>();
            Material pepperMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (renderer != null && pepperMaterial != null)
                renderer.sharedMaterial = pepperMaterial;

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Summerjam] Pepper prefab modelden oluşturuldu: {PrefabPath}");
        }

        public static Vector3 CalculateScale(GameObject instance, float targetWorldSize)
        {
            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return Vector3.one;

            Vector3 size = meshFilter.sharedMesh.bounds.size;
            // Kamera XY düzlemine bakıyor; model Y ekseninde 90° döndürülünce
            // ekranda görünen genişlik/yükseklik mesh'in Z ve Y boyutlarıdır.
            float largestVisible = Mathf.Max(size.y, size.z);
            if (largestVisible <= 0.0001f)
                return Vector3.one;

            float uniformScale = targetWorldSize / largestVisible;
            return Vector3.one * uniformScale;
        }
    }
}
#endif
