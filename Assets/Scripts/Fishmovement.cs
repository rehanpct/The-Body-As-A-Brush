using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 0.5f;

    [Header("Wobble")]
    public float wobbleAmount = 0.08f;
    public float wobbleSpeed = 2f;

    [Header("Lifetime")]
    public float lifetime = 12f;

    private float randomOffset;
    private float startY;

    void Start()
    {
        randomOffset = Random.Range(0f, 100f);
        startY = transform.position.y;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Fish faces LEFT, so move LEFT.
        transform.position +=
            Vector3.left * speed * Time.deltaTime;

        // Gentle swimming motion.
        float wobble =
            Mathf.Sin(
                (Time.time + randomOffset) * wobbleSpeed
            ) * wobbleAmount;

        Vector3 position = transform.position;
        position.y = startY + wobble;

        transform.position = position;
    }
}