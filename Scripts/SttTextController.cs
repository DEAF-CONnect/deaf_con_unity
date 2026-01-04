using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using TMPro;
[System.Serializable]
//stt 받아오기
public class AppConfig
{
    public string web_socket;
}
public class SttTextController : MonoBehaviour
{
    private WebSocket websocket;
    public string stt_websocket;
    public string serverUrl = $"{stt_websocket}/ws/stt";

    public TMP_Text textUI;


async void Start()
    {
        
        websocket = new WebSocket(serverUrl);
        websocket.OnOpen += () =>
        {
            Debug.Log("WS OPEN: connected!");
        };

        websocket.OnMessage += (bytes) =>
        {
           
            string sttM = Encoding.UTF8.GetString(bytes);
            Debug.Log("WS STT TEXT: " + sttM);
            if (textUI != null)
                textUI.text += "\n" sttM;
        };
        websocket.OnError += (err) =>
        {
            Debug.LogError(">>> WEBSOCKET ERROR: " + err);
        };
        websocket.OnClose += (code) =>
        {
            Debug.LogWarning("WebSocket closed: " + code);
        };
        await websocket.Connect();
    }  

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }
    async void OnApplicationQuit()
    {
        await websocket.Close();
    }
    private void Awake(){
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        string json = System.IO.File.ReadAllText(path);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);
        stt_websocket = cfg.stt_websocket;
    }
}
