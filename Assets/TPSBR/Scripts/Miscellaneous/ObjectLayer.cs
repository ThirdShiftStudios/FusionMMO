using UnityEngine;

namespace TPSBR
{
    public static class ObjectLayer
    {
        private static bool _initialized;

        private static int _default = -1;
        private static int _agent = -1;
        private static int _agentKCC = -1;
        private static int _projectile = -1;
        private static int _target = -1;
        private static int _interaction = -1;
        private static int _pickup = -1;
        private static int _water = -1;

        public static int Default
        {
            get
            {
                EnsureInitialized();
                return _default;
            }
        }

        public static int Agent
        {
            get
            {
                EnsureInitialized();
                return _agent;
            }
        }

        public static int AgentKCC
        {
            get
            {
                EnsureInitialized();
                return _agentKCC;
            }
        }

        public static int Projectile
        {
            get
            {
                EnsureInitialized();
                return _projectile;
            }
        }

        public static int Target
        {
            get
            {
                EnsureInitialized();
                return _target;
            }
        }

        public static int Interaction
        {
            get
            {
                EnsureInitialized();
                return _interaction;
            }
        }

        public static int Pickup
        {
            get
            {
                EnsureInitialized();
                return _pickup;
            }
        }

        public static int Water
        {
            get
            {
                EnsureInitialized();
                return _water;
            }
        }

        public static void EnsureInitialized()
        {
            if (_initialized == true)
                return;

            _initialized = true;

            _default     = LayerMask.NameToLayer("Default");
            _agent       = LayerMask.NameToLayer("Agent");
            _agentKCC    = LayerMask.NameToLayer("AgentKCC");
            _projectile  = LayerMask.NameToLayer("Projectile");
            _target      = LayerMask.NameToLayer("Target");
            _interaction = LayerMask.NameToLayer("Interaction");
            _pickup      = LayerMask.NameToLayer("Pickup");
            _water       = LayerMask.NameToLayer("Water");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            EnsureInitialized();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            EnsureInitialized();
        }
#endif
    }

    public static class ObjectLayerMask
    {
        private static bool _initialized;

        private static LayerMask _default;
        private static LayerMask _agent;
        private static LayerMask _target;
        private static LayerMask _blockingProjectiles;
        private static LayerMask _environment;
        private static LayerMask _water;

        public static LayerMask Default
        {
            get
            {
                EnsureInitialized();
                return _default;
            }
        }

        public static LayerMask Agent
        {
            get
            {
                EnsureInitialized();
                return _agent;
            }
        }

        public static LayerMask Target
        {
            get
            {
                EnsureInitialized();
                return _target;
            }
        }

        public static LayerMask BlockingProjectiles
        {
            get
            {
                EnsureInitialized();
                return _blockingProjectiles;
            }
        }

        public static LayerMask Environment
        {
            get
            {
                EnsureInitialized();
                return _environment;
            }
        }

        public static LayerMask Water
        {
            get
            {
                EnsureInitialized();
                return _water;
            }
        }

        public static void EnsureInitialized()
        {
            if (_initialized == true)
                return;

            _initialized = true;

            ObjectLayer.EnsureInitialized();

            _default = CreateMask(ObjectLayer.Default);
            _agent   = CreateMask(ObjectLayer.Agent);
            _target  = CreateMask(ObjectLayer.Target);

            _environment         = _default;
            _blockingProjectiles = _default | _agent | _target;

            _water = CreateMask(ObjectLayer.Water);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            EnsureInitialized();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            EnsureInitialized();
        }
#endif

        private static LayerMask CreateMask(int layer)
        {
            return layer >= 0 ? 1 << layer : 0;
        }
    }
}
