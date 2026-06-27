using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FishingHookController : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private string fishTag = "Bal\u0131k";
    [SerializeField] private string aquariumName = "akvaryum1";

    [Header("Movement")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float swingAngle = 60f;
    [SerializeField] private float shootSpeed = 14f;
    [SerializeField] private float returnSpeed = 18f;
    [SerializeField] private float maxDistance = 12f;

    [Header("Catch")]
    [SerializeField] private LayerMask catchLayers = -1;
    [SerializeField] private float catchRadius = 0.75f;

    [Header("Score")]
    [SerializeField] private int scoreValue = 10;

    private enum HookState
    {
        Swinging,
        Shooting,
        Returning,
        Completed
    }

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[16];
    private readonly Collider2D[] overlapHits = new Collider2D[16];

    private HookState state = HookState.Swinging;
    private Rigidbody2D hookRigidbody;
    private Collider2D hookCollider;
    private ContactFilter2D catchFilter;
    private Vector3 homeLocalPosition;
    private Vector3 shootStartPosition;
    private Vector3 shootDirection;
    private AquariumFishController fishController;
    private int score;
    private int remainingFish;
    private bool caughtThisShot;
    private float swingTime;
    private GUIStyle scoreStyle;

    private void Awake()
    {
        hookRigidbody = GetComponent<Rigidbody2D>();
        hookCollider = GetComponent<Collider2D>();

        hookRigidbody.bodyType = RigidbodyType2D.Kinematic;
        hookRigidbody.gravityScale = 0f;
        hookRigidbody.velocity = Vector2.zero;
        hookRigidbody.angularVelocity = 0f;

        ConfigureHookCollider();
        ConfigureCatchFilter();
    }

    private void Start()
    {
        if (pivot == null)
        {
            pivot = transform.parent;
        }

        if (pivot == null)
        {
            Debug.LogWarning("FishingHookController: pivot reference is missing.", this);
            enabled = false;
            return;
        }

        homeLocalPosition = transform.localPosition;

        GameObject aquarium = GameObject.Find(aquariumName);
        if (aquarium != null)
        {
            fishController = aquarium.GetComponent<AquariumFishController>();
        }

        RefreshRemainingFishCount();
        if (remainingFish <= 0)
        {
            CompleteFishing();
        }
    }

    private void Update()
    {
        switch (state)
        {
            case HookState.Swinging:
                UpdateSwing();
                HandleShootInput();
                break;

            case HookState.Shooting:
                UpdateShoot();
                break;

            case HookState.Returning:
                UpdateReturn();
                break;

            case HookState.Completed:
                KeepHookAtHome();
                break;
        }
    }

    private void ConfigureHookCollider()
    {
        hookCollider.isTrigger = true;

        if (hookCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = catchRadius / GetMaxXYScale(circleCollider.transform.lossyScale);
        }
    }

    private void ConfigureCatchFilter()
    {
        catchFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true
        };
        catchFilter.SetLayerMask(catchLayers);
    }

    private void HandleShootInput()
    {
        if (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        RefreshRemainingFishCount();
        if (remainingFish <= 0)
        {
            CompleteFishing();
            return;
        }

        BeginShoot(GetCurrentHookDirection());
    }

    private void UpdateSwing()
    {
        KeepHookAtHome();

        swingTime += Time.deltaTime;
        float angle = Mathf.Sin(swingTime * rotateSpeed * Mathf.Deg2Rad) * swingAngle;
        pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void BeginShoot(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = GetCurrentHookDirection();
        }

        ConfigureHookCollider();
        ConfigureCatchFilter();

        shootStartPosition = transform.position;
        shootDirection = direction.normalized;
        caughtThisShot = false;
        state = HookState.Shooting;

        Physics2D.SyncTransforms();
        if (TryCatchAt(transform.position))
        {
            state = HookState.Returning;
        }
    }

    private Vector3 GetCurrentHookDirection()
    {
        if (pivot != null)
        {
            Vector3 direction = -pivot.up;
            direction.z = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.down;
        }

        Vector3 fallback = -transform.up;
        fallback.z = 0f;
        return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.down;
    }

    private void UpdateShoot()
    {
        Vector3 previousPosition = transform.position;
        Vector3 nextPosition = previousPosition + shootDirection * shootSpeed * Time.deltaTime;
        Vector3 maxPosition = shootStartPosition + shootDirection * maxDistance;

        if (Vector3.Distance(shootStartPosition, nextPosition) >= maxDistance)
        {
            nextPosition = maxPosition;
        }

        TryCatchBetween(previousPosition, nextPosition);
        MoveHook(nextPosition);

        if (caughtThisShot || Vector3.Distance(shootStartPosition, transform.position) >= maxDistance - 0.001f)
        {
            state = HookState.Returning;
        }
    }

    private void UpdateReturn()
    {
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, shootStartPosition, returnSpeed * Time.deltaTime);
        MoveHook(nextPosition);

        if ((transform.position - shootStartPosition).sqrMagnitude > 0.0001f)
        {
            return;
        }

        KeepHookAtHome();

        RefreshRemainingFishCount();
        if (remainingFish <= 0)
        {
            CompleteFishing();
            return;
        }

        state = HookState.Swinging;
    }

    private void KeepHookAtHome()
    {
        transform.localPosition = homeLocalPosition;
        hookRigidbody.position = transform.position;
        hookRigidbody.velocity = Vector2.zero;
        hookRigidbody.angularVelocity = 0f;
    }

    private void MoveHook(Vector3 position)
    {
        position.z = transform.position.z;
        transform.position = position;
        hookRigidbody.position = new Vector2(position.x, position.y);
        Physics2D.SyncTransforms();
    }

    private bool TryCatchBetween(Vector3 from, Vector3 to)
    {
        if (caughtThisShot)
        {
            return true;
        }

        Vector2 delta = to - from;
        float distance = delta.magnitude;

        if (distance <= 0.0001f)
        {
            return TryCatchAt(to);
        }

        int hitCount = Physics2D.CircleCast(
            from,
            catchRadius,
            delta / distance,
            catchFilter,
            castHits,
            distance);

        for (int i = 0; i < hitCount; i++)
        {
            if (TryCatchCollider(castHits[i].collider))
            {
                return true;
            }
        }

        return TryCatchAt(to);
    }

    private bool TryCatchAt(Vector3 position)
    {
        if (caughtThisShot)
        {
            return true;
        }

        int hitCount = Physics2D.OverlapCircle(position, catchRadius, catchFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            if (TryCatchCollider(overlapHits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryCatchCollider(Collider2D other)
    {
        if (state != HookState.Shooting || caughtThisShot || other == null || other == hookCollider)
        {
            return false;
        }

        GameObject fish = ResolveFishObject(other);
        if (fish == null || fish == gameObject || fish.transform.IsChildOf(transform))
        {
            return false;
        }

        caughtThisShot = true;
        score += scoreValue;

        fish.tag = "Untagged";
        fish.SetActive(false);
        Destroy(fish);

        RefreshRemainingFishCount();
        if (fishController != null)
        {
            fishController.RefreshFishCache();
        }

        state = HookState.Returning;
        return true;
    }

    private GameObject ResolveFishObject(Collider2D other)
    {
        GameObject candidate = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (candidate.CompareTag(fishTag))
        {
            return candidate;
        }

        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(fishTag))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private void RefreshRemainingFishCount()
    {
        try
        {
            remainingFish = GameObject.FindGameObjectsWithTag(fishTag).Length;
        }
        catch (UnityException)
        {
            remainingFish = 0;
        }
    }

    private void CompleteFishing()
    {
        caughtThisShot = false;
        state = HookState.Completed;
        KeepHookAtHome();
    }

    private float GetMaxXYScale(Vector3 scale)
    {
        return Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCatchCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCatchCollider(other);
    }

    private void OnGUI()
    {
        if (scoreStyle == null)
        {
            scoreStyle = new GUIStyle(GUI.skin.label);
            scoreStyle.fontSize = 28;
            scoreStyle.normal.textColor = Color.white;
            scoreStyle.fontStyle = FontStyle.Bold;
        }

        string label = remainingFish <= 0 ? "Skor: " + score + "\nTum baliklar yakalandi" : "Skor: " + score;
        GUI.Label(new Rect(20f, 18f, 420f, 80f), label, scoreStyle);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        if (pivot != null)
        {
            Gizmos.color = new Color(0.6f, 0.6f, 1f, 0.6f);
            Gizmos.DrawLine(pivot.position, transform.position);
        }
    }
}

