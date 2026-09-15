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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void SetTheme(Theme newTheme)
    {
        currentTheme = newTheme;

        Debug.Log(
            "THEME CHANGED → " +
            currentTheme
        );
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