using System.Collections.Generic;
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

    private readonly int[] lastZoneCounts = new int[9];

    private string lastElementSignature = "";
    private string lastObservedTheme = "";

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        observationInterval = Mathf.Max(20f, observationInterval);

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

        // This observer only reports a change. AIFeedbackClient owns the
        // single settling, cooldown, duplicate-check, and request schedule.
        aiFeedbackClient.NotifyArtworkChanged("periodic_observation");
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

        if (BuildElementSignature(snapshot) != lastElementSignature)
            return true;

        string currentTheme = ThemeManager.Instance != null
            ? ThemeManager.Instance.CurrentTheme.ToString()
            : "Unknown";
        if (currentTheme != lastObservedTheme)
            return true;

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

        if (snapshot.zones != null)
        {
            for (int i = 0;
                 i < snapshot.zones.Count && i < lastZoneCounts.Length;
                 i++)
            {
                if (snapshot.zones[i].elementCount != lastZoneCounts[i])
                    return true;
            }
        }

        return false;
    }

    private string BuildElementSignature(
        CompositionAnalyzer.CompositionSnapshot snapshot
    )
    {
        List<string> elements = new List<string>();

        if (snapshot.elements != null)
        {
            foreach (CompositionAnalyzer.ElementInfo element in snapshot.elements)
            {
                elements.Add(
                    element.type + ":" +
                    Mathf.RoundToInt(element.normalizedPosition.x * 20f) + ":" +
                    Mathf.RoundToInt(element.normalizedPosition.y * 20f) + ":" +
                    Mathf.RoundToInt(element.relativeSize * 1000f)
                );
            }
        }

        elements.Sort();
        return string.Join("|", elements.ToArray());
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

        lastElementSignature =
            BuildElementSignature(snapshot);

        lastObservedTheme = ThemeManager.Instance != null
            ? ThemeManager.Instance.CurrentTheme.ToString()
            : "Unknown";

        if (snapshot.zones != null)
        {
            for (int i = 0;
                 i < snapshot.zones.Count && i < lastZoneCounts.Length;
                 i++)
            {
                lastZoneCounts[i] = snapshot.zones[i].elementCount;
            }
        }

        hasPreviousSnapshot = true;
    }
}