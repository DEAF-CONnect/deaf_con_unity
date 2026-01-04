using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;
using System;
//json 받아오기
[System.Serializable]
public class AppConfig
{
    public string unity_websocket;
}
public class UnityWebSocketPlayer : MonoBehaviour
{
    public string unity_websocket;
    public string wsUrl = $"{unity_websocket}/ws/unity";
    public SkeletonMapper skeletonMapper;

    private WebSocket websocket;
    private readonly object frameQueueLock = new object();
    private readonly Queue<FrameData> frameQueue = new Queue<FrameData>();
    //public bool active = false;
    async void Start()
    {
        websocket = new WebSocket(wsUrl);
        
        websocket.OnMessage += (bytes) =>
        {
            //if (!active)return;
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            
            FrameData[] frames = JsonHelper.FromJson<FrameData>(json);
            lock (frameQueueLock)
            {
                foreach (var f in frames)
                    frameQueue.Enqueue(f);
            }
        };

        websocket.OnError += (errorMsg) =>
        {
            Debug.LogError("WebSocket Error: " + errorMsg);
        };

        websocket.OnClose += (code) =>
        {
            Debug.LogWarning("WebSocket Closed: " + code);
        };

        _ = ConnectWebSocketDelayed(70f);
        //await websocket.Connect();
    }
    private async Task ConnectWebSocketDelayed(float delaySeconds)
    {
        Debug.Log($"{delaySeconds}초후에 연결...");
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        try
        {
            await websocket.Connect();
            Debug.Log("connect 성공");
        }
        catch (Exception ex)
        {
            Debug.LogError("connect 실패: " + ex);
        }
    }
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    //if (!active) return;
        lock (frameQueueLock)
        {
            while (frameQueue.Count > 0)
            {
                
                Debug.Log("websocket frame !");

                FrameData frame = frameQueue.Dequeue();
                
                skeletonMapper.SetFrame(frame);
                

            }

        }
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }
    private void Awake()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        string json = System.IO.File.ReadAllText(path);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);
        unity_websocket = cfg.unity_websocket;
    }
}
