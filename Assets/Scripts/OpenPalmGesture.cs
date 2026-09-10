using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class OpenPalmGesture : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Element")]
    public GameObject elementPrefab;

    [Header("Gesture Control")]
    public float rearmDelay = 0.5f;

    private bool palmDetected = false;

    // Prevents repeated spawning while palm is held
    private bool palmLocked = false;

    // Time the palm has remained released
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
                "OpenPalmGesture: HandLandmarkerRunner is not assigned!"
            );
        }
    }

    // MediaPipe callback.
    // Only read/store data here.
    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null ||
            result.handLandmarks.Count == 0)
        {
            palmDetected = false;
            return;
        }

        var hand = result.handLandmarks[0];

        if (hand.landmarks == null ||
            hand.landmarks.Count < 21)
        {
            palmDetected = false;
            return;
        }

        palmDetected = IsOpenPalm(hand);

        // Record position only when the gesture
        // is available and not already locked.
        if (palmDetected && !palmLocked)
        {
            var wrist = hand.landmarks[0];

            targetX = wrist.x;
            targetY = 1f - wrist.y;
        }
    }

    void Update()
    {
        // --------------------------------
        // OPEN PALM DETECTED
        // --------------------------------

        if (palmDetected)
        {
            rearmTimer = 0f;

            // Already spawned for this palm?
            // Do nothing.
            if (palmLocked)
                return;

            // Lock BEFORE spawning.
            palmLocked = true;

            SpawnElement();

            Debug.Log("OPEN PALM → Coral spawned");
        }

        // --------------------------------
        // PALM IS NOT DETECTED
        // --------------------------------

        else
        {
            if (!palmLocked)
                return;

            rearmTimer += Time.deltaTime;

            // Palm must remain closed/changed
            // for this amount of time before
            // another coral can be spawned.
            if (rearmTimer >= rearmDelay)
            {
                palmLocked = false;
                rearmTimer = 0f;

                Debug.Log("Open Palm gesture re-armed");
            }
        }
    }

    bool IsOpenPalm(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        bool indexOpen =
            hand.landmarks[8].y <
            hand.landmarks[6].y;

        bool middleOpen =
            hand.landmarks[12].y <
            hand.landmarks[10].y;

        bool ringOpen =
            hand.landmarks[16].y <
            hand.landmarks[14].y;

        bool pinkyOpen =
            hand.landmarks[20].y <
            hand.landmarks[18].y;

        return indexOpen &&
               middleOpen &&
               ringOpen &&
               pinkyOpen;
    }

    void SpawnElement()
    {
        if (elementPrefab == null)
        {
            Debug.LogError(
                "OpenPalmGesture: Element Prefab is not assigned!"
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

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );

        worldPosition.z = 0f;

        Instantiate(
            elementPrefab,
            worldPosition,
            Quaternion.identity
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