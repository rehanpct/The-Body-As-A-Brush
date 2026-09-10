using UnityEngine;

public class BrushTrail : MonoBehaviour
{
    [Header("Hand Tracking")]
    public HandTrackingBrush handTrackingBrush;

    [Header("Water Current")]
    public GameObject waterCurrentPrefab;

    [Header("Spawn Settings")]
    public float minimumSpawnDistance = 0.5f;
    public float spawnCooldown = 0.08f;

    [Header("Appearance")]
    public float randomRotation = 8f;
    public float randomScaleMin = 0.85f;
    public float randomScaleMax = 1.15f;

    private Vector3 lastSpawnPosition;
    private bool hasSpawnedFirst = false;

    private float nextSpawnTime = 0f;

    void Update()
    {
        if (handTrackingBrush == null)
            return;

        // Only create water currents when
        // the index-finger drawing pose is active.
        if (!handTrackingBrush.IsIndexDrawing)
        {
            hasSpawnedFirst = false;
            return;
        }

        Vector3 currentPosition =
            handTrackingBrush.brush.position;

        // First water-current segment
        if (!hasSpawnedFirst)
        {
            SpawnWaterCurrent(currentPosition);

            lastSpawnPosition = currentPosition;
            hasSpawnedFirst = true;

            return;
        }

        // Prevent spawning too frequently
        if (Time.time < nextSpawnTime)
            return;

        // Only spawn after the finger has moved
        // a meaningful distance.
        float distance =
            Vector3.Distance(
                lastSpawnPosition,
                currentPosition
            );

        if (distance < minimumSpawnDistance)
            return;

        SpawnWaterCurrent(currentPosition);

        lastSpawnPosition = currentPosition;

        nextSpawnTime =
            Time.time + spawnCooldown;
    }

    void SpawnWaterCurrent(Vector3 position)
    {
        if (waterCurrentPrefab == null)
        {
            Debug.LogError(
                "BrushTrail: Water Current Prefab is not assigned!"
            );

            return;
        }

        // Random rotation gives the current
        // a slightly organic appearance.
        float rotation =
            Random.Range(
                -randomRotation,
                randomRotation
            );

        Quaternion rotationQuaternion =
            Quaternion.Euler(
                0f,
                0f,
                rotation
            );

        GameObject current =
            Instantiate(
                waterCurrentPrefab,
                position,
                rotationQuaternion
            );

        // Slight size variation
        float scale =
            Random.Range(
                randomScaleMin,
                randomScaleMax
            );

        current.transform.localScale *= scale;
    }
}