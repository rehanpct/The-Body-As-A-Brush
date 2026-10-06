using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using BodyAsBrush.UI;
using UnityEngine.Events;

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

    [Header("Automatic Feedback Voice Output")]
    [Tooltip("Connect a text-to-speech listener here. Automatic responses bypass the text panel.")]
    [SerializeField]
    private UnityEvent<string> automaticFeedbackVoiceOutput =
        new UnityEvent<string>();

    private const int MaximumRememberedAutomaticResponses = 32;

    private readonly Queue<string> recentAutomaticResponseOrder =
        new Queue<string>();

    private readonly HashSet<string> recentAutomaticResponseKeys =
        new HashSet<string>();
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

        public int lightTrailCount;
        public int lanternCount;
        public int petalsCount;
        public int fireworksCount;
        public int waterCount;
        public int coralCount;
        public int fishSchoolCount;
        public int bubbleBurstCount;

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
        public float sessionElapsedSeconds;

        public int indexGestureCount;
        public int vSignGestureCount;
        public int openPalmGestureCount;
        public int thumbUpGestureCount;
        public float pauseTime;

        public List<CompositionElement> elements =
            new List<CompositionElement>();

        public List<CompositionAnalyzer.ZoneInfo> grid =
            new List<CompositionAnalyzer.ZoneInfo>();

        public CompositionAdvisorRecommendation advisor;
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

        public bool automatic;

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

        ConnectAutomaticVoiceOutput();

        Debug.Log(
            "[AIFeedbackClient] " +
            "Initialised. Session: " +
            sessionId
        );
    }

    // ============================================================
    private void ConnectAutomaticVoiceOutput()
    {
        if (automaticFeedbackVoiceOutput == null)
            automaticFeedbackVoiceOutput = new UnityEvent<string>();

        AIFeedbackVoice receiver = GetComponent<AIFeedbackVoice>();
        if (receiver == null)
            receiver = FindFirstObjectByType<AIFeedbackVoice>();

        if (receiver == null)
        {
            Debug.LogWarning("[AIFeedbackClient] AIFeedbackVoice not found. Automatic responses will remain silent.");
            return;
        }

        bool persistentListenerExists = false;
        for (int i = 0; i < automaticFeedbackVoiceOutput.GetPersistentEventCount(); i++)
        {
            if (automaticFeedbackVoiceOutput.GetPersistentTarget(i) == receiver &&
                automaticFeedbackVoiceOutput.GetPersistentMethodName(i) == nameof(AIFeedbackVoice.Speak))
            {
                persistentListenerExists = true;
                break;
            }
        }

        if (!persistentListenerExists)
            automaticFeedbackVoiceOutput.AddListener(receiver.Speak);

        Debug.Log("[AIFeedbackClient] Automatic voice receiver connected.");
    }

    // NORMAL EVENT FEEDBACK
    // ============================================================

    public void RequestFeedback(
        string eventType
    )
    {
        if (isRequestInProgress)
        {
            Debug.Log("[AIFeedbackClient] Skipped — request already in progress.");
            return;
        }

        if (!CanSendRequest())
            return;

        if (CompositionAnalyzer.Instance == null)
        {
            Debug.LogWarning(
                "[AIFeedbackClient] CompositionAnalyzer not found; " +
                "automatic feedback needs a current artwork snapshot."
            );
            return;
        }

        CompositionAnalyzer.CompositionSnapshot snapshot =
            CompositionAnalyzer.Instance.Analyze();

        if (snapshot == null)
            return;

        StartCoroutine(
            SendCompositionRequest(snapshot, eventType, true)
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
                snapshot,
                "composition_update",
                true
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
            SendCompositionRequest(
                snapshot,
                "artwork_review",
                false
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
    // BUILD COMPOSITION CONTEXT
    // ============================================================

    private CompositionContext BuildCompositionContext(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        CompositionContext context = new CompositionContext();
        context.currentTheme = ThemeManager.Instance != null
            ? ThemeManager.Instance.CurrentTheme.ToString()
            : "Unknown";
        context.sessionElapsedSeconds = Time.time - sessionStartTime;

        List<CompositionAnalyzer.ElementInfo> validElements =
            new List<CompositionAnalyzer.ElementInfo>();

        if (snapshot.elements != null)
        {
            foreach (CompositionAnalyzer.ElementInfo element in snapshot.elements)
            {
                if (element == null ||
                    !CompositionAdvisor.IsElementAllowed(
                        context.currentTheme, element.type))
                    continue;

                validElements.Add(element);

                CompositionElement outgoing = new CompositionElement
                {
                    type = element.type,
                    normalizedX = element.normalizedPosition.x,
                    normalizedY = element.normalizedPosition.y,
                    width = element.normalizedSize.x,
                    height = element.normalizedSize.y,
                    relativeSize = element.relativeSize
                };
                context.elements.Add(outgoing);

                switch (element.type)
                {
                    case "light_trail": context.lightTrailCount++; break;
                    case "lantern": context.lanternCount++; break;
                    case "petals": context.petalsCount++; break;
                    case "fireworks": context.fireworksCount++; break;
                    case "water_current": context.waterCount++; break;
                    case "coral": context.coralCount++; break;
                    case "fish_school": context.fishSchoolCount++; break;
                    case "bubble_burst": context.bubbleBurstCount++; break;
                }
            }
        }

        context.totalElements = validElements.Count;
        context.grid = CompositionAnalyzer.BuildZoneGrid(validElements);
        context.advisor =
            CompositionAdvisor.Analyze(validElements, context.currentTheme);

        int left = 0, right = 0, top = 0, bottom = 0;
        float largest = 0f;
        int emptyCount = 0, leftEmpty = 0, rightEmpty = 0;
        int topEmpty = 0, bottomEmpty = 0;
        context.largestEmptyRegion = "";

        foreach (CompositionAnalyzer.ElementInfo element in validElements)
        {
            if (element.normalizedPosition.x < (1f / 3f)) left++;
            else if (element.normalizedPosition.x >= (2f / 3f)) right++;
            if (element.normalizedPosition.y >= (2f / 3f)) top++;
            else if (element.normalizedPosition.y < (1f / 3f)) bottom++;

            if (element.relativeSize > largest)
            {
                largest = element.relativeSize;
                context.largeObjectType = element.type;
            }
        }

        int horizontalCount = left + right;
        int verticalCount = top + bottom;
        context.leftDensity = horizontalCount > 0
            ? (float)left / horizontalCount : 0f;
        context.rightDensity = horizontalCount > 0
            ? (float)right / horizontalCount : 0f;
        context.topDensity = verticalCount > 0
            ? (float)top / verticalCount : 0f;
        context.bottomDensity = verticalCount > 0
            ? (float)bottom / verticalCount : 0f;
        context.largestObjectPercentage = largest;
        context.hasLargeObject = largest >= 0.12f;

        foreach (CompositionAnalyzer.ZoneInfo zone in context.grid)
        {
            if (!zone.isEmpty)
                continue;

            emptyCount++;
            if (zone.name.EndsWith("_LEFT")) leftEmpty++;
            if (zone.name.EndsWith("_RIGHT")) rightEmpty++;
            if (zone.name.StartsWith("TOP_")) topEmpty++;
            if (zone.name.StartsWith("BOTTOM_")) bottomEmpty++;
            if (string.IsNullOrEmpty(context.largestEmptyRegion))
                context.largestEmptyRegion =
                    zone.name.ToLowerInvariant().Replace("_", "-");
        }

        context.emptyLeft = leftEmpty / 3f;
        context.emptyRight = rightEmpty / 3f;
        context.emptyTop = topEmpty / 3f;
        context.emptyBottom = bottomEmpty / 3f;
        context.emptySpacePercentage = emptyCount / 9f;

        float distanceTotal = 0f;
        int pairCount = 0;
        for (int i = 0; i < validElements.Count; i++)
        {
            for (int j = i + 1; j < validElements.Count; j++)
            {
                float distance = Vector2.Distance(
                    validElements[i].normalizedPosition,
                    validElements[j].normalizedPosition
                );
                distanceTotal += distance;
                pairCount++;
                if (distance <= 0.15f)
                    context.hasCluster = true;
            }
        }
        context.averageDistance = pairCount > 0
            ? distanceTotal / pairCount : 0f;

        if (ArtworkStatistics.Instance != null &&
            ArtworkStatistics.Instance.data != null)
        {
            ArtworkData data = ArtworkStatistics.Instance.data;
            context.indexGestureCount = data.indexGestureCount;
            context.vSignGestureCount = data.vSignGestureCount;
            context.openPalmGestureCount = data.openPalmGestureCount;
            context.thumbUpGestureCount = data.thumbUpGestureCount;
            context.pauseTime = data.pauseTime;
        }

        return context;
    }

    // ============================================================
    // COMPOSITION FEEDBACK REQUEST
    // ============================================================

    private IEnumerator SendCompositionRequest(
        CompositionAnalyzer.CompositionSnapshot snapshot,
        string eventType,
        bool automatic
    )
    {
        isRequestInProgress = true;

        CompositionRequest request = new CompositionRequest
        {
            projectType = "gesture_painting",
            sessionId = sessionId,
            eventType = eventType,
            occurredAtUtc = DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ssZ"
            ),
            automatic = automatic,
            context = BuildCompositionContext(snapshot)
        };

        string json = JsonUtility.ToJson(request);
        byte[] rawBody = Encoding.UTF8.GetBytes(json);

        Debug.Log(
            "[AIFeedbackClient] Sending '" + eventType +
            "' (automatic=" + automatic + ") to Render."
        );
        Debug.Log("[AIFeedbackClient] Payload: " + json);

        using (UnityWebRequest webRequest =
               new UnityWebRequest(renderUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(rawBody);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.timeout = 30;

            yield return webRequest.SendWebRequest();

            isRequestInProgress = false;
            lastRequestTime = Time.time;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                HandleAIResponse(
                    webRequest.downloadHandler.text,
                    automatic,
                    request.context
                );
            }
            else
            {
                Debug.LogError(
                    "[AIFeedbackClient] Request failed — " +
                    webRequest.error + " (HTTP " +
                    webRequest.responseCode + ")"
                );
            }
        }
    }

    // ============================================================
    private static bool IsAutomaticResponseConsistent(
        FeedbackResponse response,
        CompositionContext expectedContext
    )
    {
        CompositionAdvisorRecommendation advisor = expectedContext.advisor;
        if (response.action == null ||
            response.action.type != advisor.action ||
            response.action.target != (advisor.element ?? "") ||
            response.action.value != (advisor.zone ?? ""))
            return false;

        if (string.IsNullOrWhiteSpace(response.message) ||
            CountFeedbackSentences(response.message) > 2)
            return false;

        if (!string.IsNullOrEmpty(advisor.element) &&
            advisor.action != "NONE" &&
            !MessageMentionsElementType(response.message, advisor.element))
            return false;

        if (MessageMentionsUnsupportedOrAlternativeElement(
                response.message, expectedContext, advisor.element))
            return false;

        if ((advisor.action == "LEAVE_OPEN" || advisor.action == "NONE" ||
             advisor.action == "VARY_SIZE") &&
            MessageHasPlacementLanguage(response.message))
            return false;

        HashSet<string> mentionedZones = FindZoneMentions(response.message);
        string targetZone = CanonicalZoneName(advisor.zone);
        string sourceZone = FindSourceZone(expectedContext.grid);
        foreach (string zone in mentionedZones)
        {
            if (zone != targetZone && zone != sourceZone)
                return false;
        }

        if ((advisor.action == "ADD" || advisor.action == "BALANCE" ||
             advisor.action == "SPREAD") &&
            !mentionedZones.Contains(targetZone))
            return false;

        if ((advisor.action == "ADD" || advisor.action == "BALANCE" ||
             advisor.action == "SPREAD") &&
            !MessageHasAdvisorActionLanguage(response.message, advisor.action))
            return false;

        return true;
    }

    private static bool MessageHasAdvisorActionLanguage(string message, string action)
    {
        string[] words;
        switch (action)
        {
            case "ADD":
                words = new[] { "add", "adding", "place", "placing", "put", "try", "consider", "near", "nearby", "connect", "toward" };
                break;
            case "SPREAD":
                words = new[] { "spread", "spreading", "place", "placing", "move", "moving", "try", "consider", "near", "toward" };
                break;
            default:
                words = new[] { "balance", "balancing", "place", "placing", "add", "adding", "try", "consider", "near", "toward" };
                break;
        }

        foreach (string word in words)
            if (ContainsWholePhrase(NormalizeMessage(message), word))
                return true;
        return false;
    }

    private static bool MessageHasPlacementLanguage(string message)
    {
        string normalized = NormalizeMessage(message);
        string[] words =
        {
            "add", "adding", "place", "placed", "places", "placing",
            "put", "putting", "introduce", "introducing", "fill", "filling",
            "move", "moving", "spread", "spreading"
        };
        foreach (string word in words)
            if (ContainsWholePhrase(normalized, word))
                return true;
        return false;
    }

    private static bool MessageMentionsUnsupportedOrAlternativeElement(
        string message,
        CompositionContext context,
        string recommendedType
    )
    {
        string[] types =
        {
            "light_trail", "lantern", "petals", "fireworks",
            "water_current", "coral", "fish_school", "bubble_burst"
        };
        string[] sentences = Regex.Split(message ?? "", @"[.!?]+");
        foreach (string type in types)
        {
            if (type == recommendedType || !MessageMentionsElementType(message, type))
                continue;

            bool measured = false;
            if (context.elements != null)
            {
                foreach (CompositionElement element in context.elements)
                {
                    if (element != null && element.type == type)
                    {
                        measured = true;
                        break;
                    }
                }
            }
            if (!measured) return true;

            foreach (string sentence in sentences)
            {
                if (MessageMentionsElementType(sentence, type) &&
                    MessageHasPlacementLanguage(sentence))
                    return true;
            }
        }
        return false;
    }

    private static bool MessageMentionsElementType(string message, string type)
    {
        string[] aliases;
        switch (type)
        {
            case "light_trail": aliases = new[] { "light trail", "light trails", "trail", "trails" }; break;
            case "lantern": aliases = new[] { "lantern", "lanterns" }; break;
            case "petals": aliases = new[] { "petal", "petals" }; break;
            case "fireworks": aliases = new[] { "firework", "fireworks" }; break;
            case "water_current": aliases = new[] { "water current", "water currents" }; break;
            case "coral": aliases = new[] { "coral", "corals" }; break;
            case "fish_school": aliases = new[] { "fish", "fishes", "fish school", "fish schools" }; break;
            case "bubble_burst": aliases = new[] { "bubble", "bubbles", "bubble burst", "bubble bursts" }; break;
            default: return false;
        }

        string normalized = NormalizeMessage(message);
        foreach (string alias in aliases)
            if (ContainsWholePhrase(normalized, alias))
                return true;
        return false;
    }

    private static HashSet<string> FindZoneMentions(string message)
    {
        string normalized = Regex.Replace(NormalizeMessage(message), @"\s+", " ");
        HashSet<string> zones = new HashSet<string>();
        MatchCollection matches = Regex.Matches(
            normalized,
            @"\b(?:(?<row>top|upper|middle|bottom|lower)\s+(?<column>left|center|right)|(?<side>left|right)\s+center|center)\b"
        );

        foreach (Match match in matches)
        {
            if (match.Groups["row"].Success)
            {
                string row = match.Groups["row"].Value;
                string column = match.Groups["column"].Value;
                string normalizedRow = row == "top" || row == "upper"
                    ? "TOP"
                    : row == "bottom" || row == "lower"
                        ? "BOTTOM"
                        : "MIDDLE";
                zones.Add(normalizedRow + "_" + column.ToUpperInvariant());
            }
            else if (match.Groups["side"].Success)
            {
                zones.Add("MIDDLE_" + match.Groups["side"].Value.ToUpperInvariant());
            }
            else
            {
                zones.Add("MIDDLE_CENTER");
            }
        }

        if (Regex.IsMatch(normalized, @"\bnorth\s*west\b")) zones.Add("TOP_LEFT");
        if (Regex.IsMatch(normalized, @"\bnorth\s*east\b")) zones.Add("TOP_RIGHT");
        if (Regex.IsMatch(normalized, @"\bsouth\s*west\b")) zones.Add("BOTTOM_LEFT");
        if (Regex.IsMatch(normalized, @"\bsouth\s*east\b")) zones.Add("BOTTOM_RIGHT");
        return zones;
    }

    private static string FindSourceZone(List<CompositionAnalyzer.ZoneInfo> grid)
    {
        if (grid == null || grid.Count == 0) return "";
        CompositionAnalyzer.ZoneInfo source = null;
        foreach (CompositionAnalyzer.ZoneInfo zone in grid)
        {
            if (zone != null && (source == null || zone.elementCount > source.elementCount))
                source = zone;
        }
        return source != null ? CanonicalZoneName(source.name) : "";
    }

    private static string CanonicalZoneName(string zone)
    {
        if (string.IsNullOrWhiteSpace(zone)) return "";
        string normalized = zone.Trim().ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
        return Regex.Replace(normalized, @"_+", "_");
    }

    private static int CountFeedbackSentences(string message)
    {
        return Regex.Matches(message ?? "", @"[.!?]+(?=\s|$)").Count;
    }

    private static string NormalizeMessage(string message)
    {
        return (message ?? "").ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
    }

    private static bool ContainsWholePhrase(string text, string phrase)
    {
        int index = 0;
        while ((index = text.IndexOf(phrase, index, StringComparison.Ordinal)) >= 0)
        {
            int end = index + phrase.Length;
            bool leftBoundary = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            bool rightBoundary = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (leftBoundary && rightBoundary) return true;
            index = end;
        }
        return false;
    }

    private bool IsDuplicateAutomaticResponse(FeedbackResponse response)
    {
        string key = !string.IsNullOrWhiteSpace(response.requestId)
            ? "request:" + response.requestId.Trim()
            : "message:" + response.message.Trim();

        if (recentAutomaticResponseKeys.Contains(key))
            return true;

        recentAutomaticResponseKeys.Add(key);
        recentAutomaticResponseOrder.Enqueue(key);
        while (recentAutomaticResponseOrder.Count > MaximumRememberedAutomaticResponses)
            recentAutomaticResponseKeys.Remove(recentAutomaticResponseOrder.Dequeue());

        return false;
    }

    // HANDLE AI RESPONSE
    // ============================================================

    private void HandleAIResponse(
        string responseText
    )
    {
        HandleAIResponse(responseText, false);
    }

    private void HandleAIResponse(
        string responseText,
        bool automatic
    )
    {
        HandleAIResponse(responseText, automatic, null);
    }

    private void HandleAIResponse(
        string responseText,
        bool automatic,
        CompositionContext expectedContext
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

        if (automatic && expectedContext != null &&
            expectedContext.advisor != null &&
            !IsAutomaticResponseConsistent(response, expectedContext))
        {
            CompositionAdvisorRecommendation advisor = expectedContext.advisor;
            Debug.LogWarning(
                "[AIFeedbackClient] Automatic response conflicted with Unity's " +
                "composition advisor. Using the deterministic advisor message."
            );
            response.message = !string.IsNullOrWhiteSpace(advisor.fallbackMessage)
                ? advisor.fallbackMessage
                : "Keep some open space as you continue shaping the composition.";
            response.feedbackType = "hint";
            response.action = new FeedbackAction
            {
                type = advisor.action ?? "NONE",
                target = advisor.element ?? "",
                value = advisor.zone ?? ""
            };
            response.priority = "normal";
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

        // Automatic feedback is routed only to the configured voice output.
        // Manual reviews remain text-only in the review panel.
        if (automatic)
        {
            if (IsDuplicateAutomaticResponse(response))
            {
                Debug.Log("[AIFeedbackClient] Duplicate automatic response suppressed.");
                return;
            }

            if (automaticFeedbackVoiceOutput == null)
                automaticFeedbackVoiceOutput = new UnityEvent<string>();

            automaticFeedbackVoiceOutput.Invoke(response.message);
            return;
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
                    displayDuration,
                    response.feedbackType == "review"
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