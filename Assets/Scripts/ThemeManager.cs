using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public enum Theme
    {
        Underwater,
        Taiwan
    }

    public static ThemeManager Instance;

    [Header("Current Theme")]
    [SerializeField]
    private Theme currentTheme = Theme.Underwater;

    public Theme CurrentTheme => currentTheme;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Read the theme selected from the Main Menu.
        string savedTheme = PlayerPrefs.GetString(
            "SelectedTheme",
            "Underwater"
        );

        if (savedTheme == "Taiwan")
        {
            currentTheme = Theme.Taiwan;
        }
        else
        {
            currentTheme = Theme.Underwater;
        }

        Debug.Log("THEME LOADED → " + currentTheme);

        DontDestroyOnLoad(gameObject);
    }

    public void SetTheme(Theme newTheme)
    {
        currentTheme = newTheme;

        PlayerPrefs.SetString(
            "SelectedTheme",
            currentTheme.ToString()
        );

        PlayerPrefs.Save();

        Debug.Log("THEME CHANGED → " + currentTheme);
    }

    public bool IsUnderwater()
    {
        return currentTheme == Theme.Underwater;
    }

    public bool IsTaiwan()
    {
        return currentTheme == Theme.Taiwan;
    }
}