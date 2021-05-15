using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    // Physics classes
    public MoveStateBase C_activeState;
    public MoveGround C_moveGround;
    public MoveCliff C_moveCliff;
    //public MoveLeaning C_moveLeaning;
    public MoveJump C_moveJump;
    public MoveFly C_moveFly;

    MoveAnimationLink C_animLink;

    [Tooltip("Move Controller Globals")]
    public MoveControllerGlobals MCglobals;

    [System.Serializable]
    public class MoveControllerGlobals
    {
        [Header("Ground Movement")]
        [Tooltip("Forward acceleration")] 
        public float f_accel = 20f;
        [Tooltip("Backward acceleration")]
        public float f_maxBrake = 50f;
        [Tooltip("Speed needed for maximum brake force")]
        public float f_brakeSpeed = 2f;
        [Tooltip("Maximum forward ground speed")]
        public float f_groundSpeed = 20f;
        [Tooltip("Preferred height when standing on ground")]
        public float f_desiredHeight = 1.7f;

        [Header("Ground Detection")]
        [Tooltip("How far forward and back to detect ground")]
        public float f_groundDetectFwdBack = 1.2f;
        [Tooltip("How far left and right to detect ground")]
        public float f_groundDetectLeftRight = 0.6f;
        [Tooltip("Normal force off the ground, like a spring")]
        public float f_groundResistForce = 20f;
        [Tooltip("Damping force for the ground spring movement")]
        public float f_damping = 6f;

        [Header("Rotation")]
        [Tooltip("Horizontal torque strength")]
        public float f_horizontalTorque = 150f; // 
        [Tooltip("Vertical torque strength")]
        public float f_verticalTorque = 60f; // 
        [Tooltip("Angle needed for maximum torque")]
        public float f_maxTorqueAngle = 30f; //
        [Tooltip("Angular damping for rotation")]
        public float f_rotateDamping = 40f; // 
        [Tooltip("Height to start rotating from flat to the ground norm")]
        public float f_vertHeightCheck = 5f; // 
        [Tooltip("Distance forward to check for walls")]
        public float f_forwardHeightCheck = 3f;

        [Header("Cliffs")]
        [Tooltip("Maximum cliff speed ")]
        public float f_cliffSpeed = 8f;
        [Tooltip("Angle (measured from upvector) needed to change from ground to cliff")]
        public float f_cliffAngleThreshold = 60f;
        [Tooltip("Angle (measured from upvector) needed to change from cliff to ground")]
        public float f_groundAngleThreshold = 50f;
        [Tooltip("Force applied when jumping onto or off a cliff")]
        public float f_cliffJumpForce = 40f;
        /*[Tooltip("Angle when starting to lean on cliff")]
        public float f_cliffLeanAngle = 40f;
        [Tooltip("Angle per second going up or down when leaning")]
        public float f_leanAnglePerSec = 35f;
        [Tooltip("Angle needed to transition to climbing cliff")]
        public float f_cliffLeanThreshold = 75f;*/
        [Tooltip("Modifier for desired height on cliffs")]
        public float f_desiredHeightCliffMod = .6f;

        [Header("Jump Stuff")]
        [Tooltip("Jump force")]
        public float f_jumpForce = 10f;
        [Tooltip("Jump lifetime")]
        public float f_jumpLifeTime = .2f;
        [Tooltip("Jump angle")]
        public float f_jumpAngle = 30f;
        [Tooltip("Air acceleration")]
        public float f_airAccel = 5f;
        [Tooltip("Factor of desired height needed to be considered airborne")]
        public float f_airborneThreshold = 0.4f;

        [Header("Air Physics")]
        [Tooltip("Wind resistance constant. This times velocity = damping acceleration per second")]
        public float f_windResistance = .08f;
        [Tooltip("Min flying speed")]
        public float f_minFlySpeed = 15f;
        //[Tooltip("Max pitch angle in degrees (tilt up and down)")]
        //public float f_maxPitch = 70f;
        [Tooltip("Max roll angle in degrees (tilt left and right)")]
        public float f_maxRoll = 60f;
        [Tooltip("Tilt rotation speed")]
        public float f_tiltSpeed = 60f;
        [Tooltip("How fast velocity can match the current pitch")]
        public float f_maxMatchPitchRate = 70f;
        [Tooltip("Minimum speed for velocity can match the current pitch")]
        public float f_minMatchPitchRate = 20f;
        [Tooltip("Roll rotation speed")]
        public float f_rollSpeed = 120f;
        [Tooltip("Max torque angle for flying")]
        public float f_flyingMaxTorqueAngle = 20f;
        
        [Header("Air Controls")]
        [Tooltip("Wing force forward")]
        public float f_wingFlapForceForward = 5f;
        [Tooltip("Wing force for world upvector")]
        public float f_wingFlapForceVectorUp = 10f;
        [Tooltip("Wing force delay time")]
        public float f_wingFlapDelayTime = .3f;
        [Tooltip("Wing force lifetime")]
        public float f_wingFlapLifeTime = .66f;
        [Tooltip("Wing flapping cooldown")]
        public float f_wingFlapCooldown = 1.5f;
        [Tooltip("Barrel roll joystick threshold")]
        public float f_barrelRollThreshold = .4f;
        [Tooltip("Barrel roll rotation torque")]
        public float f_barrelRollTorque = 200f;

        [Header("Animations")]
        [Tooltip("Animator Link")]
        public Animator A_animator;
        [Tooltip("Wing Open/Close acceleration")]
        public float f_wingOpenCloseAccel = 3f;

        public Rigidbody rigid { get; set; }
        
        public float f_dotAccelVel { get; set; }
        public bool b_cliffCollide { get; set; }
        
        public Vector3[] v3list_localTerrainDetectDown { get; set; }
        public Vector3[] v3list_localTerrainDetectDir { get; set; }
        public Vector3[] v3list_terrainPoint { get; set; }
        public Vector3 v3_terrainPointLerp { get; set; }
        public float[] fList_terrainDistance { get; set; }
        public Vector3 v3_terrainNormAvg { get; set; }
        public Vector3 v3_wallNormal { get; set; }
        public Vector3 v3_groundNormal { get; set; }

        public Vector3 v3_currentVelocity { get; set; }
        public Vector3 v3_accelerate { get; set; }

        public Vector3[] v3list_rotateTo { get; set; }

        public Vector3 v3_grav { get; set; }
        public Transform t_cameraTrans { get; set; }
        public float f_capsuleLength { get; set; }

        public Vector2 v2_inputAxes { get; set; }
        public bool b_jumpButtonDown { get; set; }
        public bool b_wingsFlapStart { get; set; }
        float wingOpenAmount;
        public float f_wingOpenAmount { get { return wingOpenAmount; } set { wingOpenAmount = Mathf.Clamp(value, 0f, 1f); } }
    }

    const int i_TERRAINNUMS = 4;

#if UNITY_IOS
    Joystick joystick;
    JoyButton joyButton;
#endif

    // Use this for initialization
    void Start()
    { 
        // Set up the helper classes for each state
        C_moveGround = new MoveGround(this);
        C_moveCliff = new MoveCliff(this);
        C_moveJump = new MoveJump(this);
        C_moveFly = new MoveFly(this);

        C_animLink = new MoveAnimationLink(this);

        MCglobals.v3_grav = Physics.gravity;
        MCglobals.rigid = GetComponent<Rigidbody>();
        MCglobals.rigid.maxAngularVelocity = 20f;
        MCglobals.t_cameraTrans = Camera.main.transform;

        // Set up terrain detection system
        MCglobals.v3list_localTerrainDetectDown = new Vector3[i_TERRAINNUMS];
        Vector3 groundDetectFwd = Vector3.forward * MCglobals.f_groundDetectFwdBack * transform.lossyScale.x;
        Vector3 groundDetectRight = Vector3.right * MCglobals.f_groundDetectLeftRight * transform.lossyScale.x;
        
        MCglobals.v3list_localTerrainDetectDown[0] = groundDetectFwd + groundDetectRight; // Right arm
        MCglobals.v3list_localTerrainDetectDown[1] = groundDetectFwd - groundDetectRight; // Left arm
        MCglobals.v3list_localTerrainDetectDown[2] = -groundDetectFwd - groundDetectRight; // Left leg
        MCglobals.v3list_localTerrainDetectDown[3] = -groundDetectFwd + groundDetectRight; // Right leg

        MCglobals.v3list_localTerrainDetectDir = new Vector3[i_TERRAINNUMS];
        for (int i = 0; i < i_TERRAINNUMS; i++) { MCglobals.v3list_localTerrainDetectDir[i] = Vector3.down * MCglobals.f_vertHeightCheck; }

        MCglobals.v3list_terrainPoint = new Vector3[i_TERRAINNUMS];
        MCglobals.fList_terrainDistance = new float[i_TERRAINNUMS];


        MCglobals.v3list_rotateTo = new Vector3[2];
        MCglobals.v3list_rotateTo[0] = Vector3.forward;

        // Set capsule height
        MCglobals.f_capsuleLength = GetComponent<CapsuleCollider>().height * 0.5f * transform.lossyScale.z;

        MCglobals.f_desiredHeight *= transform.lossyScale.y;

        MCglobals.v3_terrainNormAvg = Vector3.up;
        MCglobals.v3_wallNormal = Vector3.up;

#if UNITY_IOS
        joystick = FindObjectOfType<Joystick>();
        joyButton = FindObjectOfType<JoyButton>();
#endif

        SpawnOnGround();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        C_activeState.CheckGroundCollision();
        C_activeState.CheckStateChange();
        C_activeState.FixedUpdate();

        C_animLink.SendInfoToAnim();

        // Reset input
        ResetInput();
    }

    void Update()
    {
        GetInput();       
    }

    void GetInput()
    {
#if UNITY_STANDALONE || UNITY_WEBPLAYER
        // Check for jump
        if (Input.GetButtonDown("Jump"))
            MCglobals.b_jumpButtonDown = true;

        MCglobals.v2_inputAxes = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif

#if UNITY_EDITOR
        // Check for jump
        if (Input.GetButtonDown("Jump"))
            MCglobals.b_jumpButtonDown = true;
#endif
        MCglobals.v2_inputAxes = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    }

    void ResetInput()
    {
        MCglobals.b_jumpButtonDown = false;
        MCglobals.b_wingsFlapStart = false;
    }

    void SpawnOnGround()
    {
        C_activeState = C_moveGround;

        //will store info of successful ray cast
        RaycastHit hitInfo;

        //terrain should have mesh collider and be on custom terrain 
        //layer so we don't hit other objects with our raycast
        LayerMask layer = 1 << LayerMask.NameToLayer("Terrain");

        Vector3 worldterrainDetect;
        Vector3 worldterrainDir;

        worldterrainDetect = transform.position;
        worldterrainDir = Vector3.down;
        if (Physics.Raycast(worldterrainDetect, worldterrainDir, out hitInfo, 1000f, layer))
        {
            transform.position = hitInfo.point + Vector3.up * MCglobals.f_desiredHeight;

            C_activeState = C_moveGround;
        }
        else { C_activeState = C_moveJump; }

    }
}