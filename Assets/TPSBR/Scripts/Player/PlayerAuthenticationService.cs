using System;
using System.Threading.Tasks;
using Steamworks;
using Unity.Services.Authentication;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif

namespace TPSBR
{
        public class PlayerAuthenticationService : IGlobalService
        {
                private enum AuthenticationMode
                {
                        Steam,
                        UnityAnonymous,
                }

                // PUBLIC MEMBERS

                public string PlayerId { get; private set; }
                public bool IsAuthenticated => string.IsNullOrEmpty(PlayerId) == false;
                public bool IsInitialized  { get; private set; }

                // PRIVATE MEMBERS

                private bool _steamInitialized;
                private bool _hasLoggedAuthenticationFailure;
                private AuthenticationMode _authenticationMode = AuthenticationMode.Steam;
                private Task _unityAuthenticationTask;

                // IGlobalService INTERFACE

                void IGlobalService.Initialize()
                {
                        DetermineAuthenticationMode();
                        TryAuthenticate(logFailure: true, throwOnFailure: false);
                }

                void IGlobalService.Tick()
                {
                        if (IsInitialized == false)
                        {
                                if (TryAuthenticate(logFailure: true, throwOnFailure: false) == false)
                                {
                                        return;
                                }
                        }

                        if (_authenticationMode == AuthenticationMode.Steam && _steamInitialized == true)
                        {
                                SteamAPI.RunCallbacks();
                        }
                }

                void IGlobalService.Deinitialize()
                {
                        PlayerId = default;
                        IsInitialized = false;
                        _hasLoggedAuthenticationFailure = false;
                        _unityAuthenticationTask = null;

                        if (_authenticationMode == AuthenticationMode.Steam)
                        {
                                if (_steamInitialized == true)
                                {
                                        SteamAPI.Shutdown();
                                        _steamInitialized = false;
                                }
                        }
                        else
                        {
                                if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn == true)
                                {
                                        AuthenticationService.Instance.SignOut();
                                }
                        }
                }

                // PUBLIC METHODS

                public string GetPlayerId()
                {
                        if (IsAuthenticated == false)
                        {
                                TryAuthenticate(logFailure: false, throwOnFailure: true);
                        }

                        return PlayerId;
                }

                // PRIVATE METHODS

                private bool TryAuthenticate(bool logFailure, bool throwOnFailure)
                {
                        Exception failure = null;

                        switch (_authenticationMode)
                        {
                                case AuthenticationMode.Steam:
                                {
                                        try
                                        {
                                                AuthenticateWithSteam();
                                                return true;
                                        }
                                        catch (Exception exception)
                                        {
                                                failure = exception;
                                        }

                                        break;
                                }

                                case AuthenticationMode.UnityAnonymous:
                                {
                                        if (_unityAuthenticationTask == null)
                                        {
                                                _unityAuthenticationTask = AuthenticateWithUnityAsync();
                                        }

                                        if (_unityAuthenticationTask.IsCompleted == false)
                                        {
                                                return false;
                                        }

                                        if (_unityAuthenticationTask.IsFaulted == true || _unityAuthenticationTask.IsCanceled == true)
                                        {
                                                failure = _unityAuthenticationTask.Exception?.GetBaseException() ?? new InvalidOperationException("Unity authentication failed.");
                                                _unityAuthenticationTask = null;
                                        }
                                        else
                                        {
                                                return IsAuthenticated;
                                        }

                                        break;
                                }
                        }

                        if (failure != null)
                        {
                                IsInitialized = false;

                                if (logFailure == true && _hasLoggedAuthenticationFailure == false)
                                {
                                        Debug.LogWarning(_authenticationMode == AuthenticationMode.Steam ? "Failed to authenticate with Steam." : "Failed to authenticate with Unity Services.");
                                        Debug.LogException(failure);
                                        _hasLoggedAuthenticationFailure = true;
                                }

                                if (throwOnFailure == true)
                                {
                                        throw failure;
                                }
                        }

                        return false;
                }

                private void AuthenticateWithSteam()
                {
                        if (_steamInitialized == true && string.IsNullOrEmpty(PlayerId) == false)
                        {
                                return;
                        }

                        if (_steamInitialized == false)
                        {
                                if (SteamAPI.Init() == false)
                                {
                                        throw new InvalidOperationException("SteamAPI initialization failed.");
                                }

                                _steamInitialized = true;
                        }

                        var steamId = SteamUser.GetSteamID().ToString();

                        if (string.IsNullOrEmpty(steamId) == true)
                        {
                                throw new InvalidOperationException("Unable to retrieve SteamID for local user.");
                        }

                        PlayerId = steamId;
                        IsInitialized = true;
                        _hasLoggedAuthenticationFailure = false;
                }

                private async Task AuthenticateWithUnityAsync()
                {
                        await Global.UnityServicesInitialization;

                        if (AuthenticationService.Instance == null)
                        {
                                throw new InvalidOperationException("Unity Authentication service is unavailable.");
                        }

                        if (AuthenticationService.Instance.IsSignedIn == false)
                        {
                                await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
                        }

                        var playerId = AuthenticationService.Instance.PlayerId;

                        if (string.IsNullOrEmpty(playerId) == true)
                        {
                                throw new InvalidOperationException("Unable to retrieve Unity Authentication player identifier.");
                        }

                        PlayerId = playerId;
                        IsInitialized = true;
                        _hasLoggedAuthenticationFailure = false;
                }

                private void DetermineAuthenticationMode()
                {
#if UNITY_EDITOR
                        if (ClonesManager.IsClone() == true)
                        {
                                _authenticationMode = AuthenticationMode.UnityAnonymous;
                                return;
                        }
#endif

                        _authenticationMode = AuthenticationMode.Steam;
                }
        }
}
