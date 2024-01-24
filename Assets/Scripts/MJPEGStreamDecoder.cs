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

    private const float RETRY_DELAY = 5f;
    private const int MAX_RETRIES = 3;
    private int retryCount = 0;

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
        retryCount = 0;
        StopAllCoroutines();
        ClearBufferedStreams();

        isThreadRunning = true;
        worker = new Thread(() => ReadMJPEGStreamWorker(threadID = randu.Next(65536), url));
        worker.Start();
    }

    private void StopStream()
    {
        isThreadRunning = false;
        if (worker != null && worker.IsAlive)
        {
            worker.Join();
        }

        ClearBufferedStreams();
    }

    private void InitializeRandomGenerator()
    {
        if (randu == null)
        {  
            randu = new System.Random(Random.Range(0, 65536));
        }
    }

    private void ClearBufferedStreams()
    {
        foreach (var b in trackedBuffers)
        {
            if (b != null)
                b.Close();
        }
        trackedBuffers.Clear();
    }

    void ReadMJPEGStreamWorker(int id, string url)
    {
        var webRequest = WebRequest.Create(url);
        webRequest.Method = "GET";
        List<byte> frameBuffer = new List<byte>();

        int lastByte = 0x00;
        bool addToBuffer = false;

        try
        {
            using (var response = webRequest.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (BufferedStream buffer = new BufferedStream(stream))
            {
                trackedBuffers.Add(buffer);

                int newByte;
                while (isThreadRunning && buffer != null)
                {
                    if (threadID != id)
                        return;

                    newByte = buffer.ReadByte();

                    if (newByte < 0)
                        continue;

                    if (addToBuffer)
                        frameBuffer.Add((byte)newByte);

                    if (lastByte == 0xFF) // It's a command!
                    {
                        if (!addToBuffer) // We're not reading a frame, should we be?
                        {
                            if (IsStartOfImage(newByte))
                            {
                                addToBuffer = true;
                                frameBuffer.Add((byte)lastByte);
                                frameBuffer.Add((byte)newByte);
                            }
                        }
                        else // We're reading a frame, should we stop?
                        {
                            if (newByte == 0xD9)
                            {
                                frameBuffer.Add((byte)newByte);
                                addToBuffer = false;
                                nextFrame = frameBuffer.ToArray();
                                frameBuffer.Clear();
                            }
                        }
                    }

                    lastByte = newByte;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
        finally
        {
            isThreadRunning = false;
        }

        if (retryCount < MAX_RETRIES)
        {
            retryCount++;
            Debug.LogFormat("[{0}] Retrying Connection {1}...", id, retryCount);
            ClearBufferedStreams();
            isThreadRunning = true;
            worker = new Thread(() => ReadMJPEGStreamWorker(threadID = randu.Next(65536), url));
            worker.Start();
        }
    }

    void SendFrame(byte[] bytes)
    {
        Texture2D texture2D = new Texture2D(2, 2);
        texture2D.LoadImage(bytes);

        if (texture2D.width == 2)
        {
            Destroy(texture2D);
            return; // Failure!
        }

        Graphics.Blit(texture2D, renderTexture);
        Destroy(texture2D); // Texture2Dを破棄
    }

    bool IsStartOfImage(int command)
    {
        switch (command)
        {
            case 0x8D:
                Debug.Log("Command SOI");
                return true;
            case 0xC0:
                Debug.Log("Command SOF0");
                return true;
            case 0xC2:
                Debug.Log("Command SOF2");
                return true;
            case 0xC4:
                Debug.Log("Command DHT");
                break;
            case 0xD8:
                //Debug.Log("Command DQT");
                return true;
            case 0xDD:
                Debug.Log("Command DRI");
                break;
            case 0xDA:
                Debug.Log("Command SOS");
                break;
            case 0xFE:
                Debug.Log("Command COM");
                break;
            case 0xD9:
                Debug.Log("Command EOI");
                break;
        }
        return false;
    }
}