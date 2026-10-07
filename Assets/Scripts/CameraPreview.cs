using UnityEngine;
using UnityEngine.UI;

public class CameraPreview : MonoBehaviour
{
    [Header("Source")]
    [SerializeField]
    private RawImage sourceScreen;

    [Header("Preview")]
    [SerializeField]
    private RawImage previewScreen;

    private Texture lastTexture;

    private void LateUpdate()
    {
        if (sourceScreen == null || previewScreen == null)
            return;

        Texture currentTexture = sourceScreen.texture;

        if (currentTexture == null)
            return;

        if (currentTexture != lastTexture)
        {
            previewScreen.texture = currentTexture;
            lastTexture = currentTexture;

            Debug.Log(
                "[CameraPreview] Webcam texture connected."
            );
        }

        previewScreen.uvRect = sourceScreen.uvRect;
    }
}