using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PosePlayer : MonoBehaviour
{
    public float startDelay = 0.58f;
    private float delayTimer = 0f;
    public SkeletonMapper skeletonMapper;
    public TextAsset jsonFile;

    public float scale = 1f;
    public float frameRate = 30f;
    //public bool loop = true;

    private FrameData[] frames;
    private int currentFrame = 0;
    private float timer = 0f;

    public System.Action OnPlaybackFinished;
    public bool playbackFinished = false;

    void Start()
    {
        if (jsonFile == null)
        {
            Debug.LogError("jsonFile is null!");
            playbackFinished = true;
            return;
        }

        frames = JsonHelper.FromJson<FrameData>(jsonFile.text);
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("Loaded frames are empty!");
            playbackFinished = true;
            return;
        }

    }

    void Update()
    {
        if (playbackFinished) 
            return;
        if (delayTimer < startDelay)
        {
            delayTimer += Time.deltaTime;
            return;
        }
        if (frames == null || frames.Length == 0)
            return;
        timer += Time.deltaTime;
        float frameTime = 1f / frameRate;

        
        while (timer >= frameTime)
        {
            timer -= frameTime;

            
            if (currentFrame >= frames.Length)
            {
                
                playbackFinished = true;
                Debug.Log("QWER JSON ��� ����");
                OnPlaybackFinished?.Invoke();
                return;
            }
            var frame = frames[currentFrame];
            skeletonMapper.SetFrame(frame);
            Debug.Log("������ ��� ��: " + currentFrame);
            currentFrame++;
            //currentFrame = loop ? 0 : frames.Length - 1;
        }
    }
    

}