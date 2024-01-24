using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ImageSubscriber : MonoBehaviour
{
    [SerializeField] string streamURL = "http://13.208.62.139:8080/simu/camera?_method=SUB";
    [SerializeField] RawImage imageDisplay;

    private MJPEGStreamDecoder mjpegDecoder;

    void Start()
    {
        mjpegDecoder = gameObject.AddComponent<MJPEGStreamDecoder>();
        mjpegDecoder.renderTexture = new RenderTexture(640, 480, 24);
        imageDisplay.texture = mjpegDecoder.renderTexture;
        mjpegDecoder.StartStream(streamURL);
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
