/*
 * I needed a simple MJPEG Stream Decoder and I couldn't find one that worked for me.
 * 
 * It reads a response stream and when there's a new frame it updates the render texture. 
 * That's it. No authenication or options.
 * It's something stupid simple for readimg a video stream from an equally stupid simple Arduino. 
 * 
 * I fixed most of the large memory leaks, but there's at least one small one left.
 */

using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class MJPEGStreamDecoder : MonoBehaviour
{
    [SerializeField] private bool tryOnStart = false;
    [SerializeField] private string defaultStreamURL = "http://127.0.0.1/stream";
    [SerializeField] public RenderTexture renderTexture;

    private byte[] nextFrame = null;

    private Thread worker;
    private int threadID = 0;
    private bool isThreadRunning = false;

    private static System.Random randu;
    private List<BufferedStream> trackedBuffers = new List<BufferedStream>();

    void Start()
    {
        if (tryOnStart)
            StartStream(defaultStreamURL);
    }

    private void Update()
    {
        if (nextFrame != null)
        {
            SendFrame(nextFrame);
            nextFrame = null;
        }
    }

    private void OnDestroy()
    {
        StopStream();
    }

    public void StartStream(string url)
    {
        InitializeRandomGenerator();
        StopStream(); // Ensure any previous streams are stopped before starting a new one

        isThreadRunning = true;
        worker = new Thread(() => ReadMJPEGStreamWorker(threadID = randu.Next(65536), url));
        worker.Start();
    }

    private void StopStream()
    {
        isThreadRunning = false;
        if (worker != null && worker.IsAlive)
        {
            worker.Interrupt();
            worker.Join();
        }

        ClearBufferedStreams();
    }

    private void InitializeRandomGenerator()
    {
        if (randu == null)
        {
            randu = new System.Random(UnityEngine.Random.Range(0, 65536));
        }
    }

    private void ClearBufferedStreams()
    {
        foreach (var b in trackedBuffers)
        {
            b?.Close();
        }
        trackedBuffers.Clear();
    }

    void ReadMJPEGStreamWorker(int id, string url)
    {
        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
        webRequest.Method = "GET";
        webRequest.Timeout = 5000; // 5 seconds timeout

        try
        {
            using (WebResponse response = webRequest.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (BufferedStream buffer = new BufferedStream(stream))
            {
                trackedBuffers.Add(buffer);
                List<byte> frameBuffer = new List<byte>();
                int lastByte = 0x00, newByte;
                bool addToBuffer = false;

                while (isThreadRunning)
                {
                    if (threadID != id) break;

                    newByte = buffer.ReadByte();
                    if (newByte == -1) continue; // End of stream

                    if (addToBuffer) frameBuffer.Add((byte)newByte);

                    if (lastByte == 0xFF)
                    {
                        if (!addToBuffer && IsStartOfImage(newByte))
                        {
                            addToBuffer = true;
                            frameBuffer.Add((byte)lastByte);
                            frameBuffer.Add((byte)newByte);
                        }
                        else if (addToBuffer && newByte == 0xD9) // End of image
                        {
                            frameBuffer.Add((byte)newByte);
                            addToBuffer = false;
                            nextFrame = frameBuffer.ToArray();
                            frameBuffer.Clear();
                        }
                    }

                    lastByte = newByte;
                }
            }
        }
        catch (WebException ex)
        {
            Debug.LogError($"Web request failed: {ex.Message}");
        }
        finally
        {
            isThreadRunning = false;
            ClearBufferedStreams();
        }
    }

    void SendFrame(byte[] bytes)
    {
        Texture2D texture2D = new Texture2D(2, 2);
        if (texture2D.LoadImage(bytes) && texture2D.width != 2)
        {
            Graphics.Blit(texture2D, renderTexture);
        }
        Destroy(texture2D); // Ensure the texture is destroyed to free memory
    }

    bool IsStartOfImage(int command)
    {
        return command == 0xD8; // SOI (Start of Image) marker for JPEG
    }
}
