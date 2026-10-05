using UnityEngine;

public class HangingLanternSway : MonoBehaviour
{
    [Header("Sway")]
    [SerializeField] private float angle = 3f;
    [SerializeField] private float speed = 0.8f;

    private float randomOffset;

    private void Start()
    {
        randomOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float time = Time.time + randomOffset;

        float rotation =
            Mathf.Sin(time * speed) * angle;

        transform.localRotation =
            Quaternion.Euler(0f, 0f, rotation);
    }
}