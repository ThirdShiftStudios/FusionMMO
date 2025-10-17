using System;
using Fusion;

namespace TPSBR
{
        using UnityEngine;

        public struct BodyHitData : INetworkStruct
        {
                public EHitAction Action;
                public float      Damage;
                public Vector3    RelativePosition;
                public Vector3    Direction;
                public PlayerRef  Instigator;
        }

        public abstract class Health : ContextBehaviour, IHitTarget, IHitInstigator
        {
                // PUBLIC MEMBERS

                public bool  IsAlive   => CurrentHealth > 0f;
                public float MaxHealth => _maxHealth;
                public float MaxShield => _maxShield;

                [Networked, HideInInspector]
                public float CurrentHealth { get; protected set; }
                [Networked, HideInInspector]
                public float CurrentShield { get; protected set; }

                public event Action<HitData> HitTaken;
                public event Action<HitData> HitPerformed;

                // PROTECTED MEMBERS

                [SerializeField]
                protected float _maxHealth;
                [SerializeField]
                protected float _maxShield;
                [SerializeField]
                protected float _startShield;
                [SerializeField]
                protected Transform _hitIndicatorPivot;

                [Header("Regeneration")]
                [SerializeField]
                protected float _healthRegenPerSecond;
                [SerializeField]
                protected float _maxHealthFromRegen;
                [SerializeField]
                protected int _regenTickPerSecond;
                [SerializeField]
                protected int _regenCombatDelay;

                [Networked]
                protected int _hitCount { get; set; }
                [Networked, Capacity(4)]
                protected NetworkArray<BodyHitData> _hitData { get; }

                protected int _visibleHitCount;

                protected TickTimer _regenTickTimer;
                protected float _healthRegenPerTick;
                protected float _regenTickTime;

                // PUBLIC METHODS

                public virtual void OnSpawned(Agent agent)
                {
                        _visibleHitCount = _hitCount;
                }

                public virtual void OnDespawned()
                {
                        HitTaken = null;
                        HitPerformed = null;
                }

                public virtual void OnFixedUpdate()
                {
                        if (HasStateAuthority == false)
                                return;

                        if (IsAlive == true && _healthRegenPerSecond > 0f && _regenTickTimer.ExpiredOrNotRunning(Runner) == true)
                        {
                                _regenTickTimer = TickTimer.CreateFromSeconds(Runner, _regenTickTime);

                                var healthDiff = _maxHealthFromRegen - CurrentHealth;
                                if (healthDiff <= 0f)
                                        return;

                                AddHealth(Mathf.Min(healthDiff, _healthRegenPerTick));
                        }
                }

                public void ResetRegenDelay()
                {
                        _regenTickTimer = TickTimer.CreateFromSeconds(Runner, _regenCombatDelay);
                }

                public override void CopyBackingFieldsToState(bool firstTime)
                {
                        base.CopyBackingFieldsToState(firstTime);

                        InvokeWeavedCode();

                        CurrentHealth = _maxHealth;
                        CurrentShield = _startShield;
                }

                // NetworkBehaviour INTERFACE

                public override void Render()
                {
                        if (Runner.Mode != SimulationModes.Server)
                        {
                                UpdateVisibleHits();
                        }
                }

                // MONOBEHAVIOUR

                protected virtual void Awake()
                {
                        _regenTickTime      = _regenTickPerSecond > 0 ? 1f / _regenTickPerSecond : 0f;
                        _healthRegenPerTick = _regenTickPerSecond > 0 ? _healthRegenPerSecond / _regenTickPerSecond : 0f;
                }

                // IHitTarget INTERFACE

                Transform IHitTarget.HitPivot => _hitIndicatorPivot != null ? _hitIndicatorPivot : transform;

                void IHitTarget.ProcessHit(ref HitData hitData)
                {
                        if (IsAlive == false)
                        {
                                hitData.Amount = 0;
                                return;
                        }

                        ApplyHit(ref hitData);

                        if (IsAlive == false)
                        {
                                hitData.IsFatal = true;
                                OnDeath(hitData);
                        }
                }

                // IHitInstigator INTERFACE

                void IHitInstigator.HitPerformed(HitData hitData)
                {
                        if (hitData.Amount > 0 && hitData.Target != (IHitTarget)this && Runner.IsResimulation == false)
                        {
                                HitPerformed?.Invoke(hitData);
                        }
                }

                // PROTECTED METHODS

                protected virtual void ApplyHit(ref HitData hit)
                {
                        if (IsAlive == false)
                                return;

                        if (hit.Action == EHitAction.Damage)
                        {
                                hit.Amount = ApplyDamage(hit.Amount);
                        }
                        else if (hit.Action == EHitAction.Heal)
                        {
                                hit.Amount = AddHealth(hit.Amount);
                        }
                        else if (hit.Action == EHitAction.Shield)
                        {
                                hit.Amount = AddShield(hit.Amount);
                        }

                        if (hit.Amount <= 0)
                                return;

                        if (hit.InstigatorRef == Context.LocalPlayerRef && Runner.IsForward == true)
                        {
                                HitTaken?.Invoke(hit);
                        }

                        if (HasStateAuthority == false)
                                return;

                        _hitCount++;

                        var bodyHitData = new BodyHitData
                        {
                                Action           = hit.Action,
                                Damage           = hit.Amount,
                                Direction        = hit.Direction,
                                RelativePosition = hit.Position != Vector3.zero ? hit.Position - transform.position : Vector3.zero,
                                Instigator       = hit.InstigatorRef,
                        };

                        int hitIndex = _hitCount % _hitData.Length;
                        _hitData.Set(hitIndex, bodyHitData);
                }

                protected virtual float ApplyDamage(float damage)
                {
                        if (damage <= 0f)
                                return 0f;

                        ResetRegenDelay();

                        var shieldChange = AddShield(-damage);
                        var healthChange = AddHealth(-(damage + shieldChange));

                        return -(shieldChange + healthChange);
                }

                protected float AddHealth(float health)
                {
                        float previousHealth = CurrentHealth;
                        SetHealth(CurrentHealth + health);
                        return CurrentHealth - previousHealth;
                }

                protected float AddShield(float shield)
                {
                        float previousShield = CurrentShield;
                        SetShield(CurrentShield + shield);
                        return CurrentShield - previousShield;
                }

                protected void SetHealth(float health)
                {
                        CurrentHealth = Mathf.Clamp(health, 0, _maxHealth);
                }

                protected void SetShield(float shield)
                {
                        CurrentShield = Mathf.Clamp(shield, 0, _maxShield);
                }

                protected void UpdateVisibleHits()
                {
                        if (_visibleHitCount == _hitCount)
                                return;

                        int dataCount = _hitData.Length;
                        int oldestHitData = _hitCount - dataCount + 1;

                        for (int i = Mathf.Max(_visibleHitCount + 1, oldestHitData); i <= _hitCount; i++)
                        {
                                int shotIndex = i % dataCount;
                                var bodyHitData = _hitData.Get(shotIndex);

                                var hitData = new HitData
                                {
                                        Action        = bodyHitData.Action,
                                        Amount        = bodyHitData.Damage,
                                        Position      = transform.position + bodyHitData.RelativePosition,
                                        Direction     = bodyHitData.Direction,
                                        Normal        = -bodyHitData.Direction,
                                        Target        = this,
                                        InstigatorRef = bodyHitData.Instigator,
                                        IsFatal       = i == _hitCount && CurrentHealth <= 0f,
                                };

                                OnHitTaken(hitData);
                        }

                        _visibleHitCount = _hitCount;
                }

                protected virtual void OnHitTaken(HitData hit)
                {
                        if (hit.InstigatorRef != Context.LocalPlayerRef)
                        {
                                HitTaken?.Invoke(hit);
                        }

                        if (hit.InstigatorRef.IsRealPlayer == true && hit.InstigatorRef == Context.ObservedPlayerRef)
                        {
                                var instigator = hit.Instigator;

                                if (instigator == null)
                                {
                                        var player = Context.NetworkGame.GetPlayer(hit.InstigatorRef);
                                        instigator = player != null ? player.ActiveAgent.Health as IHitInstigator : null;
                                }

                                if (instigator != null)
                                {
                                        instigator.HitPerformed(hit);
                                }
                        }
                }

                // DEBUG

                [ContextMenu("Add Health")]
                protected void Debug_AddHealth()
                {
                        CurrentHealth += 10;
                }

                [ContextMenu("Remove Health")]
                protected void Debug_RemoveHealth()
                {
                        CurrentHealth -= 10;
                }

                // ABSTRACT METHODS

                protected abstract void OnDeath(HitData hitData);
        }
}
