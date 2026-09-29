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
    public string scene3Name = "scene3";

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
            backButton.onClick.AddListener(
                BackToMainMenu
            );
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(
                SkipDialogue
            );
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(
                NoThanks
            );
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(
                YesLetsGo
            );
        }

        // -----------------------------------------------------
        // Initial character
        // -----------------------------------------------------

        ShowTalkingCharacter();

        Debug.Log(
            "================================"
        );

        Debug.Log(
            "SCENE 2 INITIALIZED"
        );

        Debug.Log(
            "================================"
        );
    }

    // =========================================================
    // BACK BUTTON
    // =========================================================

    public void BackToMainMenu()
    {
        Debug.Log(
            "Scene 2: BACK pressed."
        );

        CancelInvoke();

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }

    // =========================================================
    // SKIP BUTTON
    // =========================================================

    public void SkipDialogue()
    {
        Debug.Log(
            "Scene 2: SKIP pressed."
        );

        CancelInvoke();

        GoToScene3();
    }

    // =========================================================
    // NO BUTTON
    // =========================================================

    public void NoThanks()
    {
        Debug.Log(
            "Scene 2: NO pressed."
        );

        CancelInvoke();

        if (dialogueText != null)
        {
            dialogueText.text =
                noResponse;
        }

        ShowThinkingCharacter();

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
        Debug.Log(
            "Scene 2: YES pressed."
        );

        CancelInvoke();

        if (dialogueText != null)
        {
            dialogueText.text =
                yesResponse;
        }

        ShowExcitedCharacter();

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

        Invoke(
            nameof(GoToScene3),
            yesResponseDelay
        );
    }

    // =========================================================
    // GO TO SCENE 3
    // =========================================================

    public void GoToScene3()
    {
        Debug.Log(
            "Scene 2: Loading Scene 3 destination map."
        );

        CancelInvoke();

        SceneManager.LoadScene(
            scene3Name
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

        if (activeCharacter != null)
        {
            activeCharacter.SetActive(true);
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