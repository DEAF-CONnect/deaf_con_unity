using System;
using UnityEngine;

[System.Serializable]
public class Landmark
{
    public float x;
    public float y;
    public float visibility;
}

[System.Serializable]
public class FrameData
{
    public float t;
    public Landmark[] pose;
    public Landmark[] left_hand;
    public Landmark[] right_hand;
    public string token;
}

[System.Serializable]
public class FrameList
{
    public FrameData[] frames;
}
