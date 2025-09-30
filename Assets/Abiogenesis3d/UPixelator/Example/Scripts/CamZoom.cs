using UnityEngine;

namespace Abiogenesis3d.UPixelator_Demo
{
[ExecuteInEditMode]
public class CamZoom : MonoBehaviour
{

    public float distance = 10;
    public float rawDistance;

    public float distanceMin = 5;
    public float distanceMax = 20;

    public float sensitivity = 10;
    public float lerpSpeed = 10;

    void Start()
    {
        rawDistance = distance;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0) rawDistance -= scrollDelta * sensitivity;
        rawDistance = Mathf.Clamp(rawDistance, distanceMin, distanceMax);

        distance = Mathf.Lerp(distance, rawDistance, lerpSpeed * dt);
    }
}
}
