using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondPassSpine : SecondPassBase {

    Vector3 v3_prevEndPos, v3_prevBasePos;
    Quaternion q_baseRotate;

    public SecondPassSpine(IKControl parent) : base(parent)
    {

    }

    protected override void Setup()
    {
        base.Setup();

        v3_prevEndPos = bd_data[numBones -1].position;
        v3_prevBasePos = PAM.bd_rootData.position;
        q_baseRotate = PAM.bd_rootData.rotation;

        // for this our previous point is our bone position since we are working backwards
        for (int i = 0; i < numBones; i++)
            bd_data[i].q_prevWoldRotation = bd_data[i].rotation;
    }

    public override void DoRotate()
    {
        Transform boneTrans;
        //Quaternion lookRotate, upRotate;
        //Vector3 from, to;
        //Vector3 axis;
        //float f_angleToRotate;

        //bd_data[0].parent.localPosition = bd_rootData.v3_startingLocalPosition;

        
        Vector3 v3_spineEndPos = bd_data[numBones - 1].position;
        Vector3 v3_moveVec = Vector3.zero;

        Quaternion slerpRotate;
        Quaternion savedRotation;
        //Quaternion localRot;

        for (int i  = 0; i < numBones -1; i++)
        {
            bd_data[i].f_rotateReturnLerp = MathHelpers.Gerp(G.f_ChainBaseReturn, G.f_ChainEndReturn, (float)i / (float)(numBones - 2));
            bd_data[i].f_rotateReturnLerp = MathHelpers.RotateTimeAdjust(bd_data[i].f_rotateReturnLerp);

            boneTrans = bd_data[i].t_bone;
            savedRotation = bd_data[i + 1].rotation;

            // NEW PLAN: bone ROTATION LERP
            // well the first bone can position too, so spine is updated
            if (i == 0)
            {
                /*Vector3 lerpBase = Vector3.Lerp(v3_prevBasePos, PAM.bd_rootData.position, bd_data[i].f_rotateReturnLerp);
                PAM.bd_rootData.position = lerpBase;
                v3_prevBasePos = PAM.bd_rootData.position;*/

                slerpRotate = Quaternion.Slerp(q_baseRotate, PAM.bd_rootData.rotation, bd_data[i].f_rotateReturnLerp);
                PAM.bd_rootData.rotation = slerpRotate;
                q_baseRotate = PAM.bd_rootData.rotation;

            }
            else 
            {
                slerpRotate = Quaternion.Slerp(bd_data[i].q_prevWoldRotation, bd_data[i].rotation, bd_data[i].f_rotateReturnLerp);
                bd_data[i].rotation = slerpRotate;
                bd_data[i].q_prevWoldRotation = bd_data[i].rotation;


                /*from = boneTrans.right;
                to = bd_data[i].v3_prevPointTo - bd_data[i + 1].position;
                axis = Vector3.Cross(from, to);

                f_angleToRotate = Vector3.SignedAngle(from, to, axis);


                lookRotate = Quaternion.AngleAxis(f_angleToRotate, axis) * boneTrans.rotation;
                localRot = Quaternion.Slerp(lookRotate, boneTrans.rotation,
                bd_data[i].f_rotateReturnLerp);

                boneTrans.rotation = localRot;

                //Keep the vector up
                from = bd_data[i].v3_boneUpVector;
                to = Vector3.ProjectOnPlane(PAM.transform.up, boneTrans.right);
                axis = boneTrans.right;

                f_angleToRotate = Vector3.SignedAngle(from, to, axis);
                if (f_angleToRotate > 0.01f)
                {
                    upRotate = Quaternion.AngleAxis(Vector3.SignedAngle(from, to, axis), axis);

                    boneTrans.rotation = upRotate * boneTrans.rotation;
                }*/
            }

            if (i < numBones - 2)
            {
                bd_data[i + 1].rotation = savedRotation; 
            }

            
        }

        /*for (int i = 0; i < numBones; i++)
        {
            bd_data[i].v3_prevPointTo = bd_data[i].t_bone.position;
        }*/

        

        // Now reset spinebone rotation
        PAM.bd_rootData.localRotation = PAM.bd_rootData.q_startingLocalRot;

        // go halfsies on distance from this bone's end to spine end
        v3_moveVec = v3_spineEndPos - bd_data[numBones - 1].position;
        // And use it to set the pelvis so the end of the spine always ends in the correct local position
        Vector3 newPosition = PAM.bd_rootData.position + .5f * v3_moveVec;
        //if (Vector3.SqrMagnitude(bd_data[0].parent.position - newPosition) > 0.0001f)
        PAM.bd_rootData.position = Vector3.Lerp(PAM.bd_rootData.position, newPosition, 1f);
    }
}
