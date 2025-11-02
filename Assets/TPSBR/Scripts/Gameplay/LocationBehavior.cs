using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public sealed class LocationBehavior : ContextBehaviour
    {
        [SerializeField]
        private LocationDefinition _locationDefinition;

        [SerializeField]
        private Collider[] _colliders = Array.Empty<Collider>();

        private readonly List<LocationTriggerProxy> _proxies = new List<LocationTriggerProxy>();

        private void OnEnable()
        {
            RegisterProxies();
        }

        private void OnDisable()
        {
            UnregisterProxies();
        }

        internal void HandleAgentEntered(Agent agent)
        {
            if (agent == null)
            {
                return;
            }

            if (_locationDefinition == null)
            {
                return;
            }

            if (Runner != null && HasStateAuthority == false)
            {
                return;
            }

            if (Context == null || Context.NetworkGame == null)
            {
                return;
            }

            Player player = Context.NetworkGame.GetPlayer(agent.Object.InputAuthority);
            if (player == null)
            {
                return;
            }

            int locationId = _locationDefinition.ID;
            if (player.CurrentLocationID == locationId)
            {
                return;
            }

            player.CurrentLocationID = locationId;
        }

        private void RegisterProxies()
        {
            UnregisterProxies();

            if (_colliders == null || _colliders.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _colliders.Length; ++i)
            {
                Collider collider = _colliders[i];
                if (collider == null)
                {
                    continue;
                }

                LocationTriggerProxy proxy = collider.GetComponent<LocationTriggerProxy>();
                if (proxy == null)
                {
                    proxy = collider.gameObject.AddComponent<LocationTriggerProxy>();
                }

                proxy.AddListener(this);
                _proxies.Add(proxy);
            }
        }

        private void UnregisterProxies()
        {
            if (_proxies.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _proxies.Count; ++i)
            {
                _proxies[i]?.RemoveListener(this);
            }

            _proxies.Clear();
        }

        private sealed class LocationTriggerProxy : MonoBehaviour
        {
            private readonly List<LocationBehavior> _listeners = new List<LocationBehavior>();

            public void AddListener(LocationBehavior behavior)
            {
                if (behavior == null)
                {
                    return;
                }

                if (_listeners.Contains(behavior) == false)
                {
                    _listeners.Add(behavior);
                }
            }

            public void RemoveListener(LocationBehavior behavior)
            {
                if (behavior == null)
                {
                    return;
                }

                _listeners.Remove(behavior);
            }

            private void OnTriggerEnter(Collider other)
            {
                if (_listeners.Count == 0)
                {
                    return;
                }

                Agent agent = other.GetComponentInParent<Agent>();
                if (agent == null)
                {
                    return;
                }

                for (int i = 0; i < _listeners.Count; ++i)
                {
                    _listeners[i]?.HandleAgentEntered(agent);
                }
            }
        }
    }
}
