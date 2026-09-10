using UnityEngine;

public class VSignGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Fish")]
    public GameObject elementPrefab;

    [Header("School Settings")]
    public int elementCount = 5;
    public float horizontalSpacing = 0.8f;
    public float verticalSpacing = 0.5f;
    public float spawnCooldown = 1.5f;

    private float nextSpawnTime = 0f;
    private bool wasVSign = false;

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

        // Trigger only when entering V Sign.
        if (isVSign && !wasVSign)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnFishSchool();

                nextSpawnTime =
                    Time.time + spawnCooldown;
            }
        }

        wasVSign = isVSign;
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

        Vector2[] formation =
        {
            new Vector2(-horizontalSpacing, 0.3f),
            new Vector2(0f, 0.5f),
            new Vector2(horizontalSpacing, 0.3f),
            new Vector2(-0.5f, -0.3f),
            new Vector2(0.5f, -0.3f)
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
    }
}