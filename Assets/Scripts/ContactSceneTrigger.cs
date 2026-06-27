using UnityEngine;
using UnityEngine.SceneManagement;
using Summerjam.Utils;

/// <summary>
/// Oyuncuya takilir. Karakter, belirtilen tag'e sahip bir objeye temas ettigi an
/// hedef sahneye gecer (E tusu gerekmez). Hem solid carpisma (CharacterController)
/// hem de trigger temasi yakalanir.
/// </summary>
[DisallowMultipleComponent]
public class ContactSceneTrigger : MonoBehaviour
{
    [SerializeField] private string contactTag = "TavukDiyarı";
    [SerializeField] private string targetSceneName = "HorozÇiftlik";

    private bool m_Loading;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryTrigger(hit.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider col)
    {
        if (m_Loading || col == null || string.IsNullOrEmpty(targetSceneName))
            return;

        if (!col.CompareTag(contactTag))
            return;

        m_Loading = true;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }
}
