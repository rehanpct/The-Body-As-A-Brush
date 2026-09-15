using UnityEngine;
using UnityEngine.UI;

namespace BodyAsBrush.UI
{
    public class AtmosphericBackground : MonoBehaviour
    {
        [Header("Ambient Background Motion")]
        [SerializeField] private RectTransform mainBackgroundRect;
        [SerializeField] private float moveSpeed = 0.04f;
        [SerializeField] private float moveDistanceX = 20f;
        [SerializeField] private float moveDistanceY = 12f;

        [Header("Breathing Scale")]
        [SerializeField] private float scaleSpeed = 0.03f;
        [SerializeField] private float scaleAmount = 0.025f;

        [Header("Trace Motif Drift (Movement -> Trace -> Art)")]
        [SerializeField] private RectTransform traceMotifRect;
        [SerializeField] private float motifDriftSpeed = 12f;
        [SerializeField] private float motifWobbleSpeed = 0.5f;
        [SerializeField] private float motifWobbleAmount = 15f;
        [SerializeField] private float resetLeftX = -1100f;
        [SerializeField] private float resetRightX = 1100f;

        [Header("Alpha Pulse")]
        [SerializeField] private bool enableAlphaPulse = true;
        [SerializeField] private float pulseSpeed = 0.025f;
        [SerializeField] private float minAlpha = 0.88f;
        [SerializeField] private float maxAlpha = 1.0f;
        [SerializeField] private CanvasGroup canvasGroup;

        private Vector3 bgBasePosition;
        private Vector3 bgBaseScale;
        private Vector3 motifStartPosition;

        private void Awake()
        {
            if (mainBackgroundRect == null)
            {
                mainBackgroundRect = GetComponent<RectTransform>();
            }

            if (mainBackgroundRect != null)
            {
                bgBasePosition = mainBackgroundRect.anchoredPosition;
                bgBaseScale = mainBackgroundRect.localScale;
            }

            if (traceMotifRect != null)
            {
                motifStartPosition = traceMotifRect.anchoredPosition;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Update()
        {
            float time = Time.time;

            // Ambient background movement
            if (mainBackgroundRect != null)
            {
                float offsetX = Mathf.Sin(time * moveSpeed) * moveDistanceX;
                float offsetY = Mathf.Cos(time * moveSpeed * 0.75f) * moveDistanceY;
                mainBackgroundRect.anchoredPosition = bgBasePosition + new Vector3(offsetX, offsetY, 0f);

                float scaleMod = 1f + (Mathf.Sin(time * scaleSpeed) * scaleAmount);
                mainBackgroundRect.localScale = bgBaseScale * scaleMod;
            }

            // Trace motif slow drift across screen
            if (traceMotifRect != null)
            {
                Vector3 currentPos = traceMotifRect.anchoredPosition;
                currentPos.x += motifDriftSpeed * Time.deltaTime;
                currentPos.y = motifStartPosition.y + (Mathf.Sin(time * motifWobbleSpeed) * motifWobbleAmount);

                if (currentPos.x > resetRightX)
                {
                    currentPos.x = resetLeftX;
                }

                traceMotifRect.anchoredPosition = currentPos;
            }

            // Subtle alpha pulse
            if (enableAlphaPulse && canvasGroup != null)
            {
                float alphaLerp = (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f;
                canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, alphaLerp);
            }
        }
    }
}
