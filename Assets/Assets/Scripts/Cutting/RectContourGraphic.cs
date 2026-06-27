using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RectContourGraphic : Graphic
{
  [SerializeField] private float lineThickness = 3f;
  [SerializeField] private Color pendingColor = new Color(1f, 0.92f, 0.35f, 0.95f);
  [SerializeField] private Color completedColor = new Color(0.35f, 0.95f, 0.45f, 0.95f);

  private RectContourTracker tracker;

  public void Configure(RectContourTracker contourTracker)
  {
    tracker = contourTracker;
    SetVerticesDirty();
  }

  protected override void OnPopulateMesh(VertexHelper vertexHelper)
  {
    vertexHelper.Clear();
    if (tracker == null || rectTransform == null)
      return;

    int segments = tracker.BinCount;
    for (int i = 0; i < segments; i++)
    {
      float t0 = i / (float)segments;
      float t1 = (i + 1) / (float)segments;
      Vector2 p0 = tracker.GetPointOnContour(t0);
      Vector2 p1 = tracker.GetPointOnContour(t1);

      Color color = tracker.IsBinVisited(i) ? completedColor : pendingColor;
      AddLine(vertexHelper, p0, p1, lineThickness, color);
    }
  }

  public void Refresh()
  {
    SetVerticesDirty();
  }

  private static void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float thickness, Color color)
  {
    Vector2 direction = end - start;
    if (direction.sqrMagnitude <= Mathf.Epsilon)
      return;

    Vector2 normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
    int index = vertexHelper.currentVertCount;

    Color32 color32 = color;
    vertexHelper.AddVert(start - normal, color32, Vector2.zero);
    vertexHelper.AddVert(start + normal, color32, Vector2.zero);
    vertexHelper.AddVert(end + normal, color32, Vector2.zero);
    vertexHelper.AddVert(end - normal, color32, Vector2.zero);

    vertexHelper.AddTriangle(index, index + 1, index + 2);
    vertexHelper.AddTriangle(index, index + 2, index + 3);
  }
}
