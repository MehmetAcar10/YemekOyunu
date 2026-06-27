using System;
using System.Collections;
using Controller;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Summerjam.Utils;

[DisallowMultipleComponent]
public class ChickenCatcher : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float m_CatchRadius = 2.5f;
    [SerializeField]
    private Key m_CatchKey = Key.E;

    [Header("Odul / Geri Donus")]
    [Tooltip("Yakalanan her tavuk icin envantere eklenecek malzeme (Chicken).")]
    [SerializeField]
    private IngredientSO m_ChickenIngredient;
    [Tooltip("Tum tavuklar yakalaninca donulecek sahne.")]
    [SerializeField]
    private string m_ReturnSceneName = "Anasahne";
    [Tooltip("Son tavuk yakalandiktan sonra donmeden once beklenecek sure.")]
    [SerializeField]
    private float m_ReturnDelay = 1.2f;

    public static int CaughtCount { get; private set; }
    public static event Action<int> CaughtCountChanged;

    private static readonly Collider[] s_Buffer = new Collider[32];

    private int m_RemainingChickens;
    private bool m_Returning;

    private void Start()
    {
        CaughtCount = 0;
        m_Returning = false;
        m_RemainingChickens = FindObjectsOfType<ChickenAutoWanderInput>().Length;
        CaughtCountChanged?.Invoke(CaughtCount);
    }

    private void OnEnable()
    {
        CaughtCountChanged?.Invoke(CaughtCount);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[m_CatchKey].wasPressedThisFrame)
        {
            return;
        }

        var nearest = FindNearestChicken();
        if (nearest == null)
        {
            return;
        }

        Destroy(nearest.gameObject);
        CaughtCount++;
        CaughtCountChanged?.Invoke(CaughtCount);

        // Yakalanan tavugu envantere gonderilmek uzere kuyruga al
        if (m_ChickenIngredient != null)
            PendingPickups.Add(m_ChickenIngredient, 1);

        m_RemainingChickens--;

        // Tum tavuklar yakalandiysa Anasahne'ye don
        if (m_RemainingChickens <= 0 && !m_Returning)
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

    private ChickenAutoWanderInput FindNearestChicken()
    {
        var origin = transform.position;
        var count = Physics.OverlapSphereNonAlloc(origin, m_CatchRadius, s_Buffer, ~0, QueryTriggerInteraction.Ignore);

        ChickenAutoWanderInput best = null;
        float bestSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var chick = s_Buffer[i].GetComponentInParent<ChickenAutoWanderInput>();
            if (chick == null || chick.gameObject == gameObject)
            {
                continue;
            }

            var sq = (chick.transform.position - origin).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = chick;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, m_CatchRadius);
    }
}
