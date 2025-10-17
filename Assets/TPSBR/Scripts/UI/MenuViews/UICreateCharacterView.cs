using System;
using TMPro;
using TSS.Data;
using UnityEngine;

namespace TPSBR.UI
{
        public class UICreateCharacterView : UICloseView
        {
                [SerializeField]
                private UIList _definitionList;
                [SerializeField]
                private TMP_InputField _nameInput;
                [SerializeField]
                private UIButton _createButton;
                [SerializeField]
                private TextMeshProUGUI _errorLabel;

                private CharacterDefinition[] _definitions = Array.Empty<CharacterDefinition>();
                private int _selectedDefinitionIndex = -1;

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        if (_definitionList != null)
                        {
                                _definitionList.SelectionChanged += OnDefinitionSelectionChanged;
                                _definitionList.UpdateContent += OnUpdateDefinitionContent;
                        }

                        if (_nameInput != null)
                        {
                                _nameInput.onValueChanged.AddListener(OnNameChanged);
                        }

                        if (_createButton != null)
                        {
                                _createButton.onClick.AddListener(OnCreateButton);
                        }
                }

                protected override void OnDeinitialize()
                {
                        if (_definitionList != null)
                        {
                                _definitionList.SelectionChanged -= OnDefinitionSelectionChanged;
                                _definitionList.UpdateContent -= OnUpdateDefinitionContent;
                        }

                        if (_nameInput != null)
                        {
                                _nameInput.onValueChanged.RemoveListener(OnNameChanged);
                        }

                        if (_createButton != null)
                        {
                                _createButton.onClick.RemoveListener(OnCreateButton);
                        }

                        base.OnDeinitialize();
                }

                protected override void OnOpen()
                {
                        base.OnOpen();

                        RefreshDefinitions();
                        ResetForm();
                }

                private void RefreshDefinitions()
                {
                        var definitions = CharacterDefinition.GetAll();
                        if (definitions == null || definitions.Count == 0)
                        {
                                _definitions = Array.Empty<CharacterDefinition>();
                        }
                        else
                        {
                                _definitions = new CharacterDefinition[definitions.Count];
                                for (int i = 0; i < definitions.Count; i++)
                                {
                                        _definitions[i] = definitions[i];
                                }
                        }

                        if (_definitionList != null)
                        {
                                _definitionList.Refresh(_definitions.Length, false);
                        }

                        if (_definitions.Length > 0)
                        {
                                _selectedDefinitionIndex = 0;
                                if (_definitionList != null)
                                {
                                        _definitionList.Selection = 0;
                                }
                        }
                        else
                        {
                                _selectedDefinitionIndex = -1;
                                if (_definitionList != null)
                                {
                                        _definitionList.Selection = -1;
                                }
                        }

                        UpdateCreateButtonState();
                }

                private void ResetForm()
                {
                        if (_nameInput != null)
                        {
                                _nameInput.text = string.Empty;
                        }

                        UpdateErrorMessage(null);
                        UpdateCreateButtonState();
                }

                private void OnDefinitionSelectionChanged(int index)
                {
                        _selectedDefinitionIndex = index;
                        UpdateCreateButtonState();
                }

                private void OnUpdateDefinitionContent(int index, MonoBehaviour content)
                {
                        var view = content as UICharacterListItemView;
                        if (view == null)
                                return;

                        CharacterDefinition definition = null;
                        if (index >= 0 && index < _definitions.Length)
                        {
                                definition = _definitions[index];
                        }

                        view.SetDefinition(definition);
                }

                private void OnNameChanged(string value)
                {
                        ValidateName();
                        UpdateCreateButtonState();
                }

                private void OnCreateButton()
                {
                        if (_selectedDefinitionIndex < 0 || _selectedDefinitionIndex >= _definitions.Length)
                                return;

                        var cloud = Global.PlayerCloudSaveService;
                        if (cloud == null)
                        {
                                UpdateErrorMessage("Character service unavailable");
                                return;
                        }

                        string name = _nameInput != null ? _nameInput.text : string.Empty;
                        if (TryValidateName(name, cloud, out string sanitizedName) == false)
                        {
                                return;
                        }

                        var definition = _definitions[_selectedDefinitionIndex];
                        if (definition == null)
                        {
                                UpdateErrorMessage("Select a character type");
                                return;
                        }

                        var created = cloud.CreateCharacter(sanitizedName, definition);
                        if (created == null)
                        {
                                UpdateErrorMessage("Unable to create character");
                                return;
                        }

                        cloud.SelectCharacter(created.CharacterId);
                        CloseWithBack();
                }

                private void UpdateCreateButtonState()
                {
                        if (_createButton == null)
                                return;

                        var cloud = Global.PlayerCloudSaveService;
                        if (cloud == null)
                        {
                                _createButton.interactable = false;
                                return;
                        }

                        string name = _nameInput != null ? _nameInput.text : string.Empty;
                        bool validName = TryValidateName(name, cloud, out _);
                        bool hasDefinition = _selectedDefinitionIndex >= 0 && _selectedDefinitionIndex < _definitions.Length;

                        _createButton.interactable = validName && hasDefinition;
                }

                private void UpdateErrorMessage(string message)
                {
                        if (_errorLabel == null)
                                return;

                        if (string.IsNullOrEmpty(message) == true)
                        {
                                _errorLabel.gameObject.SetActive(false);
                                _errorLabel.text = string.Empty;
                        }
                        else
                        {
                                _errorLabel.gameObject.SetActive(true);
                                _errorLabel.text = message;
                        }
                }

                private bool ValidateName()
                {
                        var cloud = Global.PlayerCloudSaveService;
                        if (cloud == null)
                        {
                                UpdateErrorMessage("Character service unavailable");
                                return false;
                        }

                        string name = _nameInput != null ? _nameInput.text : string.Empty;
                        if (TryValidateName(name, cloud, out _) == false)
                        {
                                return false;
                        }

                        UpdateErrorMessage(null);
                        return true;
                }

                private bool TryValidateName(string name, PlayerCloudSaveService cloud, out string sanitized)
                {
                        sanitized = string.IsNullOrWhiteSpace(name) == true ? string.Empty : name.Trim();

                        if (sanitized.HasValue() == false)
                        {
                                UpdateErrorMessage("Enter a character name");
                                return false;
                        }

                        if (cloud.IsCharacterNameAvailable(sanitized) == false)
                        {
                                UpdateErrorMessage("Name already in use");
                                return false;
                        }

                        UpdateErrorMessage(null);
                        return true;
                }
        }
}
