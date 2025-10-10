using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using Fusion;
using Fusion.Photon.Realtime;

namespace TPSBR
{
        public interface IGlobalService
        {
                bool IsInitialized { get; }
                void Initialize();
                void Tick();
                void Deinitialize();
        }

        public static class Global
        {
                // PUBLIC MEMBERS

                public static GlobalSettings   Settings          { get; private set; }
                public static RuntimeSettings  RuntimeSettings   { get; private set; }
                public static PlayerAuthenticationService PlayerAuthenticationService { get; private set; }
                public static PlayerService    PlayerService     { get; private set; }
                public static PlayerCloudSaveService PlayerCloudSaveService { get; private set; }
                public static Networking       Networking        { get; private set; }
                public static MultiplayManager MultiplayManager  { get; private set; }
                public static Task             AreServicesInitialized => _servicesInitializedTask.Task;

		// PRIVATE MEMBERS

                private static readonly string LogPrefix = "[<color=green>Global</color>] ";
                private static bool _isInitialized;
                private static bool _servicesInitializationComplete;
                private static TaskCompletionSource<bool> _servicesInitializedTask;
                private static List<IGlobalService> _globalServices = new List<IGlobalService>(16);
                private static List<IGlobalService> _pendingServiceInitializations = new List<IGlobalService>(16);

                static Global()
                {
                        ResetServicesInitializationTracker();
                }

		// PUBLIC METHODS

		public static void Quit()
		{
			Deinitialize();

#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		// PRIVATE METHODS

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
                private static void InitializeSubSystem()
                {
                        Log("Subsystem registration starting");

                        if (Application.isBatchMode == true)
                        {
                                Log("Application running in batch mode - muting audio and updating player loop");
                                UnityEngine.AudioListener.volume = 0.0f;
                                PlayerLoopUtility.RemovePlayerLoopSystems(typeof(PostLateUpdate.UpdateAudio));
                        }

#if UNITY_EDITOR
			if (Application.isPlaying == false)
				return;
#endif
                        if (PlayerLoopUtility.HasPlayerLoopSystem(typeof(Global)) == false)
                        {
                                Log("Adding Global to PlayerLoop");
                                PlayerLoopUtility.AddPlayerLoopSystem(typeof(Global), typeof(Update.ScriptRunBehaviourUpdate), BeforeUpdate, AfterUpdate);
                        }

                        Application.quitting -= OnApplicationQuit;
                        Application.quitting += OnApplicationQuit;

                        Log("Subsystem registration complete");
                        _isInitialized = true;
                }

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
                private static void InitializeBeforeSceneLoad()
                {
                        Log("InitializeBeforeSceneLoad starting");

                        Initialize();

                        // You can pause network services here

                        if (ApplicationSettings.IsBatchServer == true)
                        {
                                Log("Configuring target frame rate for batch server");
                                Application.targetFrameRate = TickRate.Resolve(NetworkProjectConfig.Global.Simulation.TickRateSelection).Server;
                        }

                        if (ApplicationSettings.HasFrameRate == true)
                        {
                                Log("Applying custom frame rate");
                                Application.targetFrameRate = ApplicationSettings.FrameRate;
                        }

                        Log("InitializeBeforeSceneLoad complete");
                }

                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
                private static void InitializeAfterSceneLoad()
                {
                        // You can unpause network services here
                        Log("InitializeAfterSceneLoad invoked");
                }

                private static void Initialize()
                {
                        if (_isInitialized == false)
                                return;

                        Log("Initialize starting");

                        if (typeof(DebugManager).GetField("m_DebugActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DebugManager.instance) == null)
                        {
                                Log("Registering DebugManager inputs and actions");
                                typeof(DebugManager).GetMethod("RegisterInputs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(DebugManager.instance, null);
                                typeof(DebugManager).GetMethod("RegisterActions", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(DebugManager.instance, null);
                        }

                        GlobalSettings[] globalSettings = Resources.LoadAll<GlobalSettings>("");
                        Settings = globalSettings.Length > 0 ? Object.Instantiate(globalSettings[0]) : null;

                        Log(Settings != null ? "Global settings loaded" : "No global settings asset found");

                        RuntimeSettings = new RuntimeSettings();
                        RuntimeSettings.Initialize(Settings);
                        Log("Runtime settings initialized");

                        ResetServicesInitializationTracker();

                        Log("Preparing global services");

                        PrepareGlobalServices();

                        Log("Creating networking service");
                        Networking = CreateStaticObject<Networking>();

                        if (ApplicationSettings.UseMultiplay == true && ApplicationSettings.IsServer == true)
                        {
                                Log("Creating MultiplayManager");
                                MultiplayManager = CreateStaticObject<MultiplayManager>();
                        }

                        Log("Initialize complete");
                        _isInitialized = true;
                }

                private static void Deinitialize()
                {
                        Log("Deinitialize starting");

                        if (_isInitialized == false)
                        {
                                Log("Deinitialize called while Global not initialized");
                                return;
                        }

                        for (int i = _globalServices.Count - 1; i >= 0; i--)
                        {
                                var service = _globalServices[i];
                                if (service != null)
                                {
                                        Log($"Deinitializing service {service.GetType().Name}");
                                        service.Deinitialize();
                                }
                        }

                        ResetServicesInitializationTracker();
                        _isInitialized = false;
                        Log("Deinitialize complete");
                }

                private static void OnApplicationQuit()
                {
                        Log("Application quitting");
                        Deinitialize();
                }

                private static void BeforeUpdate()
                {
                        if (_globalServices.Count == 0)
                                return;

                        if (_servicesInitializationComplete == false)
                        {
                                TryInitializePendingServices();
                                UpdateServicesInitializedState();
                        }

                        for (int i = 0; i < _globalServices.Count; i++)
                        {
                                var service = _globalServices[i];
                                if (service == null)
                                        continue;

                                if (ReferenceEquals(service, PlayerAuthenticationService) == false && service.IsInitialized == false)
                                        continue;

                                service.Tick();
                        }
                }

		private static void AfterUpdate()
		{
			if (Application.isPlaying == false)
			{
				PlayerLoopUtility.RemovePlayerLoopSystems(typeof(Global));
			}
		}

                private static void PrepareGlobalServices()
                {
                        _globalServices.Clear();
                        _pendingServiceInitializations.Clear();
                        Log("Instantiating PlayerAuthenticationService");

                        PlayerAuthenticationService = new PlayerAuthenticationService();
                        PlayerService = new PlayerService();
                        PlayerCloudSaveService = new PlayerCloudSaveService();

                        _globalServices.Add(PlayerAuthenticationService);
                        _globalServices.Add(PlayerService);
                        _globalServices.Add(PlayerCloudSaveService);

                        Log("Initializing authentication service");
                        ((IGlobalService)PlayerAuthenticationService).Initialize();
                        QueuePendingServices();
                        TryInitializePendingServices();
                        UpdateServicesInitializedState();
                }

                private static void QueuePendingServices()
                {
                        _pendingServiceInitializations.Clear();

                        for (int i = 0; i < _globalServices.Count; i++)
                        {
                                var service = _globalServices[i];
                                if (service == null || ReferenceEquals(service, PlayerAuthenticationService) == true)
                                        continue;

                                _pendingServiceInitializations.Add(service);
                        }
                }

                private static void TryInitializePendingServices()
                {
                        if (PlayerAuthenticationService == null || PlayerAuthenticationService.IsInitialized == false)
                                return;

                        if (_pendingServiceInitializations.Count == 0)
                                return;

                        Log("Initializing deferred global services");

                        for (int i = 0; i < _pendingServiceInitializations.Count; i++)
                        {
                                var service = _pendingServiceInitializations[i];
                                if (service == null || service.IsInitialized == true)
                                        continue;

                                Log($"Initializing service {service.GetType().Name}");
                                service.Initialize();
                        }

                        _pendingServiceInitializations.Clear();
                }

                private static void UpdateServicesInitializedState()
                {
                        if (_servicesInitializationComplete == true)
                                return;

                        if (_globalServices.Count == 0)
                                return;

                        for (int i = 0; i < _globalServices.Count; i++)
                        {
                                if (_globalServices[i] == null || _globalServices[i].IsInitialized == false)
                                {
                                        return;
                                }
                        }

                        _servicesInitializationComplete = true;
                        Log("All global services initialized");
                        _servicesInitializedTask.TrySetResult(true);
                }

                private static T CreateStaticObject<T>() where T : Component
                {
                        Log($"Creating static object for {typeof(T).Name}");
                        GameObject gameObject = new GameObject(typeof(T).Name);
                        Object.DontDestroyOnLoad(gameObject);

                        return gameObject.AddComponent<T>();
                }

                private static void ResetServicesInitializationTracker()
                {
                        _servicesInitializationComplete = false;
                        _pendingServiceInitializations.Clear();
                        Log("Resetting services initialization tracker");

                        if (_servicesInitializedTask == null || _servicesInitializedTask.Task.IsCompleted == true)
                        {
                                _servicesInitializedTask = CreateServicesInitializedTask();
                                Log("Created new services initialization TaskCompletionSource");
                        }
                }

                private static TaskCompletionSource<bool> CreateServicesInitializedTask()
                {
                        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                private static void Log(string message)
                {
                        Debug.Log(LogPrefix + message);
                }
        }
}
