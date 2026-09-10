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

    private bool bothHandsDetected = false;
    private bool handsTogether = false;

    private float holdTimer = 0f;
    private bool alreadyFinished = false;

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

    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null ||
            result.handLandmarks.Count < 2)
        {
            bothHandsDetected = false;
            handsTogether = false;
            return;
        }

        bothHandsDetected = true;

        var hand1 = result.handLandmarks[0];
        var hand2 = result.handLandmarks[1];

        if (hand1.landmarks == null ||
            hand2.landmarks == null ||
            hand1.landmarks.Count < 21 ||
            hand2.landmarks.Count < 21)
        {
            handsTogether = false;
            return;
        }

        Vector2 palm1 = GetPalmCenter(hand1);
        Vector2 palm2 = GetPalmCenter(hand2);

        float distance = Vector2.Distance(palm1, palm2);

        handsTogether = distance < handsTogetherDistance;
    }

    Vector2 GetPalmCenter(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        Vector2 center = Vector2.zero;

        // Wrist + four main palm joints.
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
        if (!bothHandsDetected || !handsTogether)
        {
            holdTimer = 0f;
            return;
        }

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
        // Wait until the current frame has finished rendering.
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

        // Allow another artwork to be finished later.
        holdTimer = 0f;
        alreadyFinished = false;
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