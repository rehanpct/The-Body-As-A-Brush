using UnityEngine;

public class VSignGesture : MonoBehaviour
{
    [Header("References")]
    public GestureManager gestureManager;
    public HandTrackingBrush handTrackingBrush;

    // =========================================================
    // UNDERWATER
    // =========================================================

    [Header("Fish - Underwater")]
    public GameObject fishPrefab;

    [Header("Fish School")]
    public int fishCount = 5;

    [Tooltip("Distance between fish in the school.")]
    public float fishSpacing = 0.8f;

    [Header("Fish Movement")]
    public float fishSpeed = 0.5f;

    [Tooltip(
        "Fish disappear when the school reaches this LEFT-side X position."
    )]
    public float leftBoundary = -8f;

    [Tooltip(
        "Fish reappear at this RIGHT-side X position."
    )]
    public float rightSpawnPosition = 9f;

    // =========================================================
    // TAIWAN
    // =========================================================

    [Header("Taiwan - Floating Petals")]
    public GameObject floatingPetalsPrefab;

    [Header("Taiwan Petal Group Settings")]
    public int petalGroupCount = 3;

    [Tooltip("Spacing between Taiwan petal elements.")]
    public float petalSpacing = 0.7f;

    [Tooltip("Random rotation applied to Taiwan petal elements.")]
    public float taiwanRandomRotation = 12f;

    [Tooltip("Minimum random scale.")]
    public float taiwanRandomScaleMin = 0.8f;

    [Tooltip("Maximum random scale.")]
    public float taiwanRandomScaleMax = 1.2f;

    // =========================================================
    // STATE
    // =========================================================

    private bool wasVSign = false;

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        Debug.Log(
            "VSignGesture STARTED"
        );

        if (gestureManager == null)
        {
            Debug.LogError(
                "VSignGesture: GestureManager is NOT assigned!"
            );
        }

        if (handTrackingBrush == null)
        {
            Debug.LogError(
                "VSignGesture: HandTrackingBrush is NOT assigned!"
            );
        }

        if (fishPrefab == null)
        {
            Debug.LogWarning(
                "VSignGesture: Fish Prefab is not assigned. " +
                "This is okay if using Taiwan theme."
            );
        }

        if (floatingPetalsPrefab == null)
        {
            Debug.LogWarning(
                "VSignGesture: Floating Petals Prefab " +
                "is not assigned. " +
                "This is okay if using Underwater theme."
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (gestureManager == null ||
            handTrackingBrush == null ||
            handTrackingBrush.brush == null)
        {
            return;
        }

        bool isVSign =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.VSign;

        // =====================================================
        // V SIGN START
        // =====================================================

        if (isVSign && !wasVSign)
        {
            Debug.Log(
                "✌ V SIGN DETECTED!"
            );

            SpawnThemeElement();
        }

        wasVSign =
            isVSign;
    }

    // =========================================================
    // THEME SELECTION
    // =========================================================

    void SpawnThemeElement()
    {
        if (ThemeManager.Instance == null)
        {
            Debug.LogError(
                "VSignGesture: ThemeManager is missing!"
            );

            return;
        }

        if (ThemeManager.Instance.IsTaiwan())
        {
            SpawnTaiwanPetalGroup();
        }
        else
        {
            SpawnFishSchool();
        }
    }

    // =========================================================
    // UNDERWATER - FISH SCHOOL
    // =========================================================

    void SpawnFishSchool()
    {
        if (fishPrefab == null)
        {
            Debug.LogError(
                "VSignGesture: Fish Prefab is not assigned!"
            );

            return;
        }

        Vector3 centerPosition =
            handTrackingBrush.brush.position;

        // =====================================================
        // CREATE SCHOOL ROOT
        // =====================================================

        GameObject fishSchool =
            new GameObject(
                "FishSchool"
            );

        fishSchool.transform.position =
            centerPosition;

        fishSchool.transform.rotation =
            Quaternion.identity;

        // Entire school is one artwork object.
        fishSchool.tag =
            "ArtworkElement";

        // =====================================================
        // ADD SCHOOL MOVEMENT
        // =====================================================

        FishSchoolMovement movement =
            fishSchool.AddComponent<
                FishSchoolMovement>();

        movement.speed =
            fishSpeed;

        movement.leftBoundary =
            leftBoundary;

        movement.rightSpawnPosition =
            rightSpawnPosition;

        // =====================================================
        // CREATE FISH
        // =====================================================

        for (int i = 0;
             i < fishCount;
             i++)
        {
            Vector3 localOffset =
                GetFishFormationPosition(
                    i,
                    fishCount
                );

            Vector3 spawnPosition =
                centerPosition +
                localOffset;

            GameObject fish =
                Instantiate(
                    fishPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    fishSchool.transform
                );

            // Individual fish are not selectable.
            fish.tag =
                "Untagged";

            // Disable old individual movement.
            FishMovement oldMovement =
                fish.GetComponent<
                    FishMovement>();

            if (oldMovement != null)
            {
                oldMovement.enabled =
                    false;
            }

            // Fish should face their correct direction.
            SpriteRenderer fishRenderer =
                fish.GetComponent<
                    SpriteRenderer>();

            if (fishRenderer != null)
            {
                fishRenderer.flipX =
                    false;
            }

            // Keep original prefab scale.
            fish.transform.localScale =
                fishPrefab.transform.localScale;
        }

        // =====================================================
        // REGISTER FOR UNDO
        // =====================================================

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(
                    fishSchool
                );
        }

        if (ArtworkStatistics.Instance != null)
        {
            ArtworkStatistics.Instance
                .data.fishSchoolCount++;
        }

        Debug.Log(
            "🐟 Fish school created: " +
            fishCount +
            " fish."
        );
    }

    // =========================================================
    // TAIWAN - FLOATING PETAL GROUP
    // =========================================================

    void SpawnTaiwanPetalGroup()
    {
        if (floatingPetalsPrefab == null)
        {
            Debug.LogError(
                "VSignGesture: Floating Petals Prefab " +
                "is not assigned!"
            );

            return;
        }

        Vector3 centerPosition =
            handTrackingBrush.brush.position;

        GameObject petalGroup =
            new GameObject(
                "TaiwanPetalGroup"
            );

        petalGroup.transform.position =
            centerPosition;

        petalGroup.transform.rotation =
            Quaternion.identity;

        // Entire group is one artwork action.
        petalGroup.tag =
            "ArtworkElement";

        // =====================================================
        // CREATE PETAL ELEMENTS
        // =====================================================

        for (int i = 0;
             i < petalGroupCount;
             i++)
        {
            float xOffset =
                (i -
                (petalGroupCount - 1) / 2f) *
                petalSpacing;

            float yOffset =
                Mathf.Sin(i * 1.4f) *
                0.25f;

            Vector3 localPosition =
                new Vector3(
                    xOffset,
                    yOffset,
                    0f
                );

            GameObject petal =
                Instantiate(
                    floatingPetalsPrefab,
                    petalGroup.transform
                );

            petal.name =
                "FloatingPetal_" + i;

            petal.transform.localPosition =
                localPosition;

            petal.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(
                        -taiwanRandomRotation,
                        taiwanRandomRotation
                    )
                );

            float scale =
                Random.Range(
                    taiwanRandomScaleMin,
                    taiwanRandomScaleMax
                );

            petal.transform.localScale =
                Vector3.one * scale;

            // Only the group is selectable.
            petal.tag =
                "Untagged";
        }

        // =====================================================
        // REGISTER FOR UNDO
        // =====================================================

        if (ArtworkActionHistory.Instance != null)
        {
            ArtworkActionHistory.Instance
                .RegisterAction(
                    petalGroup
                );
        }

        Debug.Log(
            "🌸 Taiwan floating petal group created: " +
            petalGroupCount +
            " petals."
        );
    }

    // =========================================================
    // FISH FORMATION
    // =========================================================

    Vector3 GetFishFormationPosition(
        int index,
        int total
    )
    {
        // =====================================================
        // FIVE FISH FORMATION
        // =====================================================

        if (total == 5)
        {
            Vector3[] formation =
            {
                new Vector3(
                    -fishSpacing,
                    0.25f,
                    0f
                ),

                new Vector3(
                    0f,
                    0.65f,
                    0f
                ),

                new Vector3(
                    fishSpacing,
                    0.25f,
                    0f
                ),

                new Vector3(
                    -0.45f,
                    -0.45f,
                    0f
                ),

                new Vector3(
                    0.55f,
                    -0.45f,
                    0f
                )
            };

            return formation[index];
        }

        // =====================================================
        // GENERIC FORMATION
        // =====================================================

        float spacing =
            fishSpacing;

        float totalWidth =
            (total - 1) *
            spacing;

        float x =
            -totalWidth / 2f +
            index * spacing;

        float wave =
            Mathf.Sin(
                index * 1.5f
            ) * 0.3f;

        return new Vector3(
            x,
            wave,
            0f
        );
    }
}