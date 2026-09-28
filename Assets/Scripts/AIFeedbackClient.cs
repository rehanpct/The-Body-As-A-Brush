using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using BodyAsBrush.UI;

public class AIFeedbackClient : MonoBehaviour
{
    public static AIFeedbackClient Instance;

    // ============================================================
    // RENDER BACKEND
    // ============================================================

    [Header("Render Backend")]
    [Tooltip("POST endpoint on Render. Never put an OpenAI key here.")]
    [SerializeField]
    private string renderUrl =
        "https://the-body-as-a-brush.onrender.com/api/feedback";

    // ============================================================
    // COOLDOWN
    // ============================================================

    [Header("Cooldown")]
    [Tooltip("Minimum seconds between AI requests.")]
    [SerializeField]
    private float minimumCooldownSeconds = 20f;

    // ============================================================
    // SESSION
    // ============================================================

    private string sessionId;

    private float sessionStartTime;

    // ============================================================
    // REQUEST STATE
    // ============================================================

    private bool isRequestInProgress = false;

    private float lastRequestTime = -999f;

    private float currentCooldown;

    // ============================================================
    // NORMAL FEEDBACK CONTEXT
    // ============================================================

    [Serializable]
    private class FeedbackContext
    {
        public string currentTheme;

        public string objectType;

        public int totalObjectCount;

        public int waterCount;

        public int coralCount;

        public int fishSchoolCount;

        public int bubbleBurstCount;

        public int lanternCount;

        public int indexGestureCount;

        public int vSignGestureCount;

        public int openPalmGestureCount;

        public int thumbUpGestureCount;

        public float pauseTime;

        public float sessionElapsedSeconds;
    }

    // ============================================================
    // COMPOSITION CONTEXT
    // ============================================================

    [Serializable]
    private class CompositionContext
    {
        public string currentTheme;

        public int totalElements;

        public float leftDensity;

        public float rightDensity;

        public float topDensity;

        public float bottomDensity;

        public float emptyLeft;

        public float emptyRight;

        public float emptyTop;

        public float emptyBottom;

        public string largestEmptyRegion;

        public float emptySpacePercentage;

        public bool hasLargeObject;

        public string largeObjectType;

        public float largestObjectPercentage;

        public bool hasCluster;

        public float averageDistance;
    }

    // ============================================================
    // NORMAL REQUEST
    // ============================================================

    [Serializable]
    private class FeedbackRequest
    {
        public string projectType;

        public string sessionId;

        public string eventType;

        public string occurredAtUtc;

        public FeedbackContext context;
    }

    // ============================================================
    // COMPOSITION REQUEST
    // ============================================================

    [Serializable]
    private class CompositionRequest
    {
        public string projectType;

        public string sessionId;

        public string eventType;

        public string occurredAtUtc;

        public CompositionContext context;
    }

    // ============================================================
    // RESPONSE ACTION
    // ============================================================

    [Serializable]
    private class FeedbackAction
    {
        public string type;

        public string target;

        public string value;
    }

    // ============================================================
    // AI RESPONSE
    // ============================================================

    [Serializable]
    private class FeedbackResponse
    {
        public string requestId;

        public string sessionId;

        public string message;

        public string feedbackType;

        public FeedbackAction action;

        public string priority;

        public int cooldownSeconds;
    }

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        sessionId =
            Guid.NewGuid().ToString();

        sessionStartTime =
            Time.time;

        currentCooldown =
            minimumCooldownSeconds;

        Debug.Log(
            "[AIFeedbackClient] Initialised. Session: " +
            sessionId
        );
    }

    // ============================================================
    // NORMAL EVENT FEEDBACK
    // ============================================================

    public void RequestFeedback(
        string eventType
    )
    {
        if (isRequestInProgress)
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "Skipped — request in progress."
            );

            return;
        }

        float timeSinceLast =
            Time.time - lastRequestTime;

        if (timeSinceLast < currentCooldown)
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "Skipped — cooldown. " +
                (currentCooldown - timeSinceLast)
                    .ToString("F1") +
                "s remaining."
            );

            return;
        }

        StartCoroutine(
            SendFeedbackRequest(eventType)
        );
    }

    // ============================================================
    // COMPOSITION FEEDBACK
    // ============================================================

    public void RequestCompositionFeedback(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        if (snapshot == null)
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "Composition snapshot is null."
            );

            return;
        }

        if (isRequestInProgress)
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "Composition request skipped — " +
                "request already in progress."
            );

            return;
        }

        float timeSinceLast =
            Time.time - lastRequestTime;

        if (timeSinceLast < currentCooldown)
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "Composition request skipped — " +
                "cooldown."
            );

            return;
        }

        StartCoroutine(
            SendCompositionRequest(snapshot)
        );
    }

    // ============================================================
    // BUILD NORMAL CONTEXT
    // ============================================================

    private FeedbackContext BuildContext(
        string objectType
    )
    {
        FeedbackContext ctx =
            new FeedbackContext();

        ctx.objectType =
            objectType;

        ctx.sessionElapsedSeconds =
            Time.time - sessionStartTime;

        if (ThemeManager.Instance != null)
        {
            ctx.currentTheme =
                ThemeManager.Instance
                    .CurrentTheme
                    .ToString();
        }

        if (ArtworkActionHistory.Instance != null)
        {
            ctx.totalObjectCount =
                ArtworkActionHistory.Instance
                    .ActionCount;
        }

        if (ArtworkStatistics.Instance != null)
        {
            ArtworkData d =
                ArtworkStatistics.Instance.data;

            ctx.waterCount =
                d.waterCount;

            ctx.coralCount =
                d.coralCount;

            ctx.fishSchoolCount =
                d.fishSchoolCount;

            ctx.bubbleBurstCount =
                d.bubbleBurstCount;

            ctx.lanternCount =
                d.lanternCount;

            ctx.indexGestureCount =
                d.indexGestureCount;

            ctx.vSignGestureCount =
                d.vSignGestureCount;

            ctx.openPalmGestureCount =
                d.openPalmGestureCount;

            ctx.thumbUpGestureCount =
                d.thumbUpGestureCount;

            ctx.pauseTime =
                d.pauseTime;
        }

        return ctx;
    }

    // ============================================================
    // NORMAL FEEDBACK REQUEST
    // ============================================================

    private IEnumerator SendFeedbackRequest(
        string eventType
    )
    {
        isRequestInProgress = true;

        string objectType =
            eventType == "object_added"
                ? "fish_school"
                : eventType;

        FeedbackRequest requestPayload =
            new FeedbackRequest();

        requestPayload.projectType =
            "gesture_painting";

        requestPayload.sessionId =
            sessionId;

        requestPayload.eventType =
            eventType;

        requestPayload.occurredAtUtc =
            DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ssZ"
            );

        requestPayload.context =
            BuildContext(objectType);

        string json =
            JsonUtility.ToJson(
                requestPayload
            );

        byte[] rawBody =
            Encoding.UTF8.GetBytes(json);

        Debug.Log(
            "[AIFeedbackClient] " +
            "Sending '" +
            eventType +
            "' to " +
            renderUrl
        );

        Debug.Log(
            "[AIFeedbackClient] Payload: " +
            json
        );

        using (UnityWebRequest webRequest =
            new UnityWebRequest(
                renderUrl,
                "POST"
            ))
        {
            webRequest.uploadHandler =
                new UploadHandlerRaw(
                    rawBody
                );

            webRequest.downloadHandler =
                new DownloadHandlerBuffer();

            webRequest.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            webRequest.timeout = 30;

            yield return
                webRequest.SendWebRequest();

            isRequestInProgress =
                false;

            lastRequestTime =
                Time.time;

            if (webRequest.result ==
                UnityWebRequest.Result.Success)
            {
                HandleAIResponse(
                    webRequest.downloadHandler.text
                );
            }
            else
            {
                Debug.LogError(
                    "[AIFeedbackClient] " +
                    "Request failed — " +
                    webRequest.error +
                    " (HTTP " +
                    webRequest.responseCode +
                    ")"
                );
            }
        }
    }

    // ============================================================
    // COMPOSITION REQUEST
    // ============================================================

    private IEnumerator SendCompositionRequest(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        isRequestInProgress = true;

        string currentTheme =
            "Unknown";

        if (ThemeManager.Instance != null)
        {
            currentTheme =
                ThemeManager.Instance
                    .CurrentTheme
                    .ToString();
        }

        CompositionContext context =
            new CompositionContext();

        context.currentTheme =
            currentTheme;

        context.totalElements =
            snapshot.totalElements;

        context.leftDensity =
            snapshot.leftDensity;

        context.rightDensity =
            snapshot.rightDensity;

        context.topDensity =
            snapshot.topDensity;

        context.bottomDensity =
            snapshot.bottomDensity;

        if (snapshot.emptySpace != null)
        {
            context.emptyLeft =
                snapshot.emptySpace.left;

            context.emptyRight =
                snapshot.emptySpace.right;

            context.emptyTop =
                snapshot.emptySpace.top;

            context.emptyBottom =
                snapshot.emptySpace.bottom;

            context.largestEmptyRegion =
                snapshot.emptySpace
                    .largestEmptyRegion;

            context.emptySpacePercentage =
                snapshot.emptySpace
                    .largestEmptyPercentage;
        }

        context.hasLargeObject =
            snapshot.hasLargeObject;

        context.largeObjectType =
            snapshot.largeObjectType;

        context.largestObjectPercentage =
            snapshot.largestObjectPercentage;

        context.hasCluster =
            snapshot.hasCluster;

        context.averageDistance =
            snapshot.averageDistance;

        CompositionRequest request =
            new CompositionRequest();

        request.projectType =
            "gesture_painting";

        request.sessionId =
            sessionId;

        request.eventType =
            "composition_update";

        request.occurredAtUtc =
            DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ssZ"
            );

        request.context =
            context;

        string json =
            JsonUtility.ToJson(request);

        byte[] rawBody =
            Encoding.UTF8.GetBytes(json);

        Debug.Log(
            "[AIFeedbackClient] " +
            "Sending composition_update."
        );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Composition Payload: " +
            json
        );

        using (UnityWebRequest webRequest =
            new UnityWebRequest(
                renderUrl,
                "POST"
            ))
        {
            webRequest.uploadHandler =
                new UploadHandlerRaw(
                    rawBody
                );

            webRequest.downloadHandler =
                new DownloadHandlerBuffer();

            webRequest.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            webRequest.timeout = 30;

            yield return
                webRequest.SendWebRequest();

            isRequestInProgress =
                false;

            lastRequestTime =
                Time.time;

            if (webRequest.result ==
                UnityWebRequest.Result.Success)
            {
                string responseText =
                    webRequest.downloadHandler.text;

                Debug.Log(
                    "[AIFeedbackClient] " +
                    "Composition response: " +
                    responseText
                );

                HandleAIResponse(
                    responseText
                );
            }
            else
            {
                Debug.LogError(
                    "[AIFeedbackClient] " +
                    "Composition request failed — " +
                    webRequest.error +
                    " (HTTP " +
                    webRequest.responseCode +
                    ")"
                );
            }
        }
    }

    // ============================================================
    // HANDLE AI RESPONSE
    // ============================================================

    private void HandleAIResponse(
        string responseText
    )
    {
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "AI response was empty."
            );

            return;
        }

        FeedbackResponse response;

        try
        {
            response =
                JsonUtility.FromJson<FeedbackResponse>(
                    responseText
                );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[AIFeedbackClient] " +
                "Could not parse AI response: " +
                exception.Message
            );

            return;
        }

        if (response == null)
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "AI response parsed as null."
            );

            return;
        }

        if (response.cooldownSeconds > 0)
        {
            currentCooldown =
                Mathf.Max(
                    minimumCooldownSeconds,
                    response.cooldownSeconds
                );
        }

        if (string.IsNullOrEmpty(response.message))
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "Response message was empty."
            );

            return;
        }

        Debug.Log(
            "[AIFeedbackClient] " +
            "AI MESSAGE: " +
            response.message
        );

        if (AIFeedbackPanel.Instance != null)
        {
            int displayDuration =
                response.cooldownSeconds > 0
                    ? response.cooldownSeconds
                    : (int)minimumCooldownSeconds;

            AIFeedbackPanel.Instance.ShowFeedback(
                response.message,
                displayDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "AIFeedbackPanel not present. " +
                "Message: " +
                response.message
            );
        }
    }
}