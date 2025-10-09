using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Inventory))]
    public sealed class InventoryCloudSave : MonoBehaviour
    {
        [SerializeField] private string _cloudSaveKey = "player.inventory";
        [SerializeField] private float _saveDelay = 1.5f;

        private Inventory _inventory;
        private bool _saveScheduled;
        private float _scheduledSaveTime;
        private bool _isSaving;
        private bool _hasRecordedInitialState;

        private void Awake()
        {
            _inventory = GetComponent<Inventory>();
        }

        private void OnEnable()
        {
            if (_inventory == null)
                return;

            _inventory.ItemSlotChanged += OnInventorySlotChanged;
            _inventory.HotbarSlotChanged += OnHotbarSlotChanged;
        }

        private void OnDisable()
        {
            if (_inventory == null)
                return;

            _inventory.ItemSlotChanged -= OnInventorySlotChanged;
            _inventory.HotbarSlotChanged -= OnHotbarSlotChanged;

            _saveScheduled = false;
            _hasRecordedInitialState = false;
        }

        private void Update()
        {
            if (_inventory == null)
                return;

            if (_hasRecordedInitialState == false && _inventory.Object != null && _inventory.HasInputAuthority == true)
            {
                _hasRecordedInitialState = true;
                ScheduleSave(true);
            }

            if (_saveScheduled == false)
                return;

            if (_inventory.Object == null || _inventory.HasInputAuthority == false)
                return;

            if (Time.unscaledTime < _scheduledSaveTime)
                return;

            _saveScheduled = false;
            _ = SaveAsync();
        }

        private void OnInventorySlotChanged(int index, InventorySlot slot)
        {
            ScheduleSave();
        }

        private void OnHotbarSlotChanged(int index, Weapon weapon)
        {
            ScheduleSave();
        }

        private void ScheduleSave(bool immediate = false)
        {
            if (_inventory == null || _inventory.Object == null)
                return;

            if (_inventory.HasInputAuthority == false)
                return;

            _saveScheduled = true;
            _scheduledSaveTime = immediate == true
                ? Time.unscaledTime
                : Time.unscaledTime + Mathf.Max(0.1f, _saveDelay);
        }

        private async Task SaveAsync()
        {
            if (_isSaving == true)
                return;

            if (_inventory == null || _inventory.Object == null || _inventory.HasInputAuthority == false)
                return;

            if (await EnsureUnityServicesAsync() == false)
                return;

            _isSaving = true;

            try
            {
                InventorySaveState saveState = _inventory.CaptureSaveState();
                string payload = JsonUtility.ToJson(saveState);

                var data = new Dictionary<string, object>
                {
                    { _cloudSaveKey, payload },
                };

                await CloudSaveService.Instance.Data.ForceSaveAsync(data);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private static async Task<bool> EnsureUnityServicesAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (AuthenticationService.Instance.IsAuthorized == false)
                {
                    AuthenticationService.Instance.ClearSessionToken();
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                return AuthenticationService.Instance.IsAuthorized;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }
    }
}
