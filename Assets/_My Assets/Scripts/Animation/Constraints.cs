using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if (UNITY_EDITOR) 
[ExecuteInEditMode]
#endif

public class Constraints : MonoBehaviour
{
    public Transform t_objectContstrained;
    public bool positionConstrain;
    public bool rotationConstrain;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (t_objectContstrained != null)
        {
            if (positionConstrain)
                t_objectContstrained.position = transform.position;
            if (rotationConstrain)
                t_objectContstrained.rotation = transform.rotation;
        }
    }
}
