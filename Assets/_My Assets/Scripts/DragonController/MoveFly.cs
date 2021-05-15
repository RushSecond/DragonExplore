using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveFly:MoveStateBase{

    bool b_airborne;
    bool b_upsideDown;

    float f_prevXinput;

    BarrelRoll e_barrelRolling;
    enum BarrelRoll
    {
        None = 0,
        Left = 1,  
        LeftOver = 2,
        Right = 3,
        RightOver = 4          
    }
    //bool b_barrelRoll;
    //float f_barrelRollTargetAngle;

    float f_pitchAngle;
    float f_rollAngle;

    float f_wingFlapTimer;

    float f_energyCurrent;
    float currentVelocity;
    float velocityAngle;

    Vector3 v3_forwardVec;
    Vector3 v3_leftVec; // So every rotation doesn't have to be negative

    protected override void SetupState()
    {
        FlapTheWings();
        f_energyCurrent = Vector3.SqrMagnitude(G.rigid.velocity);
        f_pitchAngle = 0f;
        f_rollAngle = 0f;
        f_wingFlapTimer = 0f;
        b_airborne = true;
        b_upsideDown = false;

        f_prevXinput = 0f;
        e_barrelRolling = BarrelRoll.None;
        //b_barrelRoll = false;
        //f_barrelRollTargetAngle = 0f;
    }

    public MoveFly(MoveController parent) : base(parent) { }

    public override void FixedUpdate()
    {
        G.rigid.AddTorque(-G.rigid.angularVelocity * G.f_rotateDamping, ForceMode.Acceleration);

        GroundCollisionForces();
        AirForces();
        InputMovement();
        PitchRotation();
        RollRotation();
 
        // Reduce wing flap timer
        f_wingFlapTimer = Mathf.Max(f_wingFlapTimer - Time.fixedDeltaTime, 0f);
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
            G.v3_wallNormal = hitInfo.normal;
            G.b_cliffCollide = true; // collided with an actual cliff
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
        if (G.b_cliffCollide && Vector3.Dot(G.rigid.velocity, G.v3_wallNormal) < -2f) // Moving toward the wall
        {
            if (Vector3.Angle(G.v3_wallNormal, Vector3.up) > G.f_cliffAngleThreshold)
                DoStateChange(MC.C_moveCliff);
            else
                DoStateChange(MC.C_moveGround);

            G.f_wingOpenAmount = 0f;
            return;
        }

        if (!b_airborne)
        {
            if (Vector3.Angle(G.v3_terrainNormAvg, Vector3.up) > G.f_groundAngleThreshold)
                DoStateChange(MC.C_moveCliff);
            else
                DoStateChange(MC.C_moveGround);

            G.f_wingOpenAmount = 0f;
        }
    }

    void AirForces()
    {
        Vector3 windResist = -G.rigid.velocity * G.f_windResistance;
        DoWorkByForce(windResist + G.v3_grav);

        currentVelocity = Mathf.Sqrt(2 * f_energyCurrent);

        // Wing flapping
        if (G.b_jumpButtonDown)
            FlapTheWings();
    }

    protected override void InputMovement()
    {
        Vector3 partialGrav;
        Quaternion velRotation;

        // Do a barrel roll!
        /*if (e_barrelRolling == BarrelRoll.None && Mathf.Abs(G.v2_inputAxes.x)
            > G.f_barrelRollThreshold && f_wingFlapTimer < G.f_wingFlapCooldown * 0.6f)
        {
            if (G.v2_inputAxes.x - f_prevXinput > G.f_barrelRollThreshold)
            {
                e_barrelRolling = BarrelRoll.Right;
            }
            else if (G.v2_inputAxes.x - f_prevXinput < -G.f_barrelRollThreshold)
            {
                e_barrelRolling = BarrelRoll.Left;
            }
        }*/
        f_prevXinput = G.v2_inputAxes.x;

        // Wings open or closed
        if (e_barrelRolling != BarrelRoll.None)
            G.f_wingOpenAmount = 0f;
        else
            // Wings open or close based on pitch angle
            G.f_wingOpenAmount = 1f + G.v2_inputAxes.y;


        // Normal rolling
        f_rollAngle = G.v2_inputAxes.x * G.f_maxRoll;
        
        //turning
        velRotation = Quaternion.AngleAxis(f_rollAngle * Time.fixedDeltaTime, Vector3.up);
        G.rigid.velocity = velRotation * G.rigid.velocity;

        // Pitching
        v3_forwardVec = Vector3.Normalize(Vector3.ProjectOnPlane(G.rigid.velocity, Vector3.up));
        v3_leftVec = Vector3.Normalize(Vector3.Cross(-Vector3.up, G.rigid.velocity));
        if (b_upsideDown)
            v3_leftVec *= -1;
        velocityAngle = Vector3.SignedAngle(v3_forwardVec, G.rigid.velocity, v3_leftVec); // The way we get this prevents us from doing loops
        f_pitchAngle = G.v2_inputAxes.y * 89f;
        //if (currentVelocity < G.f_minFlySpeed)
            //f_pitchAngle = Mathf.Min(f_pitchAngle, 0f);

        float f_velAngleChange = f_pitchAngle - velocityAngle;

        if (G.v2_inputAxes.y > 0.95f) // this allows loops
            f_velAngleChange = 100f;

        if (f_velAngleChange < 0f && velocityAngle > 0 && f_pitchAngle > -0.01f) // If we are going up but want to curve down, do so with gravity
            partialGrav = G.v3_grav;
        else // going to slow means you can't get enough lift with the wings and will fall!
            partialGrav = Vector3.Lerp(G.v3_grav, Vector3.zero, currentVelocity / G.f_minFlySpeed);

        G.rigid.AddForce(partialGrav, ForceMode.Acceleration);

        // Fix how fast the velocity can pitch up and down, based on joystick input
        float pitchingSpeed = Mathf.Abs(G.f_maxMatchPitchRate * G.v2_inputAxes.y * Time.fixedDeltaTime);
        //pitchingSpeed = Mathf.Max(pitchingSpeed, G.f_minMatchPitchRate * Time.fixedDeltaTime);

        // If upward momentum, make sure it doesn't immediately get overwritten by trying to fly flat
        if (velocityAngle > 0f && f_pitchAngle > -0.01f) 
            f_velAngleChange = Mathf.Clamp(f_velAngleChange, 0f, pitchingSpeed);
        else
            f_velAngleChange = Mathf.Clamp(f_velAngleChange, -pitchingSpeed, pitchingSpeed);

        if (currentVelocity < G.f_minFlySpeed && f_velAngleChange > 0f) // can't tilt upward if going to slowly
            f_velAngleChange = Mathf.Lerp(0f, f_velAngleChange, currentVelocity * 2 / G.f_minFlySpeed - 1); //Slowly create this effect to minspeed/2

        // Check if upside down now
        if (velocityAngle + f_velAngleChange > 90f)
            b_upsideDown = !b_upsideDown;

        velRotation = Quaternion.AngleAxis(f_velAngleChange, v3_leftVec); // Finally do the rotation for velocity
        G.rigid.velocity = velRotation * Vector3.Normalize(G.rigid.velocity) * currentVelocity;

        Debug.Log("flying velocity: " + currentVelocity + ", velocity angle: " + velocityAngle);
    }

    public void DoWorkByForce(Vector3 force)
    {
        float f_workDoneByForce = Vector3.Dot(force, G.rigid.velocity * Time.fixedDeltaTime);
        f_energyCurrent = Mathf.Max(0, f_energyCurrent + f_workDoneByForce);
    }

    public void FlapTheWings()
    {
        if (f_wingFlapTimer < 0.01f && e_barrelRolling == BarrelRoll.None)
        {
            f_wingFlapTimer += G.f_wingFlapCooldown;
            Vector3 flapForce = MC.transform.forward * G.f_wingFlapForceForward + Vector3.up * G.f_wingFlapForceVectorUp;
            MC.StartCoroutine(WingFlap(flapForce, G.f_wingFlapDelayTime, G.f_wingFlapLifeTime));
        }
    }

    IEnumerator WingFlap(Vector3 velChange, float delay, float lifeSpan)
    {
        G.b_wingsFlapStart = true;
        float elapsedTime = 0f;
        float ratioPerFrame = 1f / lifeSpan;
        while (elapsedTime < delay)
        {
            yield return new WaitForFixedUpdate();
            elapsedTime += Time.fixedDeltaTime;
        }
        elapsedTime -= delay;
        while (elapsedTime < lifeSpan)
        {
            DoWorkByForce(velChange * ratioPerFrame);
            G.rigid.AddForce(velChange * ratioPerFrame, ForceMode.Acceleration);       

            yield return new WaitForFixedUpdate();
            elapsedTime += Time.fixedDeltaTime;
        }
    }

    protected void PitchRotation()
    {
        Vector3 myForward = MC.transform.forward;
        float clampAngle;
        if (f_pitchAngle >= 0f)
        {
            clampAngle = Mathf.Min(f_pitchAngle, velocityAngle);
            clampAngle = Mathf.Max(clampAngle, 0f);
        }
        else
        {
            clampAngle = Mathf.Max(f_pitchAngle, velocityAngle);
            clampAngle = Mathf.Min(clampAngle, 0f);
        }
            
        Quaternion rot = Quaternion.AngleAxis(clampAngle, v3_leftVec);
        //G.v3list_rotateTo[0] = rot * v3_forwardVec;
        G.v3list_rotateTo[0] = G.rigid.velocity;

        Vector3 axis = Vector3.Cross(G.v3list_rotateTo[0], myForward);

        ApplyTorqueTowards(myForward, G.v3list_rotateTo[0], axis, G.f_tiltSpeed);
    }

    protected void RollRotation()
    {
        Vector3 myLeft = -MC.transform.right;
        Vector3 axis;
        if (e_barrelRolling == BarrelRoll.None)
        {
            G.v3list_rotateTo[1] = Quaternion.AngleAxis(-f_rollAngle, G.v3list_rotateTo[0]) * v3_leftVec;

            axis = Vector3.Cross(G.v3list_rotateTo[1], myLeft);

            ApplyTorqueTowards(myLeft, G.v3list_rotateTo[1], axis, G.f_rollSpeed);
        }
        else if (e_barrelRolling == BarrelRoll.Left || e_barrelRolling == BarrelRoll.LeftOver)
        {
            axis = MC.transform.forward;
            G.rigid.AddTorque(G.f_barrelRollTorque * axis, ForceMode.Acceleration);
            // Barrel roll over or done?
            if (Vector3.Dot(MC.transform.up, Vector3.up) < 0f)
                e_barrelRolling = BarrelRoll.LeftOver;
            else if (Vector3.Dot(MC.transform.up, Vector3.up) > 0f && e_barrelRolling == BarrelRoll.LeftOver)
            {
                e_barrelRolling = BarrelRoll.None;
                b_upsideDown = false;
            }
        }
        else if (e_barrelRolling == BarrelRoll.Right || e_barrelRolling == BarrelRoll.RightOver)
        {
            axis = MC.transform.forward;
            G.rigid.AddTorque(-G.f_barrelRollTorque * axis, ForceMode.Acceleration);
            // Barrel roll over or done?
            if (Vector3.Dot(MC.transform.up, Vector3.up) < 0f)
                e_barrelRolling = BarrelRoll.RightOver;
            else if (Vector3.Dot(MC.transform.up, Vector3.up) > 0f && e_barrelRolling == BarrelRoll.RightOver)
            {
                e_barrelRolling = BarrelRoll.None;
                b_upsideDown = false;
            }
        }
    }

    protected override void GroundCollisionForces()
    {
        float avgDistance = MathHelpers.FindAverage(G.fList_terrainDistance);
        avgDistance -= G.f_desiredHeight;

        if (avgDistance < G.f_desiredHeight * G.f_airborneThreshold && Vector3.Dot(G.v3_terrainNormAvg, G.v3_currentVelocity) < 0)
        {
            b_airborne = false;
        }
    }

    protected override void ApplyTorqueTowards(Vector3 from, Vector3 to, Vector3 axis, float strength)
    {
        Vector3 projectedFrom = Vector3.ProjectOnPlane(from, axis);
        Vector3 projectedTo = Vector3.ProjectOnPlane(to, axis);

        float angle = Vector3.SignedAngle(projectedFrom, projectedTo, axis);
        float tor = strength * Mathf.Clamp(angle / G.f_flyingMaxTorqueAngle, -1f, 1f);
        G.rigid.AddTorque(tor * axis, ForceMode.Acceleration);
    }
}
