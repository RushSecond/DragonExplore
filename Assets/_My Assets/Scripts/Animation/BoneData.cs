using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoneData
{
    public Transform t_bone;
    public Vector3 v3_startingLocalPosition;
    public Quaternion q_startingLocalRot;
    public Quaternion q_prevWoldRotation { get; set; }
    public Vector3 v3_prevPointTo { get; set; }
    public float f_rotateReturnLerp { get; set; }
    private Vector3 v3_boneLocalUp;

    public Vector3 v3_boneUpVector
    {
        get { return t_bone.TransformVector(v3_boneLocalUp); }
    }


    public Vector3 position
    {
        get { return t_bone.position; }
        set { t_bone.position = value; }
    }
    public Quaternion rotation
    {
        get { return t_bone.rotation; }
        set { t_bone.rotation = value; }
    }
    public Quaternion localRotation
    {
        get { return t_bone.localRotation; }
        set { t_bone.localRotation = value; }
    }
    public Vector3 localScale
    {
        get { return t_bone.localScale; }
        set { t_bone.localScale = value; }
    }
    public Transform parent
    {
        get { return t_bone.parent; }
    }
    public Vector3 forward
    {
        get { return t_bone.forward; }
    }
    public Vector3 up
    {
        get { return t_bone.up; }
    }
    public Vector3 right
    {
        get { return t_bone.right; }
    }

    public BoneData(Transform bone, Vector3 upDirection)
    {
        t_bone = bone;
        v3_startingLocalPosition = bone.localPosition;
        q_startingLocalRot = bone.localRotation;

        if (Mathf.Abs(Vector3.Dot(bone.up, upDirection)) < 0.7f)
            v3_boneLocalUp = Vector3.forward;
        else
            v3_boneLocalUp = Vector3.up;
        if (Vector3.Dot(v3_boneUpVector, upDirection) < 0f)
            v3_boneLocalUp *= -1f;
    }

    public void RefreshLocals()
    {
        if (t_bone == null)
            return;

        if (v3_startingLocalPosition.sqrMagnitude < 0.01f)
            v3_startingLocalPosition = t_bone.localPosition;

        if (Quaternion.Angle(q_startingLocalRot, Quaternion.identity) < 0.01f)
            q_startingLocalRot = t_bone.localRotation;
    }
}
