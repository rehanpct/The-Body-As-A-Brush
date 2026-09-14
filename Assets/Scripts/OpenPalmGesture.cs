using UnityEngine;

public class OpenPalmGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Coral")]
    public GameObject elementPrefab;

    private bool hasSpawnedForCurrentPalm = false;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "OpenPalmGesture: GestureManager is not assigned!"
            );
        }
    }

    void Update()
    {
        if (gestureManager == null)
            return;

        bool isOpenPalm =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.OpenPalm;

        // Open palm detected
        if (isOpenPalm)
        {
            // Already spawned for this gesture
            if (hasSpawnedForCurrentPalm)
                return;

            SpawnCoral();

            hasSpawnedForCurrentPalm = true;

            return;
        }

        // Hand changed to Neutral/another gesture.
        // Allow the next Open Palm to trigger.
        hasSpawnedForCurrentPalm = false;
    }

    void SpawnCoral()
    {
        if (elementPrefab == null)
        {
            Debug.LogError(
                "OpenPalmGesture: Coral prefab is not assigned!"
            );

            return;
        }

        HandTrackingBrush brush =
            FindFirstObjectByType<HandTrackingBrush>();

        if (brush == null || brush.brush == null)
            return;

        Vector3 spawnPosition =
            brush.brush.position;

        spawnPosition.z = 0f;

        Instantiate(
            elementPrefab,
            spawnPosition,
            Quaternion.identity
        );
        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance.data.coralCount++;
        }
    }
}