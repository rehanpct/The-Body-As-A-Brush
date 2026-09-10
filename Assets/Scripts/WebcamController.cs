using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    public RawImage webcamDisplay;

    private WebCamTexture webcamTexture;

    void Start()
    {
        webcamTexture = new WebCamTexture();

        webcamDisplay.texture = webcamTexture;

        webcamTexture.Play();
    }

    void OnDestroy()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
}