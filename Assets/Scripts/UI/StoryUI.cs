using UnityEngine;
using UnityEngine.UI;

namespace BodyAsBrush.UI
{
    public class StoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button continueButton;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        public void OnContinueClicked()
        {
            Debug.Log("Next stage will be implemented in Task 2.");
        }
    }
}
