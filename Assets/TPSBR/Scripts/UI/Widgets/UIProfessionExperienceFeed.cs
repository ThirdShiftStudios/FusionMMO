using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
        public struct ProfessionExperienceFeedData : IFeedData
        {
                public Professions.ProfessionIndex Profession;
                public string ProfessionName;
                public int ExperienceAmount;
                public int NewLevel;
                public bool LevelIncreased;
        }

        public class UIProfessionExperienceFeed : UIFeedBase
        {
                [Serializable]
                private struct ProfessionDisplay
                {
                        public Professions.ProfessionIndex Profession;
                        public string DisplayName;
                        public Sprite Icon;
                }

                [SerializeField]
                private Vector2 _bottomRightOffset = new Vector2(-50f, 230f);
                [SerializeField]
                private ProfessionDisplay[] _professionDisplays;

                private Professions _professions;
                private Dictionary<Professions.ProfessionIndex, ProfessionDisplay> _displayLookup;

                public void Bind(Professions professions)
                {
                        if (_professions == professions)
                                return;

                        if (_professions != null)
                        {
                                _professions.ExperienceGained -= OnExperienceGained;
                        }

                        _professions = professions;

                        if (_professions != null)
                        {
                                _professions.ExperienceGained += OnExperienceGained;
                        }

                        HideAll();
                }

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        if (RectTransform != null)
                        {
                                RectTransform.anchorMin = new Vector2(1f, 0f);
                                RectTransform.anchorMax = new Vector2(1f, 0f);
                                RectTransform.pivot     = new Vector2(1f, 0f);
                                RectTransform.anchoredPosition = _bottomRightOffset;
                        }
                }

                protected override void OnDeinitialize()
                {
                        base.OnDeinitialize();
                        Bind(null);
                }

                protected override UIFeedItemBase[] GetFeedItems()
                {
                        return GetComponentsInChildren<UIProfessionExperienceFeedItem>(true);
                }

                private void OnExperienceGained(Professions.ProfessionIndex profession, int amount, Professions.ProfessionSnapshot previousSnapshot, Professions.ProfessionSnapshot newSnapshot)
                {
                        if (_professions == null)
                                return;

                        if (amount <= 0)
                                return;

                        var data = new ProfessionExperienceFeedData
                        {
                                Profession     = profession,
                                ProfessionName = GetProfessionName(profession),
                                ExperienceAmount = amount,
                                NewLevel       = newSnapshot.Level,
                                LevelIncreased = newSnapshot.Level > previousSnapshot.Level,
                        };

                        ShowFeed(data);
                }

                private string GetProfessionName(Professions.ProfessionIndex profession)
                {
                        if (TryGetDisplay(profession, out var display) == true && string.IsNullOrEmpty(display.DisplayName) == false)
                                return display.DisplayName;

                        return profession.ToString();
                }

                private bool TryGetDisplay(Professions.ProfessionIndex profession, out ProfessionDisplay display)
                {
                        EnsureLookup();

                        if (_displayLookup != null && _displayLookup.TryGetValue(profession, out display) == true)
                                return true;

                        display = default;
                        return false;
                }

                private void EnsureLookup()
                {
                        if (_displayLookup != null)
                                return;

                        int capacity = _professionDisplays != null ? _professionDisplays.Length : 0;
                        _displayLookup = new Dictionary<Professions.ProfessionIndex, ProfessionDisplay>(capacity);

                        if (_professionDisplays == null || _professionDisplays.Length == 0)
                                return;

                        for (int i = 0; i < _professionDisplays.Length; ++i)
                        {
                                var display = _professionDisplays[i];
                                _displayLookup[display.Profession] = display;
                        }
                }
        }
}
