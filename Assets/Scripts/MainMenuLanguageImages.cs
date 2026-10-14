
using UnityEngine;
using UnityEngine.UI;

public class MainMenuLanguageImages : MonoBehaviour
{
    [System.Serializable]
    public class LocalizedButtonImage
    {
        public string objectName;
        public Image targetImage;
        public Sprite englishSprite;
        public Sprite chineseSprite;
    }

    [Header("Main Menu Images")]
    [SerializeField]
    private LocalizedButtonImage[] localizedImages;

    private void OnEnable()
    {
        LanguageManager.LanguageChanged += RefreshImages;
        RefreshImages();
    }

    private void OnDisable()
    {
        LanguageManager.LanguageChanged -= RefreshImages;
    }

    public void RefreshImages()
    {
        bool isChinese = LanguageManager.IsTraditionalChinese;

        foreach (LocalizedButtonImage item in localizedImages)
        {
            if (item == null || item.targetImage == null)
            {
                Debug.LogWarning(
                    "[MainMenuLanguageImages] Missing Image reference."
                );
                continue;
            }

            Sprite sprite = isChinese
                ? item.chineseSprite
                : item.englishSprite;

            if (sprite == null)
            {
                Debug.LogWarning(
                    "[MainMenuLanguageImages] Missing sprite for " +
                    item.objectName
                );
                continue;
            }

            item.targetImage.sprite = sprite;
        }
    }
}
