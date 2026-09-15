using System.Collections;
using UnityEngine;

namespace BodyAsBrush.UI
{
    public class StorySequenceController : MonoBehaviour
    {
        [System.Serializable]
        public class SequenceGroup
        {
            public CanvasGroup canvasGroup;
            public RectTransform rectTransform;
            public float delay = 0.3f;
            public float fadeDuration = 0.6f;
            public float startYOffset = -15f;
        }

        [Header("Sequence Elements")]
        [SerializeField] private SequenceGroup[] sequenceGroups;

        private void Start()
        {
            // Initialize groups to hidden state
            foreach (var group in sequenceGroups)
            {
                if (group.canvasGroup != null)
                {
                    group.canvasGroup.alpha = 0f;
                }

                if (group.rectTransform != null)
                {
                    Vector3 pos = group.rectTransform.anchoredPosition;
                    pos.y += group.startYOffset;
                    group.rectTransform.anchoredPosition = pos;
                }
            }

            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            foreach (var group in sequenceGroups)
            {
                if (group.delay > 0f)
                {
                    yield return new WaitForSeconds(group.delay);
                }

                if (group.canvasGroup != null)
                {
                    StartCoroutine(FadeInGroup(group));
                }
            }
        }

        private IEnumerator FadeInGroup(SequenceGroup group)
        {
            float elapsedTime = 0f;
            float duration = Mathf.Max(0.1f, group.fadeDuration);

            Vector3 startPos = group.rectTransform != null ? group.rectTransform.anchoredPosition : Vector3.zero;
            Vector3 targetPos = group.rectTransform != null ? startPos - new Vector3(0f, group.startYOffset, 0f) : Vector3.zero;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (group.canvasGroup != null)
                {
                    group.canvasGroup.alpha = smoothT;
                }

                if (group.rectTransform != null)
                {
                    group.rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, smoothT);
                }

                yield return null;
            }

            if (group.canvasGroup != null)
            {
                group.canvasGroup.alpha = 1f;
            }

            if (group.rectTransform != null)
            {
                group.rectTransform.anchoredPosition = targetPos;
            }
        }
    }
}
