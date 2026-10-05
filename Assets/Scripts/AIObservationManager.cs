using UnityEngine;

public class AIObservationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CompositionAnalyzer compositionAnalyzer;

    [SerializeField]
    private AIFeedbackClient aiFeedbackClient;

    [Header("Observation Settings")]
    [SerializeField]
    private float observationInterval = 20f;

    [SerializeField]
    private int minimumElementChange = 1;

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugLogs = true;

    private float nextObservationTime;

    private int lastElementCount = -1;

    private float lastLeftDensity;
    private float lastRightDensity;
    private float lastTopDensity;
    private float lastBottomDensity;

    private bool hasPreviousSnapshot = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (compositionAnalyzer == null)
        {
            compositionAnalyzer =
                FindFirstObjectByType<CompositionAnalyzer>();
        }

        if (aiFeedbackClient == null)
        {
            aiFeedbackClient =
                FindFirstObjectByType<AIFeedbackClient>();
        }

        nextObservationTime =
            Time.time + observationInterval;

        if (enableDebugLogs)
        {
            Debug.Log(
                "[AIObservationManager] Started. " +
                "Observation interval: " +
                observationInterval +
                " seconds."
            );
        }

        if (compositionAnalyzer == null)
        {
            Debug.LogError(
                "[AIObservationManager] " +
                "CompositionAnalyzer not found!"
            );
        }

        if (aiFeedbackClient == null)
        {
            Debug.LogError(
                "[AIObservationManager] " +
                "AIFeedbackClient not found!"
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (Time.time < nextObservationTime)
        {
            return;
        }

        nextObservationTime =
            Time.time + observationInterval;

        if (enableDebugLogs)
        {
            Debug.Log(
                "[AIObservationManager] " +
                "20-second observation triggered."
            );
        }

        ObserveArtwork();
    }

    // =========================================================
    // OBSERVE ARTWORK
    // =========================================================

    private void ObserveArtwork()
    {
        if (compositionAnalyzer == null)
        {
            return;
        }

        if (aiFeedbackClient == null)
        {
            return;
        }

        CompositionAnalyzer.CompositionSnapshot snapshot =
            compositionAnalyzer.Analyze();

        if (snapshot == null)
        {
            return;
        }

        if (snapshot.totalElements == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    "[AIObservationManager] " +
                    "No artwork yet."
                );
            }

            return;
        }

        bool meaningfulChange =
            HasMeaningfulChange(snapshot);

        if (enableDebugLogs)
        {
            Debug.Log(
                "[AIObservationManager] " +
                "Artwork observed. " +
                "Elements: " +
                snapshot.totalElements +
                " | Meaningful change: " +
                meaningfulChange
            );
        }

        if (!meaningfulChange)
        {
            return;
        }

        UpdatePreviousSnapshot(snapshot);

        aiFeedbackClient.RequestCompositionFeedback(
            snapshot
        );
    }

    // =========================================================
    // MEANINGFUL CHANGE
    // =========================================================

    private bool HasMeaningfulChange(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        if (!hasPreviousSnapshot)
        {
            return true;
        }

        if (Mathf.Abs(
                snapshot.totalElements -
                lastElementCount
            ) >= minimumElementChange)
        {
            return true;
        }

        if (Mathf.Abs(
                snapshot.leftDensity -
                lastLeftDensity
            ) >= 0.20f)
        {
            return true;
        }

        if (Mathf.Abs(
                snapshot.rightDensity -
                lastRightDensity
            ) >= 0.20f)
        {
            return true;
        }

        if (Mathf.Abs(
                snapshot.topDensity -
                lastTopDensity
            ) >= 0.20f)
        {
            return true;
        }

        if (Mathf.Abs(
                snapshot.bottomDensity -
                lastBottomDensity
            ) >= 0.20f)
        {
            return true;
        }

        return false;
    }

    // =========================================================
    // UPDATE PREVIOUS SNAPSHOT
    // =========================================================

    private void UpdatePreviousSnapshot(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        lastElementCount =
            snapshot.totalElements;

        lastLeftDensity =
            snapshot.leftDensity;

        lastRightDensity =
            snapshot.rightDensity;

        lastTopDensity =
            snapshot.topDensity;

        lastBottomDensity =
            snapshot.bottomDensity;

        hasPreviousSnapshot = true;
    }
}