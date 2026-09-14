using UnityEngine;

public class BackgroundMovement : MonoBehaviour
{
    [Header("Movement")]
    public float horizontalAmount = 0.03f;
    public float verticalAmount = 0.02f;
    public float movementSpeed = 0.15f;

    [Header("Scale")]
    public float scaleAmount = 0.03f;

    private Vector3 startPosition;
    private Vector3 startScale;

    void Start()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
    }

    void Update()
    {
        float wave =
            Mathf.Sin(Time.time * movementSpeed);

        float wave2 =
            Mathf.Cos(Time.time * movementSpeed * 0.8f);

        // Very small background movement.
        Vector3 position = startPosition;

        position.x += wave * horizontalAmount;
        position.y += wave2 * verticalAmount;

        transform.position = position;

        // Very subtle breathing/zoom effect.
        float scaleWave =
            Mathf.Sin(Time.time * movementSpeed * 0.7f)
            * scaleAmount;

        transform.localScale =
            startScale + Vector3.one * scaleWave;
    }
}