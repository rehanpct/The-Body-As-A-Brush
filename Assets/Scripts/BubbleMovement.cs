using UnityEngine;

public class BubbleMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 0.5f;

    [Header("Wobble")]
    public float wobbleAmount = 0.08f;
    public float wobbleSpeed = 2f;

    [Header("Lifetime")]
    public float lifetime = 8f;

    private float randomOffset;
    private float startX;

    void Start()
    {
        randomOffset = Random.Range(0f, 100f);
        startX = transform.position.x;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move upward
        transform.position +=
            Vector3.up * speed * Time.deltaTime;

        // Gentle left/right movement
        float wobble =
            Mathf.Sin(
                (Time.time + randomOffset) * wobbleSpeed
            ) * wobbleAmount;

        Vector3 position = transform.position;
        position.x = startX + wobble;

        transform.position = position;
    }
}