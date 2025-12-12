using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
[System.Serializable]
public class AppConfig
{
    public string baseUrl;
}

public class PopupManager : MonoBehaviour
{
    public PopupController popup;

    // ���ÿ� concertId, songId
    public string concertId = "QWER-2025 �λ� ���� �� �佺Ƽ��";
    public string songId = "1YEBOHIieNLUL3sCMzRg";
    
    private string baseUrlConcert;
    private string baseUrlSong;

    public string baseUrl ; 
    void Start()
    {
        baseUrlConcert = $"{baseUrl}/api/concerts/{concertId}";
        baseUrlSong = $"{baseUrl}/api/concerts/{concertId}/songs/{songId}";

        StartCoroutine(FetchAndShowPopup());
    }

    private IEnumerator FetchAndShowPopup()
    {
        // 1) concertId�� artist ���� ��������
        string artist = "Unknown";
        using (UnityWebRequest reqConcert = UnityWebRequest.Get(baseUrlConcert))
        {
            yield return reqConcert.SendWebRequest();

            if (reqConcert.result == UnityWebRequest.Result.Success)
            {
                // JSON �Ľ� ��: { "artist": "QWER" }
                var json = reqConcert.downloadHandler.text;
                artist = JsonUtility.FromJson<ArtistResponse>(json).artist;
            }
        }

        // 2) songId�� songTitle ���� ��������
        string songTitle = "Unknown";
        using (UnityWebRequest reqSong = UnityWebRequest.Get(baseUrlSong))
        {
            yield return reqSong.SendWebRequest();

            if (reqSong.result == UnityWebRequest.Result.Success)
            {
                // JSON �Ľ� ��: { "title": "�����ߵ�" }
                var json = reqSong.downloadHandler.text;
                songTitle = JsonUtility.FromJson<SongResponse>(json).title;
            }
        }

        // 3) �˾� ����
        string message = $"{artist} - {songTitle}";
        popup.ShowPopup(message);
    }

    [System.Serializable]
    private class ArtistResponse
    {
        public string artist;
    }

    [System.Serializable]
    private class SongResponse
    {
        public string title;
    }
    private void Awake(){
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        string json = System.IO.File.ReadAllText(path);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);
        baseUrl; = cfg.http_baseurl+ ;
    }
}
