using UnityEngine;

public class ArtworkStatistics : MonoBehaviour
{
    public static ArtworkStatistics Instance;

    public ArtworkData data;

    [Header("References")]
    public HandTrackingBrush handTrackingBrush;
    public GestureManager gestureManager;

    [Header("Movement Settings")]
    public float movementThreshold = 0.01f;
    public float pauseSpeedThreshold = 0.05f;

    private Vector3 previousHandPosition;
    private bool hasPreviousPosition = false;

    private float totalMovementTime = 0f;
    private float pauseTimer = 0f;

    private float artworkStartTime = 0f;

    private GestureManager.Gesture previousGesture =
        GestureManager.Gesture.Neutral;

    void Awake()
    {
        Instance = this;

        data = new ArtworkData();
        data.Reset();
    }

    void Start()
    {
        // Start timing the artwork from when the scene begins.
        artworkStartTime = Time.time;

        if (handTrackingBrush == null)
        {
            Debug.LogWarning(
                "ArtworkStatistics: HandTrackingBrush is not assigned!"
            );
        }

        if (gestureManager == null)
        {
            Debug.LogWarning(
                "ArtworkStatistics: GestureManager is not assigned!"
            );
        }
    }

    void Update()
    {
        TrackHandMovement();
        TrackGestures();
    }

    // ============================================
    // HAND MOVEMENT
    // ============================================

    void TrackHandMovement()
    {
        if (handTrackingBrush == null ||
            handTrackingBrush.brush == null)
            return;

        if (!handTrackingBrush.IsHandDetected)
        {
            hasPreviousPosition = false;
            return;
        }

        Vector3 currentPosition =
            handTrackingBrush.brush.position;

        if (!hasPreviousPosition)
        {
            previousHandPosition = currentPosition;
            hasPreviousPosition = true;
            return;
        }

        float distance =
            Vector3.Distance(
                previousHandPosition,
                currentPosition
            );

        float deltaTime = Time.deltaTime;

        // Ignore extremely small tracking noise
        if (distance > movementThreshold)
        {
            data.totalHandDistance += distance;

            if (deltaTime > 0f)
            {
                float speed =
                    distance / deltaTime;

                data.maximumHandSpeed =
                    Mathf.Max(
                        data.maximumHandSpeed,
                        speed
                    );

                data.handTrackingSamples++;

                totalMovementTime += deltaTime;
            }

            // Hand is moving
            pauseTimer = 0f;
        }
        else
        {
            // Hand is basically stationary
            pauseTimer += deltaTime;
            data.pauseTime += deltaTime;
        }

        previousHandPosition = currentPosition;
    }

    // ============================================
    // GESTURE COUNTS
    // ============================================

    void TrackGestures()
    {
        if (gestureManager == null)
            return;

        GestureManager.Gesture currentGesture =
            gestureManager.CurrentGesture;

        // Only count when the gesture changes
        if (currentGesture == previousGesture)
            return;

        switch (currentGesture)
        {
            case GestureManager.Gesture.Index:
                data.indexGestureCount++;
                break;

            case GestureManager.Gesture.OpenPalm:
                data.openPalmGestureCount++;
                break;

            case GestureManager.Gesture.VSign:
                data.vSignGestureCount++;
                break;

            case GestureManager.Gesture.ThumbUp:
                data.thumbUpGestureCount++;
                break;
        }

        previousGesture = currentGesture;
    }

    // ============================================
    // FINALIZE STATISTICS
    // ============================================

    public void FinalizeStatistics()
    {
        // Calculate actual artwork creation duration.
        data.creationTime =
            Time.time - artworkStartTime;

        // Calculate average hand speed.
        if (totalMovementTime > 0f)
        {
            data.averageHandSpeed =
                data.totalHandDistance /
                totalMovementTime;
        }
        else
        {
            data.averageHandSpeed = 0f;
        }

        Debug.Log(
            "===== ARTWORK STATISTICS ====="
        );

        Debug.Log(
            "Creation Time: " +
            data.creationTime +
            " seconds"
        );

        Debug.Log(
            "Water Currents: " +
            data.waterCount
        );

        Debug.Log(
            "Coral: " +
            data.coralCount
        );

        Debug.Log(
            "Fish Schools: " +
            data.fishSchoolCount
        );

        Debug.Log(
            "Bubble Bursts: " +
            data.bubbleBurstCount
        );

        Debug.Log(
            "Index Gestures: " +
            data.indexGestureCount
        );

        Debug.Log(
            "Open Palm Gestures: " +
            data.openPalmGestureCount
        );

        Debug.Log(
            "V Sign Gestures: " +
            data.vSignGestureCount
        );

        Debug.Log(
            "Thumb Up Gestures: " +
            data.thumbUpGestureCount
        );

        Debug.Log(
            "Total Hand Distance: " +
            data.totalHandDistance
        );

        Debug.Log(
            "Average Hand Speed: " +
            data.averageHandSpeed
        );

        Debug.Log(
            "Maximum Hand Speed: " +
            data.maximumHandSpeed
        );

        Debug.Log(
            "Pause Time: " +
            data.pauseTime
        );

        Debug.Log(
            "Hand Tracking Samples: " +
            data.handTrackingSamples
        );

        Debug.Log(
            "=============================="
        );
    }
}