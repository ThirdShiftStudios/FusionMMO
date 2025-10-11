using System;
using System.IO;
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
                public string UnityProfileName => _unityProfileName;

                // PRIVATE MEMBERS

                private bool _steamInitialized;
                private bool _hasLoggedAuthenticationFailure;
                private AuthenticationMode _authenticationMode = AuthenticationMode.Steam;
                private Task _unityAuthenticationTask;
                private string _unityProfileName;

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
                        _unityProfileName = null;

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

                        if (string.IsNullOrEmpty(_unityProfileName) == false && AuthenticationService.Instance.Profile != _unityProfileName)
                        {
                                if (AuthenticationService.Instance.IsSignedIn == true)
                                {
                                        AuthenticationService.Instance.SignOut();
                                }

                                AuthenticationService.Instance.SwitchProfile(_unityProfileName);
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
                                _unityProfileName = DetermineUnityProfileNameForClone();
                                _authenticationMode = AuthenticationMode.UnityAnonymous;
                                return;
                        }
#endif

                        _unityProfileName = null;
                        _authenticationMode = AuthenticationMode.Steam;
                }

#if UNITY_EDITOR
                private string DetermineUnityProfileNameForClone()
                {
                        var projectPath = Path.GetDirectoryName(Application.dataPath);

                        if (string.IsNullOrEmpty(projectPath) == true)
                        {
                                return null;
                        }

                        var projectFolder = Path.GetFileName(projectPath);

                        return SanitizeProfileName(projectFolder);
                }
#endif

                private static string SanitizeProfileName(string profileName)
                {
                        if (string.IsNullOrEmpty(profileName) == true)
                        {
                                return null;
                        }

                        Span<char> buffer = stackalloc char[Math.Min(profileName.Length, 30)];
                        int index = 0;

                        for (int i = 0; i < profileName.Length && index < buffer.Length; i++)
                        {
                                char character = profileName[i];

                                if (char.IsLetterOrDigit(character) == false && character != '-' && character != '_')
                                {
                                        continue;
                                }

                                buffer[index++] = character;
                        }

                        if (index == 0)
                        {
                                return null;
                        }

                        return new string(buffer.Slice(0, index));
                }
        }
}
