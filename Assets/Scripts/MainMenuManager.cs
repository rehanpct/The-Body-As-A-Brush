using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string scene2Name = "Scene2";

    [Header("Main Menu")]
    public string albumSceneName = "AlbumScene";

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
    // ALBUM
    // =========================================================

    public void OpenAlbum()
    {
        Debug.Log(
            "Album button pressed."
        );

        SceneManager.LoadScene(
            albumSceneName
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