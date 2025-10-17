using Fusion;

namespace TPSBR
{
        using UnityEngine;

        public sealed class AgentHealth : Health
        {
                [Header("Attributes")]
                [SerializeField]
                private float _healthPerStrength = 20f;

                private Agent _agent;
                private Stats _stats;
                private float _baseMaxHealth;
                private bool  _baseMaxHealthInitialized;

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
                                UpdateMaxHealthFromStrength(_stats.Strength, false, _stats.Strength);
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
                        if (stat != Stats.StatIndex.Strength)
                                return;

                        UpdateMaxHealthFromStrength(newValue, true, previousValue);
                }

                private void InitializeBaseMaxHealth(int strength)
                {
                        if (_baseMaxHealthInitialized == true)
                                return;

                        _baseMaxHealth = _maxHealth - strength * _healthPerStrength;
                        _baseMaxHealthInitialized = true;
                }

                private void UpdateMaxHealthFromStrength(int newStrength, bool preserveHealthPercentage, int? previousStrengthOverride = null)
                {
                        if (_stats == null)
                                return;

                        InitializeBaseMaxHealth(newStrength);

                        float previousMaxHealth = previousStrengthOverride.HasValue
                                ? Mathf.Max(0f, _baseMaxHealth + previousStrengthOverride.Value * _healthPerStrength)
                                : _maxHealth;

                        float targetMaxHealth = Mathf.Max(0f, _baseMaxHealth + newStrength * _healthPerStrength);

                        float healthRatio = preserveHealthPercentage && previousMaxHealth > 0f ? CurrentHealth / previousMaxHealth : 1f;
                        float regenRatio  = previousMaxHealth > 0f ? _maxHealthFromRegen / previousMaxHealth : 1f;

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
                }
        }
}
