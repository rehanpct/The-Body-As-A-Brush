using UnityEngine;

public class FistGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Bubble")]
    public GameObject bubblePrefab;

    [Header("Burst Settings")]
    public int bubbleCount = 3;
    public float burstRadius = 1.5f;
    public float spawnCooldown = 1.5f;

    private float nextSpawnTime = 0f;
    private bool wasFist = false;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "FistGesture: GestureManager is not assigned!"
            );
        }
    }

    void Update()
    {
        if (gestureManager == null)
            return;

        bool isFist =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.Fist;

        // Trigger only when entering Fist.
        if (isFist && !wasFist)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnBubbleBurst();

                nextSpawnTime =
                    Time.time + spawnCooldown;
            }
        }

        wasFist = isFist;
    }

    void SpawnBubbleBurst()
    {
        if (bubblePrefab == null)
        {
            Debug.LogError(
                "FistGesture: Bubble prefab is not assigned!"
            );
            return;
        }

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
    }
}