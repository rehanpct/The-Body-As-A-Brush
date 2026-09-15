using UnityEngine;

public class OpenPalmGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Coral - Underwater")]
    public GameObject elementPrefab;

    [Header("Lantern - Taiwan")]
    public GameObject lanternPrefab;

    private bool hasSpawnedForCurrentPalm =
        false;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "OpenPalmGesture: " +
                "GestureManager is not assigned!"
            );
        }

        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "OpenPalmGesture: " +
                "ThemeManager is not present in the scene!"
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

        if (isOpenPalm)
        {
            if (hasSpawnedForCurrentPalm)
                return;

            SpawnThemeElement();

            hasSpawnedForCurrentPalm =
                true;

            return;
        }

        hasSpawnedForCurrentPalm =
            false;
    }

    void SpawnThemeElement()
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "OpenPalmGesture: " +
                "ThemeManager is missing."
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            SpawnLantern();
        }
        else
        {
            SpawnCoral();
        }
    }

    void SpawnCoral()
    {
        if (elementPrefab == null)
        {
            Debug.LogError(
                "OpenPalmGesture: " +
                "Coral prefab is not assigned!"
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

        GameObject coral =
            Instantiate(
                elementPrefab,
                spawnPosition,
                Quaternion.identity
            );

        coral.tag =
            "ArtworkElement";

        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance
                .data.coralCount++;
        }

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(coral);
        }

        Debug.Log(
            "🪸 Coral created."
        );
    }

    void SpawnLantern()
    {
        if (lanternPrefab == null)
        {
            Debug.LogError(
                "OpenPalmGesture: " +
                "Lantern prefab is not assigned!"
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

        GameObject lantern =
            Instantiate(
                lanternPrefab,
                spawnPosition,
                Quaternion.identity
            );

        lantern.tag =
            "ArtworkElement";

        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance
                .data.lanternCount++;
        }

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(lantern);
        }

        Debug.Log(
            "🏮 Lantern created."
        );
    }
}