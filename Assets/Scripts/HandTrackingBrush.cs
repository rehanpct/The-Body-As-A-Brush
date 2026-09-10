using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class HandTrackingBrush : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Brush")]
    public Transform brush;

    [Header("Movement")]
    public float smoothSpeed = 15f;

    private float targetX;
    private float targetY;

    private bool handDetected = false;

    public bool IsHandDetected => handDetected;

    // Water drawing is allowed ONLY during Index gesture.
    public bool IsIndexDrawing =>
        gestureManager != null &&
        gestureManager.CurrentGesture ==
        GestureManager.Gesture.Index;

    void Start()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated += OnHandResult;
        }
        else
        {
            Debug.LogError(
                "HandTrackingBrush: HandLandmarkerRunner is not assigned!"
            );
        }

        if (brush == null)
        {
            Debug.LogError(
                "HandTrackingBrush: Brush is not assigned!"
            );
        }

        if (gestureManager == null)
        {
            Debug.LogError(
                "HandTrackingBrush: GestureManager is not assigned!"
            );
        }
    }

    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null ||
            result.handLandmarks.Count == 0)
        {
            handDetected = false;
            return;
        }

        var hand = result.handLandmarks[0];

        if (hand.landmarks == null ||
            hand.landmarks.Count < 21)
        {
            handDetected = false;
            return;
        }

        var indexTip = hand.landmarks[8];

        targetX = indexTip.x;
        targetY = 1f - indexTip.y;

        handDetected = true;
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
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );

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
            handLandmarkerRunner.OnResultUpdated -=
                OnHandResult;
        }
    }
}