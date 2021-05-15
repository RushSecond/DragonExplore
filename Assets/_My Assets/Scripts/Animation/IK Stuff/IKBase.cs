using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IKBase
{
    protected int LENGTH;
    protected float positionAccuracy = 0.001f;
    protected IKControl CONTROL;
    protected IKControl.IKControlSettings IKCS;

    protected BoneData[] bdArr_data;
    //protected Transform[] bones;
    protected Vector3[] v3bones;
    protected Vector3[] v3bonesInit;
    protected float[] boneLengths;

    protected Transform t_target;

    protected Vector3[] rotateAxes;
    protected float[] rotateAngles;
    protected Quaternion[] rotations;
    //protected Quaternion[] q_initRotations;

    protected Vector3 v3_initUp;
    protected Vector3 v3_initForward;
    protected Vector3 v3_targetPosition;

    protected Quaternion[] solutions;

    public virtual bool Init(IKControl control, Transform p)
    {
        CONTROL = control;
        IKCS = CONTROL.IKCS;
        BoneData[] targetBones = CONTROL.bdArr_boneData;
        if (IKControl.IsBoneDataInvalid(targetBones))
            return false;
        t_target = p;
        bdArr_data = targetBones;
        LENGTH = bdArr_data.Length;


        // Get the axis of rotation for each joint
        rotateAxes = new Vector3[bdArr_data.Length - 2];
        rotateAngles = new float[bdArr_data.Length - 2];
        rotations = new Quaternion[bdArr_data.Length - 2];

        //boneLengths = new float[bones.Length - 1];

        v3bones = new Vector3[bdArr_data.Length - 1];
        v3bonesInit = new Vector3[bdArr_data.Length - 1];     
        boneLengths = new float[bdArr_data.Length - 1];

        // Set up the bones axes
        for (int i = 0; i < bdArr_data.Length - 1; i++)
        {
            v3bonesInit[i] = bdArr_data[i + 1].position - bdArr_data[i].position;
            v3bones[i] = v3bonesInit[i];


            // Get the length of each bone
           boneLengths[i] = v3bonesInit[i].magnitude;
        }

        v3_initUp = bdArr_data[0].parent.InverseTransformVector(bdArr_data[0].up);
        v3_initForward = bdArr_data[0].parent.InverseTransformVector(bdArr_data[0].forward);

        return true;
    }

    public virtual Quaternion[] Solve(Transform upTarget, bool debug)
    {
        return null;
    }

    public virtual void RotateBones()
    {
        if (solutions != null)
        {
            for (int i = 0; i < bdArr_data.Length - 1; i++)
            {
                bdArr_data[i].localRotation = solutions[i];
            }
        }
        if (CONTROL.C_secondPass != null && CONTROL.IKCS.spType != IKControl.SecondPassType.None && CONTROL.b_doSecondPass)
            CONTROL.C_secondPass.DoRotate();
    }

    protected Vector3 findEndVec()
    {
        Vector3 endVec = Vector3.zero;
        for (int i = 0; i < v3bones.Length; i++)
        {
            endVec += v3bones[i];
        }

        return endVec;
    }

    public void RotateEnd()
    {
        //Vector3 v3_forward = t_target.forward;
        //Vector3 v3_up = t_target.up;
        //Quaternion looky = Quaternion.LookRotation(v3_forward, v3_up);
        //bones[bones.Length - 1].localRotation = rotate * q_initRotations[bones.Length - 1];
        bdArr_data[bdArr_data.Length - 1].rotation = CONTROL.q_targetIKrotation;
    }

    public void RotateWingEnd(float scale, BoneData[] bones)
    {
        foreach (BoneData b in bones)
        {
            b.localRotation = Quaternion.Slerp(Quaternion.identity, b.q_startingLocalRot, scale);
        }
    }
}
