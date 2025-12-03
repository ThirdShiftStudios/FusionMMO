namespace TPSBR
{
    using UnityEngine;
    using System.Collections;

    public sealed class MenuAgent : MonoBehaviour
    {
        [SerializeField]
        private EquipmentVisualsManager _equipmentVisuals;
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private RuntimeAnimatorController _animatorController;

        private Coroutine _initialRefreshRoutine;
        private bool _isSubscribed;

        private void Awake()
        {
            if (_equipmentVisuals == null)
            {
                _equipmentVisuals = GetComponentInChildren<EquipmentVisualsManager>(true);
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void OnEnable()
        {
            SubscribeToCloud();
            RefreshActiveCharacterVisuals();
            ApplyAnimatorController();

            if (_initialRefreshRoutine == null)
            {
                _initialRefreshRoutine = StartCoroutine(WaitForCloudInitialization());
            }
        }

        private void OnDisable()
        {
            if (_initialRefreshRoutine != null)
            {
                StopCoroutine(_initialRefreshRoutine);
                _initialRefreshRoutine = null;
            }

            UnsubscribeFromCloud();
        }

        private void SubscribeToCloud()
        {
            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null || _isSubscribed == true)
                return;

            cloud.ActiveCharacterChanged += OnActiveCharacterChanged;
            cloud.CharactersChanged += OnCharactersChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeFromCloud()
        {
            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null || _isSubscribed == false)
                return;

            cloud.ActiveCharacterChanged -= OnActiveCharacterChanged;
            cloud.CharactersChanged -= OnCharactersChanged;
            _isSubscribed = false;
        }

        private void OnActiveCharacterChanged(string characterId)
        {
            RefreshActiveCharacterVisuals();
        }

        private void OnCharactersChanged()
        {
            RefreshActiveCharacterVisuals();
        }

        private void RefreshActiveCharacterVisuals()
        {
            if (_equipmentVisuals == null)
                return;

            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null || cloud.IsInitialized == false)
            {
                _equipmentVisuals.RefreshFromInventoryData(null);
                return;
            }

            var inventory = cloud.GetActiveCharacterInventorySnapshot();
            _equipmentVisuals.RefreshFromInventoryData(inventory);
        }

        private IEnumerator WaitForCloudInitialization()
        {
            PlayerCloudSaveService cloud = null;

            while (cloud == null || cloud.IsInitialized == false)
            {
                cloud = Global.PlayerCloudSaveService;

                if (cloud != null)
                {
                    SubscribeToCloud();
                }

                yield return null;
            }

            RefreshActiveCharacterVisuals();
        }

        private void ApplyAnimatorController()
        {
            if (_animator == null || _animatorController == null)
                return;

            _animator.runtimeAnimatorController = _animatorController;
        }
    }
}
