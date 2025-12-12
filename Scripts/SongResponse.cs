using System;
using UnityEngine;
// createAt
[System.Serializable]
public class Timestamp
{
    public long seconds;
    public int nanos;
}
[System.Serializable]
public class SongResponse
{
    public string songId;
    public string title;
    public string artistId;
    public string audioFileUrl;
    public String[] lyrics;
    public Timestamp createdAt;
}