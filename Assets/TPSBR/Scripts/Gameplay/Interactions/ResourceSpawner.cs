using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class ResourceSpawner : NetworkBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private ResourceNode _resourcePrefab;
        [SerializeField, Tooltip("Delay between node despawn and the next spawn attempt.")]
        private float _respawnDelay = 5f;
        [SerializeField, Tooltip("Additional offset applied to the raycast start position.")]
        private Vector3 _raycastOffset = Vector3.up;
        [SerializeField, Tooltip("Maximum distance for the downward raycast.")]
        private float _raycastDistance = 100f;
        [SerializeField, Tooltip("Layers considered valid ground for the resource node.")]
        private LayerMask _groundMask = Physics.DefaultRaycastLayers;

        [Networked] private ResourceNode ActiveNode { get; set; }
        [Networked] private TickTimer RespawnTimer { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority == true && ActiveNode == null)
            {
                TrySpawnNode();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            if (ActiveNode != null)
            {
                if (ActiveNode.Object != null && ActiveNode.Object.IsValid == true)
                    return;

                ActiveNode = null;
                StartRespawnTimer();
                return;
            }

            if (RespawnTimer.IsRunning == true && RespawnTimer.Expired(Runner) == false)
                return;

            TrySpawnNode();
        }

        private void TrySpawnNode()
        {
            if (_resourcePrefab == null)
                return;

            if (Runner == null)
                return;

            Vector3 origin = transform.position + _raycastOffset;
            var physicsScene = Runner.GetPhysicsScene();

            if (physicsScene.Raycast(origin, Vector3.down, out RaycastHit hit, _raycastDistance, _groundMask, QueryTriggerInteraction.Ignore) == false)
            {
                StartRespawnTimer();
                return;
            }

            Vector3 spawnPosition = hit.point;
            Quaternion spawnRotation = transform.rotation;

            ActiveNode = Runner.Spawn(_resourcePrefab, spawnPosition, spawnRotation);

            if (ActiveNode == null || ActiveNode.Object == null || ActiveNode.Object.IsValid == false)
            {
                ActiveNode = null;
                StartRespawnTimer();
                return;
            }

            RespawnTimer = default;
        }

        private void StartRespawnTimer()
        {
            if (Runner == null || _respawnDelay <= 0f)
            {
                RespawnTimer = default;
                return;
            }

            RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
        }
    }
}
