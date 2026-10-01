using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Scene3Manager : MonoBehaviour
{
    [Header("Visit Buttons")]
    [SerializeField] private Button nightMarketVisitButton;
    [SerializeField] private Button oceanVisitButton;

    [Header("Navigation Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;

    [Header("Scene Names")]
    [SerializeField] private string previousSceneName = "Scene2";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gestureSceneName = "Scene4";

    private void Start()
    {
        if (nightMarketVisitButton != null)
        {
            nightMarketVisitButton.onClick.AddListener(SelectTaiwan);
        }
        else
        {
            Debug.LogWarning(
                "Scene3Manager: Night Market Visit Button is not assigned."
            );
        }

        if (oceanVisitButton != null)
        {
            oceanVisitButton.onClick.AddListener(SelectUnderwater);
        }
        else
        {
            Debug.LogWarning(
                "Scene3Manager: Ocean Visit Button is not assigned."
            );
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToScene2);
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(BackToMainMenu);
        }

        Debug.Log("================================");
        Debug.Log("SCENE 3 INITIALIZED");
        Debug.Log("================================");
    }

    // =========================================================
    // TAIWAN NIGHT MARKET
    // =========================================================

    public void SelectTaiwan()
    {
        Debug.Log("Scene 3: Taiwan Night Market selected.");

        SetSelectedTheme("Taiwan");

        LoadGestureScene();
    }

    // =========================================================
    // OCEAN EXPLORATION
    // =========================================================

    public void SelectUnderwater()
    {
        Debug.Log("Scene 3: Ocean Exploration selected.");

        SetSelectedTheme("Underwater");

        LoadGestureScene();
    }

    // =========================================================
    // SET THEME
    // =========================================================

    private void SetSelectedTheme(string theme)
    {
        // Save for scenes that load later.
        PlayerPrefs.SetString("SelectedTheme", theme);
        PlayerPrefs.Save();

        Debug.Log("SelectedTheme PlayerPrefs = " + theme);

        // IMPORTANT:
        // If ThemeManager already exists from a previous session,
        // update the live singleton immediately.
        if (ThemeManager.Instance != null)
        {
            if (theme == "Taiwan")
            {
                ThemeManager.Instance.SetTheme(
                    ThemeManager.Theme.Taiwan
                );
            }
            else
            {
                ThemeManager.Instance.SetTheme(
                    ThemeManager.Theme.Underwater
                );
            }

            Debug.Log(
                "Live ThemeManager updated = " +
                ThemeManager.Instance.CurrentTheme
            );
        }
        else
        {
            Debug.Log(
                "ThemeManager instance not currently loaded. " +
                "DrawingScene will load the saved theme."
            );
        }
    }

    // =========================================================
    // LOAD SCENE 4
    // =========================================================

    private void LoadGestureScene()
    {
        Debug.Log("Scene 3: Loading Scene 4 - Gesture Guide.");

        SceneManager.LoadScene(gestureSceneName);
    }

    // =========================================================
    // BACK
    // =========================================================

    public void BackToScene2()
    {
        Debug.Log(
            "Scene 3: BACK pressed - returning to Scene 2."
        );

        SceneManager.LoadScene(previousSceneName);
    }

    // =========================================================
    // HOME
    // =========================================================

    public void BackToMainMenu()
    {
        Debug.Log(
            "Scene 3: HOME pressed - returning to Main Menu."
        );

        SceneManager.LoadScene(mainMenuSceneName);
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

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(
                BackToMainMenu
            );
        }
    }
}