using UnityEngine;

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

    public KeyCode rotateKey = KeyCode.Mouse1;

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

        if (Input.GetKeyDown(rotateKey))
        {
            startRotateMousePosition = Input.mousePosition;
        }
        else if (Input.GetKey(rotateKey))
        {
            if (Vector2.Distance(startRotateMousePosition, Input.mousePosition) > dragRotateBuffer)
                isRotating = true;
        }
        else if (Input.GetKeyUp(rotateKey))
        {
            isRotating = false;
        }

        if (isRotating) Rotate();
    }

    void Rotate()
    {
        float dt = Time.deltaTime;

        eulerAngles.y += Input.GetAxis("Mouse X") * rotationSpeed * dt;
        eulerAngles.x -= Input.GetAxis("Mouse Y") * rotationSpeed * dt;

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
}
}
