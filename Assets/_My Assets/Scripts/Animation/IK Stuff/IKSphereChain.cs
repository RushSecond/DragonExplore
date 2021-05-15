/*
Copyright (c) 2008, Rune Skovbo Johansen & Unity Technologies ApS

See the document "TERMS OF USE" included in the project folder for licencing details.
*/
using UnityEngine;
using System.Collections;

public class IKSphereChain : IKBase
{    
    float targetDistance;

    Vector3 initUpVector;
    Vector3 sphereTan;

    public override bool Init(IKControl control, Transform p)
    {
        base.Init(control, p);
        
        boneLengths = new float[bdArr_data.Length - 1];

        // Get the length of each bone
        for (int i=0; i<bdArr_data.Length-1; i++) {
        	boneLengths[i] = (bdArr_data[i+1].position-bdArr_data[i].position).magnitude;
        }
        //		positionAccuracy = legLength*0.001f;

        return true;

    }

    public override Quaternion[] Solve(Transform t_upTarget, bool debug)
    {
        if (targetDistance < IKCS.minRange) //Normalize it to minrange
        {
            targetDistance = IKCS.minRange;
        }

        v3_targetPosition = t_target.position;
        Vector3 startBonePosition = bdArr_data[0].position;
        //Vector3 moveVec = t_upTarget.position - bdArr_data[0].position;

        Transform endEffector = bdArr_data[bdArr_data.Length - 1].t_bone;

        // Set up sphere stuff
        sphereTan = t_target.forward;
        if (IKCS.b_flipUpVector)
            sphereTan *= -1;

        Vector3 sphereTanPerp = Vector3.Cross(sphereTan, v3_targetPosition - startBonePosition);
        if (sphereTanPerp.sqrMagnitude < 0.0001f) return null;

        Vector3 sphereRadiusDirection = Vector3.Normalize(Vector3.Cross(sphereTanPerp, sphereTan));

        targetDistance = (v3_targetPosition - startBonePosition).sqrMagnitude;

        // I did the math
        float sphereRadius = targetDistance / (2 * Vector3.Dot(v3_targetPosition - startBonePosition, sphereRadiusDirection));
        sphereRadius = Mathf.Clamp(sphereRadius, -10000f, 10000f);
        Vector3 sphereCenter = startBonePosition + sphereRadiusDirection * sphereRadius;

        //Vector3 sphereRadiusVec = sphereRadiusDirection * sphereRadius;

        if (debug)
        Debug.DrawLine(startBonePosition, sphereCenter, Color.red);

        
        Vector3 sphereRadiusVec;
        Vector3 axis;
        // Get the length of each bone
        for (int i = 0; i < bdArr_data.Length - 1; i++)
        {
            sphereRadiusVec = sphereCenter - bdArr_data[i].position;
            axis = Vector3.Cross(sphereRadiusVec, bdArr_data[i].right);

            float angleToRotate = Mathf.Rad2Deg * Mathf.Asin(boneLengths[i] / ( sphereRadius));

            if (boneLengths[i] > sphereRadius || angleToRotate > 60)
                angleToRotate = 60;

            bdArr_data[i].rotation = Quaternion.AngleAxis(angleToRotate, axis) * bdArr_data[i].rotation;
        }

        return null;
    }
}

// Rotate everything around the edge of a sphere


/* So the plan here is to bend everything so the total rotation from all the bones totals to 
DOUBLE the angle between the (vector of bone 1) and the (vector to target)

Then we force every bones to swivel so their upvector faces upward.
We do that by rotating so their upvector goes into the plane created by its own vector + the upvector of the upnode.
*/