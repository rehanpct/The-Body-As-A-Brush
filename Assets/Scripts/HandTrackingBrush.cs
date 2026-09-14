using System;
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
    public float smoothSpeed = 18f;

    [Header("Tracking Stability")]
    [Tooltip(
        "How long the last valid hand position " +
        "is kept when MediaPipe briefly misses a frame."
    )]
    public float trackingGracePeriod = 0.15f;

    [Tooltip(
        "Maximum movement allowed between updates."
    )]
    public float maxMovementPerFrame = 2f;

    private float targetX;
    private float targetY;

    private bool receivedHand = false;

    private float lastValidHandTime = -999f;

    private Vector3 targetWorldPosition;

    private readonly object trackingLock =
      new object();

    public bool IsHandDetected
    {
        get
        {
            lock (trackingLock)
            {
                return receivedHand;
            }
        }
    }

    public bool IsIndexDrawing =>
        gestureManager != null &&
        gestureManager.CurrentGesture ==
        GestureManager.Gesture.Index;

    void Start()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated +=
              OnHandResult;
        }
        else
        {
            Debug.LogError(
              "HandTrackingBrush: " +
              "HandLandmarkerRunner is not assigned!"
            );
        }

        if (gestureManager == null)
        {
            Debug.LogError(
              "HandTrackingBrush: " +
              "GestureManager is not assigned!"
            );
        }

        if (brush == null)
        {
            Debug.LogError(
              "HandTrackingBrush: " +
              "Brush is not assigned!"
            );
        }
    }

    private void OnHandResult(
      HandLandmarkerResult result
    )
    {
        bool foundHand = false;

        float newX = 0f;
        float newY = 0f;

        if (
          result.handLandmarks != null &&
          result.handLandmarks.Count > 0
        )
        {
            var hand =
              result.handLandmarks[0];

            if (
              hand.landmarks != null &&
              hand.landmarks.Count >= 21
            )
            {
                var indexTip =
                  hand.landmarks[8];

                newX = Mathf.Clamp01(
                  indexTip.x
                );

                newY = Mathf.Clamp01(
                  1f - indexTip.y
                );

                foundHand = true;
            }
        }

        lock (trackingLock)
        {
            if (foundHand)
            {
                targetX = newX;
                targetY = newY;

                receivedHand = true;

                /*
                 * We cannot use Time.time here because
                 * this callback may be running on a
                 * MediaPipe worker thread.
                 */
            }
            else
            {
                receivedHand = false;
            }
        }
    }

    void Update()
    {
        if (brush == null)
            return;

        float x;
        float y;
        bool hasHand;

        lock (trackingLock)
        {
            x = targetX;
            y = targetY;
            hasHand = receivedHand;
        }

        /*
         * Use Unity's main-thread time here.
         */
        if (hasHand)
        {
            lastValidHandTime =
              Time.realtimeSinceStartup;

            CalculateTargetWorldPosition(
              x,
              y
            );

            MoveBrush();
        }
        else
        {
            /*
             * Do NOT immediately remove the hand.
             *
             * MediaPipe can miss a single frame.
             * Keep the brush where it was briefly.
             */
            float timeSinceLastHand =
              Time.realtimeSinceStartup -
              lastValidHandTime;

            if (
              timeSinceLastHand <=
              trackingGracePeriod
            )
            {
                MoveBrush();
            }
        }
    }

    private void CalculateTargetWorldPosition(
      float normalizedX,
      float normalizedY
    )
    {
        Camera mainCamera =
          Camera.main;

        if (mainCamera == null)
            return;

        Vector3 screenPosition =
          new Vector3(
            normalizedX * Screen.width,
            normalizedY * Screen.height,
            10f
          );

        targetWorldPosition =
          mainCamera.ScreenToWorldPoint(
            screenPosition
          );

        targetWorldPosition.z = 0f;
    }

    private void MoveBrush()
    {
        Vector3 currentPosition =
          brush.position;

        Vector3 difference =
          targetWorldPosition -
          currentPosition;

        /*
         * Prevent one bad MediaPipe coordinate
         * from teleporting the brush.
         */
        if (
          difference.magnitude >
          maxMovementPerFrame
        )
        {
            difference =
              difference.normalized *
              maxMovementPerFrame;
        }

        Vector3 limitedTarget =
          currentPosition +
          difference;

        float interpolation =
          1f -
          Mathf.Exp(
            -smoothSpeed *
            Time.deltaTime
          );

        brush.position =
          Vector3.Lerp(
            currentPosition,
            limitedTarget,
            interpolation
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