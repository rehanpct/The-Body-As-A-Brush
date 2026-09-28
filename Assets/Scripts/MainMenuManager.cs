using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Theme Selection")]
    public GameObject themeSelectionPanel;

    [Header("Scene Names")]
    public string scene2Name = "Scene2";
    public string drawingSceneName = "DrawingScene";
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        // -----------------------------------------------------
        // Check whether Scene 2 requested Theme Selection
        // -----------------------------------------------------

        if (PlayerPrefs.GetInt("OpenThemeSelection", 0) == 1)
        {
            // Reset the request so it does not reopen
            // every time MainMenu is loaded.
            PlayerPrefs.SetInt("OpenThemeSelection", 0);
            PlayerPrefs.Save();

            OpenThemeSelection();
        }
    }

    // =========================================================
    // START / CREATE
    // =========================================================

    public void StartGame()
    {
        Debug.Log(
            "START pressed - Loading Scene 2."
        );

        SceneManager.LoadScene(
            scene2Name
        );
    }

    // =========================================================
    // THEME SELECTION
    // =========================================================

    public void OpenThemeSelection()
    {
        if (themeSelectionPanel != null)
        {
            themeSelectionPanel.SetActive(true);

            Debug.Log(
                "Theme Selection Panel opened."
            );
        }
        else
        {
            Debug.LogError(
                "MainMenuManager: " +
                "Theme Selection Panel is not assigned!"
            );
        }
    }

    // =========================================================
    // UNDERWATER THEME
    // =========================================================

    public void SelectUnderwater()
    {
        Debug.Log(
            "Theme selected: Underwater"
        );

        PlayerPrefs.SetString(
            "SelectedTheme",
            "Underwater"
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(
            drawingSceneName
        );
    }

    // =========================================================
    // TAIWAN THEME
    // =========================================================

    public void SelectTaiwan()
    {
        Debug.Log(
            "Theme selected: Taiwan"
        );

        PlayerPrefs.SetString(
            "SelectedTheme",
            "Taiwan"
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(
            drawingSceneName
        );
    }

    // =========================================================
    // BACK FROM THEME SELECTION
    // =========================================================

    public void BackToMainMenu()
    {
        if (themeSelectionPanel != null)
        {
            themeSelectionPanel.SetActive(false);

            Debug.Log(
                "Returned to Main Menu."
            );
        }
    }

    // =========================================================
    // ALBUM
    // =========================================================

    public void OpenAlbum()
    {
        Debug.Log(
            "Album button pressed."
        );

        SceneManager.LoadScene(
            "AlbumScene"
        );
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void OpenSettings()
    {
        Debug.Log(
            "Settings button pressed."
        );
    }

    // =========================================================
    // CLOSE GAME
    // =========================================================

    public void CloseGame()
    {
        Debug.Log(
            "Close button pressed."
        );

        Application.Quit();
    }
}