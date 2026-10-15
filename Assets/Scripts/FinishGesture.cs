
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FinishGesture : MonoBehaviour
{
    [Header("MediaPipe")]
    public HandLandmarkerRunner handLandmarkerRunner;

    [Header("Finish Settings")]
    public float holdDuration = 2f;
    [HideInInspector] public float handsTogetherDistance = 0.18f; // Legacy serialized value; no longer used.
    [Min(0f), Tooltip("Minimum palm-center separation in normalized image coordinates (0..1).")]
    public float minimumHandSeparation = 0.30f;
    [Min(0.05f), Tooltip("Reset the hold if no hand result arrives within this many seconds.")]
    public float trackingTimeout = 0.25f;
    public bool debugFinishGesture;

    [Header("Save Settings")]
    public string fileNamePrefix = "BodyAsBrush_Artwork_";

    [Header("Result Scene")]
    public string resultSceneName = "ArtworkResultScene";

    private bool detectedFinishPose;
    private bool detectedFirstOpen;
    private bool detectedSecondOpen;
    private bool poseInterrupted;
    private int detectedHandCount;
    private float detectedSeparation;
    private long lastHandResultTimestamp;
    private float nextDiagnosticTime;

    private float holdTimer;
    private bool alreadyFinished;
    private bool isSaving;

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

    void OnHandResult(HandLandmarkerResult result)
    {
        int hands = result.handLandmarks != null ? result.handLandmarks.Count : 0;
        bool firstOpen = false;
        bool secondOpen = false;
        float separation = 0f;
        if (hands == 2)
        {
            var first = result.handLandmarks[0];
            var second = result.handLandmarks[1];
            if (LandmarksAreValid(first) && LandmarksAreValid(second))
            {
                firstOpen = IsOpenPalm(first);
                secondOpen = IsOpenPalm(second);
                separation = Vector2.Distance(GetPalmCenter(first), GetPalmCenter(second));
            }
        }

        bool validPose = hands == 2 && firstOpen && secondOpen && separation > minimumHandSeparation;
        lock (finishLock)
        {
            detectedHandCount = hands;
            detectedFirstOpen = firstOpen;
            detectedSecondOpen = secondOpen;
            detectedSeparation = separation;
            detectedFinishPose = validPose;
            lastHandResultTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            // Do not miss a brief invalid sample if another callback arrives before Update.
            if (!validPose)
                poseInterrupted = true;
        }
    }

    private static bool LandmarksAreValid(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        if (hand.landmarks == null || hand.landmarks.Count < 21)
            return false;
        for (int i = 0; i < 21; i++)
        {
            var point = hand.landmarks[i];
            if (float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                float.IsNaN(point.y) || float.IsInfinity(point.y) ||
                float.IsNaN(point.z) || float.IsInfinity(point.z) ||
                point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f)
                return false;
        }
        return true;
    }

    private static Vector2 Point(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand, int index)
    {
        return new Vector2(hand.landmarks[index].x, hand.landmarks[index].y);
    }

    private static bool IsOpenPalm(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        if (!LandmarksAreValid(hand))
            return false;
        float palmWidth = Vector2.Distance(Point(hand, 5), Point(hand, 17));
        if (palmWidth < 0.00001f)
            return false;
        // Same wrist-distance ratios as GestureManager.IsFingerClearlyExtended.
        // Check both finger joints as well, without assuming an upward-facing hand.
        return IsFingerExtended(hand, 8, 6, 5) && IsFingerExtended(hand, 12, 10, 9) &&
            IsFingerExtended(hand, 16, 14, 13) && IsFingerExtended(hand, 20, 18, 17) &&
            Vector2.Distance(Point(hand, 4), Point(hand, 8)) > palmWidth * 0.25f;
    }

    private static bool IsFingerExtended(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand, int tip, int pip, int mcp)
    {
        Vector2 wrist = Point(hand, 0);
        Vector2 tipPoint = Point(hand, tip);
        Vector2 pipPoint = Point(hand, pip);
        Vector2 dipPoint = Point(hand, pip + 1);
        float tipDistance = Vector2.Distance(tipPoint, wrist);
        return tipDistance > Vector2.Distance(pipPoint, wrist) * 1.12f &&
            tipDistance > Vector2.Distance(Point(hand, mcp), wrist) * 1.35f &&
            Vector2.Angle(Point(hand, mcp) - pipPoint, dipPoint - pipPoint) >= 150f &&
            Vector2.Angle(pipPoint - dipPoint, tipPoint - dipPoint) >= 150f;
    }

    private static Vector2 GetPalmCenter(
        Mediapipe.Tasks.Components.Containers.NormalizedLandmarks hand)
    {
        return (Point(hand, 0) + Point(hand, 5) + Point(hand, 9) + Point(hand, 13) + Point(hand, 17)) / 5f;
    }

    // Pure hold-state transition; all Unity work and diagnostics stay on the main thread.
    private bool AdvanceHold(bool validPose, bool interrupted, float deltaTime)
    {
        if (isSaving)
            return false;
        if (!validPose || interrupted)
        {
            holdTimer = 0f;
            alreadyFinished = false;
            return false;
        }
        if (alreadyFinished)
            return false;
        holdTimer += deltaTime;
        if (holdTimer < holdDuration)
            return false;
        alreadyFinished = true;
        return true;
    }

    void Update()
    {
        if (isSaving)
            return;
        bool validPose;
        bool interrupted;
        int hands;
        bool firstOpen;
        bool secondOpen;
        float separation;
        lock (finishLock)
        {
            double age = (System.Diagnostics.Stopwatch.GetTimestamp() - lastHandResultTimestamp) /
                (double)System.Diagnostics.Stopwatch.Frequency;
            validPose = detectedFinishPose && lastHandResultTimestamp != 0 && age <= trackingTimeout;
            interrupted = poseInterrupted;
            poseInterrupted = false;
            hands = detectedHandCount;
            firstOpen = detectedFirstOpen;
            secondOpen = detectedSecondOpen;
            separation = detectedSeparation;
        }

        float previousHold = holdTimer;
        bool completed = AdvanceHold(validPose, interrupted, Time.deltaTime);
        if (debugFinishGesture)
        {
            if (previousHold > 0f && holdTimer == 0f)
                Debug.Log("[FinishGesture] Finish pose lost. Resetting.");
            if (previousHold == 0f && holdTimer > 0f)
                Debug.Log("[FinishGesture] Finish pose detected. Starting hold.");
            if (Time.unscaledTime >= nextDiagnosticTime)
            {
                nextDiagnosticTime = Time.unscaledTime + 0.5f;
                Debug.Log($"[FinishGesture] Hands: {hands}; Hand 1 open: {firstOpen}; Hand 2 open: {secondOpen}; " +
                    $"Separation: {separation:F2}; Fresh valid pose: {validPose}; Hold: {holdTimer:F1} / {holdDuration:F1}");
            }
        }
        if (completed)
        {
            Debug.Log("[FinishGesture] Finish gesture completed.");
            FinishArtwork();
        }
    }
    // ============================================
    // FINISH ARTWORK
    // ============================================

    public void FinishArtwork()
    {
        if (isSaving)
            return;

        Debug.Log("ARTWORK FINISH REQUESTED.");

        ArtworkStatistics statistics =
            ArtworkStatistics.FindStatistics();

        if (statistics == null || statistics.data == null)
        {
            Debug.LogError(
                "FinishGesture: No usable ArtworkStatistics component " +
                "was found. Check the DrawingScene Hierarchy and Console."
            );

            // Keep alreadyFinished true so the error does not spam.
            // Releasing the hands will allow another attempt.
            return;
        }

        statistics.FinalizeStatistics();

        isSaving = true;
        StartCoroutine(SaveArtwork(statistics));
    }

    // ============================================
    // SAVE ARTWORK
    // ============================================

    IEnumerator SaveArtwork(ArtworkStatistics statistics)
    {
        // Capture the completed rendered frame, then write and verify it synchronously.
        yield return new WaitForEndOfFrame();

        Texture2D captured = null;
        Texture2D verified = null;
        ArtworkResultManager manager = null;
        ArtworkResultData previousResult = null;
        bool resultStored = false;
        bool succeeded = false;
        try
        {
            if (statistics == null || statistics.data == null)
                throw new System.InvalidOperationException("Statistics became unavailable during save.");

            manager = ArtworkResultManager.Instance;
            if (manager == null)
                throw new System.InvalidOperationException("ArtworkResultManager is missing from DrawingScene.");

            if (string.IsNullOrWhiteSpace(resultSceneName) ||
                !Application.CanStreamedLevelBeLoaded(resultSceneName))
                throw new System.InvalidOperationException("Result scene is unavailable in Build Settings: " + resultSceneName);

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string path = Path.Combine(Application.persistentDataPath,
                fileNamePrefix + timestamp + ".png");

            captured = CaptureArtworkOnly(statistics.handTrackingBrush);
            if (captured == null)
                throw new System.InvalidOperationException("Screen capture returned no texture.");

            byte[] png = captured.EncodeToPNG();
            if (png == null || png.Length == 0)
                throw new System.IO.IOException("Screen capture could not be encoded as PNG.");

            File.WriteAllBytes(path, png);
            verified = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!verified.LoadImage(File.ReadAllBytes(path)) ||
                verified.width != captured.width || verified.height != captured.height)
                throw new System.IO.IOException("Saved screenshot failed read/decode verification: " + path);

            string theme = PlayerPrefs.GetString("SelectedTheme", "Underwater");
            previousResult = manager.CurrentResult;
            manager.SetResult(statistics.data, path, theme);
            resultStored = true;
            SceneManager.LoadScene(resultSceneName);
            succeeded = true;
            Debug.Log("ARTWORK FINISHED! Verified PNG saved to: " + path);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("FinishGesture: Finish failed. Separate your hands and hold again to retry. " + exception, this);
            if (resultStored && manager != null)
                manager.RestoreResult(previousResult);
        }
        finally
        {
            if (captured != null)
                Destroy(captured);
            if (verified != null)
                Destroy(verified);
            if (!succeeded)
            {
                if (statistics != null)
                    statistics.ResumeAfterFailedFinish();
                isSaving = false;
            }
        }
    }

    private Texture2D CaptureArtworkOnly(HandTrackingBrush brush)
    {
        Camera camera = Camera.main;
        ThemeBackgroundManager backgrounds = FindAnyObjectByType<ThemeBackgroundManager>();
        if (camera == null || backgrounds == null)
            throw new System.InvalidOperationException("Artwork capture needs the drawing camera and background manager.");

        Canvas oceanCanvas = backgrounds.underwaterBackground != null
            ? backgrounds.underwaterBackground.GetComponentInParent<Canvas>() : null;
        Canvas taiwanCanvas = backgrounds.taiwanBackground != null
            ? backgrounds.taiwanBackground.GetComponentInParent<Canvas>() : null;
        if (oceanCanvas == null || taiwanCanvas == null)
            throw new System.InvalidOperationException("Artwork background canvas references are missing.");

        var hiddenCanvases = new List<Canvas>();
        var hiddenRenderers = new HashSet<Renderer>();
        RenderTexture target = null;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D artwork = null;
        try
        {
            // Canvas.enabled affects rendering only; no GameObjects or scripts are disabled.
            // Keep the existing theme backgrounds, which are also rendered through a Canvas.
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas == oceanCanvas || canvas == taiwanCanvas)
                    continue;
                if (canvas.enabled)
                {
                    hiddenCanvases.Add(canvas);
                    canvas.enabled = false;
                }
                // MediaPipe annotations can use world-space renderers under their UI canvas.
                foreach (Renderer renderer in canvas.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled && hiddenRenderers.Add(renderer))
                        renderer.enabled = false;
            }
            if (brush != null && brush.brush != null)
                foreach (Renderer renderer in brush.brush.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled && hiddenRenderers.Add(renderer))
                        renderer.enabled = false;

            Canvas.ForceUpdateCanvases();
            target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new System.InvalidOperationException("The render pipeline does not support artwork-only capture.");

            // Synchronous URP render: preserve the camera view, background and generated effects.
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            artwork = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            artwork.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            artwork.Apply();
            return artwork;
        }
        catch
        {
            if (artwork != null)
                Destroy(artwork);
            throw;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (target != null)
                RenderTexture.ReleaseTemporary(target);
            foreach (Renderer renderer in hiddenRenderers)
                if (renderer != null)
                    renderer.enabled = true;
            foreach (Canvas canvas in hiddenCanvases)
                if (canvas != null)
                    canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
        }
    }

    // ============================================
    // CLEANUP
    // ============================================

    void OnDestroy()
    {
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnResultUpdated -= OnHandResult;
        }

        lock (finishLock)
        {
            detectedFinishPose = false;
            poseInterrupted = true;
        }
    }
}
