using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;

public class FinishGesture : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Finish Settings")]
    public float holdDuration = 2f;
    public float handsTogetherDistance = 0.18f;

    [Header("Save Settings")]
    public string fileNamePrefix = "BodyAsBrush_Artwork_";

    // Data received from MediaPipe callback
    private bool detectedBothHands = false;
    private bool detectedHandsTogether = false;

    // Main-thread state
    private float holdTimer = 0f;
    private bool alreadyFinished = false;

    private readonly object finishLock = new object();

    void Start()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated += OnHandResult;
        }
        else
        {
            Debug.LogError(
                "FinishGesture: HandLandmarkerRunner is not assigned!"
            );
        }
    }

    // IMPORTANT:
    // Do not use Unity API or Time.time here.
    void OnHandResult(HandLandmarkerResult result)
    {
        bool newBothHandsDetected = false;
        bool newHandsTogether = false;

        if (result.handLandmarks != null &&
            result.handLandmarks.Count >= 2)
        {
            var hand1 = result.handLandmarks[0];
            var hand2 = result.handLandmarks[1];

            if (hand1.landmarks != null &&
                hand2.landmarks != null &&
                hand1.landmarks.Count >= 21 &&
                hand2.landmarks.Count >= 21)
            {
                Vector2 palm1 = GetPalmCenter(hand1);
                Vector2 palm2 = GetPalmCenter(hand2);

                float distance =
                    Vector2.Distance(palm1, palm2);

                newBothHandsDetected = true;
                newHandsTogether =
                    distance < handsTogetherDistance;
            }
        }

        lock (finishLock)
        {
            detectedBothHands = newBothHandsDetected;
            detectedHandsTogether = newHandsTogether;
        }
    }

    Vector2 GetPalmCenter(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        Vector2 center = Vector2.zero;

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

    void Update()
    {
        bool bothHands;
        bool handsTogether;

        lock (finishLock)
        {
            bothHands = detectedBothHands;
            handsTogether = detectedHandsTogether;
        }

        // Hands separated or one hand disappeared.
        // Reset the timer and ARM the next artwork.
        if (!bothHands || !handsTogether)
        {
            holdTimer = 0f;

            if (alreadyFinished)
            {
                alreadyFinished = false;
                Debug.Log("Finish gesture re-armed.");
            }

            return;
        }

        // Already finished this hands-together event.
        if (alreadyFinished)
            return;

        holdTimer += Time.deltaTime;

        Debug.Log(
            "Hands together: " +
            holdTimer.ToString("F1") +
            " / " +
            holdDuration.ToString("F1")
        );

        if (holdTimer >= holdDuration)
        {
            alreadyFinished = true;

            FinishArtwork();
        }
    }

    void FinishArtwork()
    {
        Debug.Log("ARTWORK FINISHED!");

        StartCoroutine(SaveArtwork());
    }

    IEnumerator SaveArtwork()
    {
        yield return new WaitForEndOfFrame();

        string timestamp =
            System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string fileName =
            fileNamePrefix + timestamp + ".png";

        string path =
            System.IO.Path.Combine(
                Application.persistentDataPath,
                fileName
            );

        ScreenCapture.CaptureScreenshot(path);

        Debug.Log(
            "Artwork saved to:\n" + path
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