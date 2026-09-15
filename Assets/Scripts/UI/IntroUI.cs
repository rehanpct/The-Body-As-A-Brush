using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BodyAsBrush.UI
{
    public class IntroUI : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [SerializeField] private string targetSceneName = "StoryScene";

        [Header("UI References")]
        [SerializeField] private Button beginButton;

        private void Awake()
        {
            if (beginButton != null)
            {
                beginButton.onClick.AddListener(OnBeginExperienceClicked);
            }
        }

        private void OnDestroy()
        {
            if (beginButton != null)
            {
                beginButton.onClick.RemoveListener(OnBeginExperienceClicked);
            }
        }

        public void OnBeginExperienceClicked()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("[IntroUI] Target scene name is not specified!");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogError($"[IntroUI] Cannot load scene '{targetSceneName}'. Ensure it is added to Build Settings!");
                return;
            }

            Debug.Log($"[IntroUI] Loading scene: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
