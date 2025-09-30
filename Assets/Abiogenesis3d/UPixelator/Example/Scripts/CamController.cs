using UnityEngine;

namespace Abiogenesis3d.UPixelator_Demo
{
    [RequireComponent(typeof(CamZoom))]
    [RequireComponent(typeof(CamRotate))]
    [ExecuteInEditMode]
    public class CamController : MonoBehaviour
    {
        public Camera cam;
        public Transform target;

        // NOTE: some shaders look bad when cam is too close, add extra distance only to orthographic
        public float extraOrthoOffset;

        CamZoom camZoom;
        CamRotate camRotate;

        public float offsetY = 1;

        void Start()
        {
            camZoom = GetComponent<CamZoom>();
            camRotate = GetComponent<CamRotate>();
            cam = Camera.main;
        }

        void LateUpdate()
        {
            if (!cam || !target) return;

            Vector3 camTargetPos = target.position + Vector3.up * offsetY;
            cam.transform.rotation = camRotate.value;

            cam.transform.position = camTargetPos -cam.transform.forward * camZoom.rawDistance;

            var halfFrustumHeight = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.orthographicSize = camZoom.rawDistance * halfFrustumHeight;

            if (cam.orthographic)
                cam.transform.position -= cam.transform.forward * extraOrthoOffset;
        }
    }
}
