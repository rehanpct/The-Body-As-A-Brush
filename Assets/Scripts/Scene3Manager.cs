using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Scene3Manager : MonoBehaviour
{
    [Header("Visit Buttons")]
    public Button nightMarketVisitButton;
    public Button oceanVisitButton;

    [Header("Navigation")]
    public Button backButton;

    [Header("Scene Names")]
    public string previousSceneName = "Scene2";
    public string drawingSceneName = "DrawingScene";

    private void Start()
    {
        // =====================================================
        // NIGHT MARKET BUTTON
        // =====================================================

        if (nightMarketVisitButton != null)
        {
            nightMarketVisitButton.onClick.AddListener(
                SelectTaiwan
            );
        }
        else
        {
            Debug.LogWarning(
                "Scene3Manager: Night Market Visit Button is not assigned."
            );
        }

        // =====================================================
        // OCEAN BUTTON
        // =====================================================

        if (oceanVisitButton != null)
        {
            oceanVisitButton.onClick.AddListener(
                SelectUnderwater
            );
        }
        else
        {
            Debug.LogWarning(
                "Scene3Manager: Ocean Visit Button is not assigned."
            );
        }

        // =====================================================
        // BACK BUTTON
        // =====================================================

        if (backButton != null)
        {
            backButton.onClick.AddListener(
                BackToScene2
            );
        }
        else
        {
            Debug.LogWarning(
                "Scene3Manager: Back Button is not assigned."
            );
        }

        Debug.Log(
            "================================"
        );

        Debug.Log(
            "SCENE 3 INITIALIZED"
        );

        Debug.Log(
            "================================"
        );
    }

    // =========================================================
    // TAIWAN NIGHT MARKET
    // =========================================================

    public void SelectTaiwan()
    {
        Debug.Log(
            "Scene 3: Taiwan Night Market selected."
        );

        PlayerPrefs.SetString(
            "SelectedTheme",
            "Taiwan"
        );

        PlayerPrefs.Save();

        Debug.Log(
            "SelectedTheme = Taiwan"
        );

        LoadDrawingScene();
    }

    // =========================================================
    // OCEAN EXPLORATION
    // =========================================================

    public void SelectUnderwater()
    {
        Debug.Log(
            "Scene 3: Ocean Exploration selected."
        );

        PlayerPrefs.SetString(
            "SelectedTheme",
            "Underwater"
        );

        PlayerPrefs.Save();

        Debug.Log(
            "SelectedTheme = Underwater"
        );

        LoadDrawingScene();
    }

    // =========================================================
    // BACK TO SCENE 2
    // =========================================================

    public void BackToScene2()
    {
        Debug.Log(
            "Scene 3: BACK pressed - returning to Scene 2."
        );

        SceneManager.LoadScene(
            previousSceneName
        );
    }

    // =========================================================
    // LOAD DRAWING SCENE
    // =========================================================

    private void LoadDrawingScene()
    {
        Debug.Log(
            "Scene 3: Loading DrawingScene."
        );

        SceneManager.LoadScene(
            drawingSceneName
        );
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (nightMarketVisitButton != null)
        {
            nightMarketVisitButton.onClick.RemoveListener(
                SelectTaiwan
            );
        }

        if (oceanVisitButton != null)
        {
            oceanVisitButton.onClick.RemoveListener(
                SelectUnderwater
            );
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                BackToScene2
            );
        }
    }
}