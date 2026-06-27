using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionTriggerZone : MonoBehaviour
{
  private static readonly List<InteractionTriggerZone> ActiveZones = new List<InteractionTriggerZone>();

  [SerializeField] private Collider zoneCollider;

  public Collider ZoneCollider => zoneCollider;

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

    return bestZone;
  }

  public static bool IsPrimaryZone(InteractionTriggerZone zone)
  {
    return zone != null && GetPrimaryZone() == zone;
  }
}
