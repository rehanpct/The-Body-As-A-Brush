
using System;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    private const string LanguageKey = "CAPTRACK_LANGUAGE";

    public static event Action LanguageChanged;

    public static string CurrentLanguage
    {
        get
        {
            return PlayerPrefs.GetString(LanguageKey, "en");
        }
    }

    public static bool IsTraditionalChinese
    {
        get
        {
            return CurrentLanguage == "zh-TW";
        }
    }

    public void SetEnglish()
    {
        SaveLanguage("en");
    }

    public void SetTraditionalChinese()
    {
        SaveLanguage("zh-TW");
    }

    private void SaveLanguage(string languageCode)
    {
        PlayerPrefs.SetString(LanguageKey, languageCode);
        PlayerPrefs.Save();

        LanguageChanged?.Invoke();

        Debug.Log(
            "[LanguageManager] Language set to " +
            (languageCode == "zh-TW"
                ? "Traditional Chinese"
                : "English")
        );
    }
}
