using Fusion;

namespace TPSBR
{
    using UnityEngine;

    public sealed class AgentMana : ContextBehaviour
    {
        [Header("Attributes")]
        [SerializeField]
        private float _baseMana = 100f;

        [SerializeField]
        private float _manaPerIntelligence = 20f;

        [Networked, HideInInspector]
        public float CurrentMana { get; private set; }

        [Networked, HideInInspector]
        public float TotalMana { get; private set; }

        private Agent _agent;
        private Stats _stats;
        private bool _baseTotalManaInitialized;
        private float _baseTotalMana;

        public override void CopyBackingFieldsToState(bool firstTime)
        {
            base.CopyBackingFieldsToState(firstTime);

            InvokeWeavedCode();

            TotalMana = Mathf.Max(0f, _baseMana);
            CurrentMana = TotalMana;
        }

        public void OnSpawned(Agent agent)
        {
            if (agent != null)
            {
                _agent = agent;
            }
            else if (_agent == null)
            {
                _agent = GetComponent<Agent>();
            }

            _baseTotalManaInitialized = false;

            _stats = _agent != null ? _agent.GetComponent<Stats>() : GetComponent<Stats>();

            if (_stats != null)
            {
                _stats.StatChanged += OnStatChanged;

                if (HasStateAuthority == true)
                {
                    UpdateTotalManaFromIntelligence(_stats.Intelligence, false, _stats.Intelligence);
                }
            }
        }

        public void OnDespawned()
        {
            if (_stats != null)
            {
                _stats.StatChanged -= OnStatChanged;
            }

            _stats = null;
            _agent = null;
            _baseTotalManaInitialized = false;
            _baseTotalMana = 0f;
        }

        public float AddMana(float mana)
        {
            if (HasStateAuthority == false || mana <= 0f)
            {
                return 0f;
            }

            float previousMana = CurrentMana;
            CurrentMana = Mathf.Clamp(CurrentMana + mana, 0f, TotalMana);
            return CurrentMana - previousMana;
        }

        public bool TryConsumeMana(float mana)
        {
            if (HasStateAuthority == false || mana <= 0f)
            {
                return false;
            }

            if (CurrentMana < mana)
            {
                return false;
            }

            CurrentMana = Mathf.Clamp(CurrentMana - mana, 0f, TotalMana);
            return true;
        }

        public void SetMana(float mana)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            CurrentMana = Mathf.Clamp(mana, 0f, TotalMana);
        }

        private void Awake()
        {
            if (_agent == null)
            {
                _agent = GetComponent<Agent>();
            }
        }

        private void OnStatChanged(Stats.StatIndex stat, int previousValue, int newValue)
        {
            if (stat != Stats.StatIndex.Intelligence)
            {
                return;
            }

            UpdateTotalManaFromIntelligence(newValue, true, previousValue);
        }

        private void InitializeBaseTotalMana()
        {
            if (_baseTotalManaInitialized == true)
            {
                return;
            }

            _baseTotalMana = Mathf.Max(0f, _baseMana);
            _baseTotalManaInitialized = true;
        }

        private void UpdateTotalManaFromIntelligence(int newIntelligence, bool preserveManaPercentage, int? previousIntelligenceOverride = null)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            InitializeBaseTotalMana();

            float previousTotalMana = previousIntelligenceOverride.HasValue
                ? Mathf.Max(0f, _baseTotalMana + previousIntelligenceOverride.Value * _manaPerIntelligence)
                : TotalMana;

            float targetTotalMana = Mathf.Max(0f, _baseTotalMana + newIntelligence * _manaPerIntelligence);

            float manaRatio = preserveManaPercentage && previousTotalMana > 0f ? CurrentMana / previousTotalMana : 1f;

            TotalMana = targetTotalMana;

            if (preserveManaPercentage == true)
            {
                CurrentMana = Mathf.Clamp(TotalMana * manaRatio, 0f, TotalMana);
            }
            else
            {
                CurrentMana = TotalMana;
            }
        }
    }
}
