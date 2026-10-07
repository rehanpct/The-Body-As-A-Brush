using System.Collections.Generic;
using UnityEngine;

public class ArtworkActionHistory : MonoBehaviour
{
    public static ArtworkActionHistory Instance;

    private readonly List<GameObject> actionHistory =
        new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // REGISTER ONE ARTWORK PIECE
    // =========================================================

    public void RegisterAction(GameObject artworkRoot)
    {
        if (artworkRoot == null)
            return;

        actionHistory.Add(artworkRoot);

        Debug.Log(
            "Artwork action registered: " +
            artworkRoot.name +
            " | Total actions: " +
            actionHistory.Count
        );

        if (AIFeedbackClient.Instance != null)
        {
            AIFeedbackClient.Instance
                .NotifyArtworkChanged("object_added");
        }
    }

    // =========================================================
    // UNDO
    // =========================================================

    public void UndoLastAction()
    {
        RemoveDestroyedActions();

        if (actionHistory.Count == 0)
        {
            Debug.Log("Nothing to undo.");
            return;
        }

        int lastIndex =
            actionHistory.Count - 1;

        GameObject artwork =
            actionHistory[lastIndex];

        actionHistory.RemoveAt(lastIndex);

        if (artwork != null)
        {
            string removedArtworkName =
                artwork.name;

            if (ArtworkStatistics.Instance != null)
            {
                ArtworkStatistics.Instance
                    .RegisterArtworkElementRemoved(artwork);
            }

            // Destroy is deferred until the end of the frame.
            // Untagging first keeps the deletion snapshot current.
            artwork.tag = "Untagged";
            Destroy(artwork);

            Debug.Log(
                "UNDO: Removed artwork piece: " +
                removedArtworkName
            );

            // =================================================
            // AI FEEDBACK FOR UNDO
            // =================================================

            if (AIFeedbackClient.Instance != null)
            {
                AIFeedbackClient.Instance
                    .NotifyArtworkChanged("object_deleted");
            }
        }
    }

    // =========================================================
    // REMOVE DESTROYED OBJECTS
    // =========================================================

    void RemoveDestroyedActions()
    {
        for (int i =
            actionHistory.Count - 1;
            i >= 0;
            i--)
        {
            if (actionHistory[i] == null)
            {
                actionHistory.RemoveAt(i);
            }
        }
    }

    // =========================================================
    // CLEAR
    // =========================================================

    public void ClearHistory()
    {
        actionHistory.Clear();
    }

    // =========================================================
    // ACTION COUNT
    // =========================================================

    public int ActionCount
    {
        get
        {
            RemoveDestroyedActions();

            return actionHistory.Count;
        }
    }
}