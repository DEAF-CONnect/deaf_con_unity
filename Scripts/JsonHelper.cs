using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"frames\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.frames;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] frames;
    }
}
