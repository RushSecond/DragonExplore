using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//Copy and paste atlas settings to another atlas editor
public class AutoAnimation : EditorWindow
{
    public Animator animator;
    public AnimationClip animationClip;
    public Transform bones;

    [MenuItem("Window/Sprite Animator")]
    static void Init()
    {
        // Window Set-Up
        AutoAnimation window = EditorWindow.GetWindow(typeof(AutoAnimation), false, "AnimationGenerator", true) as AutoAnimation;
        window.minSize = new Vector2(260, 170); window.maxSize = new Vector2(260, 170);
        window.Show();
    }

    //Show UI
    void OnGUI()
    {
        animator = EditorGUILayout.ObjectField(animator, typeof(Animator), true) as Animator;
        animationClip = EditorGUILayout.ObjectField(animationClip, typeof(AnimationClip), false) as AnimationClip;
        bones = EditorGUILayout.ObjectField(bones, typeof(Transform), true) as Transform;

        EditorGUILayout.Space();

        if (animator && animationClip && bones)
        {
            if (GUILayout.Button("Generate Keyframe"))
            {
                makeAnimation();
            }
        }

        Repaint();
    }

    private void makeAnimation()
    {
        string path = AnimationUtility.CalculateTransformPath(bones, animator.transform);


        MakeKey(path, "localEulerAnglesRaw.x", bones.localEulerAngles.x);
        MakeKey(path, "localEulerAnglesRaw.y", bones.localEulerAngles.y);
        MakeKey(path, "localEulerAnglesRaw.z", bones.localEulerAngles.z);

        /*Undo.RecordObject(animationClip, "Transform position from scene");

        binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x");
        curve = AnimationUtility.GetEditorCurve(animationClip, binding);
        curve.keys[0].value = bones.localPosition.x;
        AnimationUtility.SetEditorCurve(animationClip, binding, curve);

        binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y");
        curve = AnimationUtility.GetEditorCurve(animationClip, binding);
        curve.keys[0].value = bones.localPosition.y;
        AnimationUtility.SetEditorCurve(animationClip, binding, curve);

        binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z");
        curve = AnimationUtility.GetEditorCurve(animationClip, binding);
        curve.keys[0].value = bones.localPosition.z;
        AnimationUtility.SetEditorCurve(animationClip, binding, curve);*/

        //AssetDatabase.CreateAsset(clip, "Assets/Animations/Animations/test2.anim");
    }

    void MakeKey(string path, string propertyName, float value)
    {
        EditorCurveBinding[] arrBindings = AnimationUtility.GetCurveBindings(animationClip);
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);

        AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, binding);

        for (int i = 0; i < arrBindings.Length; i++)
        {
            if (arrBindings[i].path == binding.path && arrBindings[i].propertyName == binding.propertyName)
                curve = AnimationUtility.GetEditorCurve(animationClip, arrBindings[i]);
        }

        if (curve == null)
        {
            Debug.LogWarning("Couldn't find curve for path " + path + " and property " + propertyName);
            return;
        }
        //Keyframe key = new Keyframe();
        //key.value = value;
        //curve.AddKey(key);
        //curve.MoveKey(curve.keys.Length, key);
        curve.AddKey(curve.keys[curve.keys.Length -1].time + .05f, value);
        AnimationUtility.SetEditorCurve(animationClip, binding, curve);

        //animationClip.SetCurve(path, typeof(Transform), propertyName, curve);
    }
}