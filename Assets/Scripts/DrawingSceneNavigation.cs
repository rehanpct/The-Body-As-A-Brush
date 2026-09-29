using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DrawingSceneNavigation : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button backButton;

    [Header("Previous Scene")]
    [SerializeField] private string previousSceneName = "scene3";

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToPreviousScene);
        }
        else
        {
            Debug.LogWarning(
                "DrawingSceneNavigation: Back Button is not assigned."
            );
        }
    }

    public void BackToPreviousScene()
    {
        Debug.Log(
            "DrawingScene: BACK pressed - returning to Scene 3."
        );

        SceneManager.LoadScene(previousSceneName);
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                BackToPreviousScene
            );
        }
    }
}