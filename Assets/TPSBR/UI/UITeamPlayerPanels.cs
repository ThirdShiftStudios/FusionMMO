using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
    public class UITeamPlayerPanels : UIBehaviour
    {
        private List<UITeamPlayerPanel> _teamPlayerPanels;
        [SerializeField]
        private GameObject _panelPrefab;
        private bool _initialized;

        public void UpdateTeamPlayerPanels(SceneContext context, Agent observedAgent)
        {
            if (_initialized == false)
            {
                Initialize();
            }

            var allPlayers = context.NetworkGame.ActivePlayers;
            int totalOtherPlayers = allPlayers.Count - 1;
            EnsureCapacity(totalOtherPlayers);

            for (int i = 0; i < _teamPlayerPanels.Count; i++)
            {
                var panel = _teamPlayerPanels[i];
                panel.GameObject.SetActive(false);
                panel.Buffs?.Clear();
            }
            if (totalOtherPlayers <= 0)
            {
                return;
            }
            int playerCounter = 0;

            for (int i = 0; i < allPlayers.Count; i++)
            {
                if(allPlayers[i].Object == false)continue;
                if(allPlayers[i].IsInitialized == false)continue;
                
                if(allPlayers[i].ActiveAgent == false) continue;
                if(allPlayers[i].ActiveAgent.Object == false) continue;
                if(allPlayers[i].ActiveAgent.Health == false) continue;
                if(allPlayers[i].ActiveAgent == observedAgent) continue;

                
                var panel = _teamPlayerPanels[playerCounter];
                var agent = allPlayers[i].ActiveAgent;

                panel.GameObject.SetActive(true);
                panel.Health.UpdateHealth(agent.Health);
                panel.Mana?.UpdateMana(agent.Mana);
                panel.Stamina?.UpdateStamina(agent.Stamina);
                panel.Player.SetData(context, allPlayers[i]);
                panel.Buffs?.UpdateBuffs(agent);
                playerCounter++;
            }
        }

        private void Initialize()
        {
            var uiHealthComponents = GetComponentsInChildren<UIHealth>(true);
            _teamPlayerPanels = new List<UITeamPlayerPanel>(uiHealthComponents.Length);

            for (int i = 0; i < uiHealthComponents.Length; i++)
            {
                _teamPlayerPanels.Add(new UITeamPlayerPanel(uiHealthComponents[i].gameObject));
            }

            if (uiHealthComponents.Length > 0)
            {
                _panelPrefab = uiHealthComponents[0].gameObject;
            }
            _initialized = true;
        }

        private void EnsureCapacity(int requiredCount)
        {
            if (requiredCount <= 0 || _panelPrefab == null)
                return;

            Transform parent = _panelPrefab.transform.parent != null ? _panelPrefab.transform.parent : transform;

            while (_teamPlayerPanels.Count < requiredCount)
            {
                var panelObject = Instantiate(_panelPrefab, parent);
                panelObject.name = $"{_panelPrefab.name}_{_teamPlayerPanels.Count}";
                _teamPlayerPanels.Add(new UITeamPlayerPanel(panelObject));
            }
        }

        private class UITeamPlayerPanel
        {
            public GameObject GameObject => _gameObject;
            public UIHealth Health => _health;
            public UIMana Mana => _mana;
            public UIStamina Stamina => _stamina;
            public UIPlayer Player => _player;
            public UIBuffsWidget Buffs => _buffs;

            private GameObject _gameObject;
            private UIHealth  _health;
            private UIMana    _mana;
            private UIStamina _stamina;
            private UIPlayer  _player;
            private UIBuffsWidget _buffs;

            public UITeamPlayerPanel(GameObject gameObject)
            {
                _gameObject = gameObject;
                _health = gameObject.GetComponent<UIHealth>();
                _mana = gameObject.GetComponent<UIMana>();
                _stamina = gameObject.GetComponent<UIStamina>();
                _player = gameObject.GetComponent<UIPlayer>();
                _buffs = gameObject.GetComponent<UIBuffsWidget>();
            }
        }
    }
}
