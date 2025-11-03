using Fusion;
using UnityEngine;
using DG.Tweening;
using FronkonGames.SpiceUp.Drunk;

namespace TPSBR
{
        public sealed class AgentSenses : NetworkBehaviour
        {
                [Networked, HideInInspector]
                public float EyesFlashValue { get; set; }

                [Networked][OnChangedRender(nameof(OnDrunkValueChanged))]
                [HideInInspector]
                public float DrunkValue { get; set; }

                [SerializeField]
                private Ease _eyesFlashFalloff;

                [Header("Drunk Effect")]
                [SerializeField, Range(0f, 1f)]
                private float _drunkIntensityPerDrink = 0.25f;

                [SerializeField]
                private float _drunkDurationPerDrink = 10f;

                [SerializeField, Range(0f, 1f)]
                private float _drunkMaxIntensity = 1f;

                private float _eyesFlashStartValue;
                private int _eyesFlashStartTick;
                private int _eyesFlashEndTick;

                private float _drunkStartValue;
                private float _drunkTimeRemaining;
                private float _drunkTotalDuration;

                private float _lastAppliedDrunkValue = -1f;

                public void SetEyesFlash(float value, float duration, float falloffDelay)
                {
                        if (HasStateAuthority == false)
                                return;

                        if (falloffDelay > duration - 0.1f)
                        {
                                falloffDelay = duration - 0.1f;
                        }

                        _eyesFlashStartTick = Runner.Tick + (int)(falloffDelay / Runner.DeltaTime);
                        _eyesFlashEndTick = _eyesFlashStartTick + (int)(duration / Runner.DeltaTime);

                        _eyesFlashStartValue = value;
                        EyesFlashValue = value;
                }

                public void OnBeerDrank()
                {
                        if (HasStateAuthority == false)
                                return;

                        float currentStrength = Mathf.Max(DrunkValue, 0f);
                        float newStrength = Mathf.Clamp(currentStrength + _drunkIntensityPerDrink, 0f, _drunkMaxIntensity);

                        if (_drunkTimeRemaining <= 0f)
                        {
                                _drunkTimeRemaining = _drunkDurationPerDrink;
                        }
                        else
                        {
                                _drunkTimeRemaining += _drunkDurationPerDrink;
                        }

                        _drunkTotalDuration = Mathf.Max(_drunkTimeRemaining, 0.0001f);
                        _drunkStartValue = newStrength;
                        DrunkValue = newStrength;
                }

                public override void Spawned()
                {
                        base.Spawned();

                        if (Object != null && Object.HasInputAuthority == true)
                        {
                                // Ensure the local effect reflects the current value when the behaviour spawns.
                                _lastAppliedDrunkValue = -1f;
                                ApplyDrunkPostProcess(DrunkValue);
                        }
                }

                public override void FixedUpdateNetwork()
                {
                        if (HasStateAuthority == false)
                                return;

                        int currentTick = Runner.Tick;
                        UpdateEyesFlash(currentTick);
                        UpdateDrunk();
                }

                public override void Render()
                {
                        base.Render();

                        if (Object == null || Object.HasInputAuthority == false)
                                return;

                        ApplyDrunkPostProcess(DrunkValue);
                }

                private void UpdateEyesFlash(int currentTick)
                {
                        if (currentTick >= _eyesFlashEndTick || _eyesFlashEndTick <= _eyesFlashStartTick)
                        {
                                EyesFlashValue = 0f;
                                return;
                        }

                        float progress = (currentTick - _eyesFlashStartTick) / (float)(_eyesFlashEndTick - _eyesFlashStartTick);
                        EyesFlashValue = Mathf.Lerp(_eyesFlashStartValue, 0f, DOVirtual.EasedValue(0f, 1f, progress, _eyesFlashFalloff));
                }

                private void UpdateDrunk()
                {
                        if (_drunkTimeRemaining <= 0f)
                        {
                                if (DrunkValue > 0f)
                                {
                                        DrunkValue = 0f;
                                }

                                _drunkStartValue = 0f;
                                _drunkTotalDuration = 0f;
                                _drunkTimeRemaining = 0f;
                                return;
                        }

                        _drunkTimeRemaining = Mathf.Max(0f, _drunkTimeRemaining - Runner.DeltaTime);

                        if (_drunkTotalDuration <= Mathf.Epsilon)
                        {
                                DrunkValue = 0f;
                                _drunkStartValue = 0f;
                                _drunkTotalDuration = 0f;
                                _drunkTimeRemaining = 0f;
                                return;
                        }

                        float normalized = 1f - (_drunkTimeRemaining / _drunkTotalDuration);
                        normalized = Mathf.Clamp01(normalized);
                        DrunkValue = Mathf.Lerp(_drunkStartValue, 0f, normalized);

                        if (_drunkTimeRemaining <= 0f)
                        {
                                _drunkStartValue = 0f;
                                _drunkTotalDuration = 0f;
                                _drunkTimeRemaining = 0f;
                        }
                }

                private void OnDrunkValueChanged()
                {
                        if (Object == null || Object.HasInputAuthority == false)
                                return;

                        ApplyDrunkPostProcess(DrunkValue);
                }

                private void ApplyDrunkPostProcess(float value)
                {
                        if (Mathf.Approximately(_lastAppliedDrunkValue, value) == true)
                                return;

                        Drunk effect = Drunk.Instance;
                        if (effect == null)
                                return;

                        var settings = effect.settings;
                        if (settings == null)
                                return;

                        settings.drunkenness = value;
                        settings.intensity = value > 0f ? 1f : 0f;

                        _lastAppliedDrunkValue = value;
                }

                private void OnDisable()
                {
                        if (Object != null && Object.HasInputAuthority == true)
                        {
                                _lastAppliedDrunkValue = -1f;
                                ApplyDrunkPostProcess(0f);
                        }
                }
        }
}
