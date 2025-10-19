using Fusion;

namespace TPSBR
{
    using UnityEngine;

    public sealed class AgentHealth : Health
    {
        private Agent _agent;
        private Stats _stats;
        private float _baseMaxHealth;
        private bool _baseMaxHealthInitialized;
        private float _lastStatHealthBonus;

        public override void OnSpawned(Agent agent)
        {
            base.OnSpawned(agent);

            if (agent != null)
            {
                _agent = agent;
            }
            else if (_agent == null)
            {
                _agent = GetComponent<Agent>();
            }

            _baseMaxHealthInitialized = false;

            _stats = _agent != null ? _agent.GetComponent<Stats>() : GetComponent<Stats>();

            if (_stats != null)
            {
                _stats.StatChanged += OnStatChanged;
                UpdateMaxHealthFromStats(false, null);
            }
        }

        public override void OnDespawned()
        {
            if (_stats != null)
            {
                _stats.StatChanged -= OnStatChanged;
            }

            _stats = null;
            _agent = null;
            _baseMaxHealthInitialized = false;
            _baseMaxHealth = 0f;
            _lastStatHealthBonus = 0f;

            base.OnDespawned();
        }

        protected override void Awake()
        {
            base.Awake();

            if (_agent == null)
            {
                _agent = GetComponent<Agent>();
            }
        }

        protected override void OnDeath(HitData hitData)
        {
            if (Context?.GameplayMode != null)
            {
                Context.GameplayMode.AgentDeath(_agent, hitData);
            }
        }

        private void OnStatChanged(Stats.StatIndex stat, int previousValue, int newValue)
        {
            if (_stats == null)
                return;

            float previousBonus = _stats.GetTotalHealth(stat, previousValue);
            UpdateMaxHealthFromStats(true, previousBonus);
        }

        private void InitializeBaseMaxHealth()
        {
            if (_baseMaxHealthInitialized == true)
                return;

            float currentBonus = _stats != null ? _stats.GetTotalHealth() : 0f;
            _baseMaxHealth = Mathf.Max(0f, _maxHealth - currentBonus);
            _lastStatHealthBonus = Mathf.Max(0f, currentBonus);
            _baseMaxHealthInitialized = true;
        }

        private void UpdateMaxHealthFromStats(bool preserveHealthPercentage, float? previousBonusOverride)
        {
            if (_stats == null)
                return;

            InitializeBaseMaxHealth();

            float previousBonus = previousBonusOverride.HasValue
                ? Mathf.Max(0f, previousBonusOverride.Value)
                : _lastStatHealthBonus;
            float currentBonus = Mathf.Max(0f, _stats.GetTotalHealth());

            float previousMaxHealth = Mathf.Max(0f, _baseMaxHealth + previousBonus);
            float targetMaxHealth = Mathf.Max(0f, _baseMaxHealth + currentBonus);

            float healthRatio = preserveHealthPercentage && previousMaxHealth > 0f
                ? CurrentHealth / previousMaxHealth
                : 1f;
            float regenRatio = previousMaxHealth > 0f ? _maxHealthFromRegen / previousMaxHealth : 1f;

            _maxHealth = targetMaxHealth;

            if (preserveHealthPercentage == true)
            {
                SetHealth(_maxHealth * healthRatio);
            }
            else
            {
                SetHealth(_maxHealth);
            }

            _maxHealthFromRegen = _maxHealth * regenRatio;
            _lastStatHealthBonus = currentBonus;
        }
    }
}