using System;
using System.Collections;
using System.Collections.Generic;
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

    [Tooltip(
        "POST endpoint on Render. " +
        "Never put an OpenAI API key in Unity."
    )]

    [SerializeField]
    private string renderUrl =
        "https://the-body-as-a-brush.onrender.com/api/feedback";

    // ============================================================
    // COOLDOWN
    // ============================================================

    [Header("Cooldown")]

    [Tooltip(
        "Minimum seconds between AI requests."
    )]

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
    // COMPOSITION ELEMENT
    // ============================================================

    [Serializable]
    private class CompositionElement
    {
        public string type;

        public float normalizedX;

        public float normalizedY;

        public float width;

        public float height;

        public float relativeSize;
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

        public List<CompositionElement> elements =
            new List<CompositionElement>();
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
        if (
            Instance != null &&
            Instance != this
        )
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
            "[AIFeedbackClient] " +
            "Initialised. Session: " +
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
                "Skipped — request already in progress."
            );

            return;
        }

        if (!CanSendRequest())
        {
            return;
        }

        StartCoroutine(
            SendFeedbackRequest(
                eventType
            )
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

        if (!CanSendRequest())
        {
            return;
        }

        StartCoroutine(
            SendCompositionRequest(
                snapshot
            )
        );
    }

    // ============================================================
    // ARTWORK REVIEW
    // ============================================================

    public void RequestArtworkReview()
    {
        if (isRequestInProgress)
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "Review skipped — " +
                "request already in progress."
            );

            return;
        }

        if (!CanSendRequest())
        {
            return;
        }

        if (
            CompositionAnalyzer.Instance ==
            null
        )
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "CompositionAnalyzer not found."
            );

            return;
        }

        CompositionAnalyzer.CompositionSnapshot snapshot =
            CompositionAnalyzer.Instance.Analyze();

        if (snapshot == null)
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "Could not create artwork snapshot."
            );

            return;
        }

        StartCoroutine(
            SendArtworkReview(
                snapshot
            )
        );
    }

    // ============================================================
    // REQUEST COOLDOWN
    // ============================================================

    private bool CanSendRequest()
    {
        float timeSinceLast =
            Time.time -
            lastRequestTime;

        if (
            timeSinceLast <
            currentCooldown
        )
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "Skipped — cooldown. " +
                (
                    currentCooldown -
                    timeSinceLast
                ).ToString("F1") +
                "s remaining."
            );

            return false;
        }

        return true;
    }

    // ============================================================
    // BUILD NORMAL CONTEXT
    // ============================================================

    private FeedbackContext BuildContext(
        string objectType
    )
    {
        FeedbackContext context =
            new FeedbackContext();

        context.objectType =
            objectType;

        context.sessionElapsedSeconds =
            Time.time -
            sessionStartTime;

        // --------------------------------------------------------
        // THEME
        // --------------------------------------------------------

        if (
            ThemeManager.Instance !=
            null
        )
        {
            context.currentTheme =
                ThemeManager.Instance
                    .CurrentTheme
                    .ToString();
        }
        else
        {
            context.currentTheme =
                "Unknown";
        }

        // --------------------------------------------------------
        // ACTION HISTORY
        // --------------------------------------------------------

        if (
            ArtworkActionHistory.Instance !=
            null
        )
        {
            context.totalObjectCount =
                ArtworkActionHistory.Instance
                    .ActionCount;
        }

        // --------------------------------------------------------
        // ARTWORK STATISTICS
        // --------------------------------------------------------

        if (
            ArtworkStatistics.Instance !=
            null
        )
        {
            ArtworkData data =
                ArtworkStatistics.Instance.data;

            if (data != null)
            {
                context.waterCount =
                    data.waterCount;

                context.coralCount =
                    data.coralCount;

                context.fishSchoolCount =
                    data.fishSchoolCount;

                context.bubbleBurstCount =
                    data.bubbleBurstCount;

                context.lanternCount =
                    data.lanternCount;

                context.indexGestureCount =
                    data.indexGestureCount;

                context.vSignGestureCount =
                    data.vSignGestureCount;

                context.openPalmGestureCount =
                    data.openPalmGestureCount;

                context.thumbUpGestureCount =
                    data.thumbUpGestureCount;

                context.pauseTime =
                    data.pauseTime;
            }
        }

        return context;
    }

    // ============================================================
    // BUILD COMPOSITION CONTEXT
    // ============================================================

    private CompositionContext BuildCompositionContext(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        CompositionContext context =
            new CompositionContext();

        // --------------------------------------------------------
        // THEME
        // --------------------------------------------------------

        context.currentTheme =
            "Unknown";

        if (
            ThemeManager.Instance !=
            null
        )
        {
            context.currentTheme =
                ThemeManager.Instance
                    .CurrentTheme
                    .ToString();
        }

        // --------------------------------------------------------
        // BASIC COMPOSITION
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // EMPTY SPACE
        // --------------------------------------------------------

        if (
            snapshot.emptySpace !=
            null
        )
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

        // --------------------------------------------------------
        // LARGE OBJECT
        // --------------------------------------------------------

        context.hasLargeObject =
            snapshot.hasLargeObject;

        context.largeObjectType =
            snapshot.largeObjectType;

        context.largestObjectPercentage =
            snapshot.largestObjectPercentage;

        // --------------------------------------------------------
        // CLUSTERING
        // --------------------------------------------------------

        context.hasCluster =
            snapshot.hasCluster;

        context.averageDistance =
            snapshot.averageDistance;

        // --------------------------------------------------------
        // INDIVIDUAL ELEMENTS
        // --------------------------------------------------------

        if (
            snapshot.elements !=
            null
        )
        {
            foreach (
                CompositionAnalyzer.ElementInfo
                element
                in snapshot.elements
            )
            {
                CompositionElement
                    compositionElement =
                    new CompositionElement();

                compositionElement.type =
                    element.type;

                compositionElement.normalizedX =
                    element.normalizedPosition.x;

                compositionElement.normalizedY =
                    element.normalizedPosition.y;

                compositionElement.width =
                    element.width;

                compositionElement.height =
                    element.height;

                compositionElement.relativeSize =
                    element.relativeSize;

                context.elements.Add(
                    compositionElement
                );
            }
        }

        return context;
    }

    // ============================================================
    // NORMAL FEEDBACK REQUEST
    // ============================================================

    private IEnumerator SendFeedbackRequest(
        string eventType
    )
    {
        isRequestInProgress =
            true;

        string objectType =
            eventType ==
            "object_added"
                ? "fish_school"
                : eventType;

        FeedbackRequest request =
            new FeedbackRequest();

        request.projectType =
            "gesture_painting";

        request.sessionId =
            sessionId;

        request.eventType =
            eventType;

        request.occurredAtUtc =
            DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ssZ"
            );

        request.context =
            BuildContext(
                objectType
            );

        string json =
            JsonUtility.ToJson(
                request
            );

        byte[] rawBody =
            Encoding.UTF8.GetBytes(
                json
            );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Sending '" +
            eventType +
            "' to Render."
        );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Payload: " +
            json
        );

        using (
            UnityWebRequest webRequest =
                new UnityWebRequest(
                    renderUrl,
                    "POST"
                )
        )
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

            webRequest.timeout =
                30;

            yield return
                webRequest.SendWebRequest();

            isRequestInProgress =
                false;

            lastRequestTime =
                Time.time;

            if (
                webRequest.result ==
                UnityWebRequest.Result.Success
            )
            {
                HandleAIResponse(
                    webRequest
                        .downloadHandler
                        .text
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
    // COMPOSITION FEEDBACK REQUEST
    // ============================================================

    private IEnumerator SendCompositionRequest(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        isRequestInProgress =
            true;

        CompositionContext context =
            BuildCompositionContext(
                snapshot
            );

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
            JsonUtility.ToJson(
                request
            );

        byte[] rawBody =
            Encoding.UTF8.GetBytes(
                json
            );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Sending composition_update."
        );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Composition Payload: " +
            json
        );

        using (
            UnityWebRequest webRequest =
                new UnityWebRequest(
                    renderUrl,
                    "POST"
                )
        )
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

            webRequest.timeout =
                30;

            yield return
                webRequest.SendWebRequest();

            isRequestInProgress =
                false;

            lastRequestTime =
                Time.time;

            if (
                webRequest.result ==
                UnityWebRequest.Result.Success
            )
            {
                Debug.Log(
                    "[AIFeedbackClient] " +
                    "Composition response: " +
                    webRequest
                        .downloadHandler
                        .text
                );

                HandleAIResponse(
                    webRequest
                        .downloadHandler
                        .text
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
    // ARTWORK REVIEW REQUEST
    // ============================================================

    private IEnumerator SendArtworkReview(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        isRequestInProgress =
            true;

        CompositionContext context =
            BuildCompositionContext(
                snapshot
            );

        CompositionRequest request =
            new CompositionRequest();

        request.projectType =
            "gesture_painting";

        request.sessionId =
            sessionId;

        request.eventType =
            "artwork_review";

        request.occurredAtUtc =
            DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ssZ"
            );

        request.context =
            context;

        string json =
            JsonUtility.ToJson(
                request
            );

        byte[] rawBody =
            Encoding.UTF8.GetBytes(
                json
            );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Sending artwork_review."
        );

        Debug.Log(
            "[AIFeedbackClient] " +
            "Review Payload: " +
            json
        );

        using (
            UnityWebRequest webRequest =
                new UnityWebRequest(
                    renderUrl,
                    "POST"
                )
        )
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

            webRequest.timeout =
                30;

            yield return
                webRequest.SendWebRequest();

            isRequestInProgress =
                false;

            lastRequestTime =
                Time.time;

            if (
                webRequest.result ==
                UnityWebRequest.Result.Success
            )
            {
                Debug.Log(
                    "[AIFeedbackClient] " +
                    "Artwork review response: " +
                    webRequest
                        .downloadHandler
                        .text
                );

                HandleAIResponse(
                    webRequest
                        .downloadHandler
                        .text
                );
            }
            else
            {
                Debug.LogError(
                    "[AIFeedbackClient] " +
                    "Artwork review failed — " +
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
        if (
            string.IsNullOrEmpty(
                responseText
            )
        )
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
                JsonUtility.FromJson<
                    FeedbackResponse
                >(
                    responseText
                );
        }
        catch (
            Exception exception
        )
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

        // ========================================================
        // UPDATE COOLDOWN
        // ========================================================

        if (
            response.cooldownSeconds >
            0
        )
        {
            currentCooldown =
                Mathf.Max(
                    minimumCooldownSeconds,
                    response.cooldownSeconds
                );
        }

        // ========================================================
        // MESSAGE VALIDATION
        // ========================================================

        if (
            string.IsNullOrEmpty(
                response.message
            )
        )
        {
            Debug.LogWarning(
                "[AIFeedbackClient] " +
                "AI response message was empty."
            );

            return;
        }

        // ========================================================
        // DEBUG INFORMATION
        // ========================================================

        Debug.Log(
            "[AIFeedbackClient] " +
            "AI MESSAGE: " +
            response.message
        );

        Debug.Log(
            "[AIFeedbackClient] " +
            "FEEDBACK TYPE: " +
            response.feedbackType
        );

        Debug.Log(
            "[AIFeedbackClient] " +
            "PRIORITY: " +
            response.priority
        );

        if (
            response.action !=
            null
        )
        {
            Debug.Log(
                "[AIFeedbackClient] " +
                "ACTION: " +
                response.action.type
            );

            Debug.Log(
                "[AIFeedbackClient] " +
                "ACTION TARGET: " +
                response.action.target
            );

            Debug.Log(
                "[AIFeedbackClient] " +
                "ACTION VALUE: " +
                response.action.value
            );
        }

        // ========================================================
        // DISPLAY AI FEEDBACK
        // ========================================================

        if (
            AIFeedbackPanel.Instance !=
            null
        )
        {
            int displayDuration;

            if (
                response.cooldownSeconds >
                0
            )
            {
                displayDuration =
                    response.cooldownSeconds;
            }
            else
            {
                displayDuration =
                    Mathf.RoundToInt(
                        minimumCooldownSeconds
                    );
            }

            AIFeedbackPanel.Instance
                .ShowFeedback(
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