using UnityEngine;

namespace Abiogenesis3d.UPixelator_Demo
{
    public class PhysicsMover : MonoBehaviour
    {
        public float speed = 5;
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;

            Vector3 dir = new Vector3(1, 0, 1);
            Vector3 newPosition = rb.position + dir * speed * dt;

            rb.MovePosition(newPosition);
        }
    }
}
