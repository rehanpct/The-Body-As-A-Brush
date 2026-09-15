using UnityEngine;

public class ArtworkInteractionManager : MonoBehaviour
{
    [Header("References")]
    public GestureManager gestureManager;
    public HandTrackingBrush handTrackingBrush;

    [Header("Selection")]
    public float selectionRadius = 1.2f;

    [Header("Movement")]
    public float moveSmoothSpeed = 20f;

    [Header("Two-Hand Scaling")]
    public float minimumScale = 0.15f;
    public float maximumScale = 2.5f;
    public float scaleSensitivity = 4f;

    private GameObject selectedObject;

    private bool wasPinching = false;
    private bool wasTwoHandScaling = false;

    private float previousTwoHandDistance = 0f;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "ArtworkInteractionManager: " +
                "GestureManager is not assigned!"
            );
        }

        if (handTrackingBrush == null)
        {
            Debug.LogError(
                "ArtworkInteractionManager: " +
                "HandTrackingBrush is not assigned!"
            );
        }
    }

    void Update()
    {
        if (gestureManager == null ||
            handTrackingBrush == null ||
            handTrackingBrush.brush == null)
        {
            return;
        }

        HandleUndo();
        HandlePinchMovement();
        HandleTwoHandScaling();
    }

    // =========================================================
    // UNDO
    // =========================================================

    void HandleUndo()
    {
        if (!gestureManager.ConsumeUndoTrigger())
            return;

        if (ArtworkActionHistory.Instance == null)
        {
            Debug.LogError(
                "ArtworkActionHistory is missing."
            );

            return;
        }

        ArtworkActionHistory.Instance
            .UndoLastAction();

        selectedObject = null;

        wasPinching = false;
        wasTwoHandScaling = false;
    }

    // =========================================================
    // ONE-HAND PINCH MOVEMENT
    // =========================================================

    void HandlePinchMovement()
    {
        bool isPinching =
            gestureManager.IsPinching;

        Vector3 handPosition =
            handTrackingBrush.brush.position;

        // Don't perform one-hand movement
        // while two hands are being used.
        if (gestureManager.IsTwoHandPinching)
        {
            wasPinching = false;
            return;
        }

        // -----------------------------------------------------
        // PINCH STARTED
        // -----------------------------------------------------

        if (isPinching && !wasPinching)
        {
            selectedObject =
                FindNearestArtwork(
                    handPosition
                );

            if (selectedObject != null)
            {
                // Stop animation while grabbing.
                SetArtworkMovement(
                    selectedObject,
                    false
                );

                Debug.Log(
                    "🤏 Grabbed artwork: " +
                    selectedObject.name
                );
            }
            else
            {
                Debug.Log(
                    "🤏 No artwork near hand."
                );
            }
        }

        // -----------------------------------------------------
        // MOVE ARTWORK
        // -----------------------------------------------------

        if (isPinching &&
            selectedObject != null)
        {
            selectedObject.transform.position =
                Vector3.Lerp(
                    selectedObject.transform.position,
                    handPosition,
                    moveSmoothSpeed *
                    Time.deltaTime
                );
        }

        // -----------------------------------------------------
        // PINCH RELEASED
        // -----------------------------------------------------

        if (!isPinching && wasPinching)
        {
            if (selectedObject != null)
            {
                Debug.Log(
                    "🤏 Released artwork: " +
                    selectedObject.name
                );

                // Resume animation after release.
                SetArtworkMovement(
                    selectedObject,
                    true
                );
            }

            wasPinching = false;
        }
        else
        {
            wasPinching =
                isPinching;
        }
    }

    // =========================================================
    // TWO-HAND SCALING
    // =========================================================

    void HandleTwoHandScaling()
    {
        bool scaling =
            gestureManager.IsTwoHandPinching;

        // -----------------------------------------------------
        // SCALING STARTED
        // -----------------------------------------------------

        if (scaling && !wasTwoHandScaling)
        {
            if (selectedObject != null)
            {
                previousTwoHandDistance =
                    gestureManager.TwoHandDistance;

                // Stop animation while scaling.
                SetArtworkMovement(
                    selectedObject,
                    false
                );

                Debug.Log(
                    "🤏🤏 Scaling: " +
                    selectedObject.name
                );
            }
        }

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        if (scaling &&
            selectedObject != null)
        {
            float currentDistance =
                gestureManager.TwoHandDistance;

            float difference =
                currentDistance -
                previousTwoHandDistance;

            if (Mathf.Abs(difference) >
                0.001f)
            {
                float scaleChange =
                    difference *
                    scaleSensitivity;

                Vector3 currentScale =
                    selectedObject.transform.localScale;

                float newScale =
                    currentScale.x +
                    scaleChange;

                newScale =
                    Mathf.Clamp(
                        newScale,
                        minimumScale,
                        maximumScale
                    );

                selectedObject.transform.localScale =
                    new Vector3(
                        newScale,
                        newScale,
                        currentScale.z
                    );

                previousTwoHandDistance =
                    currentDistance;
            }
        }

        // -----------------------------------------------------
        // SCALING RELEASED
        // -----------------------------------------------------

        if (!scaling &&
            wasTwoHandScaling)
        {
            if (selectedObject != null)
            {
                SetArtworkMovement(
                    selectedObject,
                    true
                );

                Debug.Log(
                    "🤏🤏 Scaling released."
                );
            }
        }

        wasTwoHandScaling =
            scaling;
    }

    // =========================================================
    // FIND NEAREST ARTWORK
    // =========================================================

    GameObject FindNearestArtwork(
        Vector3 handPosition)
    {
        GameObject[] artworkObjects =
            GameObject.FindGameObjectsWithTag(
                "ArtworkElement"
            );

        GameObject nearestObject =
            null;

        float nearestDistance =
            selectionRadius;

        foreach (GameObject obj
                 in artworkObjects)
        {
            if (obj == null)
                continue;

            float distance =
                Vector3.Distance(
                    handPosition,
                    obj.transform.position
                );

            if (distance <
                nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestObject =
                    obj;
            }
        }

        return nearestObject;
    }

    // =========================================================
    // ENABLE / DISABLE ARTWORK ANIMATION
    // =========================================================

    void SetArtworkMovement(
        GameObject artwork,
        bool enabled)
    {
        if (artwork == null)
            return;

        // -----------------------------------------------------
        // FISH SCHOOL
        // -----------------------------------------------------

        FishSchoolMovement fishSchool =
            artwork.GetComponent<
                FishSchoolMovement>();

        if (fishSchool != null)
        {
            fishSchool.enabled =
                enabled;
        }

        // -----------------------------------------------------
        // INDIVIDUAL FISH
        // -----------------------------------------------------

        FishMovement[] fishMovements =
            artwork.GetComponentsInChildren<
                FishMovement>(
                    true
                );

        foreach (FishMovement movement
                 in fishMovements)
        {
            movement.enabled =
                enabled;
        }

        // -----------------------------------------------------
        // BUBBLES
        // -----------------------------------------------------

        BubbleMovement[] bubbleMovements =
            artwork.GetComponentsInChildren<
                BubbleMovement>(
                    true
                );

        foreach (BubbleMovement movement
                 in bubbleMovements)
        {
            movement.enabled =
                enabled;
        }

        // -----------------------------------------------------
        // CORAL
        // -----------------------------------------------------

        CoralSway[] coralSways =
            artwork.GetComponentsInChildren<
                CoralSway>(
                    true
                );

        foreach (CoralSway sway
                 in coralSways)
        {
            sway.enabled =
                enabled;
        }
    }
}