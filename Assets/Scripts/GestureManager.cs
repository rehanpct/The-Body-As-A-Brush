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
        ThumbUp
    }

    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Current Gesture")]
    [SerializeField]
    private Gesture currentGesture = Gesture.Neutral;

    public Gesture CurrentGesture => currentGesture;

    [Header("Stability")]
    public float gestureHoldTime = 0.20f;

    private Gesture detectedGesture = Gesture.Neutral;

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


    // =========================================================
    // MEDIAPIPE CALLBACK
    // =========================================================

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


    // =========================================================
    // MAIN UNITY THREAD
    // =========================================================

    void Update()
    {
        Gesture newDetectedGesture;

        lock (gestureLock)
        {
            newDetectedGesture = detectedGesture;
        }

        if (newDetectedGesture == candidateGesture)
        {
            if (Time.time - candidateStartTime >= gestureHoldTime)
            {
                currentGesture = candidateGesture;
            }

            return;
        }

        candidateGesture = newDetectedGesture;
        candidateStartTime = Time.time;
    }


    // =========================================================
    // DISTANCE HELPER
    // =========================================================

    float Distance(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark a,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;

        return Mathf.Sqrt(
            dx * dx +
            dy * dy
        );
    }


    // =========================================================
    // GESTURE DETECTION
    // =========================================================

    Gesture DetectGesture(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        // -----------------------------------------------------
        // IMPORTANT LANDMARKS
        // -----------------------------------------------------
        //
        // 0  = Wrist
        //
        // Index:
        // 6  = PIP
        // 8  = Tip
        //
        // Middle:
        // 10 = PIP
        // 12 = Tip
        //
        // Ring:
        // 14 = PIP
        // 16 = Tip
        //
        // Pinky:
        // 18 = PIP
        // 20 = Tip
        //
        // Thumb:
        // 2 = MCP
        // 3 = IP
        // 4 = Tip
        // -----------------------------------------------------


        var wrist = hand.landmarks[0];


        // =====================================================
        // FINGER OPEN/CLOSED
        // =====================================================

        float openMargin = 0.02f;

        bool indexOpen =
            hand.landmarks[8].y <
            hand.landmarks[6].y - openMargin;

        bool middleOpen =
            hand.landmarks[12].y <
            hand.landmarks[10].y - openMargin;

        bool ringOpen =
            hand.landmarks[16].y <
            hand.landmarks[14].y - openMargin;

        bool pinkyOpen =
            hand.landmarks[20].y <
            hand.landmarks[18].y - openMargin;


        // =====================================================
        // CLOSED FINGERS
        // =====================================================
        //
        // Instead of relying only on Y position,
        // compare fingertip distance to the wrist.
        // A curled finger brings its tip closer to the palm.
        // =====================================================

        bool indexClosed =
            Distance(hand.landmarks[8], wrist) <
            Distance(hand.landmarks[6], wrist) * 1.10f;

        bool middleClosed =
            Distance(hand.landmarks[12], wrist) <
            Distance(hand.landmarks[10], wrist) * 1.10f;

        bool ringClosed =
            Distance(hand.landmarks[16], wrist) <
            Distance(hand.landmarks[14], wrist) * 1.10f;

        bool pinkyClosed =
            Distance(hand.landmarks[20], wrist) <
            Distance(hand.landmarks[18], wrist) * 1.10f;


        // =====================================================
        // 👍 THUMB UP
        // =====================================================

        // Thumb tip must be clearly extended away from
        // the thumb base.

        float thumbTipDistance =
            Distance(hand.landmarks[4], wrist);

        float thumbBaseDistance =
            Distance(hand.landmarks[2], wrist);

        bool thumbExtended =
            thumbTipDistance >
            thumbBaseDistance * 1.25f;


        // Thumb tip should also be clearly above the palm.

        bool thumbAbovePalm =
            hand.landmarks[4].y <
            hand.landmarks[9].y - 0.02f;


        bool thumbUp =
            thumbExtended &&
            thumbAbovePalm &&
            indexClosed &&
            middleClosed &&
            ringClosed &&
            pinkyClosed;


        // =====================================================
        // 👍 THUMB UP
        // =====================================================

        if (thumbUp)
        {
            return Gesture.ThumbUp;
        }


        // =====================================================
        // ✌️ V SIGN
        // =====================================================

        if (indexOpen &&
            middleOpen &&
            ringClosed &&
            pinkyClosed)
        {
            return Gesture.VSign;
        }


        // =====================================================
        // ✋ OPEN PALM
        // =====================================================

        if (indexOpen &&
            middleOpen &&
            ringOpen &&
            pinkyOpen)
        {
            return Gesture.OpenPalm;
        }


        // =====================================================
        // ☝️ INDEX
        // =====================================================

        if (indexOpen &&
            middleClosed &&
            ringClosed &&
            pinkyClosed)
        {
            return Gesture.Index;
        }


        // =====================================================
        // NEUTRAL
        // =====================================================

        return Gesture.Neutral;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    void OnDestroy()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated -=
                OnHandResult;
        }
    }
}