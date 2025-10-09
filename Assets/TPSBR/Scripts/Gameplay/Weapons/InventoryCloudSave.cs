using System;
using System.Collections.Generic;
using System.Reflection;
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

                    bool steamSignedIn = await TrySignInWithSteamAsync();

                    if (steamSignedIn == false)
                    {
                        Debug.LogWarning("Falling back to anonymous Unity Authentication session for Cloud Save.");
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    }
                }

                return AuthenticationService.Instance.IsAuthorized;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private static async Task<bool> TrySignInWithSteamAsync()
        {
            try
            {
                string sessionTicket = SteamTicketProvider.TryCreateSessionTicket();

                if (string.IsNullOrEmpty(sessionTicket) == true)
                    return false;

                await AuthenticationService.Instance.SignInWithExternalTokenAsync("steam", sessionTicket);
                return AuthenticationService.Instance.IsAuthorized;
            }
            catch (AuthenticationException exception)
            {
                Debug.LogException(exception);
            }
            catch (RequestFailedException exception)
            {
                Debug.LogException(exception);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return false;
        }

        private static class SteamTicketProvider
        {
            private static readonly Type _steamUserType = Type.GetType("Steamworks.SteamUser, Assembly-CSharp-firstpass") ??
                                                          Type.GetType("Steamworks.SteamUser, Steamworks.NET") ??
                                                          Type.GetType("Steamworks.SteamUser");

            private static readonly MethodInfo _getAuthSessionTicketMethod = _steamUserType?.GetMethod(
                "GetAuthSessionTicket",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(byte[]), typeof(int), typeof(uint).MakeByRefType() },
                null);

            private static readonly MethodInfo _cancelAuthTicketMethod = _steamUserType?.GetMethod(
                "CancelAuthTicket",
                BindingFlags.Public | BindingFlags.Static);

            private static bool _steamUnavailableLogged;

            public static string TryCreateSessionTicket()
            {
                if (_steamUserType == null || _getAuthSessionTicketMethod == null)
                {
                    if (_steamUnavailableLogged == false)
                    {
                        Debug.LogWarning("Steamworks session ticket API was not found. Ensure Steamworks.NET is installed and initialized before attempting Steam authentication.");
                        _steamUnavailableLogged = true;
                    }
                    return null;
                }

                byte[] ticketBuffer = new byte[1024];
                object[] parameters = { ticketBuffer, ticketBuffer.Length, 0u };

                try
                {
                    object authTicket = _getAuthSessionTicketMethod.Invoke(null, parameters);
                    uint ticketSize = (uint)parameters[2];

                    if (ticketSize == 0)
                    {
                        if (_steamUnavailableLogged == false)
                        {
                            Debug.LogWarning("Steam returned an empty authentication ticket. Confirm that Steam is running and the user is logged in before launching the game.");
                            _steamUnavailableLogged = true;
                        }
                        return null;
                    }

                    string ticket = Convert.ToBase64String(ticketBuffer, 0, (int)ticketSize);

                    if (authTicket != null && _cancelAuthTicketMethod != null)
                    {
                        try
                        {
                            _cancelAuthTicketMethod.Invoke(null, new[] { authTicket });
                        }
                        catch (Exception cancelException)
                        {
                            Debug.LogWarning(cancelException.Message);
                        }
                    }

                    return ticket;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(exception.Message);
                    return null;
                }
            }
        }
    }
}
