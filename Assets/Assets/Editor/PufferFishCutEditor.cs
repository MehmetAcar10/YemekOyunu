#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PufferFishCutEditor
{
  private const string SettingsPath = "Assets/Assets/Resources/PufferFishCutSettings.asset";
  private const string SpritePath = "Assets/Assets/Resources/PufferFishCutFish.png";
  private const string FbxPath = "Assets/Assets/balon balık1.fbx";

  [InitializeOnLoadMethod]
  private static void EnsureSettingsAsset()
  {
    EditorApplication.delayCall += () =>
    {
      PufferFishCutSettings settings = AssetDatabase.LoadAssetAtPath<PufferFishCutSettings>(SettingsPath);
      if (settings == null)
      {
        settings = ScriptableObject.CreateInstance<PufferFishCutSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
      }

      if (settings.fishSprite == null)
      {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite != null)
        {
          settings.fishSprite = sprite;
          EditorUtility.SetDirty(settings);
        }
      }

      AssetDatabase.SaveAssets();
    };
  }

  [MenuItem("Tools/SummerJam/Generate PufferFish Cut Sprite")]
  private static void GenerateFishSprite()
  {
    GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
    if (modelPrefab == null)
    {
      Debug.LogError($"FBX bulunamadi: {FbxPath}");
      return;
    }

    GameObject tempRoot = new GameObject("PufferFishCutSpriteBake");
    GameObject model = Object.Instantiate(modelPrefab, tempRoot.transform);
    model.transform.localPosition = Vector3.zero;
    model.transform.localRotation = Quaternion.identity;
    model.transform.localScale = Vector3.one;

    Bounds bounds = CalculateBounds(model);
    Vector3 center = bounds.center;

    GameObject cameraObject = new GameObject("BakeCamera");
    cameraObject.transform.SetParent(tempRoot.transform, false);
    Camera camera = cameraObject.AddComponent<Camera>();
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = new Color(0.12f, 0.16f, 0.2f, 0f);
    camera.orthographic = false;
    camera.fieldOfView = 36f;
    camera.nearClipPlane = 0.01f;
    camera.farClipPlane = 100f;

    Vector3 offset = new Vector3(-2.4f, 1.15f, -2.6f).normalized;
    float radius = bounds.extents.magnitude;
    float distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
    camera.transform.position = center + offset * distance;
    camera.transform.LookAt(center);

    GameObject lightObject = new GameObject("BakeLight");
    lightObject.transform.SetParent(tempRoot.transform, false);
    lightObject.transform.rotation = Quaternion.Euler(42f, 38f, 0f);
    Light light = lightObject.AddComponent<Light>();
    light.type = LightType.Directional;
    light.intensity = 1.15f;

    const int size = 512;
    RenderTexture renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
    Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

    camera.targetTexture = renderTexture;
    camera.Render();

    RenderTexture previous = RenderTexture.active;
    RenderTexture.active = renderTexture;
    texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
    texture.Apply();
    RenderTexture.active = previous;

    camera.targetTexture = null;
    Object.DestroyImmediate(tempRoot);
    renderTexture.Release();
    Object.DestroyImmediate(renderTexture);

    byte[] png = texture.EncodeToPNG();
    Object.DestroyImmediate(texture);
    File.WriteAllBytes(SpritePath, png);
    AssetDatabase.ImportAsset(SpritePath);

    TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
    if (importer != null)
    {
      importer.textureType = TextureImporterType.Sprite;
      importer.spriteImportMode = SpriteImportMode.Single;
      importer.alphaIsTransparency = true;
      importer.SaveAndReimport();
    }

    PufferFishCutSettings settings = AssetDatabase.LoadAssetAtPath<PufferFishCutSettings>(SettingsPath);
    if (settings == null)
    {
      settings = ScriptableObject.CreateInstance<PufferFishCutSettings>();
      AssetDatabase.CreateAsset(settings, SettingsPath);
    }

    settings.fishSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    EditorUtility.SetDirty(settings);
    AssetDatabase.SaveAssets();

    Debug.Log("PufferFish cut sprite olusturuldu: " + SpritePath);
  }

  private static Bounds CalculateBounds(GameObject root)
  {
    Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
    if (renderers.Length == 0)
      return new Bounds(root.transform.position, Vector3.one);

    Bounds bounds = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++)
      bounds.Encapsulate(renderers[i].bounds);

    return bounds;
  }
}
#endif
