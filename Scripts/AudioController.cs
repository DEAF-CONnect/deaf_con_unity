using System.Collections;
using UnityEngine;
using NativeWebSocket;
using UnityEngine.Android;
[System.Serializable]
public class AppConfig{
    public string pythonWS;
}
public class AudioController : MonoBehaviour
{
    private WebSocket ws;

    private AudioClip micClip;
    private int sampleRate = 16000; 

    public string pythonWS;
    IEnumerator Start()
    {
        /
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                yield return null;
        }
#endif

        yield return StartCoroutine(WebSocketConnectRoutine());
    }

    IEnumerator WebSocketConnectRoutine()
    {
        ws = new WebSocket(wsUrl);

        ws.OnOpen += () => Debug.Log("WebSocket Connected!");
        ws.OnError += (e) => Debug.LogError("WebSocket Error: " + e);
        ws.OnClose += (e) => Debug.Log("WebSocket Closed");

        var connectTask = ws.Connect();
        while (!connectTask.IsCompleted)
        {
            yield return null;
        }

        if (connectTask.IsFaulted)
        {
            Debug.LogError("WebSocket Connection Failed: " + connectTask.Exception);
            yield break;
        }

        Debug.Log("WebSocket Connected!");
        Debug.Log($"this is : {Microphone.devices.Length}");
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("There's no mircrophone");
            yield break;
        }else
        {
            Debug.Log("what the fuck");
        }
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Microphone device: " + device);
        }

        micClip = Microphone.Start(null, true, 5, sampleRate);
        if (micClip == null)
        {
            Debug.LogError("Mic doesn't Start");
            yield break;
        }
        Debug.Log($"Mic started successfully: {sampleRate} Hz, Channels: {micClip.channels}, Samples: {micClip.samples}");

        StartCoroutine(StreamAudioRoutine());
    }

    IEnumerator StreamAudioRoutine()
    {
        while (true)
        {
            if (micClip == null)
            {
                Debug.LogWarning("MicClip is null, skipping...");
                yield return null;
                continue;
            }

            int pos = Microphone.GetPosition(null);
            int diff = pos - prevPos;

            if (diff < 0) diff += micClip.samples;

            if (diff > 0)
            {
                float[] samples = new float[diff];
                micClip.GetData(samples, prevPos);

                byte[] pcmChunk = FloatToPCM16(samples);

                if (ws != null && ws.State == WebSocketState.Open)
                {
                    var sendTask = ws.Send(pcmChunk);
                    while (!sendTask.IsCompleted) yield return null;
                    Debug.Log($"Sent audio chunk: {pcmChunk.Length} bytes | Samples: {diff}");
                }
                else
                {
                    Debug.LogWarning("WebSocket not open. Cannot send audio.");
                }

                prevPos = pos;
            }
            else
            {
                Debug.Log("No new microphone samples to send.");
            }

            yield return new WaitForSeconds(0.02f);
        }
    }

    private byte[] FloatToPCM16(float[] samples)
    {
        int len = samples.Length;
        byte[] bytes = new byte[len * 2];

        int offset = 0;
        for (int i = 0; i < len; i++)
        {
            short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
            bytes[offset++] = (byte)(s & 0xFF);
            bytes[offset++] = (byte)((s >> 8) & 0xFF);
        }

        return bytes;
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
            ws.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        if (ws != null)
        {
            Debug.Log("Closing WebSocket...");
            await ws.Close();
        }
    }
    private void Awake(){
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        string json = System.IO.File.ReadAllText(path);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);
        pythonWS; = cfg.audio_websocket + "/audio";
    }
}
