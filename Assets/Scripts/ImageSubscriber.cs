using UnityEngine;
using UnityEngine.UI;

public class ImageSubscriber : MonoBehaviour
{
    [SerializeField] private string streamURL = "http://13.208.62.139:8080/simu/camera?_method=SUB";
    [SerializeField] private RawImage imageDisplay;
    [SerializeField] private int textureWidth = 640; // RenderTextureの幅の初期値
    [SerializeField] private int textureHeight = 480; // RenderTextureの高さの初期値

    private MJPEGStreamDecoder mjpegDecoder;

    public int TextureWidth
    {
        get { return textureWidth; }
        set
        {
            if (textureWidth != value)
            {
                textureWidth = value;
                UpdateRenderTexture();
            }
        }
    }

    public int TextureHeight
    {
        get { return textureHeight; }
        set
        {
            if (textureHeight != value)
            {
                textureHeight = value;
                UpdateRenderTexture();
            }
        }
    }

    void Start()
    {
        mjpegDecoder = gameObject.AddComponent<MJPEGStreamDecoder>();
        UpdateRenderTexture();
        mjpegDecoder.StartStream(streamURL);
    }

    private void UpdateRenderTexture()
    {
        if (mjpegDecoder.renderTexture != null)
        {
            Destroy(mjpegDecoder.renderTexture);
        }

        mjpegDecoder.renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        imageDisplay.texture = mjpegDecoder.renderTexture;
    }

    void OnDestroy()
    {
        if (mjpegDecoder != null)
        {
            mjpegDecoder.StopAllCoroutines();
            Destroy(mjpegDecoder);
        }
    }
}
