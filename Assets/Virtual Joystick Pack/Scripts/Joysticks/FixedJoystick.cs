using UnityEngine;
using UnityEngine.EventSystems;

public class FixedJoystick : Joystick
{
    Vector2 joystickPosition = Vector2.zero;
    private Camera cam = new Camera();

    void Start()
    {
#if UNITY_STANDALONE || UNITY_WEBPLAYER
        this.gameObject.SetActive(false);
        this.enabled = false;
#endif
        joystickPosition = RectTransformUtility.WorldToScreenPoint(cam, background.position);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        Vector2 direction = eventData.position - joystickPosition;
        inputVector = (direction.magnitude > background.sizeDelta.x * handleLimit / f_movementScale)
            ? direction.normalized : direction / (background.sizeDelta.x * handleLimit / f_movementScale);
        ClampJoystick();
        handle.anchoredPosition = (inputVector * background.sizeDelta.x * handleLimit);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}