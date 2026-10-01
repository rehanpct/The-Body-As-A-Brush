using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Scene4Manager : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button drawingButton;
    [SerializeField] private Button homeButton;

    [Header("Scene Names")]
    [SerializeField] private string previousSceneName = "scene3";
    [SerializeField] private string drawingSceneName = "DrawingScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToScene3);
        }
        else
        {
            Debug.LogWarning("Scene4Manager: Back Button is not assigned.");
        }

        if (drawingButton != null)
        {
            drawingButton.onClick.AddListener(GoToDrawingScene);
        }
        else
        {
            Debug.LogWarning("Scene4Manager: Drawing Button is not assigned.");
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(BackToMainMenu);
        }
        else
        {
            Debug.LogWarning("Scene4Manager: Home Button is not assigned.");
        }

        Debug.Log("Scene 4 initialized.");
    }

    public void BackToScene3()
    {
        Debug.Log("Scene 4: BACK pressed - returning to Scene 3.");

        SceneManager.LoadScene(previousSceneName);
    }

    public void GoToDrawingScene()
    {
        Debug.Log("Scene 4: DRAWING pressed - loading DrawingScene.");

        SceneManager.LoadScene(drawingSceneName);
    }

    public void BackToMainMenu()
    {
        Debug.Log("Scene 4: HOME pressed - returning to Main Menu.");

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToScene3);
        }

        if (drawingButton != null)
        {
            drawingButton.onClick.RemoveListener(GoToDrawingScene);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(BackToMainMenu);
        }
    }
}