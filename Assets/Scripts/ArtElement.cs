using UnityEngine;

public class ArtElement : MonoBehaviour
{
    void Start()
    {
        float randomSize = Random.Range(0.35f, 0.65f);

        transform.localScale = new Vector3(
            randomSize,
            randomSize,
            1f
        );

        // Keep coral in its original orientation.
        transform.rotation = Quaternion.identity;
    }
}