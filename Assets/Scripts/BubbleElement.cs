using UnityEngine;

public class BubbleElement : MonoBehaviour
{
    void Start()
    {
        // Random bubble size
        float randomSize = Random.Range(0.7f, 1.3f);

        transform.localScale *= randomSize;

        // Small random rotation
        float randomRotation = Random.Range(-10f, 10f);

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                randomRotation
            );
    }
}