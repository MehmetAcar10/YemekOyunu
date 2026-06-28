using UnityEngine;

public static class InteractZoneUtility
{
  public const string InteractZoneObjectName = "InteractZone";

  public static InteractionTriggerZone ConfigureChildZone(
    Transform parent,
    string zoneName,
    Vector3 fallbackLocalCenter,
    Vector3 fallbackLocalSize,
    float minWorldSize = 2f,
    float padding = 0.4f)
  {
    if (parent == null)
      return null;

    Transform zoneTransform = parent.Find(zoneName);
    GameObject zoneObject = zoneTransform != null
      ? zoneTransform.gameObject
      : new GameObject(zoneName);

    if (zoneTransform == null)
      zoneObject.transform.SetParent(parent, false);

    BoxCollider boxCollider = zoneObject.GetComponent<BoxCollider>();
    if (boxCollider == null)
      boxCollider = zoneObject.AddComponent<BoxCollider>();

    InteractionTriggerZone zone = zoneObject.GetComponent<InteractionTriggerZone>();
    if (zone == null)
      zone = zoneObject.AddComponent<InteractionTriggerZone>();

    if (TryFitZoneToRenderers(parent, zoneObject.transform, boxCollider, minWorldSize, padding))
    {
      zone.ConfigureCollider(boxCollider);
      return zone;
    }

    zoneObject.transform.localPosition = fallbackLocalCenter;
    zoneObject.transform.localRotation = Quaternion.identity;
    zoneObject.transform.localScale = Vector3.one;
    boxCollider.isTrigger = true;
    boxCollider.center = Vector3.zero;
    boxCollider.size = fallbackLocalSize;
    zone.ConfigureCollider(boxCollider);
    return zone;
  }

  private static bool TryFitZoneToRenderers(
    Transform parent,
    Transform zoneTransform,
    BoxCollider boxCollider,
    float minWorldSize,
    float padding)
  {
    if (!TryGetRendererBounds(parent, zoneTransform, out Bounds bounds))
      return false;

    bounds.Expand(padding);
    Vector3 minExtents = Vector3.one * (minWorldSize * 0.5f);
    bounds.extents = Vector3.Max(bounds.extents, minExtents);

    zoneTransform.SetParent(parent, true);
    zoneTransform.position = bounds.center;
    zoneTransform.rotation = Quaternion.identity;
    zoneTransform.localScale = Vector3.one;

    Vector3 lossyScale = zoneTransform.lossyScale;
    boxCollider.isTrigger = true;
    boxCollider.center = Vector3.zero;
    boxCollider.size = new Vector3(
      bounds.size.x / Mathf.Max(0.0001f, lossyScale.x),
      bounds.size.y / Mathf.Max(0.0001f, lossyScale.y),
      bounds.size.z / Mathf.Max(0.0001f, lossyScale.z));

    return true;
  }

  private static bool TryGetRendererBounds(Transform parent, Transform zoneTransform, out Bounds bounds)
  {
    bounds = default;
    bool hasBounds = false;

    Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
    for (int i = 0; i < renderers.Length; i++)
    {
      Renderer renderer = renderers[i];
      if (renderer == null || !renderer.enabled)
        continue;

      if (zoneTransform != null && renderer.transform.IsChildOf(zoneTransform))
        continue;

      if (!hasBounds)
      {
        bounds = renderer.bounds;
        hasBounds = true;
      }
      else
      {
        bounds.Encapsulate(renderer.bounds);
      }
    }

    return hasBounds;
  }
}
