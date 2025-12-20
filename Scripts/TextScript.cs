using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
[System.Serializable]
public class TimedLine
{
    public float time;
    public string text;
}
[System.Serializable]
public class AppConfig
{
    public string concertId;
    public string songId;
}

public class TextScript : MonoBehaviour
{
    public string concertId;
    public string songId;
    public TMP_Text lyricsText;
    private string baseUrl;

    void Start()
    {
        baseUrl = $"{baseUrl}/api/concerts/{concertId}/songs/";

        Debug.Log("Base URL: " + baseUrl );

        StartCoroutine(GetSongData());
    }

    IEnumerator GetSongData()
    {
        string url = baseUrl + songId; 

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                SongResponse song = JsonUtility.FromJson<SongResponse>(json);
                
                if (song != null && song.lyrics != null)
                {
                    StartCoroutine(DisplayLyricsOneByOne(song.lyrics));
                }
                else
                {
                    lyricsText.text = "";
                }
            }
            else
            {
                lyricsText.text = "Load Failed: " + req.error;
            }
        }
    }

    IEnumerator DisplayLyricsOneByOne(string[] lyrics)
    {
        lyricsText.text = ""; 

        List<(float time, string text)> timedLines = new List<(float, string)>();

        foreach (string line in lyrics)
        {
            int end = line.IndexOf(']');
            string timeStr = line.Substring(1, end - 1); 
            string text = line.Substring(end + 1).Trim();

            float t = ParseTimeToSeconds(timeStr);
            timedLines.Add((t, text));
        }
        timedLines.Sort((a, b) => a.time.CompareTo(b.time));

        float startTime = Time.time;

        foreach (var item in timedLines)
        {
            float targetTime = startTime + item.time;

            while (Time.time < targetTime)
                yield return null;

            lyricsText.text = item.text;
        }
    }
    private float ParseTimeToSeconds(string t)
    {
        // mm:ss.xx
        string[] parts = t.Split(':');           // ["00", "00.58"]
        string[] secParts = parts[1].Split('.'); // ["00", "58"]

        float minutes = float.Parse(parts[0]);
        float seconds = float.Parse(secParts[0]);
        float ms = float.Parse(secParts[1]);

        return minutes * 60f + seconds + (ms / 100f);
    }
    private void Awake(){
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
        string json = System.IO.File.ReadAllText(path);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);
        concertId = cfg.concertId;
        songId = cfg.songId;
    }
}
