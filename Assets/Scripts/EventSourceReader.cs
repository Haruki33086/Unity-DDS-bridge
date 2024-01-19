using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EvtSource
{
    public class EventSourceReader : IDisposable
    {
        private const string defaultEventType = "message";

        public delegate void MessageReceivedHandler(object sender, EventSourceMessageEventArgs e);
        public delegate void DisconnectEventHandler(object sender, DisconnectEventArgs e);

        private HttpClient httpClient;
        private Stream stream = null;
        private readonly Uri uri;

        private volatile bool isDisposed = false;
        public bool IsDisposed => isDisposed;

        private volatile bool isReading = false;
        private readonly object startLock = new object();

        private int reconnectDelay = 3000;
        private string lastEventId = string.Empty;

        public event MessageReceivedHandler MessageReceived;
        public event DisconnectEventHandler Disconnected;

        public EventSourceReader(Uri url, HttpMessageHandler handler = null)
        {
            uri = url;
            httpClient = new HttpClient(handler ?? new HttpClientHandler());
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        }

        public EventSourceReader BeginListening()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(EventSourceReader));
            }
            lock (startLock)
            {
                if (!isReading)
                {
                    isReading = true;
                    // Only start a new one if one isn't already running
                    #pragma warning disable CS4014
                    StartListeningAsync();
                    #pragma warning restore CS4014
                }
            }
            return this;
        }

        public void Dispose()
        {
            isDisposed = true;
            stream?.Dispose();
            httpClient.CancelPendingRequests();
            httpClient.Dispose();
        }

        private async Task StartListeningAsync()
        {
            try
            {
                UpdateLastEventIdHeader();
                using (HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    ValidateContentType(response.Headers);

                    stream = await response.Content.ReadAsStreamAsync();
                    await ProcessStreamAsync();
                }
            }
            catch (Exception ex)
            {
                HandleDisconnect(ex);
            }
        }

        private void UpdateLastEventIdHeader()
        {
            if (!string.IsNullOrEmpty(lastEventId))
            {
                if (httpClient.DefaultRequestHeaders.Contains("Last-Event-Id"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Last-Event-Id");
                }
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Last-Event-Id", lastEventId);
            }
        }

        private static void ValidateContentType(HttpHeaders headers)
        {
            if (headers.TryGetValues("content-type", out IEnumerable<string> contentTypes) && !contentTypes.Contains("text/event-stream"))
            {
                throw new ArgumentException("Specified URI does not return server-sent events");
            }
        }

        private async Task ProcessStreamAsync()
        {
            using var sr = new StreamReader(stream);
            string eventType = defaultEventType;
            string eventId = string.Empty;
            var dataBuilder = new StringBuilder();

            while (true)
            {
                string line = await sr.ReadLineAsync();
                if (line == null)
                {
                    break; // Stream has ended
                }

                if (string.IsNullOrEmpty(line))
                {
                    if (dataBuilder.Length > 0)
                    {
                        MessageReceived?.Invoke(this, new EventSourceMessageEventArgs(dataBuilder.ToString().Trim(), eventType, eventId));
                        dataBuilder.Clear();
                    }
                    eventId = string.Empty;
                    eventType = defaultEventType;
                    continue;
                }

                if (line.StartsWith(":"))
                {
                    continue; // Ignore comments
                }

                ParseLine(line, ref eventType, ref eventId, dataBuilder);
            }
        }

        private void ParseLine(string line, ref string eventType, ref string eventId, StringBuilder dataBuilder)
        {
            int colonIndex = line.IndexOf(':');
            string field = colonIndex == -1 ? line : line.Substring(0, colonIndex);
            string value = colonIndex == -1 ? string.Empty : line.Substring(colonIndex + 1).Trim();

            switch (field)
            {
                case "event":
                    eventType = value;
                    break;
                case "data":
                    dataBuilder.AppendLine(value);
                    break;
                case "retry":
                    if (int.TryParse(value, out int newDelay))
                    {
                        reconnectDelay = newDelay;
                    }
                    break;
                case "id":
                    lastEventId = value;
                    eventId = lastEventId;
                    break;
            }
        }

        private void HandleDisconnect(Exception ex)
        {
            isReading = false;
            Disconnected?.Invoke(this, new DisconnectEventArgs(reconnectDelay, ex));
        }
    }
}