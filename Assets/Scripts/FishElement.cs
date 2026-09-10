using UnityEngine;

public class FishElement : MonoBehaviour
{
    void Start()
    {
        // Slight size variation
        float randomSize = Random.Range(0.8f, 1.2f);

        transform.localScale *= randomSize;

        // Fish should remain mostly horizontal.
        // Only a small rotation variation.
        float randomRotation = Random.Range(-15f, 15f);

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                randomRotation
            );
    }
}