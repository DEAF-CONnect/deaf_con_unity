using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using NativeWebSocket;
using System.Collections.Generic;
using System.Collections;
// 오디오 전달
[System.Serializable]
public class AppConfig
{
    public string audio_websocket;
}
public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    public string audio_websocket;
    private string serverUrl = $"{audio_websocket}/audio";

    private AudioClip recordedClip;
    private string micDevice = null;

    private const int SampleRate = 48000;  

    private int lastSamplePosition = 0;
    private readonly object audioQueueLock = new object();
    private readonly Queue<byte[]> audioQueue = new Queue<byte[]>();

    private bool sendingLoopRunning = false ;
    //public bool active = false;
    void Start()
    {
 
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Start");
            Permission.RequestUserPermission(Permission.Microphone);
            return;
        }

        Debug.Log("Open !");


        ws = new WebSocket(serverUrl);

        ws.OnOpen += () =>
        {
            Debug.Log("[WebSocket Open]]");
            InitMicrophone();  
        };

        ws.OnError += (err) =>
        {
            Debug.LogError("[WebSocket Error]: " + err);
        };

        ws.OnClose += (code) =>
        {
            Debug.Log("[WebSocket Closed]: " + code);
        };

        _ = ConnectWebSocket(70);
    }

    private async Task ConnectWebSocket(float delaySeconds)
    {
        Debug.Log($" {delaySeconds}초후에 연결...");
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        try
        {
            await ws.Connect();
            Debug.Log("Connect() 성공(Task)");
        }
        catch (Exception ex)
        {
            Debug.LogError("Connect() 실패: " + ex);
        }
    }

    private void InitMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            Debug.Log("연결된 mic: " + micDevice);
        }
        else
        {
            micDevice = null;
            Debug.Log("Microphone.devices 가 연결되지 않았음.(null) ");
        }

        StartRecording();
    }

    private void StartRecording()
    {
        Debug.Log("녹음 시작");

        recordedClip = Microphone.Start(micDevice, true, 10, SampleRate);

        if (recordedClip == null)
        {
            Debug.LogError("녹음 실패");
            return;
        }

        Debug.Log($"녹음 길이: lengthSamples={recordedClip.samples}, freq={recordedClip.frequency}");
        lastSamplePosition = 0;
    }

    void Update()
    {
        //if (!active)
        //    return;
        try
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
#endif

            if (recordedClip == null || ws == null || ws.State != WebSocketState.Open)
                return;

            int currentPosition = Microphone.GetPosition(micDevice);
            Debug.Log($"[Update] currentPosition = {currentPosition}, lastSamplePosition = {lastSamplePosition}");

            if (currentPosition <= 0)
            {
                Debug.Log("[Update] currentPosition <= 0");
                return;
            }

            int sampleCount = currentPosition - lastSamplePosition;
            if (sampleCount < 0)
            {
                sampleCount += recordedClip.samples; 
            }

            Debug.Log($"[Update] sampleCount = {sampleCount}, totalSamples = {recordedClip.samples}");

            if (sampleCount <= 0)
            {
                Debug.Log("[Update] sampleCount <= 0");
                return;
            }

            float[] floatBuffer = new float[sampleCount];

            if (lastSamplePosition + sampleCount <= recordedClip.samples)
            {
                recordedClip.GetData(floatBuffer, lastSamplePosition);
            }
            else
            {
                int firstPart = recordedClip.samples - lastSamplePosition;
                int secondPart = sampleCount - firstPart;

                float[] temp = new float[recordedClip.samples];
                recordedClip.GetData(temp, 0);

                Array.Copy(temp, lastSamplePosition, floatBuffer, 0, firstPart);
                Array.Copy(temp, 0, floatBuffer, firstPart, secondPart);
            }

            lastSamplePosition = currentPosition;

            byte[] pcmBytes = FloatToInt16Bytes(floatBuffer);
            Debug.Log($"[Update] 성공: {pcmBytes.Length} bytes");
            //lock
            lock (audioQueueLock)
            {
                audioQueue.Enqueue(pcmBytes);
            }
            // _ = SendBytes(pcmBytes);
            /*
            * update()에서 매번 프레임을 호출함
            * SendQueuedAudio() 가 async Task , 여러개의 Send 루프가 동시 실행
            */
            _ = SendQueuedAudio();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Update] 실패 : " + ex.Message);
        }
    }

    private static byte[] FloatToInt16Bytes(float[] samples)
    {
        short[] int16Samples = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            float s = Mathf.Clamp(samples[i], -1f, 1f);
            int16Samples[i] = (short)(s * short.MaxValue);
        }

        byte[] bytes = new byte[int16Samples.Length * 2];
        Buffer.BlockCopy(int16Samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    //private async Task SendBytes(byte[] data)
    private async Task SendQueuedAudio()
    {
        
        if (sendingLoopRunning) return;
        sendingLoopRunning = true;
        try{    
            while (ws != null && ws.State == WebSocketState.Open)
            {
                byte[] data = null;
                lock (audioQueueLock)
                {
                    if (audioQueue.Count > 0) data = audioQueue.Dequeue();
                }

                if (data == null){
                    await ws.Send(data);
                }
                await Task.delay(10);
            }
        }
        finally{
            sendingLoopRunning = false;
        }
    }

    async void OnApplicationQuit()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            await ws.Close();
        }
    }
    private void Awake()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        string json = System.IO.File.ReadAllText(path);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);
        audio_websocket= cfg.audio_websocket;
    }
}
