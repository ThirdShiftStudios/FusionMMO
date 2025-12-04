using System;
using Fusion.Addons.KCC;

namespace TPSBR
{
    using UnityEngine;
    using Fusion.Addons.AnimationController;

    [Serializable]
    public sealed class MoveStateCategories
    {
        [SerializeField]
        private WeaponSize _weaponSize;
        [SerializeField]
        private MoveState _move;

        public WeaponSize WeaponSize => _weaponSize;
        public MoveState Move => _move;
    }

    public sealed class LocomotionLayer : AnimationLayer
    {
        // PRIVATE MEMBERS

        [SerializeField]
        private MoveState _move;
        [SerializeField]
        private MoveState _swim;
        [SerializeField]
        private MoveStateCategories[] _moveStateCategories;

        private KCC _kcc;
        private Agent _agent;
        private WeaponSize _lastWeaponSize = WeaponSize.Unknown;
        private bool _isSwimming;

        // AnimationState INTERFACE

        protected override void OnInitialize()
        {
            _kcc = Controller.GetComponentNoAlloc<KCC>();
            _agent = Controller.GetComponentNoAlloc<Agent>();

            if (_move != null)
            {
                _move.SetAnimationCategory(AnimationCategory.UseFirstSet);
            }

            if (_swim != null)
            {
                _swim.SetAnimationCategory(AnimationCategory.UseFirstSet);
            }

            if (_moveStateCategories != null)
            {
                for (int i = 0; i < _moveStateCategories.Length; i++)
                {
                    _moveStateCategories[i].Move.SetAnimationCategory(AnimationCategory.UseFirstSet);
                }
            }
        }

        protected override void OnSpawned()
        {
            ResetStateTracking();
        }

        protected override void OnDespawned()
        {
            ResetStateTracking();
        }

        protected override void OnFixedUpdate()
        {
            KCCData kccData = _kcc.FixedData;
            bool isSwimming = kccData.IsSwimming;

            if (isSwimming == true)
            {
                if (_isSwimming == false)
                {
                    _isSwimming = true;

                    DeactivateWeaponMoveStates(0.0f);

                    if (_swim != null)
                    {
                        _swim.Activate(0.0f);
                    }
                }

                return;
            }

            if (_isSwimming == true)
            {
                _isSwimming = false;

                if (_swim != null)
                {
                    _swim.Deactivate(0.0f);
                }

                _lastWeaponSize = WeaponSize.Unknown;
            }

            WeaponSize currentWeaponSize = _agent.Inventory.CurrentWeaponSize;
            if (_lastWeaponSize == currentWeaponSize)
            {
                return;
            }

            _lastWeaponSize = currentWeaponSize;

            DeactivateWeaponMoveStates(0.0f);

            bool found = false;

            if (_moveStateCategories != null)
            {
                for (int i = 0; i < _moveStateCategories.Length; i++)
                {
                    if (_moveStateCategories[i].WeaponSize == currentWeaponSize)
                    {
                        _moveStateCategories[i].Move.Activate(0.0f);
                        found = true;
                        continue;
                    }
                }
            }

            if (found == false)
            {
                Debug.LogError($"[LocomotionLayer]: Could not find MoveState for WeaponSize == {currentWeaponSize}");
            }
        }

        private void DeactivateWeaponMoveStates(float blendTime)
        {
            if (_move != null)
            {
                _move.Deactivate(blendTime);
            }

            if (_moveStateCategories == null)
            {
                return;
            }

            for (int i = 0; i < _moveStateCategories.Length; i++)
            {
                MoveState move = _moveStateCategories[i].Move;
                if (move != null)
                {
                    move.Deactivate(blendTime);
                }
            }
        }

        private void ResetStateTracking()
        {
            _lastWeaponSize = WeaponSize.Unknown;
            _isSwimming = false;

            DeactivateWeaponMoveStates(0.0f);

            if (_swim != null)
            {
                _swim.Deactivate(0.0f);
            }
        }
    }
}
