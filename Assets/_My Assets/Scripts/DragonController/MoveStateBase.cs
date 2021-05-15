using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveStateBase{

    protected MoveController MC;
    protected MoveController.MoveControllerGlobals G;

    public MoveStateBase(MoveController parent)
    {
        MC = parent;
        G = parent.MCglobals;
    }

    public virtual void FixedUpdate()
    {
        GroundCollisionForces();
        InputMovement();
        HorizontalRotation();
        VerticalRotation();

        // Damp things
        G.rigid.AddTorque(-G.rigid.angularVelocity * G.f_rotateDamping, ForceMode.Acceleration);
    }


    protected virtual void GroundCollisionForces() { }
    public virtual void CheckStateChange() { }
    protected virtual void InputMovement() { }
    protected virtual void HorizontalRotation() { }
    protected virtual void VerticalRotation() { }
    protected virtual void SetupState() { }

    public virtual void CheckGroundCollision()
    {
        
    }

    protected void DoStateChange(MoveStateBase change)
    {
        if (MC.C_activeState != change)
        {
            change.SetupState();
            MC.C_activeState = change;
            Debug.Log("Movestate changed to " + change.GetType());
        }
    }

    /// <summary>
    /// Smoothly rotates something from the 1st vector to the 2nd, along the axis
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="axis"></param>
    /// <param name="strength"></param>
    protected virtual void ApplyTorqueTowards(Vector3 from, Vector3 to, Vector3 axis, float strength)
    {
        Vector3 projectedFrom = Vector3.ProjectOnPlane(from, axis);
        Vector3 projectedTo = Vector3.ProjectOnPlane(to, axis);

        float angle = Vector3.SignedAngle(projectedFrom, projectedTo, axis);
        float tor = strength * Mathf.Clamp(angle / G.f_maxTorqueAngle, -1f, 1f);
        G.rigid.AddTorque(tor * axis, ForceMode.Acceleration);
    }
}
