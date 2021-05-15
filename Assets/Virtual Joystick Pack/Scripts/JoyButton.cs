using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoyButton : MonoBehaviour, IPointerDownHandler
{

#if UNITY_STANDALONE || UNITY_WEBPLAYER
    void Start()
    {
        this.gameObject.SetActive(false);
        this.enabled = false;
    }
#endif

    [HideInInspector]
    public bool Pressed;


    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
    }

    private void LateUpdate()
    {
        Pressed = false;
    }
}
