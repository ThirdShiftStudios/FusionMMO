namespace TPSBR.UI
{
        public class MenuUI : SceneUI
        {
                // SceneUI INTERFACE

                private bool _characterFlowInitialized;

                protected override void OnInitializeInternal()
                {
                        base.OnInitializeInternal();

                        Context.Input.RequestCursorVisibility(true, ECursorStateSource.Menu);

                        if (Context.PlayerData.Nickname.HasValue() == false)
                        {
                                var changeNicknameView = Open<UIChangeNicknameView>();
                                changeNicknameView.SetData("ENTER NICKNAME", true);
                        }

                        if (Global.PlayerCloudSaveService != null)
                        {
                                Global.PlayerCloudSaveService.CharactersChanged += OnCloudCharactersChanged;
                                Global.PlayerCloudSaveService.ActiveCharacterChanged += OnCloudActiveCharacterChanged;
                        }
                }

                protected override void OnDeinitializeInternal()
                {
                        Context.Input.RequestCursorVisibility(false, ECursorStateSource.Menu);

                        if (Global.PlayerCloudSaveService != null)
                        {
                                Global.PlayerCloudSaveService.CharactersChanged -= OnCloudCharactersChanged;
                                Global.PlayerCloudSaveService.ActiveCharacterChanged -= OnCloudActiveCharacterChanged;
                        }

                        _characterFlowInitialized = false;

                        base.OnDeinitializeInternal();
                }

                protected override void OnActivate()
                {
                        base.OnActivate();

                        if (Global.Networking.ErrorStatus.HasValue() == true)
                        {
                                Open<UIMultiplayerView>();
                                var errorDialog = Open<UIErrorDialogView>();

                                errorDialog.Title.text = "Connection Issue";

                                if (Global.Networking.ErrorStatus == Networking.STATUS_SERVER_CLOSED)
                                {
                                        errorDialog.Description.text = $"Server was closed.";
                                }
                                else
                                {
                                        errorDialog.Description.text = $"Failed to start network game\n\nReason:\n{Global.Networking.ErrorStatus}";
                                }

                                Global.Networking.ClearErrorStatus();
                        }

                        EnsureCharacterViews();
                }

                private void EnsureCharacterViews()
                {
                        if (_characterFlowInitialized == true)
                                return;

                        var cloud = Global.PlayerCloudSaveService;
                        if (cloud == null || cloud.IsInitialized == false)
                                return;

                        if (cloud.Characters.Count == 0)
                        {
                                var createView = Open<UICreateCharacterView>();
                                if (createView != null)
                                {
                                        var selectionView = Get<UISelectCharacterView>();
                                        if (selectionView != null)
                                        {
                                                createView.BackView = selectionView;
                                        }
                                }
                                _characterFlowInitialized = true;
                                return;
                        }

                        if (cloud.ActiveCharacterId.HasValue() == false)
                        {
                                Open<UISelectCharacterView>();
                                _characterFlowInitialized = true;
                        }
                        else
                        {
                                _characterFlowInitialized = true;
                        }
                }

                private void OnCloudCharactersChanged()
                {
                        _characterFlowInitialized = false;
                        EnsureCharacterViews();
                }

                private void OnCloudActiveCharacterChanged(string characterId)
                {
                        if (characterId.HasValue() == true)
                        {
                                _characterFlowInitialized = true;
                        }
                        else
                        {
                                _characterFlowInitialized = false;
                                EnsureCharacterViews();
                        }
                }
        }
}
