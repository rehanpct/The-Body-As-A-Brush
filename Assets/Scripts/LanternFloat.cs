using UnityEngine;

public class LanternFloat : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float upwardSpeed = 0.35f;
    [SerializeField] private float horizontalAmount = 0.15f;
    [SerializeField] private float horizontalSpeed = 0.5f;

    private Vector3 startPosition;
    private float randomOffset;

    private void Start()
    {
        startPosition = transform.position;
        randomOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float time = Time.time + randomOffset;

        // Move upward continuously.
        transform.position += Vector3.up * upwardSpeed * Time.deltaTime;

        // Gentle left/right drift.
        float drift = Mathf.Sin(time * horizontalSpeed) * horizontalAmount;

        transform.position = new Vector3(
            startPosition.x + drift,
            transform.position.y,
            startPosition.z
        );
    }
}