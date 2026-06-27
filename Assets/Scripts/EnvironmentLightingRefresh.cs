using UnityEngine;

/// <summary>
/// Sahne runtime'da (SceneManager.LoadScene ile) yuklendiginde skybox tabanli
/// ambient/yansima probe'unu yeniden hesaplar. Baked lighting olmadan sahne
/// gecislerinde ortamin kararmasini onler.
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentLightingRefresh : MonoBehaviour
{
    private void Start()
    {
        DynamicGI.UpdateEnvironment();
    }
}
