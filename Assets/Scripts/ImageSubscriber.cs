using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MJPGStreamUnity : MonoBehaviour
{
    [SerializeField] string streamURL = "http://<your ip>:<your port>/simu/camera?_method=SUB";
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
