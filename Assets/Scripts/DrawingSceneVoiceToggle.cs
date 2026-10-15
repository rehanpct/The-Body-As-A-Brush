using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared physical fallback controls: no AI requests, drawing actions or audio-source changes.
public sealed class DrawingSceneVoiceToggle : MonoBehaviour
{
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private Image muteBackground;
    [SerializeField] private Image saveGraphic;
    [SerializeField] private Sprite underwaterSaveSprite;
    [SerializeField] private Sprite taiwanSaveSprite;
    private ThemeManager.Theme lastTheme;

    private void OnEnable()
    {
        AIFeedbackVoice.MuteStateChanged += RefreshMuteState;
        LanguageManager.LanguageChanged += RefreshMuteState;
        RefreshMuteState();
    }

    private void Start() { RefreshTheme(); }

    private void Update()
    {
        if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != lastTheme)
            RefreshTheme();
    }

    private void OnDisable()
    {
        AIFeedbackVoice.MuteStateChanged -= RefreshMuteState;
        LanguageManager.LanguageChanged -= RefreshMuteState;
    }

    public void ToggleVoiceMute()
    {
        AIFeedbackVoice.SetMuted(!AIFeedbackVoice.IsMuted);
    }

    private void RefreshMuteState()
    {
        bool muted = AIFeedbackVoice.IsMuted;
        if (stateLabel != null)
            stateLabel.text = LanguageManager.IsTraditionalChinese
                ? (muted ? "語音\n靜音" : "語音\n開啟")
                : (muted ? "VOICE\nMUTED" : "VOICE\nON");
        if (muteBackground != null)
            muteBackground.color = muted ? new Color(0.45f, 0.12f, 0.12f, 1f)
                : new Color(0.04f, 0.23f, 0.32f, 1f);
    }

    private void RefreshTheme()
    {
        lastTheme = ThemeManager.Instance != null ? ThemeManager.Instance.CurrentTheme : ThemeManager.Theme.Underwater;
        if (saveGraphic != null)
            saveGraphic.sprite = lastTheme == ThemeManager.Theme.Taiwan ? taiwanSaveSprite : underwaterSaveSprite;
    }
}
