using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if (UNITY_EDITOR) 
[ExecuteInEditMode]
#endif

public class IKUpdateOrder : MonoBehaviour
{
#if (UNITY_EDITOR)
    [CustomEditor(typeof(IKUpdateOrder))]
    public class IKBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            IKUpdateOrder myScript = (IKUpdateOrder)target;

            if (GUILayout.Button("Return all IKs to original rotation"))
            {
                IKControl[] myIKs = myScript.transform.GetComponentsInChildren<IKControl>();
                if (myIKs.Length <= 0) return;
                for (int i = 0; i < myIKs.Length; i++)
                {
                    myIKs[i].ReturnToRotations(true);
                    Debug.Log(myIKs[i] + " returning to original rotations");
                }
            }
        }
    }
#endif

    public IKControl[] IkControlList;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
        for (int i = 0; i < IkControlList.Length; i++)
        {
            IkControlList[i].b_isOrdered = true;
        }

        for (int i = 0; i < IkControlList.Length; i++)
        {
            IkControlList[i].OrderedUpdate();
        }
    }
}
