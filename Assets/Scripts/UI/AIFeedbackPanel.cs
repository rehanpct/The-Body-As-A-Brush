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
                // Force TMP rich-text parsing at runtime.
                feedbackText.richText = true;
            }

            if (titleText != null)
            {
                titleText.text = "AI Feedback";

                // Title does not need rich text.
                titleText.richText = false;
            }
        }

        public void ShowFeedback(string message, int cooldownSeconds)
        {
            if (canvasGroup == null || feedbackText == null)
            {
                Debug.LogWarning(
                    "[AIFeedbackPanel] CanvasGroup or FeedbackText is not assigned."
                );

                return;
            }

            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }

            feedbackText.richText = true;
            feedbackText.text = FormatFeedback(message);

            displayCoroutine = StartCoroutine(DisplaySequence());
        }

        private string FormatFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Keep creating and exploring!";
            }

            message = message.Trim();

            // Remove any color tags that might already exist
            // in an AI response.
            message = Regex.Replace(
                message,
                @"</?color(?:=[^>]*)?>",
                "",
                RegexOptions.IgnoreCase
            );

            /*
             * ONE highlighting pass.
             *
             * This prevents words such as "fish" from being
             * highlighted again inside an existing <color> tag.
             */
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

            // Fade in
            yield return StartCoroutine(
                FadeCanvasGroup(
                    0f,
                    1f,
                    fadeInDuration
                )
            );

            // Visible for 10 seconds
            yield return new WaitForSeconds(
                displayDuration
            );

            // Fade out
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