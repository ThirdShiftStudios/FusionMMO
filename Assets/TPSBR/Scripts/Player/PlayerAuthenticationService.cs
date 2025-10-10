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

                // IGlobalService INTERFACE

                void IGlobalService.Initialize()
                {
                        try
                        {
                                AuthenticateInternal();
                        }
                        catch (Exception exception)
                        {
                                Debug.LogWarning("Failed to authenticate with Steam.");
                                Debug.LogException(exception);
                        }
                        finally
                        {
                                IsInitialized = true;
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
                        IsInitialized = false;

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
                                return AuthenticateInternal();
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

                        return PlayerId;
                }
        }
}
