using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

#if (UNITY_EDITOR) 
[ExecuteInEditMode]
#endif

public class IKControl : MonoBehaviour {
#if (UNITY_EDITOR)
    [CustomEditor(typeof(IKControl))]
    public class ObjectBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            IKControl myScript = (IKControl)target;

            if (!myScript.isControlling) // only able to save rotations if you aren't animating
            {
                if (GUILayout.Button("Save rotations and start animation"))
                {
                    myScript.SaveRotations();
                }
            }


            if (GUILayout.Button("Return to original rotations"))
            {
                myScript.ReturnToRotations(true);
                //myScript.SaveRotations();
            }
            if (GUILayout.Button("Stop animations and return to original rotations"))
            {
                myScript.isControlling = false;
                myScript.ReturnToRotations(true);
            }
        }
    }
#endif

    [Tooltip("Settings")]
    public IKControlSettings IKCS;

    [System.Serializable]
    public class IKControlSettings
    {
        public Transform t_targetEndBone;
        public Transform t_boneRoot;        
        public int i_IKparents;
        public Transform[] tList_otherBones;
        public Transform t_startTarget;
        public Transform upTarget;
        public bool b_RotateEnd = false;
        public IKType type = IKType.Simple;
        public JointType jType = JointType.PreserveForward;
        public SecondPassType spType = SecondPassType.None;
        public bool b_flipUpVector = false;
        public bool b_flipFwdVector = false;
        public float minRange = 0.0f;
        // Pre pass vars
        public float f_PrePassAccel = 1f;
        public float f_PrePassMaxSpeed = 10f;
        public float f_PrePassDistanceThreshold = .1f;
        // Second pass vars
        public float f_ChainBaseReturn;
        public float f_ChainEndReturn;

        public bool b_debug = false;
    }

    public SecondPassBase C_secondPass;

    IKBase solver;
#if (UNITY_EDITOR)
    [ReadOnly]
#endif
    public bool isControlling = true;
    public bool controlWhenUnchanged = true;

    public bool b_doPrePass { get; set; }
    public bool b_doSecondPass { get; set; }
    public bool b_isOrdered { get; set; }
    public Vector3 v3_targetIKposition { get; set; }
    Vector3 v3_localIKposition;
    public Quaternion q_targetIKrotation { get; set; }
    public Quaternion q_localStartRotation { get; set; }
    public float f_localIKscale { get; set; }
    float f_targetIKspeed { get; set; }

    public enum IKType
    { Simple = 0,
        SphereChain = 1,
        SplineChain = 2,
        Wing = 3 }

    public enum SecondPassType
    {
        None = 0,
        Tail = 1,
        Spine = 2,
        Neck = 3
    }

    public enum JointType
    {
        PreserveForward = 0,
        PreserveUp = 1,
        Hybrid = 2,
        Hybrid2 = 3,
        PointUpToTarget = 4
    }
#if (UNITY_EDITOR)
    [ReadOnly]
#endif
    public BoneData[] bdArr_boneData;
#if (UNITY_EDITOR)
    [ReadOnly]
#endif
    public BoneData[] bdArr_otherBoneData;
#if (UNITY_EDITOR)
    [ReadOnly]
#endif
    public BoneData bd_rootData;

    private void OnEnable()
    {
#if (UNITY_EDITOR)
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
#endif

        Init();
    }

    private void Awake()
    {
        Init();
    }

    // Use this for initialization
    void Init() {
        if (IKCS.b_debug) { Debug.Log(this + "init is called!"); }

        PrePassReset(0f, 1f);

        ReturnToRotations(false);

        switch (IKCS.type)
        {
            case IKType.Simple:
            case IKType.Wing:
                solver = new IKSimple();
                break;
            case IKType.SphereChain:
                solver = new IKSphereChain();
                break;
            case IKType.SplineChain:
                solver = new IKSplineChain();
                break;
            default:
                solver = new IKSimple();
                break;
        }

        if (solver.Init(this, transform) == false)
        {
            Debug.Log(this + " No bone data saved!");
            isControlling = false;
            return;
        }

        if (IKCS.spType != SecondPassType.None)
            SetupSecondPass();
    }

    void Update()
    {
        if (isControlling && (controlWhenUnchanged || transform.hasChanged))
            ReturnToRotations(false);
    }


    void LateUpdate()
    {
        if (!b_isOrdered)
            OrderedUpdate();
    }

    // Call updates in the correct order
    public void OrderedUpdate()
    {       
        if (isControlling && (controlWhenUnchanged || transform.hasChanged))
		{
            if (IsBoneDataInvalid(bdArr_boneData))
            {
                Debug.LogWarning(this + " no bonedata set. Turning off");
                isControlling = false;
                return;
            }
            if (solver == null)
            {
                Debug.LogWarning(this + " no solver set. Turning off");
                isControlling = false;
                return;
            }

            PrePass();

            Quaternion[] solutions = solver.Solve(IKCS.upTarget, IKCS.b_debug);

            solver.RotateBones();

            transform.hasChanged = false;

            if (IKCS.b_RotateEnd)
                solver.RotateEnd();

            b_doSecondPass = true;
        }
    }

    void PrePassReset(float distance, float time)
    {
        v3_targetIKposition = transform.position;
        v3_localIKposition = transform.localPosition;
        q_targetIKrotation = transform.rotation;
        q_localStartRotation = Quaternion.identity;
        f_localIKscale = transform.localScale.x;
        f_targetIKspeed = Mathf.Max(distance / time, 0f);
    }

    // smoothly interpolates the target if necessary
    void PrePass()
    {
        float time = Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 distanceGap = transform.localPosition - v3_localIKposition;
        float distance = Vector3.Magnitude(distanceGap);

        /*#if (UNITY_EDITOR)
                if (!Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (time > 0.1f)
                    {
                        PrePassReset(distance, time);
                        return;
                    }
                }
        #endif*/

        // If prepass is off, check if the target gets too far away
        if (!b_doPrePass && distance < (IKCS.f_PrePassDistanceThreshold + f_targetIKspeed)  * time)
        {
            PrePassReset(distance, time);
            return;
        }
        else
        {
            if (IKCS.b_debug) { Debug.Log(this + " turning prepass on, " + distance + " > " + (IKCS.f_PrePassDistanceThreshold + f_targetIKspeed) * time);  }
            b_doPrePass = true;        
        }

        Vector3 direction = Vector3.Normalize(distanceGap);

        // Check if we need to accelerate or deccelerate
        float t = f_targetIKspeed * f_targetIKspeed / (2 * IKCS.f_PrePassAccel * distance);
        f_targetIKspeed += time * Mathf.Lerp(IKCS.f_PrePassAccel, -IKCS.f_PrePassAccel, t);

        /*if (f_targetIKspeed * f_targetIKspeed > 2 * IKCS.f_PrePassAccel * distance)
            f_targetIKspeed -= IKCS.f_PrePassAccel * Time.deltaTime;
        else
            f_targetIKspeed += IKCS.f_PrePassAccel * Time.deltaTime;*/

        f_targetIKspeed = Mathf.Clamp(f_targetIKspeed, 0f, IKCS.f_PrePassMaxSpeed);

        // check distance. if you are close just get there
        if (distance < f_targetIKspeed * time)
        {
            PrePassReset(distance, time);
            b_doPrePass = false;
            if (IKCS.b_debug) { Debug.Log(this + " turning prepass off, " + distance + " < " + (f_targetIKspeed * time)); }
            return;               
        }
        else
        {
            v3_localIKposition = v3_localIKposition + direction * f_targetIKspeed * time;
            v3_targetIKposition = transform.parent.TransformPoint(v3_localIKposition);
        }

        float ratio = f_targetIKspeed * time / distance;

        // Now do rotatation too
        q_targetIKrotation = Quaternion.Slerp(q_targetIKrotation, transform.rotation, ratio);
        f_localIKscale = Mathf.Lerp(f_localIKscale, transform.localScale.x, ratio);

        if (IKCS.t_startTarget)
        {
            q_localStartRotation = Quaternion.Slerp(q_localStartRotation, IKCS.t_startTarget.localRotation, ratio);
        }
    }

    void SetupSecondPass()
    {
        if (IKCS.b_debug)
            Debug.Log(this + " setting up second pass of type " + IKCS.spType);

        switch (IKCS.spType)
        {
            case SecondPassType.Spine:
                C_secondPass = new SecondPassSpine(this);
                break;
            case SecondPassType.Tail:
                C_secondPass = new SecondPassTail(this);
                break;
            default:
                break;
        }

        //b_doPasses = true;
    }

    void SaveRotations()
    {
        if (IKCS.t_targetEndBone == null)
        {
            Debug.LogWarning(this + " no target IK bone!");
            return;
        }
        if (IKCS.i_IKparents < 2)
        {
            Debug.LogWarning(this + " not enough bones set!");
            return;
        }
               
        bdArr_boneData = new BoneData[IKCS.i_IKparents +1];
        Transform bone = IKCS.t_targetEndBone;
        for (int i = IKCS.i_IKparents; i >= 0 ; i--)
        {
            bdArr_boneData[i] = new BoneData(bone, transform.up);
            bone = bone.parent;
        }

        AlignTarget();

        bdArr_otherBoneData = new BoneData[IKCS.tList_otherBones.Length];
        for (int i = 0; i < IKCS.tList_otherBones.Length; i++)
        {
            bdArr_otherBoneData[i] = new BoneData(IKCS.tList_otherBones[i], transform.up);
        }

        if (IKCS.t_boneRoot != null)
            bd_rootData = new BoneData(IKCS.t_boneRoot, Vector3.up);

        isControlling = true;
        Init();      
    }

    public void ReturnToRotations(bool align)
    {
        ReturnRoot();

        if (IsBoneDataInvalid(bdArr_boneData))
        {
            Debug.Log(this + " No bone data saved!");
        }
        else
        {
            Transform iterBone = IKCS.t_targetEndBone;

            for (int i = IKCS.i_IKparents; i >= 0; i--)
            {
                if (IKCS.b_debug && i == 0)
                    Debug.Log("we reach i = 0, localrotation is " + bdArr_boneData[i].t_bone.localRotation + " align is " + align);
                bdArr_boneData[i].t_bone.localPosition = bdArr_boneData[i].v3_startingLocalPosition;
                bdArr_boneData[i].t_bone.localRotation = bdArr_boneData[i].q_startingLocalRot;
                iterBone = iterBone.parent;

                if (IKCS.b_debug && i == 0)
                    Debug.Log("now localrotation is " + bdArr_boneData[i].t_bone.localRotation);
            }
        }

        

        if (!IsBoneDataInvalid(bdArr_otherBoneData))
        {

            for (int i = 0; i < IKCS.tList_otherBones.Length; i++)
            {
                IKCS.tList_otherBones[i].localRotation = bdArr_otherBoneData[i].q_startingLocalRot;
            }
        }

        if (align)
        {
            transform.hasChanged = false;
            AlignTarget();
        }
            
    }

    void AlignTarget()
    {
        b_doSecondPass = false;
        b_doPrePass = false;

        int length = IKCS.i_IKparents;
        if (IsBoneDataInvalid(bdArr_boneData))
        {
            Debug.Log(this + " No bone data saved!");
            return;
        }
        transform.position = bdArr_boneData[length].position;
        if (IKCS.b_RotateEnd)
            transform.rotation = bdArr_boneData[length].rotation;
        else
            transform.localRotation = Quaternion.identity;

        if (IsBoneDataInvalid(bdArr_otherBoneData))
            return;

        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    void ReturnRoot()
    {
        if (bd_rootData != null && IKCS.t_boneRoot != null)
        {
            IKCS.t_boneRoot.localPosition = bd_rootData.v3_startingLocalPosition;
            IKCS.t_boneRoot.localRotation = bd_rootData.q_startingLocalRot;
        }
    }

    public static bool IsBoneDataInvalid(BoneData[] arr)
    {
        return (arr == null || arr.Length == 0);
    }
}
