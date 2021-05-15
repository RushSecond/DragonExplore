using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondPassBase
{
    protected IKControl PAM;
    protected IKControl.IKControlSettings G;

    protected BoneData[] bd_data;
    protected int numBones;

    public SecondPassBase(IKControl parent)
    {
        PAM = parent;
        G = parent.IKCS;

        numBones = G.i_IKparents + 1;
        bd_data = new BoneData[numBones];

        Setup();
    }

    protected virtual void Setup()
    {
        Transform bone = G.t_targetEndBone;

        for (int i = numBones - 1; i >= 0; i--)
        {
            bd_data[i] = new BoneData(bone, PAM.transform.up);
            if (i < numBones - 1)
            {
                bd_data[i].v3_prevPointTo = bd_data[i + 1].t_bone.position;
            }

            bone = bone.parent;
        }
    }
    public virtual void DoRotate() { }
}
