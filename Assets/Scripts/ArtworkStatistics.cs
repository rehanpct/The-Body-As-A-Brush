
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
    private bool hasPreviousPosition;

    private float totalMovementTime;
    private float pauseTimer;
    private float artworkStartTime;

    private GestureManager.Gesture previousGesture =
        GestureManager.Gesture.Neutral;

    private bool isInitialized;
    private bool isFinalized;

    public bool IsInitialized => isInitialized;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogError("ArtworkStatistics: Duplicate tracker disabled on " + gameObject.name, this);
            enabled = false;
            return;
        }

        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
            return;

        // Each scene instance owns one fresh drawing session, never result data.
        data = new ArtworkData();
        data.Reset();
        artworkStartTime = Time.time;

        isInitialized = true;

        Debug.Log(
            "ArtworkStatistics initialized: " +
            gameObject.name +
            " | Scene: " + gameObject.scene.name +
            " | Active: " + gameObject.activeInHierarchy
        );
    }

    void Start()
    {
        EnsureInitialized();

        if (handTrackingBrush == null)
        {
            Debug.LogWarning(
                "ArtworkStatistics: HandTrackingBrush is not assigned."
            );
        }

        if (gestureManager == null)
        {
            Debug.LogWarning(
                "ArtworkStatistics: GestureManager is not assigned."
            );
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!isInitialized || isFinalized || data == null)
            return;

        TrackHandMovement();
        TrackGestures();
    }

    public static ArtworkStatistics FindStatistics()
    {
        // First choice: the existing singleton.
        if (Instance != null && Instance.isActiveAndEnabled)
        {
            Instance.EnsureInitialized();

            if (Instance.data != null)
                return Instance;
        }

        // Include inactive objects for diagnosis, but prefer active ones.
        ArtworkStatistics[] candidates =
            FindObjectsByType<ArtworkStatistics>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        ArtworkStatistics fallback = null;

        foreach (ArtworkStatistics candidate in candidates)
        {
            if (candidate == null)
                continue;

            if (candidate.isActiveAndEnabled)
            {
                candidate.EnsureInitialized();

                if (candidate.data != null)
                {
                    Instance = candidate;

                    Debug.LogWarning(
                        "ArtworkStatistics recovered: " +
                        candidate.gameObject.name +
                        " | Scene: " + candidate.gameObject.scene.name
                    );

                    return candidate;
                }
            }

            if (fallback == null)
                fallback = candidate;
        }

        if (fallback != null)
        {
            Debug.LogError(
                "ArtworkStatistics was found, but is not active and enabled. " +
                "Object: " + fallback.gameObject.name +
                " | Active in hierarchy: " +
                fallback.gameObject.activeInHierarchy +
                " | Component enabled: " + fallback.enabled
            );

            return null;
        }

        Debug.LogError(
            "ArtworkStatistics: No component exists in the loaded scenes. " +
            "Check whether the component or its GameObject was removed, " +
            "destroyed, or never added to DrawingScene."
        );

        return null;
    }

    void TrackHandMovement()
    {
        if (handTrackingBrush == null ||
            handTrackingBrush.brush == null)
        {
            return;
        }

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

        float distance = Vector3.Distance(
            previousHandPosition,
            currentPosition
        );

        float deltaTime = Time.deltaTime;

        if (distance > movementThreshold)
        {
            data.totalHandDistance += distance;

            if (deltaTime > 0f)
            {
                float speed = distance / deltaTime;

                data.maximumHandSpeed = Mathf.Max(
                    data.maximumHandSpeed,
                    speed
                );

                data.handTrackingSamples++;
                totalMovementTime += deltaTime;
            }

            pauseTimer = 0f;
        }
        else
        {
            pauseTimer += deltaTime;
            data.pauseTime += deltaTime;
        }

        previousHandPosition = currentPosition;
    }

    void TrackGestures()
    {
        if (gestureManager == null)
            return;

        GestureManager.Gesture currentGesture =
            gestureManager.CurrentGesture;

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

    public void RegisterArtworkElementCreated(GameObject artworkRoot)
    {
        AdjustArtworkElementCount(artworkRoot, 1);
    }

    public void RegisterArtworkElementRemoved(GameObject artworkRoot)
    {
        AdjustArtworkElementCount(artworkRoot, -1);
    }

    private void AdjustArtworkElementCount(
        GameObject artworkRoot,
        int delta)
    {
        if (isFinalized || data == null || artworkRoot == null)
            return;

        string elementType =
            CompositionAnalyzer.GetElementType(artworkRoot);

        switch (elementType)
        {
            case "water_current":
                data.waterCount =
                    Mathf.Max(0, data.waterCount + delta);
                break;

            case "coral":
                data.coralCount =
                    Mathf.Max(0, data.coralCount + delta);
                break;

            case "fish_school":
                data.fishSchoolCount =
                    Mathf.Max(0, data.fishSchoolCount + delta);
                break;

            case "bubble_burst":
                data.bubbleBurstCount =
                    Mathf.Max(0, data.bubbleBurstCount + delta);
                break;

            case "lantern":
                data.lanternCount =
                    Mathf.Max(0, data.lanternCount + delta);
                break;

            case "light_trail":
                data.lightTrailCount =
                    Mathf.Max(0, data.lightTrailCount + delta);
                break;

            case "petals":
                data.petalsCount =
                    Mathf.Max(0, data.petalsCount + delta);
                break;

            case "fireworks":
                data.fireworksCount =
                    Mathf.Max(0, data.fireworksCount + delta);
                break;
        }
    }

    public void FinalizeStatistics()
    {
        if (isFinalized)
            return;

        EnsureInitialized();

        if (data == null)
        {
            Debug.LogError(
                "ArtworkStatistics: Cannot finalize because data is null."
            );
            return;
        }

        data.creationTime = Mathf.Max(
            0f,
            Time.time - artworkStartTime
        );

        data.averageHandSpeed = totalMovementTime > 0f
            ? data.totalHandDistance / totalMovementTime
            : 0f;

        isFinalized = true;

        Debug.Log("===== ARTWORK STATISTICS =====");
        Debug.Log("Creation Time: " + data.creationTime);
        Debug.Log("Water Currents: " + data.waterCount);
        Debug.Log("Coral: " + data.coralCount);
        Debug.Log("Fish Schools: " + data.fishSchoolCount);
        Debug.Log("Bubble Bursts: " + data.bubbleBurstCount);
        Debug.Log("Lanterns: " + data.lanternCount);
        Debug.Log("Light Trails: " + data.lightTrailCount);
        Debug.Log("Petals: " + data.petalsCount);
        Debug.Log("Fireworks: " + data.fireworksCount);
        Debug.Log("Index Gestures: " + data.indexGestureCount);
        Debug.Log("Open Palm Gestures: " + data.openPalmGestureCount);
        Debug.Log("V Sign Gestures: " + data.vSignGestureCount);
        Debug.Log("Thumb Up Gestures: " + data.thumbUpGestureCount);
        Debug.Log("Total Hand Distance: " + data.totalHandDistance);
        Debug.Log("Average Hand Speed: " + data.averageHandSpeed);
        Debug.Log("Maximum Hand Speed: " + data.maximumHandSpeed);
        Debug.Log("Pause Time: " + data.pauseTime);
        Debug.Log("Hand Tracking Samples: " + data.handTrackingSamples);
        Debug.Log("==============================");
    }

    public void ResumeAfterFailedFinish()
    {
        // Keep the same session and counters; do not bridge the capture pause.
        isFinalized = false;
        hasPreviousPosition = false;
    }
}
