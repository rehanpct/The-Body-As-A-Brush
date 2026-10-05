using UnityEngine;

public class PetalFloat : MonoBehaviour
{
    [Header("Fall")]
    [SerializeField] private float fallSpeed = 0.12f;

    [Header("Very Small Drift")]
    [SerializeField] private float driftSpeed = 0.03f;

    private float driftDirection;
    private float randomOffset;

    private void Start()
    {
        driftDirection = Random.Range(-1f, 1f);
        randomOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        // Move straight down
        transform.Translate(
            Vector3.down * fallSpeed * Time.deltaTime,
            Space.World
        );

        // Extremely small natural drift
        float drift =
            Mathf.Sin(Time.time * 0.4f + randomOffset)
            * driftSpeed;

        transform.Translate(
            Vector3.right *
            drift *
            Time.deltaTime *
            driftDirection,
            Space.World
        );
    }
}