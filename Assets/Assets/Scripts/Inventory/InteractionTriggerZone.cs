using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionTriggerZone : MonoBehaviour
{
  private static readonly List<InteractionTriggerZone> ActiveZones = new List<InteractionTriggerZone>();

  [SerializeField] private Collider zoneCollider;

  public Collider ZoneCollider => zoneCollider;

  public void ConfigureCollider(Collider collider)
  {
    zoneCollider = collider;
    if (zoneCollider != null)
      zoneCollider.isTrigger = true;
  }

  private void Awake()
  {
    if (zoneCollider == null)
      zoneCollider = GetComponent<Collider>();

    if (zoneCollider != null)
      zoneCollider.isTrigger = true;
  }

  private void OnEnable()
  {
    if (!ActiveZones.Contains(this))
      ActiveZones.Add(this);
  }

  private void OnDisable()
  {
    ActiveZones.Remove(this);
  }

  public bool ContainsPoint(Vector3 worldPoint)
  {
    return zoneCollider != null && zoneCollider.bounds.Contains(worldPoint);
  }

  public float GetInteriorScore(Vector3 worldPoint)
  {
    if (zoneCollider == null || !zoneCollider.bounds.Contains(worldPoint))
      return -1f;

    Bounds bounds = zoneCollider.bounds;
    float marginX = Mathf.Min(worldPoint.x - bounds.min.x, bounds.max.x - worldPoint.x);
    float marginY = Mathf.Min(worldPoint.y - bounds.min.y, bounds.max.y - worldPoint.y);
    float marginZ = Mathf.Min(worldPoint.z - bounds.min.z, bounds.max.z - worldPoint.z);
    return Mathf.Min(marginX, Mathf.Min(marginY, marginZ));
  }

  public static Vector3 GetInteractionPoint()
  {
    PlayerInteractionBody body = Object.FindObjectOfType<PlayerInteractionBody>();
    if (body != null)
      return body.WorldPosition;

    Camera camera = Camera.main;
    if (camera == null)
      return Vector3.zero;

    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
    Plane plane = new Plane(Vector3.up, new Vector3(0f, 0.5f, 0f));
    return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : camera.transform.position;
  }

  public static InteractionTriggerZone GetPrimaryZone()
  {
    return GetPrimaryZone(GetInteractionPoint());
  }

  public static InteractionTriggerZone GetPrimaryZone(Vector3 worldPoint)
  {
    InteractionTriggerZone bestZone = null;
    float bestScore = -1f;
    float bestVolume = float.MaxValue;

    for (int i = 0; i < ActiveZones.Count; i++)
    {
      InteractionTriggerZone zone = ActiveZones[i];
      if (zone == null)
        continue;

      float score = zone.GetInteriorScore(worldPoint);
      if (score < 0f)
        continue;

      float volume = zone.zoneCollider.bounds.size.sqrMagnitude;

      if (score > bestScore || (Mathf.Approximately(score, bestScore) && volume < bestVolume))
      {
        bestScore = score;
        bestVolume = volume;
        bestZone = zone;
      }
    }

    if (bestZone != null)
      return bestZone;

    float proximityRadius = SummerJamMechanicsSettings.Instance.proximityInteractRadius;
    float bestRank = float.MaxValue;
    Vector3 lookDirection = GetLookDirection(worldPoint);

    for (int i = 0; i < ActiveZones.Count; i++)
    {
      InteractionTriggerZone zone = ActiveZones[i];
      if (zone == null)
        continue;

      float distance = zone.GetClosestSurfaceDistance(worldPoint);
      if (distance > proximityRadius)
        continue;

      float lookAlignment = zone.GetLookAlignment(worldPoint, lookDirection);
      float rank = distance - lookAlignment * 0.5f;
      float volume = zone.zoneCollider != null
        ? zone.zoneCollider.bounds.size.sqrMagnitude
        : float.MaxValue;

      if (rank < bestRank || (Mathf.Approximately(rank, bestRank) && volume < bestVolume))
      {
        bestRank = rank;
        bestVolume = volume;
        bestZone = zone;
      }
    }

    return bestZone;
  }

  private float GetLookAlignment(Vector3 worldPoint, Vector3 lookDirection)
  {
    if (zoneCollider == null || lookDirection.sqrMagnitude < 0.0001f)
      return 0f;

    Vector3 toZone = zoneCollider.bounds.center - worldPoint;
    if (toZone.sqrMagnitude < 0.0001f)
      return 1f;

    return Mathf.Max(0f, Vector3.Dot(toZone.normalized, lookDirection.normalized));
  }

  private static Vector3 GetLookDirection(Vector3 worldPoint)
  {
    Camera camera = Camera.main;
    if (camera != null)
      return camera.transform.forward;

    PlayerInteractionBody body = Object.FindObjectOfType<PlayerInteractionBody>();
    if (body != null)
      return body.transform.forward;

    return Vector3.forward;
  }

  public float GetClosestSurfaceDistance(Vector3 worldPoint)
  {
    if (zoneCollider == null)
      return float.MaxValue;

    Vector3 closestPoint = zoneCollider.ClosestPoint(worldPoint);
    return Vector3.Distance(worldPoint, closestPoint);
  }

  public static bool IsPrimaryZone(InteractionTriggerZone zone)
  {
    return zone != null && GetPrimaryZone() == zone;
  }
}
