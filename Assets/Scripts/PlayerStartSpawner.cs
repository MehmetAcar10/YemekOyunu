using UnityEngine;

/// <summary>
/// Bu objenin uzerine takilir. Sahne acildiginda oyuncuyu (Player tag'li)
/// bu objenin konum ve yatay (yaw) yonune isinlar. Anasahne her yuklendiginde calisir.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStartSpawner : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
            return;

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        // Karakteri dik tutmak icin sadece Y (yaw) rotasyonunu uygula
        Quaternion yawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        player.transform.SetPositionAndRotation(transform.position, yawRotation);
        Physics.SyncTransforms();

        if (controller != null)
            controller.enabled = wasEnabled;
    }
}
