using UnityEngine;
using UnityEngine.InputSystem;

public class ElementSpawner : MonoBehaviour
{
    public GestureDetector gestureDetector;

    public GameObject coralPrefab;
    public GameObject fishPrefab;
    public GameObject bubblePrefab;

    public float spawnCooldown = 0.3f;

    private float nextSpawnTime = 0f;

    void Update()
    {
        if (gestureDetector == null)
        {
            return;
        }

        if (gestureDetector.coralGesture)
        {
            SpawnElement(coralPrefab);
        }

        if (gestureDetector.fishGesture)
        {
            SpawnElement(fishPrefab);
        }

        if (gestureDetector.bubbleGesture)
        {
            SpawnElement(bubblePrefab);
        }
    }

    void SpawnElement(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3(
            mousePosition.x,
            mousePosition.y,
            10f
        );

        Vector3 worldPosition =
            Camera.main.ScreenToWorldPoint(screenPosition);

        Instantiate(
            prefab,
            worldPosition,
            Quaternion.identity
        );

        nextSpawnTime = Time.time + spawnCooldown;
    }
}