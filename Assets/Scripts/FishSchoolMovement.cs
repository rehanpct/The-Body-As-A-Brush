using UnityEngine;

public class FishSchoolMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 0.5f;

    [Header("Loop Boundaries")]
    public float leftBoundary = -8f;
    public float rightSpawnPosition = 9f;

    private SpriteRenderer[] fishRenderers;

    void Start()
    {
        fishRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        // Fish already face the correct direction.
        SetFishFacingCorrectDirection();
    }

    void Update()
    {
        // Move from RIGHT to LEFT.
        transform.position +=
            Vector3.left *
            speed *
            Time.deltaTime;

        // Reached LEFT boundary.
        if (transform.position.x <= leftBoundary)
        {
            RespawnAtRight();
        }
    }

    void RespawnAtRight()
    {
        Vector3 position =
            transform.position;

        // Reappear at the RIGHT side.
        position.x =
            rightSpawnPosition;

        transform.position =
            position;

        Debug.Log(
            "🐟 Fish reached left edge and respawned at right."
        );
    }

    void SetFishFacingCorrectDirection()
    {
        if (fishRenderers == null)
            return;

        foreach (SpriteRenderer fish
                 in fishRenderers)
        {
            if (fish == null)
                continue;

            // IMPORTANT:
            // Keep the original fish orientation.
            fish.flipX = false;
        }
    }
}