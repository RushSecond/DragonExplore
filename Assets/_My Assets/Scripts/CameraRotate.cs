using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
public class CameraRotate : MonoBehaviour {

    public Transform target;
    public Vector3 v3_targetOffset = Vector3.zero;

    public float distance = 10.0f;
   
    public float xSpeed = 50.0f;
    public float ySpeed = 120.0f;
    public float rotateSpeed = .05f;

    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    public float distanceMin = .5f;
    public float distanceMax = 15f;

    Vector3 angles;
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;

    Vector3 v3_targetOffsetX;
    Vector3 v3_targetOffsetY;
    // Use this for initialization
    void Start()
    {
        v3_targetOffsetX = Vector3.ProjectOnPlane(v3_targetOffset, Vector3.up);
        v3_targetOffsetY = Vector3.Project(v3_targetOffset, Vector3.up);
        angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        z = angles.z;
    }

    void LateUpdate()
    {
        if (target)
        {            
            Vector3 realTargetPosition = target.position + target.TransformVector(v3_targetOffsetX) + v3_targetOffsetY;
#if UNITY_STANDALONE || UNITY_WEBPLAYER
            x += Input.GetAxis("Mouse X") * xSpeed;
            y -= Input.GetAxis("Mouse Y") * ySpeed;   
            y = ClampAngle(y, yMinLimit, yMaxLimit);
#else
            x = target.eulerAngles.y;
            y = angles.x + target.eulerAngles.x;
#endif        
#if UNITY_STANDALONE || UNITY_WEBPLAYER
            Quaternion rotation = Quaternion.Euler(y, x, z);
#else
            float newRotateSpeed = MathHelpers.RotateTimeAdjust(rotateSpeed);
            Quaternion rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(y, x, z), newRotateSpeed);
#endif

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            RaycastHit hit;
            float hitDistance = 0f;
            if (Physics.Raycast(realTargetPosition, transform.position - realTargetPosition, out hit, distance, LayerMask.NameToLayer("Terrain")))
            {
                hitDistance = hit.distance;
            }
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance + hitDistance);
            Vector3 position = rotation * negDistance + realTargetPosition;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
