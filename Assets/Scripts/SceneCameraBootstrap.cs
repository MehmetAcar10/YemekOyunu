using UnityEngine;
using Cinemachine;

/// <summary>
/// Sahnedeki FPS kamera kurulumunu duzeltir:
/// - MainCamera ve Cinemachine VCam'i oyuncu cocugundan cikarir (scale bozulmasini onler)
/// - VCam'i PlayerCameraRoot'a baglar
/// - Malzeme modellerindeki gereksiz kameralari kapatir
/// </summary>
[DefaultExecutionOrder(-100)]
public class SceneCameraBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoConfigureOnSceneLoad()
    {
        ConfigureSceneCameras();
    }

    public static void ConfigureSceneCameras()
    {
        Transform cameraTarget = FindPlayerCameraTarget();
        Camera mainCamera = ResolveMainCamera();

        if (mainCamera != null)
            DetachFromPlayer(mainCamera.transform);

        DisableRogueCameras(mainCamera);
        ConfigureVirtualCameras(cameraTarget);

        if (mainCamera != null && !mainCamera.enabled)
            mainCamera.enabled = true;
    }

    private static Camera ResolveMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            return mainCamera;

        GameObject tagged = GameObject.FindGameObjectWithTag("MainCamera");
        return tagged != null ? tagged.GetComponent<Camera>() : null;
    }

    private static Transform FindPlayerCameraTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        Transform[] children = player.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].CompareTag("CinemachineTarget"))
                return children[i];
        }

        return player.transform;
    }

    private static void DetachFromPlayer(Transform cameraTransform)
    {
        if (cameraTransform.parent == null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || !cameraTransform.IsChildOf(player.transform))
            return;

        cameraTransform.SetParent(null, true);
    }

    private static void DisableRogueCameras(Camera mainCamera)
    {
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || camera == mainCamera)
                continue;

            camera.enabled = false;
        }
    }

    private static void ConfigureVirtualCameras(Transform cameraTarget)
    {
        CinemachineVirtualCamera[] virtualCameras =
            Object.FindObjectsOfType<CinemachineVirtualCamera>(true);

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < virtualCameras.Length; i++)
        {
            CinemachineVirtualCamera virtualCamera = virtualCameras[i];
            if (virtualCamera == null)
                continue;

            if (player != null && virtualCamera.transform.IsChildOf(player.transform))
                virtualCamera.transform.SetParent(null, true);

            if (cameraTarget != null)
            {
                virtualCamera.Follow = cameraTarget;
                virtualCamera.LookAt = cameraTarget;
            }

            virtualCamera.m_Priority = 10;
            DisableCameraNoise(virtualCamera);
        }
    }

    private static void DisableCameraNoise(CinemachineVirtualCamera virtualCamera)
    {
        CinemachineBasicMultiChannelPerlin noise =
            virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise != null)
            noise.m_AmplitudeGain = 0f;
    }

    /// <summary>
    /// Ingredient veya baska prefab icinden gelen gorseldeki kameralari kapatir.
    /// </summary>
    public static void DisableEmbeddedCameras(GameObject root)
    {
        if (root == null)
            return;

        Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = false;
    }
}
