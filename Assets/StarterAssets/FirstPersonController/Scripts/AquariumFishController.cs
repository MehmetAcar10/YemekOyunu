using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AquariumFishController : MonoBehaviour
{
    [SerializeField] private string fishTag = "Balık";
    [SerializeField] private Vector3 aquariumPadding = new Vector3(4f, 3f, 1.25f);
    [SerializeField] private Vector2 swimSpeedRange = new Vector2(1.5f, 3.25f);
    [SerializeField] private Vector2 waitAtTargetRange = new Vector2(0.2f, 0.8f);
    [SerializeField] private float reachDistance = 0.75f;
    [SerializeField] private float turnSpeed = 4f;
    [SerializeField] private float bobAmplitude = 0.35f;
    [SerializeField] private float bobFrequency = 1.1f;

    private class FishState
    {
        public Transform transform;
        public Vector3 targetPosition;
        public float speed;
        public float waitTimer;
        public float bobPhase;
        public float facingSign;
        public float baseScaleX;
    }

    private readonly List<FishState> fishStates = new List<FishState>();
    private Bounds aquariumBounds;
    private bool hasBounds;

    private void Start()
    {
        aquariumBounds = BuildAquariumBounds();
        hasBounds = aquariumBounds.size.sqrMagnitude > 0.01f;
        if (!hasBounds)
        {
            Debug.LogWarning("AquariumFishController could not determine aquarium bounds.", this);
            enabled = false;
            return;
        }

        RefreshFishCache();
    }

    public void RefreshFishCache()
    {
        fishStates.Clear();

        GameObject[] fishObjects;
        try
        {
            fishObjects = GameObject.FindGameObjectsWithTag(fishTag);
        }
        catch (UnityException)
        {
            return;
        }

        for (int i = 0; i < fishObjects.Length; i++)
        {
            GameObject fish = fishObjects[i];
            if (fish == null || !fish.activeInHierarchy)
            {
                continue;
            }

            FishState state = new FishState
            {
                transform = fish.transform,
                bobPhase = Random.value * Mathf.PI * 2f,
                baseScaleX = Mathf.Abs(fish.transform.localScale.x),
                facingSign = Mathf.Sign(fish.transform.localScale.x == 0f ? 1f : fish.transform.localScale.x),
            };
            state.speed = Random.Range(swimSpeedRange.x, swimSpeedRange.y);
            state.targetPosition = PickTarget(state.transform.position);
            fishStates.Add(state);
        }
    }

    private void Update()
    {
        if (!hasBounds)
        {
            return;
        }

        float dt = Time.deltaTime;

        for (int i = 0; i < fishStates.Count; i++)
        {
            FishState state = fishStates[i];
            if (state.transform == null)
            {
                continue;
            }

            if (state.waitTimer > 0f)
            {
                state.waitTimer -= dt;
                ApplyBob(state, dt);
                continue;
            }

            Vector3 current = state.transform.position;
            Vector3 toTarget = state.targetPosition - current;
            toTarget.z = 0f;
            float distance = toTarget.magnitude;

            if (distance <= reachDistance)
            {
                state.waitTimer = Random.Range(waitAtTargetRange.x, waitAtTargetRange.y);
                state.speed = Random.Range(swimSpeedRange.x, swimSpeedRange.y);
                state.targetPosition = PickTarget(current);
                continue;
            }

            Vector3 dir = toTarget / distance;
            Vector3 step = dir * state.speed * dt;
            Vector3 newPos = current + step;
            state.transform.position = newPos;

            ApplyBob(state, dt);
            UpdateFacing(state, dir.x);
        }
    }

    private void ApplyBob(FishState state, float dt)
    {
        state.bobPhase += bobFrequency * dt * Mathf.PI * 2f;
        float bobDelta = Mathf.Cos(state.bobPhase) * bobAmplitude * bobFrequency * Mathf.PI * 2f * dt;
        Vector3 pos = state.transform.position;
        pos.y += bobDelta;
        state.transform.position = pos;
    }

    private void UpdateFacing(FishState state, float dirX)
    {
        if (Mathf.Abs(dirX) < 0.05f)
        {
            return;
        }

        float wanted = Mathf.Sign(dirX);
        if (Mathf.Approximately(wanted, state.facingSign))
        {
            return;
        }

        state.facingSign = Mathf.MoveTowards(state.facingSign, wanted, turnSpeed * Time.deltaTime);
        Vector3 ls = state.transform.localScale;
        ls.x = state.baseScaleX * (state.facingSign >= 0f ? 1f : -1f);
        state.transform.localScale = ls;
    }

    private Vector3 PickTarget(Vector3 current)
    {
        float minX = aquariumBounds.min.x + aquariumPadding.x;
        float maxX = aquariumBounds.max.x - aquariumPadding.x;
        float minY = aquariumBounds.min.y + aquariumPadding.y;
        float maxY = aquariumBounds.max.y - aquariumPadding.y;

        if (maxX <= minX) { minX = aquariumBounds.center.x - 0.1f; maxX = aquariumBounds.center.x + 0.1f; }
        if (maxY <= minY) { minY = aquariumBounds.center.y - 0.1f; maxY = aquariumBounds.center.y + 0.1f; }

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);
        return new Vector3(x, y, current.z);
    }

    private Bounds BuildAquariumBounds()
    {
        return new Bounds(transform.position, transform.lossyScale);
    }
}
