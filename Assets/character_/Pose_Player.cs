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

        // JSON 최상위 객체로 읽기
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

        /*if (timer >= frameTime)
        {
            ApplyFrame(frames[currentFrame]);
            currentFrame = (currentFrame + 1) % frames.Length;
            timer = 0f;
        }*/
        while (timer >= frameTime)
        {
            timer -= frameTime;
            //Debug.Log("Applying frame: " + currentFrame + ", t=" + frames[currentFrame].t);

            
            if (currentFrame >= frames.Length)
            {
                /*if (!loop)
                {
                    playbackFinished = true;
                    OnPlaybackFinished?.Invoke();
                    return;
                }
                else
                {
                    currentFrame = 0;
                }*/
                playbackFinished = true;
                Debug.Log("QWER JSON 재생 종료");
                OnPlaybackFinished?.Invoke();
                return;
            }
            var frame = frames[currentFrame];
            skeletonMapper.SetFrame(frame);
            Debug.Log("프레임 재생 중: " + currentFrame);
            currentFrame++;
            //currentFrame = loop ? 0 : frames.Length - 1;
        }
    }
    /*void ApplyFrame(FrameData frame)
    {
        skeletonMapper.ApplyPose(frame.pose, scale);
        skeletonMapper.ApplyHand(frame.left_hand, true, scale);
        skeletonMapper.ApplyHand(frame.right_hand, false, scale);
    }
    */

}