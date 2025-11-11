using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSBR.UI
{
	public class GameplayUI : SceneUI
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _gameOverScreenDelay = 3f;

		
		private UIDeathView _deathView;
		private UIGameplayInventoryView _inventoryView;
		
		private bool _gameOverShown;
		private Coroutine _gameOverCoroutine;
		
		private UIAdminConsoleView _adminConsoleView;

		// PUBLIC METHODS

		public void RefreshCursorVisibility()
		{
			bool showCursor = false;

			if (_views != null)
			{
				for (int i = 0; i < _views.Count; i++)
				{
					var view = _views[i];

					if (view.IsOpen == true && view.NeedsCursor == true)
					{
						showCursor = true;
						break;
					}
				}
			}

			Context.Input.RequestCursorVisibility(showCursor, ECursorStateSource.UI);
		}

		// SceneUI INTERFACE

		protected override void OnInitializeInternal()
		{
			base.OnInitializeInternal();

                        EnsureCraftingStationView();
                        EnsureFishingView();
                        EnsureSlotMachineView();

                        _deathView = Get<UIDeathView>();
                        _inventoryView = Get<UIGameplayInventoryView>();
                        Get<UIFishingView>();
            _adminConsoleView = Get<UIAdminConsoleView>();
        }

		protected override void OnActivate()
		{
			base.OnActivate();

			if (Context.Runner.Mode == Fusion.SimulationModes.Server)
			{
				Open<UIDedicatedServerView>();
			}
            _adminConsoleView.Close();
        }

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			
			if (_gameOverCoroutine != null)
			{
				StopCoroutine(_gameOverCoroutine);
				_gameOverCoroutine = null;
			}
			
			_gameOverShown = false;
		}

		protected override void OnTickInternal()
		{
			base.OnTickInternal();

			if (_gameOverShown == true)
				return;
			if (Context.Runner == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			var player = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
			if (player == null || player.Statistics.IsAlive == true)
			{
				_deathView.Close();
			}
			else
			{
				_deathView.Open();
			}

			bool toggleInventory = Keyboard.current.tabKey.wasPressedThisFrame;
			if (toggleInventory)
			{
				_inventoryView.Show(!_inventoryView.MenuVisible);
			}

            bool toggleConsole = Keyboard.current.backquoteKey.wasPressedThisFrame;
            if (toggleConsole)
            {
				if (_adminConsoleView.IsOpen)
					_adminConsoleView.Close();
				else
					_adminConsoleView.Open();

            }

            if (Context.GameplayMode.State == GameplayMode.EState.Finished && _gameOverCoroutine == null)
			{
				_gameOverCoroutine = StartCoroutine(ShowGameOver_Coroutine(_gameOverScreenDelay));
			}
		}

		protected override void OnViewOpened(UIView view)
		{
			RefreshCursorVisibility();
		}

		protected override void OnViewClosed(UIView view)
		{
			RefreshCursorVisibility();
		}
		
		// PRIVATE METHODS

                private void EnsureCraftingStationView()
                {
                        if (Get<UICraftingStationView>() != null)
                                return;

                        CreateViewFromResource<UICraftingStationView>(UICraftingStationView.ResourcePath);
                }

                private void EnsureFishingView()
                {
                        if (Get<UIFishingView>() != null)
                                return;

                        CreateViewFromResource<UIFishingView>(UIFishingView.ResourcePath);
                }

                private void EnsureSlotMachineView()
                {
                        if (Get<UISlotMachineView>() != null)
                                return;

                        CreateViewFromResource<UISlotMachineView>(UISlotMachineView.ResourcePath);
                }

		private IEnumerator ShowGameOver_Coroutine(float delay)
		{
			yield return new WaitForSeconds(delay);
			
			_gameOverShown = true;
			
			_deathView.Close();
			Close<UIGameplayView>();
			Close<UIScoreboardView>();
			Close<UIGameplayMenu>();
			Close<UIAnnouncementsView>();
			Close<UIGameplayInventoryView>();
			
			Open<UIGameOverView>();

			_gameOverCoroutine = null;
		}
	}
}
