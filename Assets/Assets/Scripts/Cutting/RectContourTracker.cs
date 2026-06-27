using System;
using UnityEngine;

public class RectContourTracker
{
  private bool[] visitedBins;
  private float halfWidth;
  private float halfHeight;
  private float tolerance;
  private float perimeter;
  private int binCount;
  private Vector2 centerOffset;

  public float SuccessThreshold { get; set; } = 0.95f;

  public int BinCount => binCount;

  public float Progress
  {
    get
    {
      if (visitedBins == null || binCount == 0)
        return 0f;

      int visited = 0;
      for (int i = 0; i < binCount; i++)
      {
        if (visitedBins[i])
          visited++;
      }

      return visited / (float)binCount;
    }
  }

  public bool IsSuccess => Progress >= SuccessThreshold;

  public void Setup(
    float width,
    float height,
    int bins,
    float traceTolerance,
    float successThreshold,
    Vector2 center = default)
  {
    halfWidth = width * 0.5f;
    halfHeight = height * 0.5f;
    centerOffset = center;
    binCount = Mathf.Max(8, bins);
    tolerance = Mathf.Max(0.0001f, traceTolerance);
    SuccessThreshold = Mathf.Clamp01(successThreshold);
    perimeter = 2f * (width + height);
    visitedBins = new bool[binCount];
  }

  public void Reset()
  {
    if (visitedBins == null)
      return;

    Array.Clear(visitedBins, 0, visitedBins.Length);
  }

  public void TryMarkPoint(Vector2 point)
  {
    if (visitedBins == null)
      return;

    if (!TryGetClosestPointOnContour(point - centerOffset, out Vector2 closest, out float distance))
      return;

    if (distance > tolerance)
      return;

    float arcLength = GetArcLengthForPoint(closest);
    int binIndex = Mathf.Clamp(Mathf.FloorToInt(arcLength / perimeter * binCount), 0, binCount - 1);
    visitedBins[binIndex] = true;
  }

  public bool IsBinVisited(int binIndex)
  {
    if (visitedBins == null || binIndex < 0 || binIndex >= binCount)
      return false;

    return visitedBins[binIndex];
  }

  public Vector2 GetPointOnContour(float normalizedArc)
  {
    float arcLength = Mathf.Repeat(normalizedArc, 1f) * perimeter;
    return GetPointAtArcLength(arcLength) + centerOffset;
  }

  private bool TryGetClosestPointOnContour(Vector2 point, out Vector2 closest, out float distance)
  {
    closest = Vector2.zero;
    distance = float.MaxValue;

    TryEdgeClosest(point, new Vector2(-halfWidth, halfHeight), new Vector2(halfWidth, halfHeight), ref closest, ref distance);
    TryEdgeClosest(point, new Vector2(halfWidth, halfHeight), new Vector2(halfWidth, -halfHeight), ref closest, ref distance);
    TryEdgeClosest(point, new Vector2(halfWidth, -halfHeight), new Vector2(-halfWidth, -halfHeight), ref closest, ref distance);
    TryEdgeClosest(point, new Vector2(-halfWidth, -halfHeight), new Vector2(-halfWidth, halfHeight), ref closest, ref distance);

    return distance < float.MaxValue;
  }

  private static void TryEdgeClosest(Vector2 point, Vector2 a, Vector2 b, ref Vector2 closest, ref float distance)
  {
    Vector2 ab = b - a;
    float lengthSq = ab.sqrMagnitude;
    if (lengthSq <= Mathf.Epsilon)
      return;

    float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
    Vector2 candidate = a + ab * t;
    float candidateDistance = Vector2.Distance(point, candidate);
    if (candidateDistance >= distance)
      return;

    distance = candidateDistance;
    closest = candidate;
  }

  private float GetArcLengthForPoint(Vector2 point)
  {
    const float epsilon = 0.001f;
    float topLength = halfWidth * 2f;

    if (Mathf.Abs(point.y - halfHeight) <= epsilon && point.x >= -halfWidth && point.x <= halfWidth)
      return point.x + halfWidth;

    if (Mathf.Abs(point.x - halfWidth) <= epsilon && point.y <= halfHeight && point.y >= -halfHeight)
      return topLength + (halfHeight - point.y);

    if (Mathf.Abs(point.y + halfHeight) <= epsilon && point.x <= halfWidth && point.x >= -halfWidth)
      return topLength + halfHeight * 2f + (halfWidth - point.x);

    return topLength + halfHeight * 2f + halfWidth * 2f + (point.y + halfHeight);
  }

  private Vector2 GetPointAtArcLength(float arcLength)
  {
    arcLength = Mathf.Repeat(arcLength, perimeter);
    float topLength = halfWidth * 2f;
    float rightLength = halfHeight * 2f;
    float bottomLength = halfWidth * 2f;

    if (arcLength <= topLength)
      return new Vector2(-halfWidth + arcLength, halfHeight);

    arcLength -= topLength;
    if (arcLength <= rightLength)
      return new Vector2(halfWidth, halfHeight - arcLength);

    arcLength -= rightLength;
    if (arcLength <= bottomLength)
      return new Vector2(halfWidth - arcLength, -halfHeight);

    arcLength -= bottomLength;
    return new Vector2(-halfWidth, -halfHeight + arcLength);
  }
}
