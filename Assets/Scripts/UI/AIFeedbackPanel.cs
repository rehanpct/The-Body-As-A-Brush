using System.Collections;
using TMPro;
using UnityEngine;

namespace BodyAsBrush.UI
{
    /// <summary>
    /// AIFeedbackPanel — singleton UI panel that displays AI coaching messages
    /// returned from the Render backend.
    ///
    /// Attach to a Panel GameObject that is a child of the existing Canvas
    /// in StoryScene. Wire CanvasGroup and FeedbackText in the Inspector.
    ///
    /// Panel sits at the bottom-centre of the screen, above the painting layer.
    /// It fades in, holds for cooldownSeconds, then fades out automatically.
    /// It does not block gesture input (blocksRaycasts = false).
    /// </summary>
    public class AIFeedbackPanel : MonoBehaviour
    {
        public static AIFeedbackPanel Instance;

        // =========================================================
        // INSPECTOR
        // =========================================================

        [Header("UI References")]
        [Tooltip("CanvasGroup on this Panel for fade in/out.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("TextMeshProUGUI that displays the AI message.")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Animation")]
        [Tooltip("Seconds to fade in.")]
        [SerializeField] private float fadeInDuration  = 0.5f;

        [Tooltip("Seconds to fade out.")]
        [SerializeField] private float fadeOutDuration = 0.8f;

        // =========================================================
        // STATE
        // =========================================================

        private Coroutine displayCoroutine;

        // =========================================================
        // AWAKE
        // =========================================================

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Start fully hidden, non-interactive
            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        // =========================================================
        // PUBLIC API
        // =========================================================

        /// <summary>
        /// Display an AI feedback message for the given duration.
        /// Safe to call while a message is already showing (restarts it).
        /// </summary>
        public void ShowFeedback(string message, int cooldownSeconds)
        {
            if (canvasGroup == null || feedbackText == null)
            {
                Debug.LogWarning(
                    "[AIFeedbackPanel] CanvasGroup or FeedbackText " +
                    "not assigned in Inspector!"
                );
                return;
            }

            // Stop any current display before starting a new one
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }

            feedbackText.text = message;

            displayCoroutine = StartCoroutine(
                DisplaySequence(cooldownSeconds)
            );
        }

        // =========================================================
        // DISPLAY SEQUENCE
        // =========================================================

        private IEnumerator DisplaySequence(int cooldownSeconds)
        {
            // Fade in
            yield return StartCoroutine(
                FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration)
            );

            // Hold (total cooldown minus the fade times)
            float holdTime = Mathf.Max(
                1f,
                cooldownSeconds - fadeInDuration - fadeOutDuration
            );

            yield return new WaitForSeconds(holdTime);

            // Fade out
            yield return StartCoroutine(
                FadeCanvasGroup(canvasGroup, 1f, 0f, fadeOutDuration)
            );

            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        // =========================================================
        // FADE HELPER
        // =========================================================

        private IEnumerator FadeCanvasGroup(
            CanvasGroup group,
            float startAlpha,
            float endAlpha,
            float duration)
        {
            float elapsed = 0f;
            group.alpha   = startAlpha;

            while (elapsed < duration)
            {
                elapsed     += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / duration);
                group.alpha  = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            group.alpha = endAlpha;
        }
    }
}
