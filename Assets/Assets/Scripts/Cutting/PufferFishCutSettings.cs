using UnityEngine;

[CreateAssetMenu(fileName = "PufferFishCutSettings", menuName = "SummerJam/Puffer Fish Cut Settings")]
public class PufferFishCutSettings : ScriptableObject
{
  [Header("Gorsel")]
  public Sprite fishSprite;
  public string fishSpriteResourcesPath = "PufferFishCutFish";

  [Header("Kontur")]
  [Range(0.05f, 0.9f)]
  public float areaFraction = 0.3f;

  [Min(0.1f)]
  public float rectAspectRatio = 1f;

  [Range(0.5f, 1f)]
  public float successThreshold = 0.95f;

  [Min(8)]
  public int contourBins = 120;

  [Range(0.01f, 0.2f)]
  public float toleranceNormalized = 0.06f;

  [Range(0f, 0.35f)]
  public float spriteContentInset = 0.1f;

  [Range(0.5f, 0.98f)]
  public float maxRectInsideFish = 0.85f;

  [Header("UI")]
  public string title = "Balon Baligi Kes";
  public string instruction = "Sol tik basili tutup sari dikdortgen konturu takip edin.";
  public string successMessage = "Kesim basarili!";
  public string failMessage = "Kontur tamamlanmadi. Tekrar deneyin.";

  [Min(0f)]
  public float successCloseDelay = 0.2f;

  private static PufferFishCutSettings instance;

  public static PufferFishCutSettings Instance
  {
    get
    {
      if (instance != null)
        return instance;

      instance = Resources.Load<PufferFishCutSettings>("PufferFishCutSettings");
      if (instance != null)
        return instance;

      instance = CreateInstance<PufferFishCutSettings>();
      return instance;
    }
  }

  public Sprite ResolveFishSprite()
  {
    if (fishSprite != null)
      return fishSprite;

    if (string.IsNullOrWhiteSpace(fishSpriteResourcesPath))
      return null;

    return Resources.Load<Sprite>(fishSpriteResourcesPath);
  }
}
