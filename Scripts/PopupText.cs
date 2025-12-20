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

    public string concertId = "QWER-2025 부산 국제 록 페스티벌";
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
        string artist = "Unknown";
        using (UnityWebRequest reqConcert = UnityWebRequest.Get(baseUrlConcert))
        {
            yield return reqConcert.SendWebRequest();

            if (reqConcert.result == UnityWebRequest.Result.Success)
            {
                var json = reqConcert.downloadHandler.text;
                artist = JsonUtility.FromJson<ArtistResponse>(json).artist;
            }
        }

        string songTitle = "Unknown";
        using (UnityWebRequest reqSong = UnityWebRequest.Get(baseUrlSong))
        {
            yield return reqSong.SendWebRequest();

            if (reqSong.result == UnityWebRequest.Result.Success)
            {
                var json = reqSong.downloadHandler.text;
                songTitle = JsonUtility.FromJson<SongResponse>(json).title;
            }
        }

        string message = $"{artist} - {songTitle}"; //popup에 띄우기
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
