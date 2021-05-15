using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour {

    public Transform following;
    public float distance = 10f;


    Vector3 vecFromMeToFollow = Vector3.zero;

	// Use this for initialization
	void Start () {
        vecFromMeToFollow = Vector3.Normalize(following.position - transform.position) * distance;

    }
	
	// Update is called once per frame
	void Update () {
        if (following)
        {
            Vector3 horizontalLookDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            Vector3 horizontalOffsetDirection = Vector3.ProjectOnPlane(vecFromMeToFollow, Vector3.up);
            Vector3 verticalOffsetDirection = Vector3.Project(vecFromMeToFollow, Vector3.up);

            Quaternion rotation = Quaternion.FromToRotation(horizontalOffsetDirection, horizontalLookDirection);

            //Vector3 followHorizontalFacing = Vector3.ProjectOnPlane(following.forward, Vector3.up);
            //Vector3 myHorizontal = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            //transform.RotateAround(transform.position, Vector3.up, Vector3.SignedAngle(myHorizontal, followHorizontalFacing, Vector3.up) * 2 * Time.deltaTime);

            Vector3 newPosition = following.position - (rotation * horizontalOffsetDirection) - verticalOffsetDirection;
            //newPosition = following.position - horizontalOffsetDirection - verticalOffsetDirection;

            //transform.position = Vector3.Lerp(transform.position, newPosition, 20 * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, newPosition, 1f);
        }
	}
}
