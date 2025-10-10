using System;
using Steamworks;
using UnityEngine;

namespace TPSBR
{
        public class PlayerAuthenticationService : IGlobalService
        {
                // PUBLIC MEMBERS

                public string PlayerId { get; private set; }
                public bool IsAuthenticated => string.IsNullOrEmpty(PlayerId) == false;
                public bool IsInitialized  { get; private set; }

                // PRIVATE MEMBERS

                private bool _steamInitialized;
                private bool _hasLoggedAuthenticationFailure;

                // IGlobalService INTERFACE

                void IGlobalService.Initialize()
                {
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

                        if (_steamInitialized == true)
                        {
                                SteamAPI.RunCallbacks();
                        }
                }

                void IGlobalService.Deinitialize()
                {
                        PlayerId = default;
                        IsInitialized = false;
                        _hasLoggedAuthenticationFailure = false;

                        if (_steamInitialized == true)
                        {
                                SteamAPI.Shutdown();
                                _steamInitialized = false;
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

                private string AuthenticateInternal()
                {
                        if (_steamInitialized == true && string.IsNullOrEmpty(PlayerId) == false)
                        {
                                return PlayerId;
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

                        return PlayerId;
                }

                private bool TryAuthenticate(bool logFailure, bool throwOnFailure)
                {
                        try
                        {
                                AuthenticateInternal();
                                return true;
                        }
                        catch (Exception exception)
                        {
                                IsInitialized = false;

                                if (logFailure == true && _hasLoggedAuthenticationFailure == false)
                                {
                                        Debug.LogWarning("Failed to authenticate with Steam.");
                                        Debug.LogException(exception);
                                        _hasLoggedAuthenticationFailure = true;
                                }

                                if (throwOnFailure == true)
                                {
                                        throw;
                                }

                                return false;
                        }
                }
        }
}
