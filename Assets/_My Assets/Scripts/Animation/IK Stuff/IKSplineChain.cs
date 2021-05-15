using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKSplineChain : IKBase
{
    public override bool Init(IKControl control, Transform p)
    {
        base.Init(control, p);

        v3bones = new Vector3[bdArr_data.Length - 1];

        return true;
    }

    public override Quaternion[] Solve(Transform upTarget, bool debug)
    {
        v3_targetPosition = CONTROL.v3_targetIKposition;
        //v3_targetPosition = t_target.position;

        // Turn bones into vectors for calculations
        for (int i = 0; i < bdArr_data.Length - 1; i++)
        {
            v3bones[i] = bdArr_data[i + 1].position - bdArr_data[i].position;
        }

        // Gets how much the target rotated
        Quaternion endRotation = t_target.rotation;
        Quaternion inverseStartRotation = Quaternion.Inverse(IKCS.t_startTarget.rotation);

        //Quaternion endRotation = t_target.rotation * CONTROL.q_localIKrotation;
        //Quaternion inverseStartRotation = Quaternion.Inverse(IKCS.t_startTarget.rotation * CONTROL.q_localStartRotation);

        Vector3 endBonePosition = bdArr_data[bdArr_data.Length - 1].position;

        // used to rotate individual bones
        Vector3 fromHereToTarget; // Vector from a certain bone to the end position
        Vector3 angledPosition; // gets the position of this bone if it were rotated the same way as the target gizmo
        float chainWeight; // How much weighting should we give to the angledPosition instead of starting position
        Vector3[] lookAtPosition = new Vector3[bdArr_data.Length]; // Use weight to give a look position for the bone
        Quaternion boneRotation; // Rotate toward the lookatPosition
        

        for (int i = 1; i < bdArr_data.Length; i++)
        {
            fromHereToTarget = endBonePosition - bdArr_data[i].position;

            // gets the position of this bone if it were rotated the same way as the target gizmo
            angledPosition = v3_targetPosition - (endRotation * inverseStartRotation * fromHereToTarget);
            chainWeight = MathHelpers.SCurve((float)(i) / (float)(bdArr_data.Length));

            lookAtPosition[i-1] = Vector3.Lerp(bdArr_data[i].position, angledPosition, chainWeight);

            

            // Rotate at the end so you don't mess up other calculations
            //bdArr_data[i - 1].rotation = boneRotation * bdArr_data[i - 1].rotation;
        }

        lookAtPosition[LENGTH - 1] = (endRotation * inverseStartRotation) * bdArr_data[LENGTH - 1].right;

        for (int i = 0; i < bdArr_data.Length - 1; i++)
        {
            boneRotation = Quaternion.FromToRotation(bdArr_data[i + 1].position - bdArr_data[i].position, lookAtPosition[i] - bdArr_data[i].position);

            if (debug)
                Debug.DrawLine(lookAtPosition[i], lookAtPosition[i] + t_target.right);

            bdArr_data[i].rotation = boneRotation * bdArr_data[i].rotation;
        }

        boneRotation = Quaternion.FromToRotation(bdArr_data[LENGTH - 1].right, lookAtPosition[LENGTH - 1]);

        bdArr_data[LENGTH-1].rotation = boneRotation * bdArr_data[LENGTH-1].rotation;

        //bdArr_data[bdArr_data.Length - 1].rotation = CONTROL.q_localIKrotation * (bdArr_data[bdArr_data.Length - 2].rotation * bdArr_data[bdArr_data.Length - 1].q_startingLocalRot);

        return null;
    }
}
