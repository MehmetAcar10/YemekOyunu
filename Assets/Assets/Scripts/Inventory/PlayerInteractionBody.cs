using UnityEngine;

public class PlayerInteractionBody : MonoBehaviour
{
  [Tooltip("Eğer doluysa bu transform takip edilir. Boşsa 'Player' tag'li GameObject bulunur. FPS oyununda etkileşim noktası karakterin kendisidir.")]
  [SerializeField] private Transform followTarget;
  [SerializeField] private string playerTag = "Player";
  [SerializeField] private Vector3 followOffset = Vector3.zero;

  [Header("Fallback (top-down mouse projection)")]
  [Tooltip("Follow target hiçbir şekilde bulunamazsa eski fare projeksiyonu kullanılır.")]
  [SerializeField] private Camera interactionCamera;
  [SerializeField] private float planeHeight = 0.5f;

  public Vector3 WorldPosition => transform.position;

  private Plane interactionPlane;

  private void Awake()
  {
    if (followTarget == null)
    {
      var player = GameObject.FindGameObjectWithTag(playerTag);
      if (player != null)
        followTarget = player.transform;
    }

    if (interactionCamera == null)
      interactionCamera = Camera.main;

    interactionPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
  }

  private void Update()
  {
    if (followTarget != null)
    {
      transform.position = followTarget.position + followOffset;
      return;
    }

    if (interactionCamera == null)
      return;

    Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
    if (!interactionPlane.Raycast(ray, out float distance))
      return;

    transform.position = ray.GetPoint(distance);
  }
}
