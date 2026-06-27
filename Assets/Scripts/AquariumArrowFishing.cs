using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Summerjam.Utils;

[DisallowMultipleComponent]
public class AquariumArrowFishing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private string m_AquariumName = "akvaryum1";
    [SerializeField] private string m_FishTag = "Balık";

    [Header("Odul / Geri Donus")]
    [Tooltip("Yakalanan her balik icin envantere eklenecek malzeme (PufferFish).")]
    [SerializeField] private IngredientSO m_FishIngredient;
    [Tooltip("Tum baliklar yakalaninca donulecek sahne.")]
    [SerializeField] private string m_ReturnSceneName = "Anasahne";
    [Tooltip("Son balik yakalandiktan sonra sahneye donmeden once beklenecek sure.")]
    [SerializeField] private float m_ReturnDelay = 1.2f;

    [Header("Movement")]
    [SerializeField] private float m_SweepSpeed = 14f;
    [SerializeField] private float m_DropSpeed = 26f;
    [SerializeField] private float m_ReturnSpeed = 30f;

    [Header("Catch")]
    [SerializeField] private float m_CatchRadius = 2.5f;

    [Header("Play area margins (from aquarium bounds)")]
    [SerializeField] private float m_SideMargin = 2f;
    [SerializeField] private float m_TopMargin = 2f;
    [SerializeField] private float m_BottomMargin = 2f;
    [SerializeField] private float m_ZOffset = -1f;

    private enum State { Sweeping, Dropping, Returning }

    private State m_State = State.Sweeping;
    private float m_MinX, m_MaxX, m_TopY, m_BottomY, m_PlaneZ;
    private int m_SweepDir = 1;
    private float m_DropX;
    private Transform m_CaughtFish;
    private AquariumFishController m_FishController;
    private int m_RemainingFish;
    private bool m_Returning;

    private static int s_Score;
    private GUIStyle m_Style;

    private void Start()
    {
        var aquarium = GameObject.Find(m_AquariumName);
        if (aquarium == null)
        {
            Debug.LogWarning("AquariumArrowFishing: '" + m_AquariumName + "' bulunamadı.", this);
            enabled = false;
            return;
        }

        m_FishController = aquarium.GetComponent<AquariumFishController>();
        s_Score = 0;
        m_Returning = false;

        try { m_RemainingFish = GameObject.FindGameObjectsWithTag(m_FishTag).Length; }
        catch (UnityException) { m_RemainingFish = 0; }

        Bounds b = ComputeBounds(aquarium);
        m_MinX = b.min.x + m_SideMargin;
        m_MaxX = b.max.x - m_SideMargin;
        m_TopY = b.max.y - m_TopMargin;
        m_BottomY = b.min.y + m_BottomMargin;
        m_PlaneZ = b.center.z + m_ZOffset;

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, m_MinX, m_MaxX), m_TopY, m_PlaneZ);
    }

    private static Bounds ComputeBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, root.transform.lossyScale);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    private void Update()
    {
        // Kesme widget'i aciksa balik tutma tamamen duraklar
        if (ContourCutSession.IsActive)
            return;

        switch (m_State)
        {
            case State.Sweeping:
                UpdateSweep();
                break;
            case State.Dropping:
                UpdateDrop();
                break;
            case State.Returning:
                UpdateReturn();
                break;
        }
    }

    private void UpdateSweep()
    {
        Vector3 p = transform.position;
        p.x += m_SweepDir * m_SweepSpeed * Time.deltaTime;

        if (p.x >= m_MaxX)
        {
            p.x = m_MaxX;
            m_SweepDir = -1;
        }
        else if (p.x <= m_MinX)
        {
            p.x = m_MinX;
            m_SweepDir = 1;
        }

        p.y = m_TopY;
        p.z = m_PlaneZ;
        transform.position = p;

        if (Input.GetMouseButtonDown(0))
        {
            m_DropX = p.x;
            m_State = State.Dropping;
        }
    }

    private void UpdateDrop()
    {
        Vector3 p = transform.position;
        p.y -= m_DropSpeed * Time.deltaTime;
        p.x = m_DropX;
        p.z = m_PlaneZ;

        if (p.y <= m_BottomY)
        {
            p.y = m_BottomY;
        }
        transform.position = p;

        Transform fish = FindFishNear(p);
        if (fish != null)
        {
            m_CaughtFish = fish;
            m_CaughtFish.SetParent(transform, true);
            m_State = State.Returning;
            return;
        }

        if (p.y <= m_BottomY)
        {
            m_State = State.Returning;
        }
    }

    private void UpdateReturn()
    {
        Vector3 p = transform.position;
        p.y = Mathf.MoveTowards(p.y, m_TopY, m_ReturnSpeed * Time.deltaTime);
        p.x = m_DropX;
        p.z = m_PlaneZ;
        transform.position = p;

        if (p.y < m_TopY)
        {
            return;
        }

        if (m_CaughtFish != null)
        {
            var fishGo = m_CaughtFish.gameObject;
            m_CaughtFish = null;
            Destroy(fishGo);
            s_Score++;

            // Balik her halukarda akvaryumdan gider
            m_RemainingFish--;
            if (m_FishController != null)
            {
                m_FishController.RefreshFishCache();
            }

            m_State = State.Sweeping;

            // Kesme widget'ini baslat; sonuc OnCutComplete'te islenir.
            // Widget acikken Update guard'i sayesinde ok duraklar.
            if (m_FishIngredient != null &&
                PufferFishCutPopupWidget.TryStartCut(m_FishIngredient, null, null, OnCutComplete))
            {
                return;
            }

            // Widget baslatilamazsa eski davranisa dus (aninda say)
            OnCutComplete(true);
            return;
        }

        m_State = State.Sweeping;
    }

    private void OnCutComplete(bool success)
    {
        // Sadece kontur basarili olursa balik envantere kuyruga alinir
        if (success && m_FishIngredient != null)
            PendingPickups.Add(m_FishIngredient, 1);

        // Tum baliklar tutulduysa (son kesim de bittikten sonra) Anasahne'ye don
        if (m_RemainingFish <= 0 && !m_Returning)
        {
            m_Returning = true;
            StartCoroutine(ReturnToSceneAfterDelay());
        }
    }

    private IEnumerator ReturnToSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(m_ReturnDelay);

        if (string.IsNullOrEmpty(m_ReturnSceneName))
            yield break;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(m_ReturnSceneName);
        else
            SceneManager.LoadScene(m_ReturnSceneName);
    }

    private Transform FindFishNear(Vector3 point)
    {
        GameObject[] fish;
        try
        {
            fish = GameObject.FindGameObjectsWithTag(m_FishTag);
        }
        catch (UnityException)
        {
            return null;
        }

        Transform best = null;
        float bestSq = m_CatchRadius * m_CatchRadius;
        for (int i = 0; i < fish.Length; i++)
        {
            if (fish[i] == null)
            {
                continue;
            }

            Vector3 fp = fish[i].transform.position;
            float dx = fp.x - point.x;
            float dy = fp.y - point.y;
            float sq = dx * dx + dy * dy;
            if (sq <= bestSq)
            {
                bestSq = sq;
                best = fish[i].transform;
            }
        }
        return best;
    }

    private void OnGUI()
    {
        if (m_Style == null)
        {
            m_Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            m_Style.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(20f, 18f, 460f, 60f), "Yakalanan Balık: " + s_Score, m_Style);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, m_CatchRadius);
    }
}
