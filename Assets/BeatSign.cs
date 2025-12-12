using System.Collections;
using UnityEngine;

[System.Serializable]
public class BeatJsonData
{
    public float[] beats;
}

public class BeatSign : MonoBehaviour
{
    public TextAsset beatJsonFile;
    private float[] beats;

    void Start()
    {
        LoadBeats();
        StartCoroutine(PlayBeats());
    }
    void LoadBeats()
    {
        if (beatJsonFile == null)
        {
            Debug.LogError("[BeatVibration] beatJsonFile�� ����!");
            return;
        }

        BeatJsonData data = JsonUtility.FromJson<BeatJsonData>(beatJsonFile.text);
        beats = data.beats;

        if (beats == null || beats.Length == 0)
            Debug.LogError("[BeatVibration] beats �������");
        else
            Debug.Log($"[BeatVibration] beats {beats.Length}�� �ε�");
    }
    IEnumerator PlayBeats()
    {
        if (beats == null || beats.Length == 0)
            yield break;

        float startTime = Time.time;

        foreach (float beatTime in beats)
        {
            float targetTime = startTime + beatTime;

            // ��Ȯ�� �ð����� ��ٸ�
            while (Time.time < targetTime)
                yield return null;

            TriggerVibration();
        }

        Debug.Log("[BeatVibration] ��� ��Ʈ ���� �Ϸ�");
    }

    // --------------------------------------------------
    // 3) ���� Ʈ����
    // --------------------------------------------------
    void TriggerVibration()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        Debug.Log("?? VIBRATE!");
    }
}
