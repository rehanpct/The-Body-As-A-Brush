using UnityEngine;

public class ThumbUpGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    // =========================================================
    // UNDERWATER
    // =========================================================

    [Header("Bubble - Underwater")]
    public GameObject bubblePrefab;

    [Header("Bubble Burst Settings")]
    public int bubbleCount = 3;

    public float burstRadius = 1.5f;

    // =========================================================
    // TAIWAN
    // =========================================================

    [Header("Fireworks - Taiwan")]
    public GameObject fireworksPrefab;

    [Header("Fireworks Settings")]
    public float fireworksScale = 1f;

    // =========================================================
    // STATE
    // =========================================================

    private bool hasSpawnedForCurrentThumb =
        false;

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "ThumbUpGesture: " +
                "GestureManager is not assigned!"
            );
        }

        if (bubblePrefab == null)
        {
            Debug.LogWarning(
                "ThumbUpGesture: " +
                "Bubble Prefab is not assigned. " +
                "This is okay if using Taiwan theme."
            );
        }

        if (fireworksPrefab == null)
        {
            Debug.LogWarning(
                "ThumbUpGesture: " +
                "Fireworks Prefab is not assigned. " +
                "This is okay if using Underwater theme."
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (gestureManager == null)
            return;

        bool isThumbUp =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.ThumbUp;

        if (isThumbUp)
        {
            if (hasSpawnedForCurrentThumb)
                return;

            SpawnThemeEffect();

            hasSpawnedForCurrentThumb =
                true;

            return;
        }

        hasSpawnedForCurrentThumb =
            false;
    }

    // =========================================================
    // THEME SELECTION
    // =========================================================

    void SpawnThemeEffect()
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "ThumbUpGesture: " +
                "ThemeManager is missing!"
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            SpawnFireworks();
        }
        else
        {
            SpawnBubbleBurst();
        }
    }

    // =========================================================
    // UNDERWATER - BUBBLE BURST
    // =========================================================

    void SpawnBubbleBurst()
    {
        if (bubblePrefab == null)
        {
            Debug.LogError(
                "ThumbUpGesture: " +
                "Bubble Prefab is not assigned!"
            );

            return;
        }

        HandTrackingBrush brush =
            FindFirstObjectByType<
                HandTrackingBrush>();

        if (brush == null ||
            brush.brush == null)
        {
            return;
        }

        Vector3 centerPosition =
            brush.brush.position;

        centerPosition.z = 0f;

        // =====================================================
        // CREATE BURST ROOT
        // =====================================================

        GameObject bubbleBurst =
            new GameObject(
                "BubbleBurst"
            );

        bubbleBurst.transform.position =
            centerPosition;

        bubbleBurst.transform.rotation =
            Quaternion.identity;

        bubbleBurst.tag =
            "ArtworkElement";

        // =====================================================
        // CREATE BUBBLES
        // =====================================================

        for (int i = 0;
             i < bubbleCount;
             i++)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle *
                burstRadius;

            Vector3 spawnPosition =
                centerPosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            GameObject bubble =
                Instantiate(
                    bubblePrefab,
                    spawnPosition,
                    Quaternion.identity,
                    bubbleBurst.transform
                );

            // Only the root is selectable.
            bubble.tag =
                "Untagged";
        }

        // =====================================================
        // STATISTICS
        // =====================================================

        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance
                .data.bubbleBurstCount++;
        }

        // =====================================================
        // UNDO
        // =====================================================

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(
                    bubbleBurst
                );
        }

        Debug.Log(
            "🫧 Bubble burst created: " +
            bubbleCount +
            " bubbles."
        );
    }

    // =========================================================
    // TAIWAN - FIREWORKS
    // =========================================================

    void SpawnFireworks()
    {
        if (fireworksPrefab == null)
        {
            Debug.LogError(
                "ThumbUpGesture: " +
                "Fireworks Prefab is not assigned!"
            );

            return;
        }

        HandTrackingBrush brush =
            FindFirstObjectByType<
                HandTrackingBrush>();

        if (brush == null ||
            brush.brush == null)
        {
            return;
        }

        Vector3 spawnPosition =
            brush.brush.position;

        spawnPosition.z = 0f;

        GameObject fireworks =
            Instantiate(
                fireworksPrefab,
                spawnPosition,
                Quaternion.identity
            );

        fireworks.name =
            "FireworksBurst";

        fireworks.tag =
            "ArtworkElement";

        fireworks.transform.localScale *=
            fireworksScale;

        // =====================================================
        // UNDO
        // =====================================================

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(
                    fireworks
                );
        }

        Debug.Log(
            "🎆 Taiwan fireworks burst created."
        );
    }
}