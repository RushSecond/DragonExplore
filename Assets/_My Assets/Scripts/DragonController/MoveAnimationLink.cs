using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IMPORTANT! right now this class is only for dragons!!

// next time we use this class for animating a different model, use this as a base template
// and extend from this. You can then make an enum setting in MoveController to pick between
// classes based on the type of animator that model has

public static class DragonAnimHashes
{
    // Parameter hashes
    public static int flying = Animator.StringToHash("Flying");
    public static int speedFactor = Animator.StringToHash("SpeedFactor");
    public static int wingFlapTrigger = Animator.StringToHash("WingFlapTrigger");
    public static int wingOpenAmount = Animator.StringToHash("WingOpenAmount");
}

public class MoveAnimationLink
{
    Animator A_anim;
    MoveController.MoveControllerGlobals G;
    MoveController MC;
    float f_wingOpenVel;
    float f_currentWingOpen;

    public MoveAnimationLink(MoveController master)
    {
        MC = master;
        G = master.MCglobals;
        A_anim = G.A_animator;
        f_wingOpenVel = 0f;
        f_currentWingOpen = 0f;


        //MC.StartCoroutine(WingOpenAnimation());
    }

    public void SendInfoToAnim()
    {
        if (MC.C_activeState == MC.C_moveFly && G.b_wingsFlapStart)
            A_anim.SetTrigger(DragonAnimHashes.wingFlapTrigger);

        A_anim.SetBool(DragonAnimHashes.flying, MC.C_activeState == MC.C_moveFly);
        A_anim.SetFloat(DragonAnimHashes.speedFactor, G.rigid.velocity.magnitude / G.f_groundSpeed);

        A_anim.SetFloat(DragonAnimHashes.wingOpenAmount, G.f_wingOpenAmount);

        //WingOpenAnimation();
    }

    IEnumerator WingOpenAnimation()
    {
        float wingDistance;
        while (true)
        {
            wingDistance = G.f_wingOpenAmount - f_currentWingOpen;
            if (Mathf.Abs(wingDistance) < .01f)
            {
                f_currentWingOpen = G.f_wingOpenAmount;
                f_wingOpenVel = 0f;
            }
            // need to decelerate to stop in time
            else if (Mathf.Abs(wingDistance) < f_wingOpenVel * f_wingOpenVel / (2 * G.f_wingOpenCloseAccel)) 
                f_wingOpenVel += G.f_wingOpenCloseAccel * -Mathf.Sign(wingDistance) * Time.deltaTime;
            else
                f_wingOpenVel += G.f_wingOpenCloseAccel * Mathf.Sign(wingDistance) * Time.deltaTime;

            f_currentWingOpen += f_wingOpenVel * Time.deltaTime;

            if (f_currentWingOpen < 0f || f_currentWingOpen > 1f)
            {
                f_currentWingOpen = Mathf.Clamp(f_currentWingOpen, 0f, 1f);
                f_wingOpenVel = 0f;
            }

            A_anim.SetFloat(DragonAnimHashes.wingOpenAmount, f_currentWingOpen);

            yield return null;
        }
    }
}
