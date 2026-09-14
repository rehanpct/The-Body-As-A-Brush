using UnityEngine;

public class VSignGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Fish")]
    public GameObject elementPrefab;

    [Header("School Settings")]
    public int elementCount = 5;

    public float spawnRadius = 1.0f;

    private bool hasSpawnedForCurrentV = false;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "VSignGesture: GestureManager is not assigned!"
            );
        }
    }

    void Update()
    {
        if (gestureManager == null)
            return;

        bool isVSign =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.VSign;

        // --------------------------------
        // V SIGN DETECTED
        // --------------------------------
        if (isVSign)
        {
            // Already spawned for this V sign
            if (hasSpawnedForCurrentV)
                return;

            SpawnFishSchool();

            hasSpawnedForCurrentV = true;

            return;
        }

        // --------------------------------
        // V SIGN RELEASED
        // --------------------------------
        // Allows the next V sign to trigger.
        hasSpawnedForCurrentV = false;
    }

    void SpawnFishSchool()
    {
        if (elementPrefab == null)
        {
            Debug.LogError(
                "VSignGesture: Fish prefab is not assigned!"
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

        // Fish formation
        Vector2[] formation =
        {
            new Vector2(-0.8f,  0.2f),
            new Vector2( 0.0f,  0.5f),
            new Vector2( 0.8f,  0.2f),
            new Vector2(-0.4f, -0.3f),
            new Vector2( 0.4f, -0.3f)
        };

        int count =
            Mathf.Min(
                elementCount,
                formation.Length
            );

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition =
                centerPosition +
                new Vector3(
                    formation[i].x,
                    formation[i].y,
                    0f
                );

            Instantiate(
                elementPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }

        // Count ONE fish school action
        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance.data.fishSchoolCount++;
        }
    }
}