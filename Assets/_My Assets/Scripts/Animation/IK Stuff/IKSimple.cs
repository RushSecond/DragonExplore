using UnityEngine;
using System.Collections;

public class IKSimple : IKBase
{

    
    protected const int MAXITERATIONS = 10;

    protected Transform endEffector;
    //float[] boneLengths;
    //float legLength = 0;
    protected float currentDistance;
    protected float targetDistance;
    // Search for right joint bendings to get target distance between hip and foot
    protected float bendingLow, bendingHigh;
    protected bool minIsFound = false;
    protected bool bendMore = false;
    protected int tries = 0;
    protected float bendingNew;
    protected float newAngle;

    protected Vector3 endVec;

    protected Vector3 targetPlaneNormal;
    protected Vector3 upPlaneNormal;
    protected Vector3 rootUp_TargetPlaneProjection;
    protected Vector3 rootUp_UpPlaneProjection;

    /*public override void Init(Transform[] targetBones, Transform p)
    {
        base.Init(targetBones, p);
    }*/

    public override Quaternion[] Solve(Transform t_upTarget, bool debug)
    {
        if (bdArr_data == null)
        {
            Debug.LogWarning(this + " solver failed due to error!");
            return null;
        }

        v3_targetPosition = CONTROL.v3_targetIKposition;
        // Return quats so individual bones can rotate toward them in a controlled manner
        Quaternion[] solutions = new Quaternion[bdArr_data.Length - 1];
        endEffector = bdArr_data[bdArr_data.Length - 1].t_bone;

        //public Transform targetEffector = null; //the actual target end effector that we move around

        // Turn bones into vectors for calculations
        for (int i = 0; i < bdArr_data.Length - 1; i++)
        {
            v3bonesInit[i] = bdArr_data[i + 1].position - bdArr_data[i].position;
            v3bones[i] = v3bonesInit[i];
        }

        for (int i = 0; i < bdArr_data.Length - 2; i++)
        {
            //rotateAxes[i] = Quaternion.Inverse(bones[i].rotation) * Vector3.Cross(v3trash1, v3trash2);
            //rotateAxes[i] = rotateAxes[i].normalized;	//NIKA commented out

            rotateAngles[i] = Vector3.Angle(v3bones[i], -v3bones[i+1]);

            rotations[i] = bdArr_data[i + 1].rotation;
            rotateAxes[i] = Vector3.Cross(v3bonesInit[i], v3bonesInit[i + 1]);
        }

        // Check if endEffector already close
        if (Vector3.Distance(endEffector.position, v3_targetPosition) < positionAccuracy)
            return null;

        currentDistance = (endEffector.position - bdArr_data[0].position).sqrMagnitude;
        targetDistance = (v3_targetPosition - bdArr_data[0].position).sqrMagnitude;

        if (targetDistance < IKCS.minRange) //Normalize it to minrange
        {
            targetDistance = IKCS.minRange;
        }

        minIsFound = false;
        bendMore = false;
        bendingHigh = 1f;
        bendingLow = 0f; // Cause its a like a binary search OOOOOH
        Quaternion fullRot;

        if (targetDistance > currentDistance)
            minIsFound = true;
        else
            bendMore = true;

        tries = 0;
        while (Mathf.Abs(currentDistance - targetDistance) > positionAccuracy && tries <= MAXITERATIONS)
        {
            endVec = bdArr_data[0].position;
            tries++;
            //bendingNew = bendingHigh;

            //if (minIsFound)
                bendingNew = (bendingLow + bendingHigh) * 0.5f;
            fullRot = Quaternion.identity;

            for (int i = 0; i < bdArr_data.Length - 2; i++)
            //for (int i = bdArr_data.Length - 3; i >= 0; i--)
            {
                if (!bendMore)
                    newAngle = Mathf.Lerp(180f - positionAccuracy, rotateAngles[i], bendingNew);
                else
                    //newAngle = Mathf.Max(rotateAngles[i] - (180f / MAXITERATIONS) * bendingNew, 0f);
                    newAngle = Mathf.Lerp(rotateAngles[i], 0f, bendingNew);

                fullRot = Quaternion.AngleAxis((rotateAngles[i] - newAngle), Vector3.Cross(v3bones[i], v3bones[i + 1])) * fullRot;

                //bones[i + 1].localRotation = Quaternion.AngleAxis((rotateAngles[i] - newAngle), rotateAxes[i]) * rotations[i];
                //solutions[i + 1] = Quaternion.AngleAxis((rotateAngles[i] - newAngle), rotateAxes[i]) * rotations[i];
                v3bones[i + 1] = fullRot * v3bonesInit[i + 1];

                
            }

            for (int i = 0; i < bdArr_data.Length - 2; i++)
            {
                endVec += v3bones[i];
                if (debug)
                    Debug.DrawLine(endVec + Vector3.right, endVec + Vector3.right + v3bones[i + 1], Color.white);
            }

            endVec = findEndVec();

            

            //currentDistance = (endEffector.position - bones[0].position).sqrMagnitude;
            currentDistance = endVec.sqrMagnitude;
            if (targetDistance > currentDistance)
            { minIsFound = true; }
            else if (newAngle < positionAccuracy) // impossible to reach target, so stop
                break;

            if (targetDistance > currentDistance) bendingHigh = bendingNew;
            else bendingLow = bendingNew;

            if (bendingHigh < 0.001f) break;
        }

        // testing to see if bones can moved just the one time
        //for (int i = 0; i < bdArr_data.Length - 2; i++)
        for (int i = bdArr_data.Length - 3; i >= 0; i--)
        {
            bdArr_data[i + 1].rotation = Quaternion.AngleAxis((rotateAngles[i] - newAngle), rotateAxes[i]) * rotations[i] ;
        }

        bdArr_data[0].rotation = (
            Quaternion.AngleAxis(
                Vector3.Angle(endEffector.position - bdArr_data[0].position, v3_targetPosition - bdArr_data[0].position),
                Vector3.Cross(endEffector.position - bdArr_data[0].position, v3_targetPosition - bdArr_data[0].position)
            ) * bdArr_data[0].rotation);

        Vector3 swappyUp = bdArr_data[0].up;
        Vector3 swappyFwd = bdArr_data[0].forward;
        Vector3 swappyInitUp = v3_initUp;
        Vector3 swappyInitFwd = v3_initForward;
        Vector3 swappyParentUp = t_target.parent.up;

        if (IKCS.b_flipFwdVector)
        {
            swappyFwd *= -1f;
            swappyInitFwd *= -1f;
        }

        if (IKCS.b_flipUpVector)
        {
            swappyUp *= -1f;
            swappyParentUp *= -1f;
            swappyInitUp *= -1f;
        }



        // vector from start to end
        targetPlaneNormal = v3_targetPosition - bdArr_data[0].position;

        if (t_upTarget && IKCS.jType == IKControl.JointType.PointUpToTarget)
        {
            Vector3 upTarget = t_upTarget.position;
            // A plane containing the start, the end, and the up point
            upPlaneNormal = Vector3.Cross(upTarget - bdArr_data[0].position, v3_targetPosition - bdArr_data[0].position);

            // The up vector made parallel to the end plane
            rootUp_TargetPlaneProjection = Vector3.ProjectOnPlane(swappyUp, targetPlaneNormal);

            // If this vector is facing away from the forwardvector, the bone is gonna get flipped
            if (Vector3.Dot(rootUp_TargetPlaneProjection, -bdArr_data[0].right) < 0)
                rootUp_TargetPlaneProjection = -rootUp_TargetPlaneProjection;

            //... and then cast to the plane vector. now its perpendicular to everything
            //rootUp_UpPlaneProjection = rootUp_TargetPlaneProjection - Vector3.Project(rootUp_TargetPlaneProjection, upPlaneNormal);
            rootUp_UpPlaneProjection = Vector3.ProjectOnPlane(upTarget - bdArr_data[0].position, targetPlaneNormal);
        }
        else if (IKCS.jType == IKControl.JointType.PreserveForward)
        {
            // A plane containing the start, the end, and the up point
            //upPlaneNormal = Vector3.Cross(upTarget - bones[0].position, target - bones[0].position);

            // my vector is perpendicular to the up vector
            rootUp_TargetPlaneProjection = Vector3.ProjectOnPlane(swappyFwd, targetPlaneNormal);

            // target is crossed with forward
            rootUp_UpPlaneProjection = Vector3.Cross(bdArr_data[0].parent.TransformVector(swappyInitUp), targetPlaneNormal);
        }
        else if (IKCS.jType == IKControl.JointType.PreserveUp)
        {
            // my vector is perpendicular to the up vector
            rootUp_TargetPlaneProjection = Vector3.ProjectOnPlane(swappyUp, targetPlaneNormal);

            // If this vector is facing away from the forwardvector, the bone is gonna get flipped
            //if (Vector3.Dot(rootUp_TargetPlaneProjection, -bones[0].right) < 0)
            //rootUp_TargetPlaneProjection = -rootUp_TargetPlaneProjection;

            // target is crossed with initial forward
            rootUp_UpPlaneProjection = Vector3.Cross(targetPlaneNormal, bdArr_data[0].parent.TransformVector(swappyInitFwd));
        }
        else if (IKCS.jType == IKControl.JointType.Hybrid)
        {
            float twistAngle = 90f - Vector3.Angle(targetPlaneNormal, t_target.parent.right);

            // A plane containing the start, the end, and the up point
            upPlaneNormal = Vector3.Cross(Vector3.Slerp(t_target.parent.forward, Mathf.Sign(twistAngle) * swappyParentUp, Mathf.Abs(twistAngle / 90f)),
                v3_targetPosition - bdArr_data[0].position);

            // The up vector made parallel to the end plane
            rootUp_TargetPlaneProjection = Vector3.ProjectOnPlane(swappyUp, targetPlaneNormal);

            //... and then cast to the plane vector. now its perpendicular to everything
            rootUp_UpPlaneProjection = rootUp_TargetPlaneProjection - Vector3.Project(rootUp_TargetPlaneProjection, upPlaneNormal);

            // If this vector is facing away from the upvector, the bone is gonna get flipped
            if (Vector3.Dot(rootUp_UpPlaneProjection, t_target.parent.forward) < 0)
                rootUp_UpPlaneProjection = -rootUp_UpPlaneProjection;
        }
        else if (IKCS.jType == IKControl.JointType.Hybrid2)
        {
            float twistAngle = 90f - Vector3.Angle(targetPlaneNormal, t_target.parent.right);

            // A plane containing the start, the end, and the up point
            upPlaneNormal = Vector3.Cross(Vector3.Slerp(swappyParentUp, Mathf.Sign(twistAngle) * t_target.parent.forward, Mathf.Abs(twistAngle / 90f)),
                v3_targetPosition - bdArr_data[0].position);

            // The up vector made parallel to the end plane
            rootUp_TargetPlaneProjection = Vector3.ProjectOnPlane(swappyFwd, targetPlaneNormal);

            //... and then cast to the plane vector. now its perpendicular to everything
            rootUp_UpPlaneProjection = rootUp_TargetPlaneProjection - Vector3.Project(rootUp_TargetPlaneProjection, upPlaneNormal);

            // If this vector is facing away from the upvector, the bone is gonna get flipped
            if (Vector3.Dot(rootUp_UpPlaneProjection, swappyParentUp) < 0)
                rootUp_UpPlaneProjection = -rootUp_UpPlaneProjection;
        }


        //Debug.DrawLine(bones[0].position + Vector3.right, bones[0].position + bones[0].up + Vector3.right, Color.red);
        if (debug)
        {
            Debug.DrawLine(bdArr_data[0].position, bdArr_data[0].position - bdArr_data[0].up, Color.red);
            Debug.DrawLine(bdArr_data[0].position, bdArr_data[0].position + rootUp_TargetPlaneProjection, Color.green);
            Debug.DrawLine(bdArr_data[0].position + Vector3.right, bdArr_data[0].position + Vector3.right + rootUp_UpPlaneProjection, Color.blue);
        }

        //Debug.Log(rootUp_UpPlaneProjection);
        //Debug.Log(rootUp_TargetPlaneProjection + " " + upPlaneNormal);
        //Debug.Log(Vector3.Angle(rootUp_TargetPlaneProjection, rootUp_UpPlaneProjection));

        /*bones[0].rotation = (
            Quaternion.AngleAxis(
                Vector3.Angle(endEffector.position - bones[0].position, target - bones[0].position),
                Vector3.Cross(endEffector.position - bones[0].position, target - bones[0].position)
            ) * Quaternion.AngleAxis(
                Vector3.Angle(rootUp_TargetPlaneProjection, rootUp_UpPlaneProjection),
                Vector3.Cross(rootUp_TargetPlaneProjection, rootUp_UpPlaneProjection)
            )
            * bones[0].rotation
        );*/

        solutions[0] = (
            Quaternion.AngleAxis(
                Vector3.Angle(rootUp_TargetPlaneProjection, rootUp_UpPlaneProjection),
                Vector3.Cross(rootUp_TargetPlaneProjection, rootUp_UpPlaneProjection)
            )
            * bdArr_data[0].rotation
        );

        bdArr_data[0].rotation = solutions[0];

        this.solutions = solutions;

        return solutions;
    }

    public override void RotateBones()
    {
        //if (solutions != null)
            //bdArr_data[0].rotation = solutions[0];

        if (CONTROL.bdArr_otherBoneData != null)
            RotateWingEnd(CONTROL.f_localIKscale, CONTROL.bdArr_otherBoneData);
    }
}
