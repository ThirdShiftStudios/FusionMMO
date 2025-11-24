using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    /// <summary>
    /// Manages Terrain tree instances and only spawns networked TreeNodes near active players.
    /// This avoids spawning every harvestable tree at once while still keeping visual parity with the terrain trees.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TerrainTreeManager : ContextBehaviour
    {
        private struct ManagedTree
        {
            public Vector3 WorldPosition;
            public float RotationY;
            public float WidthScale;
            public float HeightScale;
            public bool IsHiddenLocally;
        }

        public struct TreeState : INetworkStruct
        {
            public NetworkBool IsNetworked;
            public NetworkBool IsHarvested;

            public void SetNetworked(bool value) => IsNetworked = value;
            public void SetHarvested(bool value) => IsHarvested = value;
        }

        private const int MAX_TREES_PER_TILE = 1024;

        [Header("References")]
        [SerializeField]
        private Terrain _terrain;
        [SerializeField]
        private TreeNode _treeNodePrefab;

        [Header("Activation")]
        [SerializeField, Tooltip("Radius around a player where trees get converted into networked TreeNodes.")]
        private float _activationRadius = 45f;
        [SerializeField, Tooltip("Radius at which active TreeNodes are despawned and returned to terrain visuals.")]
        private float _deactivationRadius = 60f;
        [SerializeField, Tooltip("Maximum number of simultaneously networked trees for this tile.")]
        private int _maxActiveTrees = 24;
        [SerializeField, Tooltip("How often (in seconds) player proximity is evaluated.")]
        private float _evaluationInterval = 1.5f;
        [SerializeField, Tooltip("Hide the Terrain tree instance whenever a networked TreeNode is active or harvested.")]
        private bool _hideTerrainTreeWhenNetworked = true;

        [Header("Regrowth")]
        [SerializeField, Tooltip("Seconds before a harvested tree regrows and becomes available again. Set to 0 to disable regrowth.")]
        private float _regrowDuration = 120f;

        [Networked, Capacity(MAX_TREES_PER_TILE)] private NetworkArray<TreeState> _treeStates { get; }
        [Networked, Capacity(MAX_TREES_PER_TILE)] private NetworkArray<float> _regrowCompletionTimes { get; }
        [Networked] private int _managedTreeCount { get; set; }

        private readonly Dictionary<int, TreeNode> _activeTreeNodes = new Dictionary<int, TreeNode>();
        private readonly Dictionary<int, Action<Agent>> _harvestCallbacks = new Dictionary<int, Action<Agent>>();
        private ManagedTree[] _managedTrees = System.Array.Empty<ManagedTree>();
        private TreeInstance[] _originalTreeInstances = System.Array.Empty<TreeInstance>();
        private TerrainData _terrainData;
        private float _nextEvaluationTime;
        private float _activationRadiusSqr;
        private float _deactivationRadiusSqr;

        public override void Spawned()
        {
            base.Spawned();

            CacheTerrainTrees();
            _activationRadiusSqr = _activationRadius * _activationRadius;
            _deactivationRadiusSqr = _deactivationRadius * _deactivationRadius;

            if (HasStateAuthority == true)
            {
                _managedTreeCount = _managedTrees.Length;
                ResetTreeStates();
                ResetRegrowTimers();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            RestoreAllTerrainTrees();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            if (Runner.IsForward == false)
                return;

            if (_treeNodePrefab == null || _managedTreeCount == 0)
                return;

            if (Context == null || Context.NetworkGame == null)
                return;

            if (Runner.SimulationTime < _nextEvaluationTime)
                return;

            _nextEvaluationTime = (float)Runner.SimulationTime + _evaluationInterval;

            CleanupInvalidNodes();
            UpdateRegrowth();
            EvaluateActiveTrees();
        }

        public override void Render()
        {
            base.Render();

            if (_hideTerrainTreeWhenNetworked == false)
                return;

            SyncTerrainVisibility();
        }

        private void CacheTerrainTrees()
        {
            if (_terrain == null)
                _terrain = GetComponent<Terrain>();

            if (_terrain == null)
                return;

            _terrainData = _terrain.terrainData;
            if (_terrainData == null)
                return;

            _originalTreeInstances = _terrainData.treeInstances;
            int managedCount = Mathf.Min(_originalTreeInstances.Length, MAX_TREES_PER_TILE);

            if (_originalTreeInstances.Length > MAX_TREES_PER_TILE)
            {
                Debug.LogWarning($"{nameof(TerrainTreeManager)} on {_terrain.name} capped managed trees to {MAX_TREES_PER_TILE}.");
            }

            _managedTrees = new ManagedTree[managedCount];
            Vector3 terrainPosition = _terrain.transform.position;
            Vector3 terrainSize = _terrainData.size;

            for (int i = 0; i < managedCount; ++i)
            {
                TreeInstance instance = _originalTreeInstances[i];
                Vector3 worldPosition = Vector3.Scale(instance.position, terrainSize) + terrainPosition;
                _managedTrees[i] = new ManagedTree
                {
                    WorldPosition = worldPosition,
                    RotationY = instance.rotation * Mathf.Rad2Deg,
                    WidthScale = instance.widthScale,
                    HeightScale = instance.heightScale,
                    IsHiddenLocally = false,
                };
            }
        }

        private void ResetTreeStates()
        {
            for (int i = 0; i < _managedTreeCount; ++i)
            {
                SetTreeState(i, false, false);
            }
        }

        private void ResetRegrowTimers()
        {
            for (int i = 0; i < _managedTreeCount; ++i)
            {
                _regrowCompletionTimes.Set(i, 0f);
            }
        }

        private void SetTreeState(int index, bool isNetworked, bool isHarvested)
        {
            TreeState state = _treeStates[index];
            state.SetNetworked(isNetworked);
            state.SetHarvested(isHarvested);
            _treeStates.Set(index, state);
        }

        private void EvaluateActiveTrees()
        {
            List<Player> activePlayers = Context.NetworkGame.ActivePlayers;
            if (activePlayers == null || activePlayers.Count == 0)
                return;

            // Despawn nodes that moved too far or were harvested.
            var toRelease = ListPool.Get<int>(_activeTreeNodes.Count);

            foreach (var pair in _activeTreeNodes)
            {
                int treeIndex = pair.Key;
                TreeNode node = pair.Value;
                if (node == null || node.Object == null || node.Object.IsValid == false)
                {
                    SetTreeState(treeIndex, false, _treeStates[treeIndex].IsHarvested == true);
                    toRelease.Add(treeIndex);
                    continue;
                }

                float minDistanceSqr = GetClosestPlayerDistanceSqr(pair.Key, activePlayers);
                bool wasHarvested = _treeStates[treeIndex].IsHarvested == true;

                if (minDistanceSqr > _deactivationRadiusSqr || wasHarvested == true)
                {
                    toRelease.Add(treeIndex);
                }
            }

            for (int i = 0; i < toRelease.Count; ++i)
            {
                int treeIndex = toRelease[i];
                if (_activeTreeNodes.TryGetValue(treeIndex, out TreeNode node) == true)
                {
                    DespawnTreeNode(treeIndex, node);
                }
            }

            ListPool.Return(toRelease);

            // Spawn new nodes near players.
            if (_activeTreeNodes.Count >= _maxActiveTrees)
                return;

            foreach (Player player in activePlayers)
            {
                if (player == null)
                    continue;

                Agent agent = player.ActiveAgent;
                if (agent == null)
                    continue;

                int candidate = FindClosestAvailableTree(agent.transform.position);
                if (candidate < 0)
                    continue;

                SpawnTreeNode(candidate);

                if (_activeTreeNodes.Count >= _maxActiveTrees)
                    break;
            }
        }

        private void CleanupInvalidNodes()
        {
            var toRemove = ListPool.Get<int>(_activeTreeNodes.Count);

            foreach (var pair in _activeTreeNodes)
            {
                TreeNode node = pair.Value;
                if (node != null && node.Object != null && node.Object.IsValid == true)
                    continue;

                toRemove.Add(pair.Key);
            }

            for (int i = 0; i < toRemove.Count; ++i)
            {
                int index = toRemove[i];
                _activeTreeNodes.Remove(index);
                _harvestCallbacks.Remove(index);
                TreeState state = _treeStates[index];
                state.SetNetworked(false);
                _treeStates.Set(index, state);
            }

            ListPool.Return(toRemove);
        }

        private void UpdateRegrowth()
        {
            double now = Runner.SimulationTime;

            for (int i = 0; i < _managedTreeCount; ++i)
            {
                if (_treeStates[i].IsHarvested == false)
                    continue;

                float completionTime = _regrowCompletionTimes[i];
                if (completionTime <= 0f || now < completionTime)
                    continue;

                SetTreeState(i, false, false);
                _regrowCompletionTimes.Set(i, 0f);
            }
        }

        private int FindClosestAvailableTree(Vector3 position)
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _managedTreeCount; ++i)
            {
                TreeState state = _treeStates[i];

                if (state.IsHarvested == true || state.IsNetworked == true)
                    continue;

                float distanceSqr = (position - _managedTrees[i].WorldPosition).sqrMagnitude;
                if (distanceSqr > _activationRadiusSqr)
                    continue;

                if (distanceSqr < bestDistance)
                {
                    bestDistance = distanceSqr;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private float GetClosestPlayerDistanceSqr(int treeIndex, List<Player> players)
        {
            float closest = float.MaxValue;
            Vector3 treePosition = _managedTrees[treeIndex].WorldPosition;

            for (int i = 0; i < players.Count; ++i)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                Agent agent = player.ActiveAgent;
                if (agent == null)
                    continue;

                float distanceSqr = (agent.transform.position - treePosition).sqrMagnitude;
                if (distanceSqr < closest)
                {
                    closest = distanceSqr;
                }
            }

            return closest;
        }

        private void SpawnTreeNode(int treeIndex)
        {
            if (_treeNodePrefab == null)
                return;

            ManagedTree managedTree = _managedTrees[treeIndex];

            TreeNode treeNode = Runner.Spawn(_treeNodePrefab, managedTree.WorldPosition, Quaternion.Euler(0f, managedTree.RotationY, 0f), null, (runner, obj) =>
            {
                TreeNode spawnedNode = obj.GetComponent<TreeNode>();
                spawnedNode.ResetTree();
            });

            if (treeNode == null)
                return;

            Action<Agent> harvestedCallback = agent => MarkTreeHarvested(treeIndex, treeNode);
            treeNode.ChoppingCompleted += harvestedCallback;
            _harvestCallbacks[treeIndex] = harvestedCallback;

            _activeTreeNodes[treeIndex] = treeNode;

            SetTreeState(treeIndex, true, _treeStates[treeIndex].IsHarvested == true);
        }

        private void DespawnTreeNode(int treeIndex, TreeNode node)
        {
            if (_harvestCallbacks.TryGetValue(treeIndex, out Action<Agent> callback) == true)
            {
                if (node != null)
                {
                    node.ChoppingCompleted -= callback;
                }

                _harvestCallbacks.Remove(treeIndex);
            }

            if (node != null)
            {
                if (node.Object != null && node.Object.IsValid == true)
                {
                    Runner.Despawn(node.Object);
                }
            }

            _activeTreeNodes.Remove(treeIndex);

            SetTreeState(treeIndex, false, _treeStates[treeIndex].IsHarvested == true);
        }

        private void MarkTreeHarvested(int treeIndex, TreeNode node)
        {
            float regrowTime = _regrowDuration > 0f ? (float)Runner.SimulationTime + _regrowDuration : 0f;
            _regrowCompletionTimes.Set(treeIndex, regrowTime);
            SetTreeState(treeIndex, false, true);
            DespawnTreeNode(treeIndex, node);
        }

        private void SyncTerrainVisibility()
        {
            if (_terrainData == null)
                return;

            for (int i = 0; i < _managedTreeCount; ++i)
            {
                ManagedTree managedTree = _managedTrees[i];
                bool shouldHide = _treeStates[i].IsHarvested == true || _treeStates[i].IsNetworked == true;

                if (managedTree.IsHiddenLocally == shouldHide)
                    continue;

                if (shouldHide)
                {
                    HideTerrainTree(i);
                }
                else
                {
                    ShowTerrainTree(i);
                }

                managedTree.IsHiddenLocally = shouldHide;
                _managedTrees[i] = managedTree;
            }
        }

        private void HideTerrainTree(int index)
        {
            if (_terrainData == null || index < 0 || index >= _originalTreeInstances.Length)
                return;

            TreeInstance instance = _terrainData.GetTreeInstance(index);
            instance.widthScale = 0f;
            instance.heightScale = 0f;
            _terrainData.SetTreeInstance(index, instance);
        }

        private void ShowTerrainTree(int index)
        {
            if (_terrainData == null || index < 0 || index >= _originalTreeInstances.Length)
                return;

            _terrainData.SetTreeInstance(index, _originalTreeInstances[index]);
        }

        private void RestoreAllTerrainTrees()
        {
            if (_terrainData == null || _originalTreeInstances == null || _originalTreeInstances.Length == 0)
                return;

            _terrainData.SetTreeInstances(_originalTreeInstances, false);
        }
    }
}
