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

    [Header("Gesture Stability")]
    public float gestureHoldTime = 0.20f;

    [Header("⭕ Circle / Undo")]
    public float circleDistanceThreshold = 0.065f;
    public float circleHoldTime = 0.20f;

    [Header("🤏 Pinch / Move")]
    public float pinchDistanceThreshold = 0.14f;
    public float pinchHoldTime = 0.15f;

    [Header("🤏🤏 Two-Hand Scale")]
    public float twoHandPinchDistanceThreshold = 0.14f;
    public float twoHandScaleSensitivity = 1.5f;

    private Gesture detectedGesture = Gesture.Neutral;

    private Gesture candidateGesture = Gesture.Neutral;
    private float candidateStartTime = 0f;

    private bool detectedCircle = false;
    private bool detectedPinch = false;

    private float detectedPinchDistance = 0f;

    private bool detectedBothHands = false;
    private bool detectedTwoHandPinch = false;
    private float detectedTwoHandDistance = 0f;

    private bool circleStable = false;
    private float circleStartTime = 0f;

    private bool pinchStable = false;
    private float pinchStartTime = 0f;

    private bool twoHandPinchStable = false;

    private bool undoTriggered = false;

    private readonly object gestureLock =
        new object();

    // =========================================================
    // PUBLIC PINCH
    // =========================================================

    public bool IsPinching
    {
        get
        {
            lock (gestureLock)
            {
                return pinchStable;
            }
        }
    }

    public float PinchDistance
    {
        get
        {
            lock (gestureLock)
            {
                return detectedPinchDistance;
            }
        }
    }

    // =========================================================
    // PUBLIC TWO-HAND SCALING
    // =========================================================

    public bool BothHandsDetected
    {
        get
        {
            lock (gestureLock)
            {
                return detectedBothHands;
            }
        }
    }

    public bool IsTwoHandPinching
    {
        get
        {
            lock (gestureLock)
            {
                return twoHandPinchStable;
            }
        }
    }

    public float TwoHandDistance
    {
        get
        {
            lock (gestureLock)
            {
                return detectedTwoHandDistance;
            }
        }
    }

    // =========================================================
    // UNDO
    // =========================================================

    public bool ConsumeUndoTrigger()
    {
        lock (gestureLock)
        {
            if (!undoTriggered)
                return false;

            undoTriggered = false;

            return true;
        }
    }

    // =========================================================
    // START
    // =========================================================

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
                "GestureManager: HandLandmarkerRunner is not assigned!"
            );
        }
    }

    // =========================================================
    // MEDIAPIPE CALLBACK
    // =========================================================

    void OnHandResult(
        HandLandmarkerResult result)
    {
        Gesture newGesture =
            Gesture.Neutral;

        bool newCircle = false;
        bool newPinch = false;

        float newPinchDistance = 0f;

        bool newBothHands = false;
        bool newTwoHandPinch = false;
        float newTwoHandDistance = 0f;

        if (result.handLandmarks != null &&
            result.handLandmarks.Count > 0)
        {
            var hand =
                result.handLandmarks[0];

            if (hand.landmarks != null &&
                hand.landmarks.Count >= 21)
            {
                newPinch =
                    DetectPinch(
                        hand,
                        out newPinchDistance
                    );

                if (!newPinch)
                {
                    newCircle =
                        DetectCircle(hand);
                }

                newGesture =
                    DetectGesture(hand);
            }

            // =================================================
            // SECOND HAND
            // =================================================

            if (result.handLandmarks.Count >= 2)
            {
                var hand2 =
                    result.handLandmarks[1];

                if (hand2.landmarks != null &&
                    hand2.landmarks.Count >= 21)
                {
                    var hand1 =
                        result.handLandmarks[0];

                    Vector2 palm1 =
                        GetPalmCenter(hand1);

                    Vector2 palm2 =
                        GetPalmCenter(hand2);

                    newTwoHandDistance =
                        Vector2.Distance(
                            palm1,
                            palm2
                        );

                    newBothHands = true;

                    bool firstHandPinch =
                        DetectPinch(
                            hand1,
                            out _
                        );

                    bool secondHandPinch =
                        DetectPinch(
                            hand2,
                            out _
                        );

                    newTwoHandPinch =
                        firstHandPinch &&
                        secondHandPinch;
                }
            }
        }

        lock (gestureLock)
        {
            detectedGesture =
                newGesture;

            detectedCircle =
                newCircle;

            detectedPinch =
                newPinch;

            detectedPinchDistance =
                newPinchDistance;

            detectedBothHands =
                newBothHands;

            detectedTwoHandPinch =
                newTwoHandPinch;

            detectedTwoHandDistance =
                newTwoHandDistance;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        Gesture newDetectedGesture;
        bool circle;
        bool pinch;
        bool bothHands;
        bool twoHandPinch;

        lock (gestureLock)
        {
            newDetectedGesture =
                detectedGesture;

            circle =
                detectedCircle;

            pinch =
                detectedPinch;

            bothHands =
                detectedBothHands;

            twoHandPinch =
                detectedTwoHandPinch;
        }

        // =====================================================
        // NORMAL GESTURE STABILITY
        // =====================================================

        if (newDetectedGesture ==
            candidateGesture)
        {
            if (Time.time -
                candidateStartTime >=
                gestureHoldTime)
            {
                currentGesture =
                    candidateGesture;
            }
        }
        else
        {
            candidateGesture =
                newDetectedGesture;

            candidateStartTime =
                Time.time;
        }

        // =====================================================
        // CIRCLE / UNDO
        // =====================================================

        if (pinch ||
            twoHandPinch)
        {
            circleStartTime = 0f;
            circleStable = false;
        }
        else if (circle)
        {
            if (!circleStable)
            {
                if (circleStartTime == 0f)
                {
                    circleStartTime =
                        Time.time;
                }

                if (Time.time -
                    circleStartTime >=
                    circleHoldTime)
                {
                    circleStable = true;

                    lock (gestureLock)
                    {
                        undoTriggered = true;
                    }

                    Debug.Log(
                        "⭕ Undo gesture triggered."
                    );
                }
            }
        }
        else
        {
            circleStartTime = 0f;
            circleStable = false;
        }

        // =====================================================
        // ONE-HAND PINCH
        // =====================================================

        if (pinch)
        {
            if (!pinchStable)
            {
                if (pinchStartTime == 0f)
                {
                    pinchStartTime =
                        Time.time;
                }

                if (Time.time -
                    pinchStartTime >=
                    pinchHoldTime)
                {
                    pinchStable = true;

                    Debug.Log(
                        "🤏 Pinch started."
                    );
                }
            }
        }
        else
        {
            if (pinchStable)
            {
                Debug.Log(
                    "🤏 Pinch released."
                );
            }

            pinchStartTime = 0f;
            pinchStable = false;
        }

        // =====================================================
        // TWO-HAND PINCH
        // =====================================================

        if (bothHands &&
            twoHandPinch)
        {
            if (!twoHandPinchStable)
            {
                twoHandPinchStable = true;

                Debug.Log(
                    "🤏🤏 Two-hand scaling started."
                );
            }
        }
        else
        {
            if (twoHandPinchStable)
            {
                Debug.Log(
                    "🤏🤏 Two-hand scaling released."
                );
            }

            twoHandPinchStable = false;
        }
    }

    // =========================================================
    // PALM CENTER
    // =========================================================

    Vector2 GetPalmCenter(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        Vector2 center =
            Vector2.zero;

        center += new Vector2(
            hand.landmarks[0].x,
            hand.landmarks[0].y
        );

        center += new Vector2(
            hand.landmarks[5].x,
            hand.landmarks[5].y
        );

        center += new Vector2(
            hand.landmarks[9].x,
            hand.landmarks[9].y
        );

        center += new Vector2(
            hand.landmarks[13].x,
            hand.landmarks[13].y
        );

        center += new Vector2(
            hand.landmarks[17].x,
            hand.landmarks[17].y
        );

        return center / 5f;
    }

    // =========================================================
    // DISTANCE
    // =========================================================

    float Distance(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark a,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark b)
    {
        float dx =
            a.x - b.x;

        float dy =
            a.y - b.y;

        return Mathf.Sqrt(
            dx * dx +
            dy * dy
        );
    }

    // =========================================================
    // FINGER EXTENDED
    // =========================================================

    bool IsFingerClearlyExtended(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand,
        int tipIndex,
        int pipIndex,
        int mcpIndex)
    {
        var wrist =
            hand.landmarks[0];

        float tipDistance =
            Distance(
                hand.landmarks[tipIndex],
                wrist
            );

        float pipDistance =
            Distance(
                hand.landmarks[pipIndex],
                wrist
            );

        float mcpDistance =
            Distance(
                hand.landmarks[mcpIndex],
                wrist
            );

        return
            tipDistance >
            pipDistance * 1.12f &&
            tipDistance >
            mcpDistance * 1.35f;
    }

    // =========================================================
    // FINGER CURLED
    // =========================================================

    bool IsFingerClearlyCurled(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand,
        int tipIndex,
        int pipIndex)
    {
        var wrist =
            hand.landmarks[0];

        float tipDistance =
            Distance(
                hand.landmarks[tipIndex],
                wrist
            );

        float pipDistance =
            Distance(
                hand.landmarks[pipIndex],
                wrist
            );

        return
            tipDistance <
            pipDistance * 1.10f;
    }

    // =========================================================
    // CIRCLE
    // =========================================================

    bool DetectCircle(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        float thumbIndexDistance =
            Distance(
                hand.landmarks[4],
                hand.landmarks[8]
            );

        bool thumbIndexTouching =
            thumbIndexDistance <
            circleDistanceThreshold;

        bool middleExtended =
            IsFingerClearlyExtended(
                hand,
                12,
                10,
                9
            );

        bool ringExtended =
            IsFingerClearlyExtended(
                hand,
                16,
                14,
                13
            );

        bool pinkyExtended =
            IsFingerClearlyExtended(
                hand,
                20,
                18,
                17
            );

        return
            thumbIndexTouching &&
            middleExtended &&
            ringExtended &&
            pinkyExtended;
    }

    // =========================================================
    // PINCH
    // =========================================================

    bool DetectPinch(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand,
        out float pinchDistance)
    {
        pinchDistance =
            Distance(
                hand.landmarks[4],
                hand.landmarks[8]
            );

        bool thumbIndexNear =
            pinchDistance <
            pinchDistanceThreshold;

        bool middleCurled =
            IsFingerClearlyCurled(
                hand,
                12,
                10
            );

        bool ringCurled =
            IsFingerClearlyCurled(
                hand,
                16,
                14
            );

        bool pinkyCurled =
            IsFingerClearlyCurled(
                hand,
                20,
                18
            );

        return
            thumbIndexNear &&
            middleCurled &&
            ringCurled &&
            pinkyCurled;
    }

    // =========================================================
    // NORMAL GESTURES
    // =========================================================

    Gesture DetectGesture(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        var wrist =
            hand.landmarks[0];

        float openMargin =
            0.02f;

        bool indexOpen =
            hand.landmarks[8].y <
            hand.landmarks[6].y -
            openMargin;

        bool middleOpen =
            hand.landmarks[12].y <
            hand.landmarks[10].y -
            openMargin;

        bool ringOpen =
            hand.landmarks[16].y <
            hand.landmarks[14].y -
            openMargin;

        bool pinkyOpen =
            hand.landmarks[20].y <
            hand.landmarks[18].y -
            openMargin;

        bool indexClosed =
            Distance(
                hand.landmarks[8],
                wrist
            ) <
            Distance(
                hand.landmarks[6],
                wrist
            ) * 1.10f;

        bool middleClosed =
            Distance(
                hand.landmarks[12],
                wrist
            ) <
            Distance(
                hand.landmarks[10],
                wrist
            ) * 1.10f;

        bool ringClosed =
            Distance(
                hand.landmarks[16],
                wrist
            ) <
            Distance(
                hand.landmarks[14],
                wrist
            ) * 1.10f;

        bool pinkyClosed =
            Distance(
                hand.landmarks[20],
                wrist
            ) <
            Distance(
                hand.landmarks[18],
                wrist
            ) * 1.10f;

        // =====================================================
        // THUMB UP
        // =====================================================

        float thumbTipDistance =
            Distance(
                hand.landmarks[4],
                wrist
            );

        float thumbBaseDistance =
            Distance(
                hand.landmarks[2],
                wrist
            );

        bool thumbExtended =
            thumbTipDistance >
            thumbBaseDistance *
            1.25f;

        bool thumbAbovePalm =
            hand.landmarks[4].y <
            hand.landmarks[9].y -
            0.02f;

        bool thumbUp =
            thumbExtended &&
            thumbAbovePalm &&
            indexClosed &&
            middleClosed &&
            ringClosed &&
            pinkyClosed;

        if (thumbUp)
        {
            return Gesture.ThumbUp;
        }

        // =====================================================
        // V SIGN
        // =====================================================

        if (indexOpen &&
            middleOpen &&
            ringClosed &&
            pinkyClosed)
        {
            return Gesture.VSign;
        }

        // =====================================================
        // OPEN PALM
        // =====================================================

        if (indexOpen &&
            middleOpen &&
            ringOpen &&
            pinkyOpen)
        {
            return Gesture.OpenPalm;
        }

        // =====================================================
        // INDEX
        // =====================================================

        if (indexOpen &&
            middleClosed &&
            ringClosed &&
            pinkyClosed)
        {
            return Gesture.Index;
        }

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