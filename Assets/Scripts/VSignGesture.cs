using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class VSignGesture : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Fish")]
    public GameObject elementPrefab;

    [Header("School Settings")]
    public int elementCount = 5;
    public float horizontalSpacing = 0.8f;
    public float verticalSpacing = 0.5f;

    [Header("Gesture Control")]
    public float rearmDelay = 0.5f;

    private bool vSignDetected = false;

    // Prevents repeated schools while V is held
    private bool vSignLocked = false;

    // Time since V was released
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
                "VSignGesture: HandLandmarkerRunner is not assigned!"
            );
        }
    }

    // MediaPipe callback.
    // Only read/store landmark data here.
    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null ||
            result.handLandmarks.Count == 0)
        {
            vSignDetected = false;
            return;
        }

        var hand = result.handLandmarks[0];

        if (hand.landmarks == null ||
            hand.landmarks.Count < 21)
        {
            vSignDetected = false;
            return;
        }

        vSignDetected = IsVSign(hand);

        // Only record position while the gesture
        // is available and unlocked.
        if (vSignDetected && !vSignLocked)
        {
            var wrist = hand.landmarks[0];

            targetX = wrist.x;
            targetY = 1f - wrist.y;
        }
    }

    void Update()
    {
        // --------------------------------
        // V SIGN DETECTED
        // --------------------------------

        if (vSignDetected)
        {
            rearmTimer = 0f;

            // Already spawned for this V?
            if (vSignLocked)
                return;

            // Lock BEFORE spawning.
            vSignLocked = true;

            SpawnFishSchool();

            Debug.Log("V SIGN → Fish school spawned");
        }

        // --------------------------------
        // V SIGN NOT DETECTED
        // --------------------------------

        else
        {
            if (!vSignLocked)
                return;

            rearmTimer += Time.deltaTime;

            // V must remain released for this
            // amount of time before another school
            // can be created.
            if (rearmTimer >= rearmDelay)
            {
                vSignLocked = false;
                rearmTimer = 0f;

                Debug.Log("V sign gesture re-armed");
            }
        }
    }

    bool IsVSign(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        // Index must be clearly open
        bool indexOpen =
            hand.landmarks[8].y <
            hand.landmarks[6].y;

        // Middle must be clearly open
        bool middleOpen =
            hand.landmarks[12].y <
            hand.landmarks[10].y;

        // Ring must be clearly closed
        bool ringClosed =
            hand.landmarks[16].y >
            hand.landmarks[14].y;

        // Pinky must be clearly closed
        bool pinkyClosed =
            hand.landmarks[20].y >
            hand.landmarks[18].y;

        // Thumb should NOT be fully extended.
        bool thumbClosed =
            hand.landmarks[4].x >
            hand.landmarks[3].x;

        return indexOpen &&
               middleOpen &&
               ringClosed &&
               pinkyClosed &&
               thumbClosed;
    }

    void SpawnFishSchool()
    {
        if (elementPrefab == null)
        {
            Debug.LogError(
                "VSignGesture: Fish prefab is not assigned!"
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
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );

        centerPosition.z = 0f;

        // Deliberate school formation.
        Vector2[] formation =
        {
            new Vector2(-horizontalSpacing, 0.3f),
            new Vector2(0f, 0.5f),
            new Vector2(horizontalSpacing, 0.3f),
            new Vector2(-0.5f, -0.3f),
            new Vector2(0.5f, -0.3f)
        };

        int count = Mathf.Min(
            elementCount,
            formation.Length
        );

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition =
                centerPosition +
                new Vector3(
                    formation[i].x,
                    formation[i].y,
                    0f
                );

            Instantiate(
                elementPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }
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