using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class HandTrackingBrush : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Brush")]
    public Transform brush;

    [Header("Movement")]
    public float smoothSpeed = 15f;

    private float targetX;
    private float targetY;

    private bool handDetected = false;
    private bool indexDrawingPose = false;

    public bool IsHandDetected => handDetected;

    // This will be used by BrushTrail.
    public bool IsIndexDrawing => indexDrawingPose;

    void Start()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated += OnHandResult;
        }
        else
        {
            Debug.LogError("HandLandmarkerRunner is not assigned!");
        }

        if (brush == null)
        {
            Debug.LogError("Brush is not assigned!");
        }
    }

    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null ||
            result.handLandmarks.Count == 0)
        {
            handDetected = false;
            indexDrawingPose = false;
            return;
        }

        var hand = result.handLandmarks[0];

        if (hand.landmarks == null ||
            hand.landmarks.Count < 21)
        {
            handDetected = false;
            indexDrawingPose = false;
            return;
        }

        // Index fingertip = landmark 8.
        var indexTip = hand.landmarks[8];

        targetX = indexTip.x;
        targetY = 1f - indexTip.y;

        handDetected = true;

        // Check whether index finger is extended
        // while the other three fingers are folded.
        indexDrawingPose = IsIndexFingerPose(hand);
    }

    bool IsIndexFingerPose(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        bool indexOpen =
            hand.landmarks[8].y <
            hand.landmarks[6].y;

        bool middleClosed =
            hand.landmarks[12].y >
            hand.landmarks[10].y;

        bool ringClosed =
            hand.landmarks[16].y >
            hand.landmarks[14].y;

        bool pinkyClosed =
            hand.landmarks[20].y >
            hand.landmarks[18].y;

        return indexOpen &&
               middleClosed &&
               ringClosed &&
               pinkyClosed;
    }

    void Update()
    {
        if (!handDetected || brush == null)
            return;

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector3 screenPosition = new Vector3(
            targetX * Screen.width,
            targetY * Screen.height,
            10f
        );

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(screenPosition);

        worldPosition.z = 0f;

        brush.position = Vector3.Lerp(
            brush.position,
            worldPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    void OnDestroy()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated -= OnHandResult;
        }
    }
}