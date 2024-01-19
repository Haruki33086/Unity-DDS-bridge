using System;
using UnityEngine;
using EvtSource;
using System.Threading.Tasks;
using System.Collections;

public class Float64Subscriber : MonoBehaviour
{   
    public string restApi = "http://<your ip>:8000/";
    public string scope = "<your scope>";
    public string topic = "<your topic>";

    private EventSourceReader eventSource;

    private async void Start()
    {
        await StartEventSourceAsync();
    }

    private async Task StartEventSourceAsync()
    {
        try
        {   
            string keyExpr = restApi + scope + topic;
            Uri uri = new Uri(keyExpr);
            eventSource = new EventSourceReader(uri);

            eventSource.MessageReceived += OnMessageReceived;
            eventSource.Disconnected += OnDisconnected;

            await Task.Run(() => eventSource.BeginListening());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting EventSource: {ex.Message}");
        }
    }

    private void OnMessageReceived(object sender, EventSourceMessageEventArgs e)
    {
        string jsonData = e.Message;

        try
        {   
            Float64Data sample = JsonUtility.FromJson<Float64Data>(jsonData);
            byte[] decodedBytes = Convert.FromBase64String(sample.value);

            CSCDR.CDRReader reader = new CSCDR.CDRReader(decodedBytes);
            double data = reader.ReadDouble();
            Debug.Log($"{topic}: {data}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing data: {ex.Message}");
        }
    }

    private void OnDisconnected(object sender, DisconnectEventArgs e)
    {
        Debug.LogWarning($"EventSource disconnected. Reconnect in {e.ReconnectDelay} milliseconds.");
        StartCoroutine(ReconnectAfterDelay(e.ReconnectDelay));
    }

    private IEnumerator ReconnectAfterDelay(int delay)
    {
        yield return new WaitForSeconds(delay / 1000.0f); // convert milliseconds to seconds
        yield return StartCoroutine(StartEventSourceAsyncAsCoroutine());
    }

    private IEnumerator StartEventSourceAsyncAsCoroutine()
    {
        var task = StartEventSourceAsync();
        while (!task.IsCompleted)
        {
            yield return null;
        }
        if (task.Exception != null)
        {
            Debug.LogError($"Error in reconnecting: {task.Exception.InnerException.Message}");
        }
    }

    private void OnDestroy()
    {
        eventSource?.Dispose();
    }

    [Serializable]
    private class Float64Data
    {
        public string value;
    }
}