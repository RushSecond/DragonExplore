using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public class MoveLeaning : MoveState {

    Vector3 v3_leaningNormal, v3_leanAxis;
    bool endLean;

    public MoveLeaning(MoveController parent) : base(parent)
    {}

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void CheckStateChange()
    {
        if (endLean)
            DoStateChange(MC.C_moveGround);
        else if (Vector3.Angle(MC.transform.up, G.v3_wallNormal) < 90 - G.f_cliffLeanThreshold)
            DoStateChange(MC.C_moveCliff);
    }

    public override void CheckGroundCollision()
    {
        G.v3_currentVelocity = G.rigid.velocity;
        Vector3 currentPosition = MC.transform.position;

        //will store info of successful ray cast
        RaycastHit hitInfo;

        //terrain should have mesh collider and be on custom terrain 
        //layer so we don't hit other objects with our raycast
        LayerMask layer = 1 << LayerMask.NameToLayer("Terrain");

        Vector3 worldterrainDetect;
        Vector3 worldterrainDir;
        // Raycast loop for terrain below (and in front)
        for (int i = 0; i < G.v3list_localTerrainDetectDown.Length; i++)
        {
            worldterrainDetect = MC.transform.TransformPoint(G.v3list_localTerrainDetectDown[i]);
            if (i >= 2)
                worldterrainDir = Vector3.down; // Use down vector instead for back legs
            else
                worldterrainDir = Vector3.ProjectOnPlane(MC.transform.forward, Vector3.up);

            if (Physics.Raycast(worldterrainDetect, worldterrainDir, out hitInfo, G.v3list_localTerrainDetectDir[i].magnitude, layer))
            {
                G.v3list_terrainPoint[i] = hitInfo.point;
                G.fList_terrainDistance[i] = Vector3.Magnitude(worldterrainDetect - G.v3list_terrainPoint[i]);
                G.v3list_terrainNorm[i] = hitInfo.normal;
                Debug.DrawLine(worldterrainDetect, G.v3list_terrainPoint[i], Color.blue);
            }
            else
            {
                Debug.DrawLine(worldterrainDetect, worldterrainDetect + worldterrainDir, Color.red);
                G.v3list_terrainPoint[i] = Vector3.down * 100f;
                G.fList_terrainDistance[i] = 100;
                G.v3list_terrainNorm[i] = Vector3.down * 100f;
            }
        }

        G.v3_wallNormal = Vector3.Slerp(G.v3list_terrainNorm[0], G.v3list_terrainNorm[1], 0.5f);

        //G.v3_terrainNormAvg = Vector3.up;
        Vector3 v1 = G.v3list_terrainPoint[1] - G.v3list_terrainPoint[0];
        Vector3 v2 = G.v3list_terrainPoint[2] - G.v3list_terrainPoint[0];

        //G.v3_wallNormal = Vector3.Normalize(Vector3.Cross(v1, Vector3.up));
        v3_leanAxis = Vector3.Cross(G.v3_wallNormal, Vector3.up);

        G.v3_terrainNormAvg = Vector3.Normalize(Vector3.Cross(v1, v2));
        // And make sure its facing upward
        G.v3_terrainNormAvg *= Mathf.Sign(Vector3.Dot(G.v3_terrainNormAvg, Vector3.up));

        

        Debug.DrawLine(currentPosition, currentPosition + G.v3_terrainNormAvg * 2f);
        Debug.DrawLine(MC.transform.position, MC.transform.position + v3_leaningNormal * 3f, Color.cyan);
    }

    protected override void InputMovement()
    {
        bool haveWeStarted = Vector3.Angle(MC.transform.up, v3_leaningNormal) < 10f;
        // lean further up cliff
        if (Input.GetAxis("Vertical") > 0)
        {
            Quaternion rot = Quaternion.AngleAxis(-G.f_leanAnglePerSec * Time.fixedDeltaTime, v3_leanAxis);
            v3_leaningNormal = rot * v3_leaningNormal;
        }
        else if (haveWeStarted && (Input.GetAxis("Vertical") < 0 || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.5f))
        {
            endLean = true;
        }
    }

    protected override void HorizontalRotation()
    {
        // Make sure we are facing up the cliff
        Vector3 axis = MC.transform.up;
        Vector3 myHorizontal = MC.transform.forward;
        G.v3list_rotateTo[0] = Vector3.Cross(v3_leanAxis, MC.transform.up); // this goes up the cliff

        ApplyTorque(myHorizontal, G.v3list_rotateTo[0], axis, G.f_horizontalTorque);
    }

    protected override void VerticalRotation()
    {
        G.v3list_rotateTo[1] = v3_leaningNormal;

        Vector3 myVertical = MC.transform.up;
        Vector3 axis = Vector3.Normalize(Vector3.Cross(myVertical, G.v3list_rotateTo[1]));

        ApplyTorque(myVertical, G.v3list_rotateTo[1], axis, G.f_verticalTorque * 3f);
    }

    protected override void GroundCollisionForces()
    {

        Vector3 groundForce;

        float minDistanceWall = Mathf.Min(G.fList_terrainDistance[0], G.fList_terrainDistance[1]);
        minDistanceWall = Mathf.Max(0, minDistanceWall - G.f_desiredHeight);

        float minDistanceGround = Mathf.Min(G.fList_terrainDistance[2], G.fList_terrainDistance[3]);
        minDistanceGround = Mathf.Max(0, minDistanceGround - G.f_desiredHeight);
        G.v3_groundNormal = Vector3.Slerp(G.v3list_terrainNorm[2], G.v3list_terrainNorm[3], 0.5f);

        // Resist the wall
        groundForce = -G.v3_wallNormal * minDistanceWall * G.f_groundResistForce;


        // Resist the ground
        groundForce -= G.v3_groundNormal * minDistanceGround * G.f_groundResistForce;

        // damping
        groundForce -= G.v3_currentVelocity * G.f_damping;

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
        endLean = false;
        v3_leanAxis = Vector3.Cross(G.v3_wallNormal, Vector3.up);
        Quaternion rot = Quaternion.AngleAxis(90f - G.f_cliffLeanAngle, v3_leanAxis);
        v3_leaningNormal = rot * G.v3_wallNormal;
    }
}*/
