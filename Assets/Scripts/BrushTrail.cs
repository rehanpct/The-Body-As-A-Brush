using UnityEngine;

public class BrushTrail : MonoBehaviour
{
    [Header("Hand Tracking")]
    public HandTrackingBrush handTrackingBrush;

    [Header("Water Current")]
    public GameObject waterCurrentPrefab;

    [Header("Spawn Settings")]
    public float randomRotation = 8f;
    public float randomScaleMin = 0.85f;
    public float randomScaleMax = 1.15f;

    // Prevents continuous spawning while Index is held
    private bool hasSpawnedForCurrentIndex = false;

    void Update()
    {
        if (handTrackingBrush == null)
            return;

        bool isIndex =
            handTrackingBrush.IsIndexDrawing;

        // --------------------------------
        // INDEX STARTED
        // --------------------------------
        if (isIndex)
        {
            // Already spawned for this Index gesture
            if (hasSpawnedForCurrentIndex)
                return;

            // Spawn exactly ONE water current
            SpawnWaterCurrent(
                handTrackingBrush.brush.position
            );

            hasSpawnedForCurrentIndex = true;

            return;
        }

        // --------------------------------
        // INDEX RELEASED / NEUTRAL
        // --------------------------------
        // This arms the system for the next Index.
        hasSpawnedForCurrentIndex = false;
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

        position.z = 0f;

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
            if (ArtworkStatistics.Instance != null)
            {
                ArtworkStatistics.Instance.data.waterCount++;
            }

        float scale =
            Random.Range(
                randomScaleMin,
                randomScaleMax
            );

        current.transform.localScale *= scale;
    }
}