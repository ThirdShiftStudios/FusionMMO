using System;
using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public class UISelectCharacterView : UICloseView
    {
        [SerializeField]
        private UICharacterList _characterList;
        [SerializeField]
        private UIButton _createCharacterButton;
        [SerializeField]
        private UIButton _selectCharacterButton;
        [SerializeField]
        private TextMeshProUGUI _activeCharacterLabel;
        [SerializeField]
        private TextMeshProUGUI _emptyStateLabel;
        [SerializeField]
        private UIBehaviour _emptyStateGroup;

        private string _selectedCharacterId;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_characterList != null)
            {
                _characterList.SelectionChanged += OnSelectionChanged;
                _characterList.UpdateContent += OnUpdateCharacterContent;
            }

            if (_createCharacterButton != null)
            {
                _createCharacterButton.onClick.AddListener(OnCreateCharacterButton);
            }

            if (_selectCharacterButton != null)
            {
                _selectCharacterButton.onClick.AddListener(OnSelectCharacterButton);
            }
        }

        protected override void OnDeinitialize()
        {
            if (_characterList != null)
            {
                _characterList.SelectionChanged -= OnSelectionChanged;
                _characterList.UpdateContent -= OnUpdateCharacterContent;
            }

            if (_createCharacterButton != null)
            {
                _createCharacterButton.onClick.RemoveListener(OnCreateCharacterButton);
            }

            if (_selectCharacterButton != null)
            {
                _selectCharacterButton.onClick.RemoveListener(OnSelectCharacterButton);
            }

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            var cloud = Global.PlayerCloudSaveService;
            if (cloud != null)
            {
                cloud.CharactersChanged += OnCharactersChanged;
                cloud.ActiveCharacterChanged += OnActiveCharacterChanged;
            }

            RefreshCharacters();
        }

        protected override void OnClose()
        {
            var cloud = Global.PlayerCloudSaveService;
            if (cloud != null)
            {
                cloud.CharactersChanged -= OnCharactersChanged;
                cloud.ActiveCharacterChanged -= OnActiveCharacterChanged;
            }

            base.OnClose();
        }

        private void OnCharactersChanged()
        {
            RefreshCharacters();
        }

        private void OnActiveCharacterChanged(string characterId)
        {
            if (string.Equals(_selectedCharacterId, characterId, StringComparison.Ordinal) == true)
            {
                UpdateSelectButtonState();
            }

            UpdateActiveCharacterLabel();
        }

        private void OnSelectionChanged(int index)
        {
            var cloud = Global.PlayerCloudSaveService;
            var characters = cloud != null ? cloud.GetCharacters() : null;

            if (characters != null && index >= 0 && index < characters.Count)
            {
                _selectedCharacterId = characters[index]?.CharacterId;
            }
            else
            {
                _selectedCharacterId = null;
            }

            UpdateSelectButtonState();

            if (cloud != null)
            {
                TryApplySelectedCharacter(cloud);
            }

            UpdateActiveCharacterLabel();
        }

        private void OnUpdateCharacterContent(int index, MonoBehaviour content)
        {
            var view = content as UICharacterListItemView;
            if (view == null)
                return;

            var cloud = Global.PlayerCloudSaveService;
            var characters = cloud != null ? cloud.GetCharacters() : null;
            PlayerCharacterSaveData character = null;

            if (characters != null && index >= 0 && index < characters.Count)
            {
                character = characters[index];
            }

            view.SetCharacter(character);
        }

        private void OnCreateCharacterButton()
        {
            var createView = SceneUI.Get<UICreateCharacterView>();
            if (createView == null)
                return;

            createView.BackView = this;
            createView.Open();
            Close();
        }

        private void OnSelectCharacterButton()
        {
            if (_selectedCharacterId.HasValue() == false)
                return;

            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null)
                return;

            if (TryApplySelectedCharacter(cloud) == true)
            {
                CloseWithBack();
            }
        }

        private void RefreshCharacters()
        {
            var cloud = Global.PlayerCloudSaveService;
            var characters = cloud != null ? cloud.GetCharacters() : null;
            int count = characters != null ? characters.Count : 0;

            if (_characterList != null)
            {
                _characterList.Refresh(count, false);
            }

            if (count > 0 && cloud != null)
            {
                var activeId = cloud.ActiveCharacterId;
                if (activeId.HasValue() == true)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var character = characters[i];
                        if (character != null && string.Equals(character.CharacterId, activeId, StringComparison.Ordinal) == true)
                        {
                            _characterList.Selection = i;
                            _selectedCharacterId = character.CharacterId;
                            break;
                        }
                    }
                }

                if (_selectedCharacterId.HasValue() == false && count > 0)
                {
                    _characterList.Selection = 0;
                    _selectedCharacterId = characters[0]?.CharacterId;
                }
            }
            else
            {
                _selectedCharacterId = null;
                if (_characterList != null)
                {
                    _characterList.Selection = -1;
                }
            }

            UpdateEmptyState(count == 0);
            UpdateSelectButtonState();
            UpdateActiveCharacterLabel();
        }

        private void UpdateSelectButtonState()
        {
            if (_selectCharacterButton != null)
            {
                _selectCharacterButton.interactable = _selectedCharacterId.HasValue();
            }
        }

        private void UpdateActiveCharacterLabel()
        {
            if (_activeCharacterLabel == null)
                return;

            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null)
            {
                _activeCharacterLabel.text = string.Empty;
                return;
            }

            var selectedCharacter = _selectedCharacterId.HasValue() ? cloud.GetCharacter(_selectedCharacterId) : null;
            var activeCharacter = selectedCharacter != null ? selectedCharacter : cloud.GetCharacter(cloud.ActiveCharacterId);

            _activeCharacterLabel.text = activeCharacter != null ? activeCharacter.CharacterName : string.Empty;
        }

        private bool TryApplySelectedCharacter(PlayerCloudSaveService cloud)
        {
            if (cloud == null || _selectedCharacterId.HasValue() == false)
                return false;

            if (string.Equals(cloud.ActiveCharacterId, _selectedCharacterId, StringComparison.Ordinal) == true)
                return true;

            return cloud.SelectCharacter(_selectedCharacterId);
        }

        private void UpdateEmptyState(bool isEmpty)
        {
            if (_emptyStateGroup != null)
            {
                _emptyStateGroup.SetActive(isEmpty);
            }

            if (_emptyStateLabel != null)
            {
                _emptyStateLabel.gameObject.SetActive(isEmpty);
                if (isEmpty == true)
                {
                    _emptyStateLabel.text = "No characters found";
                }
            }
        }
    }
}
