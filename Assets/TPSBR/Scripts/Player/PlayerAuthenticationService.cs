using System;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace TPSBR
{
        public class PlayerAuthenticationService : IGlobalService
        {
                // PUBLIC MEMBERS

                public string PlayerId { get; private set; }
                public bool IsAuthenticated => string.IsNullOrEmpty(PlayerId) == false;

                // PRIVATE MEMBERS

                private Task<string> _authenticationTask;
                private bool _steamInitialized;

                // IGlobalService INTERFACE

                async void IGlobalService.Initialize()
                {
                        try
                        {
                                await GetPlayerIdAsync();
                        }
                        catch (Exception exception)
                        {
                                Debug.LogWarning("Failed to authenticate with Steam.");
                                Debug.LogException(exception);
                        }
                }

                void IGlobalService.Tick()
                {
                        if (_steamInitialized == true)
                        {
                                SteamAPI.RunCallbacks();
                        }
                }

                void IGlobalService.Deinitialize()
                {
                        PlayerId = default;
                        _authenticationTask = null;

                        if (_steamInitialized == true)
                        {
                                SteamAPI.Shutdown();
                                _steamInitialized = false;
                        }
                }

                // PUBLIC METHODS

                public Task<string> GetPlayerIdAsync()
                {
                        if (_authenticationTask == null || _authenticationTask.IsFaulted == true || _authenticationTask.IsCanceled == true)
                        {
                                _authenticationTask = AuthenticateInternalAsync();
                        }

                        return _authenticationTask;
                }

                // PRIVATE METHODS

                private async Task<string> AuthenticateInternalAsync()
                {
                        if (_steamInitialized == true && string.IsNullOrEmpty(PlayerId) == false)
                        {
                                return PlayerId;
                        }

                        await Task.Yield();

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

                        return PlayerId;
                }
        }
}
