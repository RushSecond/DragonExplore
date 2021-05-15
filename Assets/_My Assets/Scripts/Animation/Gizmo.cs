using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Gizmo : MonoBehaviour {

#if (UNITY_EDITOR)
    [CustomEditor(typeof(Gizmo))]
    public class ObjectBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Gizmo myScript = (Gizmo)target;

            if (GUILayout.Button("Align to Transform"))
            {
                myScript.Align();
            }
        }
    }
#endif

    public Transform t_align;

    public float gizmoSize = 0.5f;
    public Color gizmoColor = Color.white;

    // Use this for initialization
    void Start()
    {


    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
    }

    // Update is called once per frame
    void Align()
    {
        if (!t_align)
            return;
        transform.position = t_align.position;
        transform.rotation = t_align.rotation;
        transform.localScale = t_align.localScale;
    }
}
