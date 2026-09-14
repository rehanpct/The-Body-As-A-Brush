using UnityEngine;

public class ThumbUpGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Bubble")]
    public GameObject bubblePrefab;

    [Header("Burst Settings")]
    public int bubbleCount = 3;
    public float burstRadius = 1.5f;

    private bool hasSpawnedForCurrentThumb = false;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "ThumbUpGesture: GestureManager is not assigned!"
            );
        }

        if (bubblePrefab == null)
        {
            Debug.LogError(
                "ThumbUpGesture: Bubble Prefab is not assigned!"
            );
        }
    }

    void Update()
    {
        if (gestureManager == null)
            return;

        bool isThumbUp =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.ThumbUp;

        // --------------------------------
        // THUMB UP DETECTED
        // --------------------------------
        if (isThumbUp)
        {
            // Already spawned for this Thumb Up
            if (hasSpawnedForCurrentThumb)
                return;

            SpawnBubbleBurst();

            hasSpawnedForCurrentThumb = true;

            return;
        }

        // --------------------------------
        // THUMB UP RELEASED
        // --------------------------------
        // Allows the next Thumb Up to trigger.
        hasSpawnedForCurrentThumb = false;
    }

    void SpawnBubbleBurst()
    {
        if (bubblePrefab == null)
            return;

        HandTrackingBrush brush =
            FindFirstObjectByType<HandTrackingBrush>();

        if (brush == null || brush.brush == null)
            return;

        Vector3 centerPosition =
            brush.brush.position;

        centerPosition.z = 0f;

        for (int i = 0; i < bubbleCount; i++)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle * burstRadius;

            Vector3 spawnPosition =
                centerPosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            Instantiate(
                bubblePrefab,
                spawnPosition,
                Quaternion.identity
            );
        }

        // Count ONE bubble burst action
        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance.data.bubbleBurstCount++;
        }
    }
}