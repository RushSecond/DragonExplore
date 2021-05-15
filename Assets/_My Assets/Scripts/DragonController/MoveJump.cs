using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveJump : MoveStateBase {

    bool b_airborne = true;

    public MoveJump(MoveController parent) : base(parent) { }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void CheckGroundCollision()
    {
        G.v3_currentVelocity = G.rigid.velocity;
        Vector3 currentPosition = MC.transform.position;
        int length = G.v3list_localTerrainDetectDown.Length;
        int invalid = -1;
        int validPointsFound = length;

        //will store info of successful ray cast
        RaycastHit hitInfo;

        //terrain should have mesh collider and be on custom terrain 
        //layer so we don't hit other objects with our raycast
        LayerMask layer = 1 << LayerMask.NameToLayer("Terrain");

        Vector3 worldterrainDetect;
        Vector3 worldterrainDir;
        // Raycast loop for terrain below
        for (int i = 0; i < length; i++)
        {
            worldterrainDetect = MC.transform.TransformPoint(G.v3list_localTerrainDetectDown[i]);
            worldterrainDir = G.rigid.velocity * .25f + -MC.transform.up * G.f_vertHeightCheck;
            if (Physics.SphereCast(worldterrainDetect, 0.2f, worldterrainDir, out hitInfo, worldterrainDir.magnitude, layer))
            {
                G.v3list_terrainPoint[i] = hitInfo.point;
                G.fList_terrainDistance[i] = Vector3.Magnitude(worldterrainDetect - G.v3list_terrainPoint[i]);
                Debug.DrawLine(worldterrainDetect, G.v3list_terrainPoint[i], Color.blue);
            }
            else
            {
                invalid = i;
                validPointsFound--;
                Debug.DrawLine(worldterrainDetect, worldterrainDetect + worldterrainDir, Color.red);
                G.v3list_terrainPoint[i] = worldterrainDetect + worldterrainDir;
                G.fList_terrainDistance[i] = G.f_vertHeightCheck;
            }
        }

        G.b_cliffCollide = false;

        // Raycast to check for cliffs ahead
        if (Physics.SphereCast(currentPosition, 0.3f, MC.transform.forward, out hitInfo, G.f_capsuleLength * 1.1f, layer))
        {
            Debug.DrawLine(currentPosition, hitInfo.point, Color.blue);
            // hit terrain
            if (Vector3.Angle(hitInfo.normal, Vector3.up) > G.f_cliffAngleThreshold)
            {
                G.v3_wallNormal = hitInfo.normal;
                G.b_cliffCollide = true; // collided with an actual cliff
            }
        }
        else
        {
            Debug.DrawLine(currentPosition, currentPosition + MC.transform.forward * G.f_capsuleLength * 1.1f, Color.red);
        }


        Vector3 v1, v2;
        if (validPointsFound == 3) //We need the 3 points that don't suck
        {
            int index1 = (invalid + 1) % length; int index2 = (invalid + 2) % length; int index3 = (invalid + 3) % length;
            v1 = G.v3list_terrainPoint[index1] - G.v3list_terrainPoint[index2];
            v2 = G.v3list_terrainPoint[index1] - G.v3list_terrainPoint[index3];
        }
        else
        {
            /*G.v3_terrainNormAvg = Vector3.up;
            return;*/
            G.v3list_terrainPoint[2] = Vector3.Lerp(G.v3list_terrainPoint[2], G.v3list_terrainPoint[3], 0.5f);
            v1 = G.v3list_terrainPoint[0] - G.v3list_terrainPoint[1];
            v2 = G.v3list_terrainPoint[0] - G.v3list_terrainPoint[2];
        }

        G.v3_terrainNormAvg = Vector3.Normalize(Vector3.Cross(v2, v1));
        //And make sure its facing upward
        G.v3_terrainNormAvg *= Mathf.Sign(Vector3.Dot(G.v3_terrainNormAvg, Vector3.up));


        Debug.DrawLine(currentPosition, currentPosition + G.v3_terrainNormAvg * 3f);
    }

    public override void CheckStateChange()
    {
        if (G.b_jumpButtonDown)
        {
            DoStateChange(MC.C_moveFly);
            return;
        }

        if (G.b_cliffCollide && Vector3.Dot(G.rigid.velocity, G.v3_wallNormal) < -2f) // Moving toward the wall
        {
            DoStateChange(MC.C_moveCliff);
            return;
        }

        if (!b_airborne)
        {
            if (Vector3.Angle(G.v3_terrainNormAvg, Vector3.up) > G.f_groundAngleThreshold)
                DoStateChange(MC.C_moveCliff);
            else
                DoStateChange(MC.C_moveGround);
        }
    }

    protected override void InputMovement()
    {
        Vector3 v3_movementPlane = Vector3.up;

        G.v3_currentVelocity = Vector3.ProjectOnPlane(G.rigid.velocity, v3_movementPlane);

        //Vector3 veloNorm = Vector3.Normalize(G.v3_currentVelocity);
        Vector3 rightVec = Vector3.Normalize(Vector3.Cross(v3_movementPlane, Vector3.ProjectOnPlane(G.t_cameraTrans.forward, Vector3.up)));
        Vector3 fwdVec = Vector3.Normalize(Vector3.Cross(G.t_cameraTrans.right, v3_movementPlane));

        G.v3_accelerate = Vector3.zero;

        G.v3_accelerate = Vector3.zero;
        G.v3_accelerate += fwdVec * G.f_airAccel * G.v2_inputAxes.y;
        G.v3_accelerate += rightVec * G.f_airAccel * G.v2_inputAxes.x;

        // Acceleration found
        G.rigid.AddForce(G.v3_accelerate);

        G.v3_currentVelocity += Vector3.Project(G.rigid.velocity, v3_movementPlane); ;
        G.rigid.velocity = G.v3_currentVelocity;
    }

    protected override void HorizontalRotation()
    {
        // Horizontal stuff
        //Vector3 axis = MC.transform.up;
        Vector3 axis = Vector3.up;
        Vector3 myHorizontal = MC.transform.forward;

        G.v3list_rotateTo[0] = G.v3_currentVelocity;


        ApplyTorqueTowards(myHorizontal, G.v3list_rotateTo[0], axis, G.f_horizontalTorque);
    }

    protected override void VerticalRotation()
    {
        //Vector3 myRight = Vector3.Cross(Vector3.up, G.rigid.velocity);

        //G.v3list_rotateTo[1] = Vector3.Cross(G.rigid.velocity, myRight);
        G.v3list_rotateTo[1] = Vector3.Normalize(Vector3.ProjectOnPlane(Vector3.up, G.rigid.velocity)) + Vector3.up;

        Vector3 myVertical = MC.transform.up;
        Vector3 axis = Vector3.Normalize(Vector3.Cross(myVertical, G.v3list_rotateTo[1]));

        ApplyTorqueTowards(myVertical, G.v3list_rotateTo[1], axis, G.f_verticalTorque);
    }

    protected override void GroundCollisionForces()
    {
        G.rigid.AddForce(G.v3_grav, ForceMode.Acceleration);

        float avgDistance = MathHelpers.FindAverage(G.fList_terrainDistance);
        avgDistance -= G.f_desiredHeight;

        if (avgDistance < G.f_desiredHeight * G.f_airborneThreshold && Vector3.Dot(G.v3_terrainNormAvg, G.v3_currentVelocity) < 0)
        {
            b_airborne = false;
        }
    }

    protected override void SetupState()
    {
        b_airborne = true;
    }
}
