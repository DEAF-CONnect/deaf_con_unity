using System;
using System.Collections.Generic;
using UnityEngine;

// FrameData�� Landmark ����ü�� �ܺο��� ���ǵǾ��ٰ� �����մϴ�.
// public class FrameData { public float t; public Landmark[] pose; public Landmark[] left_hand; public Landmark[] right_hand; }
// public struct Landmark { public float x, y, z; public float visibility; }

#region ===== Skeleton Mapper (Rotation-only) =====
public class SkeletonMapper : MonoBehaviour
{
    private FrameData latestFrame;

    [Header("Avatar")]
    public Animator animator;

    [Header("Scale/Depth")]
    [Tooltip("����ȭ(0~1) ��ǥ�� ����� ��ȯ�� ���� ũ�� ����")]
    public float worldScale = 1.5f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float emaAlpha = 0.3f; // 0.6: ���� ����(�������) / 0.2: ����(������)

    [Header("Arm Fine Tuning")]
    public Vector3 leftUpperArmOffsetEuler = Vector3.zero;
    public Vector3 rightUpperArmOffsetEuler = Vector3.zero;
    public Vector3 leftLowerArmOffsetEuler = Vector3.zero;
    public Vector3 rightLowerArmOffsetEuler = Vector3.zero;


    class BoneBindInfo
    {
        public Quaternion bindRot;
        public Vector3 bindDirWorld;

    }
    
    private readonly Dictionary<HumanBodyBones, BoneBindInfo> _boneBind = new();
    private readonly Dictionary<HumanBodyBones, Vector3> _emaDir = new();
    [Header("2.5D Depth")]
    public Camera refCam;
    public float z0 = 2.0f; 
    public float depthGain = 0.8f;

    [Range(0f, 1f)] public float depthEma = 0.3f; 
    public int refInitFrames = 15; 
    private float _Wref = 0f;
    private int _WaccumCount = 0;
    private float _depth;

    // Pose(33): 11=LShoulder, 12=RShoulder, 13=LElbow, 14=RElbow, 15=LWrist, 16=RWrist,
    //           23=LHip, 24=RHip, 25=LKnee, 26=RKnee, 27=LAnkle, 28=RAnkle

    bool Vis(Landmark[] a, int i) => a != null && i < a.Length && a[i].visibility > 0f;
    void Awake()
    {
        if (!animator)
            animator = GetComponent<Animator>();

    }
    public void SetFrame(FrameData f)
    {
        latestFrame = f;
    }


    void LateUpdate()
    {
        if (latestFrame != null)
        {
            if (latestFrame.pose != null && latestFrame.pose.Length >= 9)
            {
                UpdateDepth(latestFrame.pose);
            }


            ApplyFrame(latestFrame);
        }
    }


    float ShoulderWidth(Landmark[] pose)
    {
        if (!Vis(pose, 5) || !Vis(pose, 2)) return -1f;
        float dx = pose[5].x - pose[2].x;
        float dy = pose[5].y - pose[2].y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    void UpdateDepth(Landmark[] pose)
    {
        // Right Shoulder(2), Left Shoulder(5)
        var r = pose[2];
        var l = pose[5];

        float dx = r.x - l.x;
        float dy = r.y - l.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        float targetDepth = 1.0f / Mathf.Max(dist, 0.001f);

        _depth = Mathf.Lerp(_depth, targetDepth, Time.deltaTime * 5f);
    }

    Vector3 ToWorld2_5D(Landmark lm)
    {
        if (!refCam)
        {
            float x = (lm.x - 0.5f) * worldScale;
            float y = (0.5f - lm.y) * worldScale;
            return new Vector3(x, y, 0f);
        }

        float nx = lm.x - 0.5f;   
        float ny = 0.5f - lm.y;

        Vector3 local =
            refCam.transform.right * (nx * worldScale) +
            refCam.transform.up * (ny * worldScale) +
            refCam.transform.forward * Mathf.Max(0.01f, _depth);

        return refCam.transform.position + local;

    }

    void Start()
    {
        CacheBone(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
        CacheBone(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        CacheBone(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
        CacheBone(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);

        
        CacheBone(HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate);
        CacheBone(HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal);
        CacheBone(HumanBodyBones.LeftThumbDistal, null);

        CacheBone(HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate);
        CacheBone(HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal);
        CacheBone(HumanBodyBones.LeftIndexDistal, null);

        CacheBone(HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate);
        CacheBone(HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal);
        CacheBone(HumanBodyBones.LeftMiddleDistal, null);

        CacheBone(HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate);
        CacheBone(HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal);
        CacheBone(HumanBodyBones.LeftRingDistal, null);

        CacheBone(HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate);
        CacheBone(HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal);
        CacheBone(HumanBodyBones.LeftLittleDistal, null);

        // ������
        CacheBone(HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate);
        CacheBone(HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal);
        CacheBone(HumanBodyBones.RightThumbDistal, null);

        CacheBone(HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate);
        CacheBone(HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal);
        CacheBone(HumanBodyBones.RightIndexDistal, null);

        CacheBone(HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate);
        CacheBone(HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal);
        CacheBone(HumanBodyBones.RightMiddleDistal, null);

        CacheBone(HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate);
        CacheBone(HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal);
        CacheBone(HumanBodyBones.RightRingDistal, null);

        CacheBone(HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate);
        CacheBone(HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal);
        CacheBone(HumanBodyBones.RightLittleDistal, null);
    }

    void CacheBone(HumanBodyBones bone, HumanBodyBones? childBone)
    {
        Transform t = animator.GetBoneTransform(bone);
        if (!t) return;

        Vector3 dirWorld;
        
        if (childBone.HasValue)
        {
            Transform c = animator.GetBoneTransform(childBone.Value);
            if (c)
                dirWorld = (c.position - t.position).normalized;  // ���ε� ����� ������ ���ϴ� ����
            else
                dirWorld = (t.rotation * Vector3.forward).normalized;
        }
        else
        {
            dirWorld = (t.rotation * Vector3.forward).normalized;
        }
        
        _boneBind[bone] = new BoneBindInfo
        {
            bindRot = t.rotation,
            bindDirWorld = dirWorld
        };
    }

    public void ApplyFrame(FrameData f)
    {
        if (f == null) return;
        if( f.pose == null)
        {
            Debug.LogWarning("Frame or pose is null. Applying hands only.");
            ApplyHands(f.left_hand, true);
            ApplyHands(f.right_hand, false);
            return;
        }

        if (f.pose.Length < 25)
        {
            Debug.LogWarning($"Pose too short (len={f.pose.Length}). Applying hands only.");
            ApplyHands(f.left_hand, true);
            ApplyHands(f.right_hand, false);
            return;
        }

        ApplyPose(f.pose, f.left_hand, f.right_hand);
        ApplyHands(f.left_hand, true);
        ApplyHands(f.right_hand, false);
    }

    public void ApplyPose(Landmark[] pose, Landmark[] leftHand, Landmark[] rightHand)
    {
        if (pose == null || pose.Length < 19)
        {
            Debug.LogWarning("pose too short");
            return;
        }

        // LEFT ARM: shoulder=15, elbow=17, wrist=leftHand[0]
        ApplyArmWithHand(
            pose,
            leftHand,
            5,
            6,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            true);

        // RIGHT ARM: shoulder=16, elbow=18, wrist=rightHand[0]
        ApplyArmWithHand(
            pose,
            rightHand,
            2,
            3,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            false);
    }



    public void ApplyHands(Landmark[] hand, bool isLeft)
    {
        if (hand == null || hand.Length < 21) return;

        if (isLeft)
        {
            ApplyFingerChain(hand, 5, 6, 7, 8, HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal);
            ApplyFingerChain(hand, 9, 10, 11, 12, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal);
            ApplyFingerChain(hand, 13, 14, 15, 16, HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal);
            ApplyFingerChain(hand, 17, 18, 19, 20, HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal);
            ApplyFingerChain(hand, 1, 2, 3, 4, HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal);
        }
        else
        {
            ApplyFingerChain(hand, 5, 6, 7, 8, HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal);
            ApplyFingerChain(hand, 9, 10, 11, 12, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal);
            ApplyFingerChain(hand, 13, 14, 15, 16, HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal);
            ApplyFingerChain(hand, 17, 18, 19, 20, HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal);
            ApplyFingerChain(hand, 1, 2, 3, 4, HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal);
        }
    }

    #region ---- Internals ----


    void ApplyArmWithHand(
    Landmark[] pose, Landmark[] hand,
    int shoulderIdx, int elbowIdx,
    HumanBodyBones upper, HumanBodyBones lower,
    bool isLeft)
    {
        if (hand == null || hand.Length == 0) return;

        Vector3 pShoulderW = ToWorld2_5D(pose[shoulderIdx]);
        Vector3 pElbowW = ToWorld2_5D(pose[elbowIdx]);
        Vector3 pWristW = ToWorld2_5D(hand[0]);

        Vector3 dirUpperW;
        Vector3 dirLowerW;

        if (refCam)
        {
            Vector3 sC = refCam.transform.InverseTransformPoint(pShoulderW);
            Vector3 eC = refCam.transform.InverseTransformPoint(pElbowW);
            Vector3 wC = refCam.transform.InverseTransformPoint(pWristW);

            Vector3 dirUpperC = eC - sC;
            Vector3 dirLowerC = wC - eC;

            if (dirUpperC.z > 0f) dirUpperC.z = -dirUpperC.z;
            dirUpperC.z -= 0.1f;

            if (dirLowerC.z > 0f) dirLowerC.z = -dirLowerC.z;
            dirLowerC.z -= 0.1f;

            dirUpperC.Normalize();
            dirLowerC.Normalize();

            dirUpperW = refCam.transform.TransformDirection(dirUpperC);
            dirLowerW = refCam.transform.TransformDirection(dirLowerC);
        }
        else
        {
            dirUpperW = SafeDir(pElbowW - pShoulderW);
            dirLowerW = SafeDir(pWristW - pElbowW);
        }
        Vector3 armNormal = Vector3.Cross(dirUpperW, dirLowerW).normalized;

        if (armNormal.sqrMagnitude < 0.001f) armNormal = isLeft ? Vector3.up : Vector3.down;
        ApplyBoneLook(upper, dirUpperW);
        ApplyBoneLook(lower, dirLowerW);
    }



    void ApplyFingerChain(Landmark[] h, int a, int b, int c, int tip,
                          HumanBodyBones prox, HumanBodyBones inter, HumanBodyBones dist)
    {
        if (h == null || h.Length <= Math.Max(a, Math.Max(b, Math.Max(c, tip))))
            return;

        Vector3 pA = ToWorld2_5D(h[a]);
        Vector3 pB = ToWorld2_5D(h[b]);
        Vector3 pC = ToWorld2_5D(h[c]);
        Vector3 pTp = ToWorld2_5D(h[tip]);

        Vector3 dProx = SafeDir(pB - pA);
        Vector3 dInter = SafeDir(pC - pB);
        Vector3 dDist = SafeDir(pTp - pC);
        
        ApplyBoneLook(prox, dProx);
        ApplyBoneLook(inter, dInter);
        ApplyBoneLook(dist, dDist);
    }
    
    private readonly Dictionary<HumanBodyBones, Vector3> _emaFingerDir = new();

    void ApplyFingerLook(HumanBodyBones bone, Vector3 worldDir)
    {
        worldDir = SafeDir(worldDir);

        if (_emaFingerDir.TryGetValue(bone, out var prev))
            worldDir = Vector3.Normalize((1f - emaAlpha) * prev + emaAlpha * worldDir);

        _emaFingerDir[bone] = worldDir;

        Transform t = animator.GetBoneTransform(bone);
        if (!t) return;

        t.rotation = Quaternion.LookRotation(worldDir, Vector3.up);
    }


    void ApplyBoneLook(HumanBodyBones bone, Vector3 worldDir)
    {
        Transform t = animator.GetBoneTransform(bone);
        if (!t) return;

        worldDir = SafeDir(worldDir);

        // EMA
        if (_emaDir.TryGetValue(bone, out var prev))
            worldDir = Vector3.Normalize((1f - emaAlpha) * prev + emaAlpha * worldDir);
        _emaDir[bone] = worldDir;

        if (_boneBind.TryGetValue(bone, out var info))
        {
            Quaternion fromTo = Quaternion.FromToRotation(info.bindDirWorld, worldDir);
            Quaternion result = fromTo * info.bindRot;

            
            switch (bone)
            {
                case HumanBodyBones.LeftUpperArm:
                    result *= Quaternion.Euler(leftUpperArmOffsetEuler);
                    break;
                case HumanBodyBones.RightUpperArm:
                    result *= Quaternion.Euler(rightUpperArmOffsetEuler);
                    break;
                case HumanBodyBones.LeftLowerArm:
                    result *= Quaternion.Euler(leftLowerArmOffsetEuler);
                    break;
                case HumanBodyBones.RightLowerArm:
                    result *= Quaternion.Euler(rightLowerArmOffsetEuler);
                    break;
            }

            t.rotation = result;
        }
        else
        {
            t.rotation = Quaternion.LookRotation(worldDir, Vector3.up);
        }
    }

    bool IsArmBone(HumanBodyBones bone)
    {
        return bone == HumanBodyBones.LeftUpperArm
            || bone == HumanBodyBones.LeftLowerArm
            || bone == HumanBodyBones.RightUpperArm
            || bone == HumanBodyBones.RightLowerArm;
    }

    static Vector3 SafeDir(Vector3 v)
    {
        if (v.sqrMagnitude < 1e-8f) return Vector3.forward;
        return v.normalized;
    }
    #endregion

}
#endregion