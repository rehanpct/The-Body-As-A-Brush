using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class GestureManager : MonoBehaviour
{
    public enum Gesture
    {
        Neutral,
        Index,
        OpenPalm,
        VSign,
        Fist
    }

    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Current Gesture")]
    [SerializeField]
    private Gesture currentGesture = Gesture.Neutral;

    public Gesture CurrentGesture => currentGesture;

    [Header("Stability")]
    public float gestureHoldTime = 0.20f;

    // Data received from MediaPipe thread
    private Gesture detectedGesture = Gesture.Neutral;

    // Main-thread state
    private Gesture candidateGesture = Gesture.Neutral;
    private float candidateStartTime = 0f;

    private readonly object gestureLock = new object();

    void Start()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated += OnHandResult;
        }
        else
        {
            Debug.LogError(
                "GestureManager: HandLandmarkerRunner is not assigned!"
            );
        }
    }

    // IMPORTANT:
    // This callback may run on a background thread.
    // Do NOT use Unity API here.
    void OnHandResult(HandLandmarkerResult result)
    {
        Gesture newGesture = Gesture.Neutral;

        if (result.handLandmarks != null &&
            result.handLandmarks.Count > 0)
        {
            var hand = result.handLandmarks[0];

            if (hand.landmarks != null &&
                hand.landmarks.Count >= 21)
            {
                newGesture = DetectGesture(hand);
            }
        }

        lock (gestureLock)
        {
            detectedGesture = newGesture;
        }
    }

    // Everything involving Time.time happens here,
    // on Unity's main thread.
    void Update()
    {
        Gesture newDetectedGesture;

        lock (gestureLock)
        {
            newDetectedGesture = detectedGesture;
        }

        // Same candidate
        if (newDetectedGesture == candidateGesture)
        {
            if (Time.time - candidateStartTime >= gestureHoldTime)
            {
                currentGesture = candidateGesture;
            }

            return;
        }

        // New candidate
        candidateGesture = newDetectedGesture;
        candidateStartTime = Time.time;
    }

    Gesture DetectGesture(
    Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
{
    // Finger state based on tip vs PIP joint.
    // This is more reliable for our front-facing webcam setup.

    bool indexOpen =
        hand.landmarks[8].y <
        hand.landmarks[6].y - 0.02f;

    bool middleOpen =
        hand.landmarks[12].y <
        hand.landmarks[10].y - 0.02f;

    bool ringOpen =
        hand.landmarks[16].y <
        hand.landmarks[14].y - 0.02f;

    bool pinkyOpen =
        hand.landmarks[20].y <
        hand.landmarks[18].y - 0.02f;

    bool indexClosed =
        hand.landmarks[8].y >
        hand.landmarks[6].y + 0.01f;

    bool middleClosed =
        hand.landmarks[12].y >
        hand.landmarks[10].y + 0.01f;

    bool ringClosed =
        hand.landmarks[16].y >
        hand.landmarks[14].y + 0.01f;

    bool pinkyClosed =
        hand.landmarks[20].y >
        hand.landmarks[18].y + 0.01f;


    // -------------------------
    // FIST
    // -------------------------
    if (indexClosed &&
        middleClosed &&
        ringClosed &&
        pinkyClosed)
    {
        return Gesture.Fist;
    }


    // -------------------------
    // V SIGN
    // -------------------------
    // Index + middle clearly open.
    // Ring + pinky clearly closed.
    if (indexOpen &&
        middleOpen &&
        ringClosed &&
        pinkyClosed)
    {
        return Gesture.VSign;
    }


    // -------------------------
    // OPEN PALM
    // -------------------------
    if (indexOpen &&
        middleOpen &&
        ringOpen &&
        pinkyOpen)
    {
        return Gesture.OpenPalm;
    }


    // -------------------------
    // INDEX FINGER
    // -------------------------
    // ONLY index open.
    // All other fingers must be clearly closed.
    if (indexOpen &&
        middleClosed &&
        ringClosed &&
        pinkyClosed)
    {
        return Gesture.Index;
    }


    // -------------------------
    // EVERYTHING ELSE
    // -------------------------
    return Gesture.Neutral;
}
}