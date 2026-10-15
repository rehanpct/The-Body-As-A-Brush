using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace BodyAsBrush.UI
{
    public class AIFeedbackPanel : MonoBehaviour
    {
        public static AIFeedbackPanel Instance;

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 10f;
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Highlight Colors")]
        [SerializeField] private string highlightColor = "#FFD54F";
        [SerializeField] private string coralColor = "#FF8A65";

        private Coroutine displayCoroutine;
        public string DisplayedFeedback => feedbackText != null ? feedbackText.text : string.Empty;

        private void OnEnable()
        {
            LanguageManager.LanguageChanged += RefreshLanguage;
        }

        private void OnDisable()
        {
            LanguageManager.LanguageChanged -= RefreshLanguage;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (feedbackText != null)
            {
                // Keep TextMeshPro rich-text parsing enabled for highlights.
                feedbackText.richText = true;
            }

            if (titleText != null)
            {
                titleText.richText = false;
                RefreshTitle();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            LanguageManager.LanguageChanged -= RefreshLanguage;
        }

        private bool IsTraditionalChinese()
        {
            return LanguageManager.IsTraditionalChinese;
        }

        private void RefreshTitle()
        {
            if (titleText == null)
            {
                return;
            }

            titleText.text = GetLocalizedTitle(false);
        }

        private void RefreshLanguage()
        {
            RefreshTitle();
            if (feedbackText != null && !string.IsNullOrWhiteSpace(feedbackText.text))
                feedbackText.text = GetFallbackMessage();
        }

        private string GetLocalizedTitle(bool isCreativeReview)
        {
            if (IsTraditionalChinese())
            {
                return isCreativeReview ? "AI 創作評析" : "AI 創作回饋";
            }

            return isCreativeReview ? "AI Creative Review" : "AI Feedback";
        }

        private string GetFallbackMessage()
        {
            return IsTraditionalChinese()
                ? "繼續創作，探索更多可能！"
                : "Keep creating and exploring!";
        }

        public void ShowFeedback(string message, int cooldownSeconds)
        {
            ShowFeedback(message, cooldownSeconds, false);
        }

        public void ShowFeedback(
            string message,
            int cooldownSeconds,
            bool isCreativeReview
        )
        {
            if (canvasGroup == null)
            {
                Debug.LogError("[ManualFeedback] AIFeedbackPanel.canvasGroup is not assigned on " + name, this);
                return;
            }
            if (feedbackText == null)
            {
                Debug.LogError("[ManualFeedback] AIFeedbackPanel.feedbackText is not assigned on " + name, this);
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (!enabled)
                enabled = true;
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError("[ManualFeedback] AIFeedbackPanel is hidden by an inactive parent: " +
                    transform.parent.name, this);
                return;
            }

            if (titleText != null)
            {
                titleText.text = GetLocalizedTitle(isCreativeReview);
            }

            if (displayCoroutine != null)
            {
                // Stop the previous sequence and its separately started fade coroutine.
                StopAllCoroutines();
            }

            feedbackText.richText = true;
            feedbackText.text = FormatFeedback(message);
            Debug.Log("[AIFeedbackPanel] Language = " + LanguageManager.CurrentLanguage +
                "; Displaying feedback = " + feedbackText.text);

            displayCoroutine = StartCoroutine(DisplaySequence());
        }

        private string FormatFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return GetFallbackMessage();
            }

            message = message.Trim();

            // Remove any color tags that might already exist in an AI response.
            message = Regex.Replace(
                message,
                @"</?color(?:=[^>]*)?>",
                "",
                RegexOptions.IgnoreCase
            );

            // Highlight English keywords only. Chinese feedback remains unchanged.
            string pattern =
                @"coral|fish schools|movement|fish|bubbles|water|balance|composition|color|space";

            return Regex.Replace(
                message,
                pattern,
                match =>
                {
                    string word = match.Value;

                    if (word.Equals(
                        "coral",
                        System.StringComparison.OrdinalIgnoreCase))
                    {
                        return $"<color={coralColor}>{word}</color>";
                    }

                    return $"<color={highlightColor}>{word}</color>";
                },
                RegexOptions.IgnoreCase
            );
        }

        private IEnumerator DisplaySequence()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            yield return StartCoroutine(
                FadeCanvasGroup(
                    0f,
                    1f,
                    fadeInDuration
                )
            );

            yield return new WaitForSeconds(displayDuration);

            yield return StartCoroutine(
                FadeCanvasGroup(
                    1f,
                    0f,
                    fadeOutDuration
                )
            );

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            displayCoroutine = null;
        }

        private IEnumerator FadeCanvasGroup(
            float startAlpha,
            float endAlpha,
            float duration)
        {
            float elapsed = 0f;

            canvasGroup.alpha = startAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                // Smooth easing.
                t = t * t * (3f - 2f * t);

                canvasGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    t
                );

                yield return null;
            }

            canvasGroup.alpha = endAlpha;
        }
    }
}
