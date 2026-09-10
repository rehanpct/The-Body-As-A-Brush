using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class FistGesture : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Bubble")]
    public GameObject bubblePrefab;

    [Header("Burst Settings")]
    public int bubbleCount = 3;
    public float burstRadius = 1.5f;

    [Header("Gesture Control")]
    public float rearmDelay = 0.5f;

    private bool fistDetected = false;

    // Prevents repeated spawning while fist is held
    private bool fistLocked = false;

    // Used to make sure the fist has actually been released
    private float rearmTimer = 0f;

    private float targetX;
    private float targetY;

    void Start()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated += OnHandResult;
        }
        else
        {
            Debug.LogError(
                "FistGesture: HandLandmarkerRunner is not assigned!"
            );
        }
    }

    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null ||
            result.handLandmarks.Count == 0)
        {
            fistDetected = false;
            return;
        }

        var hand = result.handLandmarks[0];

        if (hand.landmarks == null ||
            hand.landmarks.Count < 21)
        {
            fistDetected = false;
            return;
        }

        fistDetected = IsFist(hand);

        // Only record the position when the fist is detected
        // and the gesture is currently unlocked.
        if (fistDetected && !fistLocked)
        {
            var wrist = hand.landmarks[0];

            targetX = wrist.x;
            targetY = 1f - wrist.y;
        }
    }

    void Update()
    {
        // --------------------------------
        // FIST IS DETECTED
        // --------------------------------

        if (fistDetected)
        {
            rearmTimer = 0f;

            // Already triggered?
            // Do absolutely nothing.
            if (fistLocked)
                return;

            // Lock immediately BEFORE spawning.
            fistLocked = true;

            SpawnBubbleBurst();

            Debug.Log("FIST → Bubble burst");
        }

        // --------------------------------
        // FIST IS NOT DETECTED
        // --------------------------------

        else
        {
            if (!fistLocked)
                return;

            rearmTimer += Time.deltaTime;

            // The fist must remain released
            // for the re-arm delay.
            if (rearmTimer >= rearmDelay)
            {
                fistLocked = false;
                rearmTimer = 0f;

                Debug.Log("Fist gesture re-armed");
            }
        }
    }

    bool IsFist(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        bool indexClosed =
            hand.landmarks[8].y >
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

        return indexClosed &&
               middleClosed &&
               ringClosed &&
               pinkyClosed;
    }

    void SpawnBubbleBurst()
    {
        if (bubblePrefab == null)
        {
            Debug.LogError(
                "FistGesture: Bubble Prefab is not assigned!"
            );

            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector3 screenPosition = new Vector3(
            targetX * Screen.width,
            targetY * Screen.height,
            10f
        );

        Vector3 centerPosition =
            mainCamera.ScreenToWorldPoint(screenPosition);

        centerPosition.z = 0f;

        for (int i = 0; i < bubbleCount; i++)
        {
            Vector2 randomOffset =
                Random.insideUnitCircle * burstRadius;

            Vector3 spawnPosition =
                centerPosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            Instantiate(
                bubblePrefab,
                spawnPosition,
                Quaternion.identity
            );
        }
    }

    void OnDestroy()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated -= OnHandResult;
        }
    }
}