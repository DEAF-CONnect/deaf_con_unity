using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;
using System;
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
            //lock(messageQueue)
            //{
            //    messageQueue.Enqueue(json);
            //}
            // ?????? JSON ?�� ???��? ??????? ?????? ???? ???
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
        Debug.Log($"������ ���� ��� {delaySeconds}��...");
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        try
        {
            await websocket.Connect();
            Debug.Log("������ ���� �Ϸ�");
        }
        catch (Exception ex)
        {
            Debug.LogError("������ ���� ����: " + ex);
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
            if (frameQueue.Count > 0)
            {
                //lock (messageQueue)
                //{
                //    json = messageQueue.Dequeue();
                //}
                Debug.Log("websocket frame !");

                FrameData frame = frameQueue.Dequeue();
                //var frame = JsonUtility.FromJson<FrameData>(frame);
                //FrameData frame = JsonUtility.FromJson<FrameData>(json);
                //if (frame != null)
                //{
                skeletonMapper.SetFrame(frame);
                //}

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
