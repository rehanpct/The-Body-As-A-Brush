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

        // =====================================================
        // CENTRAL AI FEEDBACK TRIGGER
        // =====================================================
        //
        // Every artwork creation that uses RegisterAction()
        // can now trigger the AI feedback system.
        //
        // AIFeedbackClient handles the cooldown, so this does
        // not send unlimited requests.
        // =====================================================

        if (AIFeedbackClient.Instance != null)
        {
            AIFeedbackClient.Instance
                .RequestFeedback("object_added");
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
                    .RequestFeedback("object_deleted");
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