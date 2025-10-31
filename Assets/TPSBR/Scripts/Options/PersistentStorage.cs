using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

namespace TPSBR
{
    public static class PersistentStorage
    {
        // CONSTANTS

        private const string TRUE_VALUE = "1";
        private const string FALSE_VALUE = "0";

        // PRIVATE MEMBERS

        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>(128, StringComparer.Ordinal);
        private static readonly HashSet<string> _dirtyKeys = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> _deletedKeys = new HashSet<string>(StringComparer.Ordinal);
        private static readonly object _initializationLock = new object();

        private static Task _initializationTask;
        private static bool _isInitialized;
        private static bool _cloudSaveReady;

        // PUBLIC METHODS

        public static bool GetBool(string key, bool defaultValue = false)
        {
            string storedValue = GetRawValue(key);

            if (string.IsNullOrEmpty(storedValue) == true)
                return defaultValue;

            if (string.Equals(storedValue, TRUE_VALUE, StringComparison.Ordinal))
                return true;

            if (string.Equals(storedValue, FALSE_VALUE, StringComparison.Ordinal))
                return false;

            if (bool.TryParse(storedValue, out bool parsedValue) == true)
                return parsedValue;

            return defaultValue;
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            string storedValue = GetRawValue(key);

            if (string.IsNullOrEmpty(storedValue) == true)
                return defaultValue;

            if (float.TryParse(storedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue) == true)
                return parsedValue;

            return defaultValue;
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            string storedValue = GetRawValue(key);

            if (string.IsNullOrEmpty(storedValue) == true)
                return defaultValue;

            if (int.TryParse(storedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue) == true)
                return parsedValue;

            return defaultValue;
        }

        public static string GetString(string key, string defaultValue = null)
        {
            string storedValue = GetRawValue(key);

            if (storedValue == null)
                return defaultValue;

            return storedValue;
        }

        public static T GetObject<T>(string key, T defaultValue = default)
        {
            string objectJson = GetRawValue(key);

            if (string.IsNullOrEmpty(objectJson) == true || string.Equals(objectJson, "null", StringComparison.OrdinalIgnoreCase) == true)
                return defaultValue;

            try
            {
                return JsonUtility.FromJson<T>(objectJson);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PersistentStorage.GetObject - Failed to deserialize data for key {key}.");
                Debug.LogException(exception);
                return defaultValue;
            }
        }

        public static void SetBool(string key, bool value, bool saveImmediately = true)
        {
            SetRawValue(key, value == true ? TRUE_VALUE : FALSE_VALUE, saveImmediately);
        }

        public static void SetInt(string key, int value, bool saveImmediately = true)
        {
            SetRawValue(key, value.ToString(CultureInfo.InvariantCulture), saveImmediately);
        }

        public static void SetFloat(string key, float value, bool saveImmediately = true)
        {
            SetRawValue(key, value.ToString(CultureInfo.InvariantCulture), saveImmediately);
        }

        public static void SetString(string key, string value, bool saveImmediately = true)
        {
            if (value == null)
            {
                Delete(key, saveImmediately);
                return;
            }

            SetRawValue(key, value, saveImmediately);
        }

        public static void SetObject(string key, object value, bool saveImmediately = true)
        {
            if (value == null)
            {
                Delete(key, saveImmediately);
                return;
            }

            try
            {
                string objectJson = JsonUtility.ToJson(value);
                SetRawValue(key, objectJson, saveImmediately);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PersistentStorage.SetObject - Failed to serialize value for key {key}.");
                Debug.LogException(exception);
            }
        }

        public static void Delete(string key, bool saveImmediately = true)
        {
            if (string.IsNullOrEmpty(key) == true)
                return;

            EnsureInitialized();

            _cache.Remove(key);
            _dirtyKeys.Remove(key);
            _deletedKeys.Add(key);

            if (saveImmediately == true)
            {
                SaveInternal();
            }
        }

        public static void Save()
        {
            EnsureInitialized();
            SaveInternal();
        }

        // PRIVATE METHODS

        private static string GetRawValue(string key)
        {
            if (string.IsNullOrEmpty(key) == true)
                return null;

            EnsureInitialized();

            if (_cache.TryGetValue(key, out string cachedValue) == true)
                return cachedValue;

            if (_cloudSaveReady == false)
                return null;

            try
            {
                var dataService = CloudSaveService.Instance?.Data;

                if (dataService == null)
                    return null;

                var keys = new HashSet<string> { key };
                var result = dataService.LoadAsync(keys).GetAwaiter().GetResult();

                if (result != null && result.TryGetValue(key, out var item) == true)
                {
                    string value = item?.ToString();

                    if (string.IsNullOrEmpty(value) == false)
                    {
                        _cache[key] = value;
                        return value;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PersistentStorage.GetRawValue - Failed to load key {key} from Unity Cloud Save.");
                Debug.LogException(exception);
            }

            return null;
        }

        private static void SetRawValue(string key, string value, bool saveImmediately)
        {
            if (string.IsNullOrEmpty(key) == true)
                return;

            EnsureInitialized();

            _cache[key] = value;
            _dirtyKeys.Add(key);
            _deletedKeys.Remove(key);

            if (saveImmediately == true)
            {
                SaveInternal();
            }
        }

        private static void SaveInternal()
        {
            if (_cloudSaveReady == false)
            {
                if (_dirtyKeys.Count > 0 || _deletedKeys.Count > 0)
                {
                    Debug.LogWarning("PersistentStorage.Save - Unity Cloud Save is unavailable; changes cannot be persisted.");
                }

                return;
            }

            try
            {
                var dataService = CloudSaveService.Instance?.Data;

                if (dataService == null)
                {
                    Debug.LogWarning("PersistentStorage.Save - Unity Cloud Save data service is unavailable.");
                    return;
                }

                if (_deletedKeys.Count > 0)
                {
                    var keysToDelete = new List<string>(_deletedKeys);

                    foreach (var key in keysToDelete)
                    {
                        dataService.ForceDeleteAsync(key).GetAwaiter().GetResult();
                    }

                    _deletedKeys.Clear();
                }

                if (_dirtyKeys.Count > 0)
                {
                    var data = new Dictionary<string, object>(_dirtyKeys.Count);

                    foreach (var key in _dirtyKeys)
                    {
                        if (_cache.TryGetValue(key, out string value) == true)
                        {
                            data[key] = value;
                        }
                    }

                    if (data.Count > 0)
                    {
                        dataService.ForceSaveAsync(data).GetAwaiter().GetResult();
                    }

                    _dirtyKeys.Clear();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PersistentStorage.Save - Failed to persist data to Unity Cloud Save.");
                Debug.LogException(exception);
            }
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized == true)
                return;

            lock (_initializationLock)
            {
                if (_initializationTask == null)
                {
                    _initializationTask = InitializeAsync();
                }
            }

            try
            {
                if (_initializationTask != null)
                {
                    _initializationTask.GetAwaiter().GetResult();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PersistentStorage - Initialization failed.");
                Debug.LogException(exception);
            }
        }

        private static async Task InitializeAsync()
        {
            try
            {
                await Global.UnityServicesInitialization;
                await EnsureSignedInAsync();

                _cloudSaveReady = AuthenticationService.Instance != null &&
                                  AuthenticationService.Instance.IsSignedIn == true &&
                                  CloudSaveService.Instance != null;
            }
            catch (Exception exception)
            {
                _cloudSaveReady = false;
                Debug.LogWarning("PersistentStorage - Failed to initialize Unity Cloud Save.");
                Debug.LogException(exception);
            }
            finally
            {
                _isInitialized = true;
            }
        }

        private static async Task EnsureSignedInAsync()
        {
            var authenticationInstance = AuthenticationService.Instance;

            if (authenticationInstance == null)
                return;

            string desiredProfile = Global.PlayerAuthenticationService?.UnityProfileName;

            string sanitizedProfile = SanitizeProfileName(desiredProfile);

            if (string.IsNullOrEmpty(sanitizedProfile) == false && authenticationInstance.Profile != sanitizedProfile)
            {
                if (authenticationInstance.IsSignedIn == true)
                {
                    authenticationInstance.SignOut();
                }

                authenticationInstance.SwitchProfile(sanitizedProfile);
            }

            if (authenticationInstance.IsSignedIn == true)
                return;

            await authenticationInstance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
        }

        private static string SanitizeProfileName(string profileName)
        {
            if (string.IsNullOrEmpty(profileName) == true)
                return null;

            Span<char> buffer = stackalloc char[Math.Min(profileName.Length, 30)];
            int index = 0;

            for (int i = 0; i < profileName.Length && index < buffer.Length; i++)
            {
                char character = profileName[i];

                if (char.IsLetterOrDigit(character) == false && character != '-' && character != '_')
                    continue;

                buffer[index++] = character;
            }

            if (index == 0)
                return null;

            return new string(buffer.Slice(0, index));
        }
    }
}
