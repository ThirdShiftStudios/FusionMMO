using System;
using UnityEngine;

namespace TPSBR.UI
{
    public class UICharacterDetailsView : UIWidget
    {
        [SerializeField] private UICharacterDetails characterDetails;
        [SerializeField] private UIStatDetails _statDetails;
        [SerializeField] private UIProfessionDetails _professionDetails;

        private string _lastCharacterName;
        private string _lastCharacterClass;
        private int _lastCharacterLevel = -1;
        private bool _hasCharacterData;

        private int[] _statBuffer;
        private bool _hasStatData;

        private Professions.ProfessionSnapshot[] _professionBuffer;
        private bool _hasProfessionData;

        public void UpdateCharacterDetails(PlayerData playerData)
        {
            if (characterDetails == null || playerData == null)
            {
                return;
            }

            string characterName = playerData.CharacterName;
            string characterClass = playerData.GetCharacterClassName();
            int characterLevel = playerData.Level;

            if (_hasCharacterData == true &&
                _lastCharacterLevel == characterLevel &&
                string.Equals(_lastCharacterName, characterName, StringComparison.Ordinal) == true &&
                string.Equals(_lastCharacterClass, characterClass, StringComparison.Ordinal) == true)
            {
                return;
            }

            _lastCharacterName = characterName;
            _lastCharacterClass = characterClass;
            _lastCharacterLevel = characterLevel;
            _hasCharacterData = true;

            characterDetails.SetData(playerData);
        }

        public void UpdateStats(Stats stats)
        {
            if (_statDetails == null)
            {
                return;
            }

            if (stats == null)
            {
                if (_hasStatData == true)
                {
                    _statDetails.SetStats(null);
                    _statBuffer = null;
                    _hasStatData = false;
                }

                return;
            }

            if (_statBuffer == null || _statBuffer.Length != Stats.Count)
            {
                _statBuffer = new int[Stats.Count];
            }

            bool changed = _hasStatData == false;

            for (int i = 0; i < Stats.Count; ++i)
            {
                int statValue = stats.GetStat(i);

                if (_statBuffer[i] != statValue)
                {
                    _statBuffer[i] = statValue;
                    changed = true;
                }
            }

            if (changed == true)
            {
                _statDetails.SetStats(_statBuffer);
                _hasStatData = true;
            }
        }

        public void UpdateProfessions(Professions professions)
        {
            if (_professionDetails == null)
            {
                return;
            }

            if (professions == null)
            {
                if (_hasProfessionData == true)
                {
                    _professionDetails.SetProfessions(null);
                    _professionBuffer = null;
                    _hasProfessionData = false;
                }

                return;
            }

            if (_professionBuffer == null || _professionBuffer.Length != Professions.Count)
            {
                _professionBuffer = new Professions.ProfessionSnapshot[Professions.Count];
            }

            bool changed = _hasProfessionData == false;

            for (int i = 0; i < Professions.Count; ++i)
            {
                var snapshot = professions.GetSnapshot(i);
                var cachedSnapshot = _professionBuffer[i];

                if (_hasProfessionData == false ||
                    cachedSnapshot.Level != snapshot.Level ||
                    cachedSnapshot.Experience != snapshot.Experience ||
                    cachedSnapshot.ExperienceToNextLevel != snapshot.ExperienceToNextLevel)
                {
                    _professionBuffer[i] = snapshot;
                    changed = true;
                }
            }

            if (changed == true)
            {
                _professionDetails.SetProfessions(_professionBuffer);
                _hasProfessionData = true;
            }
        }
    }
}
