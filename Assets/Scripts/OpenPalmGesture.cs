using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class OpenPalmGesture : MonoBehaviour
{
    [Header("Gesture Manager")]
    public GestureManager gestureManager;

    [Header("Coral")]
    public GameObject elementPrefab;

    [Header("Settings")]
    public float spawnCooldown = 1.0f;

    private float nextSpawnTime = 0f;
    private bool wasOpenPalm = false;

    private float targetX;
    private float targetY;

    void Start()
    {
        if (gestureManager == null)
        {
            Debug.LogError(
                "OpenPalmGesture: GestureManager is not assigned!"
            );
        }
    }

    void Update()
    {
        if (gestureManager == null)
            return;

        bool isOpenPalm =
            gestureManager.CurrentGesture ==
            GestureManager.Gesture.OpenPalm;

        // Only trigger when entering Open Palm.
        if (isOpenPalm && !wasOpenPalm)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnCoral();
                nextSpawnTime =
                    Time.time + spawnCooldown;
            }
        }

        wasOpenPalm = isOpenPalm;
    }

    void SpawnCoral()
    {
        if (elementPrefab == null)
        {
            Debug.LogError(
                "OpenPalmGesture: Coral prefab is not assigned!"
            );
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // Use current hand position from HandTrackingBrush.
        HandTrackingBrush brush =
            FindFirstObjectByType<HandTrackingBrush>();

        if (brush == null || brush.brush == null)
            return;

        Vector3 spawnPosition =
            brush.brush.position;

        spawnPosition.z = 0f;

        Instantiate(
            elementPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}