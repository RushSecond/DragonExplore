using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCliff : MoveStateBase{

    Vector3 v3_movementPlane;
    Vector3 v3_cliffFwd, v3_cliffRight;
    bool b_groundCollide, b_airborne;
    bool b_cliffJump;

    public MoveCliff(MoveController parent):base(parent)
    {
    }

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
        for (int i = 0; i < G.v3list_localTerrainDetectDown.Length; i++)
        {
            worldterrainDetect = MC.transform.TransformPoint(G.v3list_localTerrainDetectDown[i]);
            worldterrainDir = Vector3.ProjectOnPlane(MC.transform.rotation * G.v3list_localTerrainDetectDir[i], Vector3.up);
            if (Physics.SphereCast(worldterrainDetect, .5f, worldterrainDir, out hitInfo, G.v3list_localTerrainDetectDir[i].magnitude * 3f, layer))
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
                G.v3list_terrainPoint[i] = worldterrainDetect + worldterrainDir * G.f_vertHeightCheck;
                G.fList_terrainDistance[i] = G.f_vertHeightCheck;
            }
        }

        b_groundCollide = false;

        // Raycast to check for ground behind
        if (Physics.SphereCast(currentPosition, 0.3f, -MC.transform.forward, out hitInfo, G.f_capsuleLength * 1.1f, layer))
        {
            Debug.DrawLine(currentPosition, hitInfo.point, Color.blue);
            // hit terrain
            if (Vector3.Angle(hitInfo.normal, Vector3.up) < G.f_groundAngleThreshold)
            {
                G.v3_groundNormal = hitInfo.normal;
                b_groundCollide = true; // collided with actual ground
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
        G.v3_wallNormal = G.v3_terrainNormAvg;
        // And make sure its facing upward
        //G.v3_terrainNormAvg *= Mathf.Sign(Vector3.Dot(G.v3_terrainNormAvg, Vector3.up));


        /*for (int i = 0; i < G.v3list_terrainNorm.Length - 1; i++)
        {
            if (G.v3list_terrainNorm[i].sqrMagnitude < 50f)
            {
                G.v3_terrainNormAvg = Vector3.Slerp(G.v3_terrainNormAvg, G.v3list_terrainNorm[i], 1f / (float)(i + 1));
            }
        }*/

        Debug.DrawLine(currentPosition, currentPosition + G.v3_terrainNormAvg * 3f);
    }

    public override void CheckStateChange()
    {
        if (b_airborne)
        {
            DoStateChange(MC.C_moveJump);
            return;
        }

        if (b_groundCollide && Vector3.Dot(G.rigid.velocity, G.v3_groundNormal) < -2f)
        {
            G.rigid.AddForce(G.f_cliffJumpForce * v3_movementPlane, ForceMode.VelocityChange);
            DoStateChange(MC.C_moveGround);
            return;
        }

        if (Vector3.Angle(G.v3_terrainNormAvg, Vector3.up) < G.f_groundAngleThreshold)
        {
            DoStateChange(MC.C_moveGround);
            return;
        }
    }

    protected override void InputMovement()
    {
        v3_movementPlane = G.v3_terrainNormAvg;
        G.v3_accelerate = Vector3.zero;

        G.v3_currentVelocity = Vector3.ProjectOnPlane(G.rigid.velocity, v3_movementPlane);

        // Only acceleration if you aren't just now jumping onto the cliff
        if (!b_cliffJump)
        {
            Vector3 veloNorm = Vector3.Normalize(G.v3_currentVelocity);
            v3_cliffRight = Vector3.Cross(G.v3_terrainNormAvg, Vector3.up);
            v3_cliffFwd = Vector3.Cross(v3_cliffRight, G.v3_terrainNormAvg);

            if (G.v3_currentVelocity.magnitude > G.f_cliffSpeed)
                G.v3_currentVelocity = veloNorm * G.f_cliffSpeed;

            G.v3_accelerate += v3_cliffFwd * G.f_accel * G.v2_inputAxes.y;
            G.v3_accelerate += v3_cliffRight * G.f_accel * G.v2_inputAxes.x;

            G.f_dotAccelVel = Vector3.Dot(veloNorm, Vector3.Normalize(G.v3_accelerate));

            // Acceleration found
            G.rigid.AddForce(G.v3_accelerate);
        }
        else if (G.rigid.velocity.magnitude < G.f_cliffSpeed)//end cliff jumping when speed is low enough
        {
            b_cliffJump = false;
        }
        Vector3 brakeForce = Vector3.ClampMagnitude(-G.v3_currentVelocity, G.f_brakeSpeed) / G.f_brakeSpeed * G.f_maxBrake;

        if (G.f_dotAccelVel <= 0.01f) // If accel and velocity are not in the same direction, cancel it all
            G.rigid.AddForce(brakeForce);
        else //Just cancel the part going perpendicular to accel
            G.rigid.AddForce(Vector3.ProjectOnPlane(brakeForce, G.v3_accelerate));

        G.v3_currentVelocity += Vector3.Project(G.rigid.velocity, v3_movementPlane);
        G.rigid.velocity = G.v3_currentVelocity;

        // Jump
        if (G.b_jumpButtonDown)
        {
            G.rigid.AddForce(G.f_jumpForce * v3_movementPlane, ForceMode.VelocityChange);
            b_airborne = true;
        }
    }

    protected override void HorizontalRotation()
    {
        // Make sure we are facing up the cliff
        /*Vector3 axis = G.v3_terrainNormAvg;
        Vector3 myHorizontal = MC.transform.forward;
        G.v3list_rotateTo[0] = v3_cliffFwd;*/

        Vector3 axis = MC.transform.up;
        Vector3 myHorizontal = MC.transform.forward;
        G.v3list_rotateTo[0] = v3_cliffFwd;

        ApplyTorqueTowards(myHorizontal, G.v3list_rotateTo[0], axis, G.f_horizontalTorque);
    }

    protected override void VerticalRotation()
    {
        // Should handle tilt as well, just rotate transform.up to the slerp vector
        //float minDistance = Mathf.Min(G.fList_terrainDistance) - G.f_desiredHeight;

        G.v3list_rotateTo[1] = G.v3_terrainNormAvg;

        Vector3 myVertical = MC.transform.up;
        Vector3 axis = Vector3.Normalize(Vector3.Cross(myVertical, G.v3list_rotateTo[1]));

        ApplyTorqueTowards(myVertical, G.v3list_rotateTo[1], axis, G.f_verticalTorque);
    }

    protected override void GroundCollisionForces()
    {
        //if (b_airborne) { rigid.AddForce(v3_grav, ForceMode.Acceleration); return; }

        float minDistance = Mathf.Min(G.fList_terrainDistance);
        minDistance -= G.f_desiredHeight *G.f_desiredHeightCliffMod;
        Vector3 groundForce;

        //if (minDistance < G.f_desiredHeight * G.f_desiredHeightCliffMod)
        //{
            // Resist the ground
            groundForce = -G.v3_terrainNormAvg * minDistance * G.f_groundResistForce - Vector3.Project(G.v3_currentVelocity, G.v3_terrainNormAvg) * G.f_damping;

            G.rigid.AddForce(groundForce);
            //Debug.Log(groundForce);
        //}
        //else
        //{
        //    Debug.Log("not hitting the ground! time for grav");
        //    G.rigid.AddForce(G.v3_grav, ForceMode.Acceleration);
        //}
    }

    protected override void SetupState()
    {
        b_airborne = false;
        G.v3_terrainNormAvg = G.v3_wallNormal;

        if (G.rigid.velocity.magnitude > G.f_cliffSpeed)
            b_cliffJump = true;
        else
            b_cliffJump = false;
    }
}
