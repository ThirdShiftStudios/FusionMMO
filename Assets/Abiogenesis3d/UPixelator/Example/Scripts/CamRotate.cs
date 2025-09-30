using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Abiogenesis3d.UPixelator_Demo
{
[ExecuteInEditMode]
public class CamRotate : MonoBehaviour
{
    [HideInInspector]
    public Quaternion value;

    [Range(0, 100)]
    public int dragRotateBuffer = 20;
    Vector2 startRotateMousePosition;
    bool isRotating;

    public MouseButton rotateMouseButton = MouseButton.Right;

    Camera cam;

    Vector3 eulerAngles;

    public float minAngleX = 10;
    public float maxAngleX = 89;

    public float rotationSpeed = 200;

    // TODO: move to module
    // public Vector2 mousePosition;

    void Start()
    {
        cam = Camera.main;

        eulerAngles = cam.transform.eulerAngles;
        Rotate();
    }

    void Update()
    {
        if (!Application.isPlaying) isRotating = true;

        if (IsRotateButtonPressedThisFrame())
        {
            startRotateMousePosition = GetMousePosition();
        }
        else if (IsRotateButtonPressed())
        {
            if (Vector2.Distance(startRotateMousePosition, GetMousePosition()) > dragRotateBuffer)
                isRotating = true;
        }
        else if (IsRotateButtonReleasedThisFrame())
        {
            isRotating = false;
        }

        if (isRotating) Rotate();
    }

    void Rotate()
    {
        float dt = Time.deltaTime;

        Vector2 delta = GetMouseDelta();
        eulerAngles.y += delta.x * rotationSpeed * dt;
        eulerAngles.x -= delta.y * rotationSpeed * dt;

#if UNITY_EDITOR
        if (!Application.isPlaying && cam) eulerAngles = cam.transform.eulerAngles;
#endif
        eulerAngles.x = ClampAngle(eulerAngles.x, minAngleX, maxAngleX);

        value = Quaternion.Euler(eulerAngles);
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        return Mathf.Clamp(angle % 360, min, max);
    }

    Vector2 GetMousePosition()
    {
        var mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
    }

    Vector2 GetMouseDelta()
    {
        var mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }

    bool IsRotateButtonPressed()
    {
        return GetRotateButtonControl()?.isPressed ?? false;
    }

    bool IsRotateButtonPressedThisFrame()
    {
        return GetRotateButtonControl()?.wasPressedThisFrame ?? false;
    }

    bool IsRotateButtonReleasedThisFrame()
    {
        return GetRotateButtonControl()?.wasReleasedThisFrame ?? false;
    }

    ButtonControl GetRotateButtonControl()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return null;

        return rotateMouseButton switch
        {
            MouseButton.Left => mouse.leftButton,
            MouseButton.Right => mouse.rightButton,
            MouseButton.Middle => mouse.middleButton,
            MouseButton.Forward => mouse.forwardButton,
            MouseButton.Back => mouse.backButton,
            _ => null
        };
    }
}
}
