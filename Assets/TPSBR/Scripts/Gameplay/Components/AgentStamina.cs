using Fusion;

namespace TPSBR
{
        using UnityEngine;

        public sealed class AgentStamina : ContextBehaviour
        {
                [SerializeField]
                private float _totalStamina = 100f;

                [SerializeField]
                private float _startStamina = 100f;

                [Networked, HideInInspector]
                public float CurrentStamina { get; private set; }

                public float TotalStamina => _totalStamina;

                public void OnSpawned(Agent agent)
                {
                        SetStamina(_startStamina > 0f ? _startStamina : _totalStamina);
                }

                public void OnDespawned()
                {
                        CurrentStamina = 0f;
                }

                public override void Spawned()
                {
                        base.Spawned();

                        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, _totalStamina);
                }

                public override void FixedUpdateNetwork()
                {
                        base.FixedUpdateNetwork();

                        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, _totalStamina);
                }

                public float AddStamina(float amount)
                {
                        if (HasStateAuthority == false || amount == 0f)
                                return 0f;

                        float previous = CurrentStamina;
                        SetStamina(CurrentStamina + amount);
                        return CurrentStamina - previous;
                }

                public bool ConsumeStamina(float amount)
                {
                        if (HasStateAuthority == false)
                                return false;

                        if (amount <= 0f)
                                return true;

                        if (CurrentStamina < amount)
                                return false;

                        SetStamina(CurrentStamina - amount);
                        return true;
                }

                public void SetTotalStamina(float totalStamina, bool preserveCurrentPercentage = true)
                {
                        if (HasStateAuthority == false)
                                return;

                        float previousTotal = _totalStamina;
                        float currentRatio = previousTotal > 0f ? CurrentStamina / previousTotal : 1f;

                        _totalStamina = Mathf.Max(0f, totalStamina);

                        if (preserveCurrentPercentage == true)
                        {
                                SetStamina(_totalStamina * currentRatio);
                        }
                        else
                        {
                                SetStamina(_totalStamina);
                        }
                }

                public void SetStamina(float stamina)
                {
                        CurrentStamina = Mathf.Clamp(stamina, 0f, _totalStamina);
                }

                public override void CopyBackingFieldsToState(bool firstTime)
                {
                        base.CopyBackingFieldsToState(firstTime);

                        InvokeWeavedCode();

                        SetStamina(Mathf.Clamp(_startStamina, 0f, _totalStamina));
                }
        }
}
