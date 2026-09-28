using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Scene2Manager : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button backButton;
    public Button skipButton;
    public Button noButton;
    public Button yesButton;

    [Header("Dialogue")]
    public TMP_Text dialogueText;

    [Header("Character States")]
    public GameObject idleCharacter;
    public GameObject talkingCharacter;
    public GameObject thinkingCharacter;
    public GameObject pointingCharacter;
    public GameObject excitedCharacter;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Dialogue Settings")]
    [TextArea(3, 6)]
    public string initialDialogue =
        "Hello! I just arrived in Taiwan. Can you help me take photos at some tourist spots in Taiwan so I can save them in my photo album?";

    [TextArea(2, 4)]
    public string noResponse =
        "Oh, that's okay! Maybe another time. Have a great day!";

    [TextArea(2, 4)]
    public string yesResponse =
        "Awesome! Let's explore Taiwan together!";

    [Header("Transition")]
    public float yesResponseDelay = 1.5f;

    private void Start()
    {
        // -----------------------------------------------------
        // Set initial dialogue
        // -----------------------------------------------------

        if (dialogueText != null)
        {
            dialogueText.text = initialDialogue;
        }
        else
        {
            Debug.LogWarning(
                "Scene2Manager: Dialogue Text is not assigned."
            );
        }

        // -----------------------------------------------------
        // Connect buttons
        // -----------------------------------------------------

        if (backButton != null)
        {
            backButton.onClick.AddListener(BackToMainMenu);
        }
        else
        {
            Debug.LogWarning(
                "Scene2Manager: Back Button is not assigned."
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipDialogue);
        }
        else
        {
            Debug.LogWarning(
                "Scene2Manager: Skip Button is not assigned."
            );
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(NoThanks);
        }
        else
        {
            Debug.LogWarning(
                "Scene2Manager: No Button is not assigned."
            );
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(YesLetsGo);
        }
        else
        {
            Debug.LogWarning(
                "Scene2Manager: Yes Button is not assigned."
            );
        }

        // -----------------------------------------------------
        // Initial character
        // -----------------------------------------------------

        ShowTalkingCharacter();

        Debug.Log("================================");
        Debug.Log("SCENE 2 INITIALIZED");
        Debug.Log("================================");
    }

    // =========================================================
    // BACK BUTTON
    // =========================================================

    public void BackToMainMenu()
    {
        Debug.Log("Scene 2: BACK pressed.");

        CancelInvoke();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // =========================================================
    // SKIP BUTTON
    // =========================================================

    public void SkipDialogue()
    {
        Debug.Log("Scene 2: SKIP pressed.");

        CancelInvoke();

        GoToThemeSelection();
    }

    // =========================================================
    // NO BUTTON
    // =========================================================

    public void NoThanks()
    {
        Debug.Log("Scene 2: NO pressed.");

        CancelInvoke();

        // Change dialogue
        if (dialogueText != null)
        {
            dialogueText.text = noResponse;
        }

        // Change Nizki expression
        ShowThinkingCharacter();

        // Hide both choices
        if (noButton != null)
        {
            noButton.gameObject.SetActive(false);
        }

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(false);
        }

        Debug.Log(
            "Scene 2: Nizki switched to Thinking."
        );
    }

    // =========================================================
    // YES BUTTON
    // =========================================================

    public void YesLetsGo()
    {
        Debug.Log("Scene 2: YES pressed.");

        CancelInvoke();

        // Change dialogue
        if (dialogueText != null)
        {
            dialogueText.text = yesResponse;
        }

        // Change Nizki expression
        ShowExcitedCharacter();

        // Hide both choices
        if (noButton != null)
        {
            noButton.gameObject.SetActive(false);
        }

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(false);
        }

        Debug.Log(
            "Scene 2: Nizki switched to Excited."
        );

        // Give player time to see the response
        Invoke(
            nameof(GoToThemeSelection),
            yesResponseDelay
        );
    }

    // =========================================================
    // THEME SELECTION
    // =========================================================

    public void GoToThemeSelection()
    {
        Debug.Log(
            "Scene 2: Returning to Main Menu and opening Theme Selection."
        );

        PlayerPrefs.SetInt(
            "OpenThemeSelection",
            1
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }

    // =========================================================
    // CHARACTER STATES
    // =========================================================

    public void ShowIdleCharacter()
    {
        SetCharacterState(
            idleCharacter
        );
    }

    public void ShowTalkingCharacter()
    {
        SetCharacterState(
            talkingCharacter
        );
    }

    public void ShowThinkingCharacter()
    {
        SetCharacterState(
            thinkingCharacter
        );
    }

    public void ShowPointingCharacter()
    {
        SetCharacterState(
            pointingCharacter
        );
    }

    public void ShowExcitedCharacter()
    {
        SetCharacterState(
            excitedCharacter
        );
    }

    private void SetCharacterState(
        GameObject activeCharacter
    )
    {
        // -----------------------------------------------------
        // Disable every character state
        // -----------------------------------------------------

        if (idleCharacter != null)
        {
            idleCharacter.SetActive(false);
        }

        if (talkingCharacter != null)
        {
            talkingCharacter.SetActive(false);
        }

        if (thinkingCharacter != null)
        {
            thinkingCharacter.SetActive(false);
        }

        if (pointingCharacter != null)
        {
            pointingCharacter.SetActive(false);
        }

        if (excitedCharacter != null)
        {
            excitedCharacter.SetActive(false);
        }

        // -----------------------------------------------------
        // Enable selected state
        // -----------------------------------------------------

        if (activeCharacter != null)
        {
            activeCharacter.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Scene2Manager: Selected character state is not assigned."
            );
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        CancelInvoke();

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(
                BackToMainMenu
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                SkipDialogue
            );
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(
                NoThanks
            );
        }

        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(
                YesLetsGo
            );
        }
    }
}