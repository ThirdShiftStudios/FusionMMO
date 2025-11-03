using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public sealed partial class LocationBehavior : ContextBehaviour
    {
        [SerializeField]
        private LocationDefinition _locationDefinition;

        [SerializeField]
        private Collider[] _colliders;

        private readonly List<LocationTriggerProxy> _proxies = new List<LocationTriggerProxy>();

        public override void Spawned()
        {
            base.Spawned();
            RegisterProxies();
        }
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            UnregisterProxies();
        }

        public override void FixedUpdateNetwork()
        {
            // Location changes are handled by LocationTriggerProxy.OnEnter to avoid
            // performing per-frame proximity checks.
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
            AssignLocation(player);
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

        private void AssignLocation(Player player)
        {
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

            string playerName = string.IsNullOrEmpty(player.Nickname) ? "Unknown" : player.Nickname;
            string locationName = string.IsNullOrEmpty(_locationDefinition.Name) ? locationId.ToString() : _locationDefinition.Name;

            Debug.Log($"Player '{playerName}' entered location '{locationName}' (ID {locationId}).", this);

            AnnounceLocationEntry(playerName, locationName);
        }

        private void AnnounceLocationEntry(string playerName, string locationName)
        {
            if (Context == null)
            {
                return;
            }

            Announcer announcer = Context.Announcer;
            if (announcer == null)
            {
                return;
            }

            AnnouncementData announcement = new AnnouncementData
            {
                Channel = EAnnouncementChannel.None,
                TextMessage = $"{playerName} entered {locationName}",
                Color = Color.white,
            };

            announcer.Announce?.Invoke(announcement);
        }
    }
}
