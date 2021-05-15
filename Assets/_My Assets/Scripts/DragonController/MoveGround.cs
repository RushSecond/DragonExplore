using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveGround : MoveStateBase
{
    Vector3 v3_movementPlane;
    bool b_airborne = false;
    bool b_offCliff = true;

    public MoveGround(MoveController parent) : base(parent) { }

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
            worldterrainDir = MC.transform.rotation * G.v3list_localTerrainDetectDir[i];
            if (Physics.SphereCast(worldterrainDetect, 0.2f, worldterrainDir, out hitInfo, G.v3list_localTerrainDetectDir[i].magnitude, layer))
            {
                G.v3list_terrainPoint[i] = hitInfo.point;
                G.fList_terrainDistance[i] = Vector3.Magnitude(worldterrainDetect - G.v3list_terrainPoint[i]);
                Debug.DrawLine(worldterrainDetect, G.v3list_terrainPoint[i], Color.blue);
            }
            else { invalid = i; }

            if (invalid == i)
            {
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
        

        //G.v3_terrainNormAvg = Vector3.up;
        G.v3_terrainNormAvg = Vector3.Normalize(Vector3.Cross(v2, v1));
        //And make sure its facing upward
        G.v3_terrainNormAvg *= Mathf.Sign(Vector3.Dot(G.v3_terrainNormAvg, Vector3.up));

        // Change terrainNorm to upvector to avoid sticking to a cliff you just jumped off of
        if (b_offCliff)
        {
            if (Vector3.Angle(G.v3_terrainNormAvg, Vector3.up) > G.f_cliffAngleThreshold)
            {
                G.v3_terrainNormAvg = Vector3.up;
            }
            else
            {
                b_offCliff = false;
            }
        }

        Debug.DrawLine(currentPosition, currentPosition + G.v3_terrainNormAvg * 3f);
    }

    public override void CheckStateChange()
    {
        if (b_airborne)
        {
            DoStateChange(MC.C_moveJump);
            return;
        }

        if (G.b_cliffCollide && Vector3.Dot(G.rigid.velocity, G.v3_wallNormal) < -2f) // Try not doing lean cuz it sucks
        {
            G.rigid.AddForce(G.f_cliffJumpForce * v3_movementPlane, ForceMode.VelocityChange);
            DoStateChange(MC.C_moveCliff);  
            return;
        }

        // Change to cliff if going up a smooth cliff
        if (Vector3.Angle(G.v3_terrainNormAvg, Vector3.up) > G.f_cliffAngleThreshold)
            
        {
            if (Vector3.Dot(MC.transform.forward, Vector3.up) < 0)
                DoStateChange(MC.C_moveJump);
            else
                DoStateChange(MC.C_moveCliff);
        }
    }

    protected override void InputMovement()
    {
        if (Vector3.Angle(G.v3_terrainNormAvg, Vector3.up) > G.f_cliffAngleThreshold) // Use the up plane instead if this is too cliff-like
            v3_movementPlane = Vector3.up;
        else
            v3_movementPlane = G.v3_terrainNormAvg;

        G.v3_currentVelocity = Vector3.ProjectOnPlane(G.rigid.velocity, v3_movementPlane);

        Vector3 veloNorm = Vector3.Normalize(G.v3_currentVelocity);
        Vector3 rightVec = Vector3.Normalize(Vector3.Cross(v3_movementPlane, Vector3.ProjectOnPlane(G.t_cameraTrans.forward, Vector3.up)));
        Vector3 fwdVec = Vector3.Normalize(Vector3.Cross(G.t_cameraTrans.right, v3_movementPlane));

        G.v3_accelerate = Vector3.zero;
        G.v3_accelerate += fwdVec * G.f_accel * G.v2_inputAxes.y;
        G.v3_accelerate += rightVec * G.f_accel * G.v2_inputAxes.x;

        if (G.v3_currentVelocity.magnitude > G.f_groundSpeed) //if you are going too fast, you can't accelerate in that direction
            G.v3_accelerate = Vector3.ProjectOnPlane(G.v3_accelerate, veloNorm);

        G.f_dotAccelVel = Vector3.Dot(veloNorm, Vector3.Normalize(G.v3_accelerate));

        // Acceleration found
        G.rigid.AddForce(G.v3_accelerate);
        Vector3 brakeForce = Vector3.ClampMagnitude(-G.v3_currentVelocity, G.f_brakeSpeed) / G.f_brakeSpeed * G.f_maxBrake;

        G.v3_currentVelocity += Vector3.Project(G.rigid.velocity, v3_movementPlane); ;
        G.rigid.velocity = G.v3_currentVelocity;

        if (G.f_dotAccelVel <= 0.01f) // If accel and velocity are not in the same direction, cancel it all
            G.rigid.AddForce(brakeForce);
        else //Just cancel the part going perpendicular to accel
            G.rigid.AddForce(Vector3.ProjectOnPlane(brakeForce, G.v3_accelerate));

        // Jump
        if (G.b_jumpButtonDown)
        {
            Vector3 jumpDirection = Vector3.Slerp(MC.transform.forward, MC.transform.up, G.f_jumpAngle / 90f);

            IEnumerator coroutine = JumpForce(G.f_jumpForce * jumpDirection, G.f_jumpLifeTime);
            MC.StartCoroutine(coroutine);
            //G.rigid.AddForce(G.f_jumpForce * v3_movementPlane, ForceMode.VelocityChange);
            b_airborne = true;
        }
    }

    IEnumerator JumpForce(Vector3 velChange, float lifeSpan)
    {
        float elapsedTime = 0f;
        float ratioPerFrame = Time.fixedDeltaTime / lifeSpan;
        while (elapsedTime < lifeSpan)
        {
            G.rigid.AddForce(velChange * ratioPerFrame, ForceMode.VelocityChange);

            yield return new WaitForFixedUpdate();
            elapsedTime += Time.fixedDeltaTime;
        }
    }

    protected override void HorizontalRotation()
    {
        // Horizontal stuff
        //Vector3 axis = MC.transform.up;
        Vector3 axis = v3_movementPlane;
        Vector3 myHorizontal = MC.transform.forward;

        if (G.v3_accelerate.sqrMagnitude > 1)
        {
            //if (G.f_dotAccelVel >= -0.5f || G.v3_currentVelocity.magnitude > G.f_groundSpeed * .6f)
            if (G.f_dotAccelVel >= -0.5f)
                //G.v3list_rotateTo[0] = G.v3_currentVelocity;
                G.v3list_rotateTo[0] = Vector3.Slerp(G.v3_accelerate, G.v3_currentVelocity, G.v3_currentVelocity.magnitude / (G.f_groundSpeed * .6f));
            else
                G.v3list_rotateTo[0] = G.v3_accelerate;

                //G.v3list_rotateTo[0] = Vector3.Slerp(G.v3list_rotateTo[0], myHorizontal, 0.3f);
        }
        else
        {
            float angle = Vector3.Angle(myHorizontal, G.v3list_rotateTo[0]);
            if (angle > 5f)
            {
                G.v3list_rotateTo[0] = Vector3.Slerp(myHorizontal, G.v3list_rotateTo[0], 5f / angle);
            }
        } 



        ApplyTorqueTowards(myHorizontal, G.v3list_rotateTo[0], axis, G.f_horizontalTorque);
    }

    protected override void VerticalRotation()
    {
        // Should handle tilt as well, just rotate transform.up to the slerp vector

        //if (b_hasMoved)
        //{
        float minDistance = Mathf.Min(G.fList_terrainDistance) - G.f_desiredHeight;

        G.v3list_rotateTo[1] = Vector3.Slerp(G.v3_terrainNormAvg,
            Vector3.up,
            Mathf.Clamp(minDistance, 0f, G.f_vertHeightCheck) / G.f_vertHeightCheck);

        Vector3 myVertical = MC.transform.up;
        Vector3 axis = Vector3.Normalize(Vector3.Cross(myVertical, G.v3list_rotateTo[1]));

        ApplyTorqueTowards(myVertical, G.v3list_rotateTo[1], axis, G.f_verticalTorque);
    }

    protected override void GroundCollisionForces()
    {
        //if (b_airborne) { rigid.AddForce(v3_grav, ForceMode.Acceleration); return; }

        float avgDistance = MathHelpers.FindAverage(G.fList_terrainDistance);
        float minDistance = Mathf.Min(G.fList_terrainDistance);
        minDistance -= G.f_desiredHeight;
        avgDistance -= G.f_desiredHeight;
        Vector3 groundForce;

        if (minDistance < G.f_desiredHeight * G.f_airborneThreshold)
        {
            /*if (minDistance > G.f_desiredHeight * 1.03f)
            {
                //Attach to the ground
                groundForce = G.v3_grav - Vector3.Project(G.v3_currentVelocity, G.v3_terrainNormAvg) * G.f_damping;
            }
            else
            {*/

                // Resist the ground
                groundForce = -G.v3_terrainNormAvg * minDistance * G.f_groundResistForce - Vector3.Project(G.v3_currentVelocity, G.v3_terrainNormAvg) * G.f_damping;
                //groundForce.y = Mathf.Max(groundForce.y, G.v3_grav.y);
            //}

            G.rigid.AddForce(groundForce);
            //Debug.Log(groundForce);
        }
        else
        {
            b_airborne = true;
            G.rigid.AddForce(G.v3_grav, ForceMode.Acceleration);
        }
    }

    protected override void SetupState()
    {
        G.v3_terrainNormAvg = G.v3_groundNormal;
        G.v3_accelerate = Vector3.zero;
        b_airborne = false;
        b_offCliff = true;
    }
}
