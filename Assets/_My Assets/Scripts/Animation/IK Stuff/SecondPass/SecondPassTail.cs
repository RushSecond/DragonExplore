using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondPassTail : SecondPassBase {

    public SecondPassTail(IKControl parent) : base(parent)
    {
        
    }

    public override void DoRotate()
    {
        Transform boneTrans;
        Quaternion lookRotate, upRotate;
        Vector3 from, to;
        Vector3 axis;
        float f_angleToRotate;
        for (int i = 0; i < numBones - 1; i++)
        {
            bd_data[i].f_rotateReturnLerp = Mathf.Lerp(G.f_ChainBaseReturn, G.f_ChainEndReturn, (float)i / (float)(numBones - 2));

            // algorithm to set this correctly based on time
            bd_data[i].f_rotateReturnLerp = MathHelpers.RotateTimeAdjust(bd_data[i].f_rotateReturnLerp);

            boneTrans = bd_data[i].t_bone;

            from = -boneTrans.right;
            to = bd_data[i].v3_prevPointTo - boneTrans.position;
            axis = Vector3.Cross(from, to);

            f_angleToRotate = Vector3.SignedAngle(from, to, axis);
      


            lookRotate = Quaternion.AngleAxis(f_angleToRotate, axis) * boneTrans.rotation;
            Quaternion localRot = Quaternion.Slerp(lookRotate, boneTrans.rotation,
            bd_data[i].f_rotateReturnLerp);

            boneTrans.rotation = localRot;

            //Debug.Log(localRot);
            //data[i].t_bone.rotation = lookRotate * data[i].t_bone.rotation;

            //Keep the vector up
            from = bd_data[i].v3_boneUpVector;
            to = Vector3.ProjectOnPlane(PAM.transform.up, boneTrans.right);
            axis = boneTrans.right;

            f_angleToRotate = Vector3.SignedAngle(from, to, axis);
            upRotate = Quaternion.AngleAxis(Vector3.SignedAngle(from, to, axis), axis);

            boneTrans.rotation = upRotate * boneTrans.rotation;

            // Now after rotating, move the prev position
            bd_data[i].v3_prevPointTo = bd_data[i + 1].t_bone.position;
        }
        
    }
}
