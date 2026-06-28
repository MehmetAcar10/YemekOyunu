using UnityEngine;

/// <summary>/// Bu objenin uzerine takilir. Sahne acildiginda oyuncuyu (Player tag'li)
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

        FixConflictingColliders(player);

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        Quaternion yawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        player.transform.SetPositionAndRotation(transform.position, yawRotation);
        Physics.SyncTransforms();

        if (controller != null)
            controller.enabled = wasEnabled;

        SceneCameraBootstrap.ConfigureSceneCameras();
    }

    private static void FixConflictingColliders(GameObject player)
    {
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
            return;

        // CharacterController ile ayni objedeki fizik collider'lari hareketi kilitleyebilir.
        foreach (CapsuleCollider capsuleCollider in player.GetComponents<CapsuleCollider>())
            capsuleCollider.enabled = false;

        foreach (BoxCollider boxCollider in player.GetComponents<BoxCollider>())
            boxCollider.enabled = false;

        foreach (SphereCollider sphereCollider in player.GetComponents<SphereCollider>())
            sphereCollider.enabled = false;
    }
}
