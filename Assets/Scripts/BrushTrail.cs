using UnityEngine;

public class BrushTrail : MonoBehaviour
{
    [Header("Hand Tracking")]
    public HandTrackingBrush handTrackingBrush;

    [Header("Water Current - Underwater")]
    public GameObject waterCurrentPrefab;

    [Header("Golden Light Trail - Taiwan")]
    public GameObject goldenLightTrailPrefab;

    [Header("Spawn Settings")]
    public float randomRotation = 8f;
    public float randomScaleMin = 0.85f;
    public float randomScaleMax = 1.15f;

    private bool hasSpawnedForCurrentIndex =
        false;

    void Update()
    {
        if (handTrackingBrush == null)
            return;

        bool isIndex =
            handTrackingBrush.IsIndexDrawing;

        if (isIndex)
        {
            if (hasSpawnedForCurrentIndex)
                return;

            SpawnThemeTrail(
                handTrackingBrush.brush.position
            );

            hasSpawnedForCurrentIndex =
                true;

            return;
        }

        hasSpawnedForCurrentIndex =
            false;
    }

    void SpawnThemeTrail(
        Vector3 position)
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "BrushTrail: ThemeManager " +
                "is missing!"
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            SpawnGoldenLightTrail(position);
        }
        else
        {
            SpawnWaterCurrent(position);
        }
    }

    // =====================================================
    // UNDERWATER
    // =====================================================

    void SpawnWaterCurrent(
        Vector3 position)
    {
        if (waterCurrentPrefab == null)
        {
            Debug.LogError(
                "BrushTrail: Water Current Prefab " +
                "is not assigned!"
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

        current.tag =
            "ArtworkElement";

        float scale =
            Random.Range(
                randomScaleMin,
                randomScaleMax
            );

        current.transform.localScale *=
            scale;

        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance
                .data.waterCount++;
        }

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(current);
        }

        Debug.Log(
            "🌊 Water current created."
        );
    }

    // =====================================================
    // TAIWAN
    // =====================================================

    void SpawnGoldenLightTrail(
        Vector3 position)
    {
        if (goldenLightTrailPrefab == null)
        {
            Debug.LogError(
                "BrushTrail: Golden Light Trail " +
                "Prefab is not assigned!"
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

        GameObject trail =
            Instantiate(
                goldenLightTrailPrefab,
                position,
                rotationQuaternion
            );

        trail.tag =
            "ArtworkElement";

        float scale =
            Random.Range(
                randomScaleMin,
                randomScaleMax
            );

        trail.transform.localScale *=
            scale;

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(trail);
        }

        Debug.Log(
            "✨ Golden light trail created."
        );
    }
}