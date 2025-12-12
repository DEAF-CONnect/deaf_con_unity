using System;
using System.Collections.Generic;
using UnityEngine;

// FrameData와 Landmark 구조체는 외부에서 정의되었다고 가정합니다.
// public class FrameData { public float t; public Landmark[] pose; public Landmark[] left_hand; public Landmark[] right_hand; }
// public struct Landmark { public float x, y, z; public float visibility; }

#region ===== Skeleton Mapper (Rotation-only) =====
public class SkeletonMapper : MonoBehaviour
{
    private FrameData latestFrame;

    [Header("Avatar")]
    public Animator animator;

    [Header("Scale/Depth")]
    [Tooltip("정규화(0~1) 좌표를 월드로 변환할 때의 크기 배율")]
    public float worldScale = 1.5f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float emaAlpha = 0.3f; // 0.6: 반응 빠름(노이즈↑) / 0.2: 안정(지연↑)

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
    // ===== 2.5D Depth 변수 (기존 로직 유지) =====
    [Header("2.5D Depth")]
    public Camera refCam;       // 투영 기준 카메라
    public float z0 = 2.0f;     // 기본 깊이(미터)
    public float depthGain = 0.8f; // k (깊이 보정 강도)

    [Range(0f, 1f)] public float depthEma = 0.3f; // 깊이 스무딩
    public int refInitFrames = 15; // Wref 초기화에 사용할 프레임 개수

    private float _Wref = 0f;
    private int _WaccumCount = 0;
    private float _depth;        // 현재 EMA된 깊이

    // MediaPipe Index 참고 (주요부만)
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

    // IK 로직이 없으므로 OnAnimatorIK 메서드는 제거/주석 처리합니다.
    // private void OnAnimatorIK(int layerIndex) { /* IK logic removed */ }

    void LateUpdate()
    {
        if (latestFrame != null)
        {
            if (latestFrame.pose != null && latestFrame.pose.Length >= 9)
            {
                // 깊이 계산은 유지 (2.5D 월드 좌표 변환을 위해 필요)
                UpdateDepth(latestFrame.pose);
            }

            // IK Targets 업데이트 로직 제거
            // UpdateIKTargets(latestFrame); // 제거됨

            // 포즈 및 손가락 회전만 적용
            ApplyFrame(latestFrame);
        }
    }

    // (ShoulderWidth, UpdateDepth, ToWorld2_5D 메서드는 기존과 동일하게 유지)

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

        // 2D 상의 어깨 거리
        float dx = r.x - l.x;
        float dy = r.y - l.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        // dist가 작을수록 멀리, 클수록 가까이 있다고 가정해서
        // 간단하게 깊이 스케일을 만든다 (임의 값, 필요시 조정)
        float targetDepth = 1.0f / Mathf.Max(dist, 0.001f);

        // _depth 값을 서서히 보간해서 튀지 않게
        _depth = Mathf.Lerp(_depth, targetDepth, Time.deltaTime * 5f);
    }

    Vector3 ToWorld2_5D(Landmark lm)
    {
        if (!refCam)
        {
            // 카메라 없으면 대략적인 평면 변환
            float x = (lm.x - 0.5f) * worldScale;
            float y = (0.5f - lm.y) * worldScale;
            return new Vector3(x, y, 0f);
        }

        // 0~1 → 화면 중심 기준 좌표 (-0.5 ~ 0.5)
        float nx = lm.x - 0.5f;   // 오른쪽이 +X
        float ny = 0.5f - lm.y;   // 위쪽이 +Y

        // 카메라 로컬 좌표계 기준 위치
        Vector3 local =
            refCam.transform.right * (nx * worldScale) +
            refCam.transform.up * (ny * worldScale) +
            refCam.transform.forward * Mathf.Max(0.01f, _depth);

        // 카메라 위치에서 이동
        return refCam.transform.position + local;
        /***
         * float nx = 1f - lm.x;
        float ny = lm.y;

        if (refCam)
        {
            float px = nx * Screen.width;
            float py = (1f - ny) * Screen.height;
            return refCam.ScreenToWorldPoint(new Vector3(px, py, Mathf.Max(0.01f, _depth)));
        }

        // 카메라 없을 경우 기준 변환
        return new Vector3((nx - 0.5f) * worldScale, (0.5f - ny) * worldScale, 0f);
         * 
         * 
         * 
         * **/

    }

    void Start()
    {
        /*
        if (refCam)
        {
            Vector3 fwd = refCam.transform.forward;
            fwd.y = 0f;  // 수평만 유지
            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }*/
        // IK를 사용하지 않으므로, 팔/다리/상체/손가락 등 움직여야 할 모든 뼈의 바인드 회전을 캐시합니다.
        CacheBone(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
        CacheBone(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        CacheBone(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
        CacheBone(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);

        // 다리
        //CacheBone(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg);
        //CacheBone(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        //CacheBone(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg);
        //CacheBone(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

        // 척추/머리 (자식이 있으면 자식 방향, 없으면 본의 forward 사용)
        //CacheBone(HumanBodyBones.Spine, HumanBodyBones.Chest);
        //CacheBone(HumanBodyBones.Chest, HumanBodyBones.UpperChest);
        //CacheBone(HumanBodyBones.UpperChest, HumanBodyBones.Neck);
        //CacheBone(HumanBodyBones.Head, null); // forward 축 사용

        // 손가락 (proximal → intermediate, intermediate → distal, distal은 tip 쪽이 없으니 forward 사용)
        // 왼손
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

        // 오른손
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
                dirWorld = (c.position - t.position).normalized;  // 바인드 포즈에서 실제로 향하던 방향
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

    // ======== 공개 API ========

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

        // 너의 JSON은 pose가 25개이므로, 최소 길이를 25로 맞춘다
        if (f.pose.Length < 25)
        {
            Debug.LogWarning($"Pose too short (len={f.pose.Length}). Applying hands only.");
            ApplyHands(f.left_hand, true);
            ApplyHands(f.right_hand, false);
            return;
        }

        // 여기까지 왔으면 몸 + 손 모두 적용
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

        // 각 손가락: Proximal/Intermediate/Distal을 Tip 방향으로 회전
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

    // (ApplyLimbByPoints, ApplyFingerChain 메서드는 기존과 동일하게 유지)
    /*
    void ApplyLimbByPoints(Landmark[] arr, int rootIdx, int midIdx, int tipIdx,
                       HumanBodyBones upper, HumanBodyBones lower)
    {
        if (arr == null || arr.Length <= Math.Max(rootIdx, Math.Max(midIdx, tipIdx)))
            return;

        // 1) 현재 월드 좌표
        Vector3 pRootW = ToWorld2_5D(arr[rootIdx]);
        Vector3 pMidW = ToWorld2_5D(arr[midIdx]);
        Vector3 pTipW = ToWorld2_5D(arr[tipIdx]);

        Vector3 dirUpperW = SafeDir(pMidW - pRootW); // 어깨 → 팔꿈치
        Vector3 dirLowerW = SafeDir(pTipW - pMidW);  // 팔꿈치 → 손목

        ApplyBoneLook(upper, dirUpperW);
        ApplyBoneLook(lower, dirLowerW);


        Vector3 dirUpperW;
        Vector3 dirLowerW;

        if (refCam)
        {
            // 2) 카메라 로컬 좌표로 변환
            Vector3 pRootC = refCam.transform.InverseTransformPoint(pRootW);
            Vector3 pMidC = refCam.transform.InverseTransformPoint(pMidW);
            Vector3 pTipC = refCam.transform.InverseTransformPoint(pTipW);

            // 3) 카메라 기준 방향 계산
            Vector3 dirUpperC = pMidC - pRootC; // 어깨→팔꿈치
            Vector3 dirLowerC = pTipC - pMidC;  // 팔꿈치→손목

            // ▶ 항상 "카메라 앞쪽"에서 움직이도록 z를 앞(+값)으로 밀어줌
            dirUpperC.z = Mathf.Abs(dirUpperC.z) + 0.1f;
            dirLowerC.z = Mathf.Abs(dirLowerC.z) + 0.1f;

            dirUpperC.Normalize();
            dirLowerC.Normalize();

            // 4) 다시 월드 방향으로 변환
            dirUpperW = refCam.transform.TransformDirection(dirUpperC);
            dirLowerW = refCam.transform.TransformDirection(dirLowerC);
        }
        else
        {
            // 카메라 없으면 기존 방식 유지
            dirUpperW = SafeDir(pMidW - pRootW);
            dirLowerW = SafeDir(pTipW - pMidW);
        }

        // 5) 최종 회전 적용
        ApplyBoneLook(upper, dirUpperW);
        ApplyBoneLook(lower, dirLowerW);
      }*/
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
            // 카메라 기준 로컬 변환
            Vector3 sC = refCam.transform.InverseTransformPoint(pShoulderW);
            Vector3 eC = refCam.transform.InverseTransformPoint(pElbowW);
            Vector3 wC = refCam.transform.InverseTransformPoint(pWristW);

            Vector3 dirUpperC = eC - sC;
            Vector3 dirLowerC = wC - eC;

            // === 핵심: z축은 무조건 앞쪽(+z)으로 ===
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
        /*추가부분 ApplyFingerLook
        
        ApplyFingerLook(prox, dProx);

        // Intermediate는 Proximal 기준 회전
        Transform tProx = animator.GetBoneTransform(prox);
        if (tProx)
        {
            Vector3 interDirLocal = tProx.InverseTransformDirection(dInter);
            animator.GetBoneTransform(inter).rotation = tProx.rotation * Quaternion.LookRotation(interDirLocal, tProx.up);
        }

        // Distal은 Intermediate 기준 회전
        Transform tInter = animator.GetBoneTransform(inter);
        if (tInter)
        {
            Vector3 distDirLocal = tInter.InverseTransformDirection(dDist);
            animator.GetBoneTransform(dist).rotation = tInter.rotation * Quaternion.LookRotation(distDirLocal, tInter.up);
        }
        /*잠시수정부분*/
        ApplyBoneLook(prox, dProx);
        ApplyBoneLook(inter, dInter);
        ApplyBoneLook(dist, dDist);
    }
    //private Dictionary<HumanBodyBones, Quaternion> boneOffset = new Dictionary<HumanBodyBones, Quaternion>();

    // IK 제어 뼈 목록 (IK가 제거되었으므로, 이 Set은 이제 비어 있거나, 다른 용도로 사용되지 않는다면 제거 가능)
    // 순수 회전 방식에서는 모든 뼈에 회전을 적용해야 합니다.
    //private static readonly HashSet<HumanBodyBones> IkControlledBones = new HashSet<HumanBodyBones>();
    /*추가부분 */
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

        // EMA 스무딩
        if (_emaDir.TryGetValue(bone, out var prev))
            worldDir = Vector3.Normalize((1f - emaAlpha) * prev + emaAlpha * worldDir);
        _emaDir[bone] = worldDir;

        if (_boneBind.TryGetValue(bone, out var info))
        {
            Quaternion fromTo = Quaternion.FromToRotation(info.bindDirWorld, worldDir);
            Quaternion result = fromTo * info.bindRot;

            // 여기서 팔에만 보정각도 적용
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