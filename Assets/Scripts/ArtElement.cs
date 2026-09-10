using UnityEngine;

public class ArtElement : MonoBehaviour
{
    void Start()
    {
        // Random size
        float randomSize = Random.Range(0.35f, 0.65f);

        transform.localScale = new Vector3(
            randomSize,
            randomSize,
            1f
        );

        // Random rotation
        float randomRotation =
            Random.Range(0f, 360f);

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                randomRotation
            );
    }
}