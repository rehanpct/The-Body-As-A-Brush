using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 25f;

    [Header("Loop Settings")]
    [SerializeField] private float extraPadding = 150f;

    private RectTransform cloud;
    private RectTransform canvas;

    private void Awake()
    {
        cloud = GetComponent<RectTransform>();

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null)
        {
            canvas = parentCanvas.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (cloud == null || canvas == null)
            return;

        Vector2 position = cloud.anchoredPosition;

        // Move from left to right
        position.x += speed * Time.deltaTime;

        // Right edge of the Canvas
        float rightEdge =
            canvas.rect.width / 2f +
            cloud.rect.width / 2f +
            extraPadding;

        // Left edge of the Canvas
        float leftEdge =
            -canvas.rect.width / 2f -
            cloud.rect.width / 2f -
            extraPadding;

        // When cloud leaves the right side,
        // instantly bring it back to the left.
        if (position.x > rightEdge)
        {
            position.x = leftEdge;
        }

        cloud.anchoredPosition = position;
    }
}