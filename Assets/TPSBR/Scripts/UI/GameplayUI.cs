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
		private UIGameplayInventory _inventoryView;
		private const string _vendorViewResourcePath = "UI/GameplayViews/UIVendorView";
		
		private bool _gameOverShown;
		private Coroutine _gameOverCoroutine;
		

		// PUBLIC METHODS

		public void RefreshCursorVisibility()
		{
			bool showCursor = false;

			for (int i = 0; i < _views.Length; i++)
			{
				var view = _views[i];

				if (view.IsOpen == true && view.NeedsCursor == true)
				{
					showCursor = true;
					break;
				}
			}

			Context.Input.RequestCursorVisibility(showCursor, ECursorStateSource.UI);
		}

		// SceneUI INTERFACE

		protected override void OnInitializeInternal()
		{
			base.OnInitializeInternal();

			EnsureVendorView();

			_deathView = Get<UIDeathView>();
			_inventoryView = Get<UIGameplayInventory>();
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			if (Context.Runner.Mode == Fusion.SimulationModes.Server)
			{
				Open<UIDedicatedServerView>();
			}
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

			bool toggleInventory = Keyboard.current.iKey.wasPressedThisFrame;
			if (toggleInventory)
			{
				_inventoryView.Show(!_inventoryView.MenuVisible);
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
		
		private IEnumerator ShowGameOver_Coroutine(float delay)
		{
			yield return new WaitForSeconds(delay);
			
			_gameOverShown = true;
			
			_deathView.Close();
			Close<UIGameplayView>();
			Close<UIScoreboardView>();
			Close<UIGameplayMenu>();
			Close<UIAnnouncementsView>();
			Close<UIGameplayInventory>();
			
			Open<UIGameOverView>();

			_gameOverCoroutine = null;
		}

		private void EnsureVendorView()
		{
			if (Get<UIVendorView>() != null)
				return;

			if (Canvas == null)
				return;

			var vendorPrefab = Resources.Load<UIVendorView>(_vendorViewResourcePath);

			if (vendorPrefab == null)
			{
				Debug.LogWarning($"Unable to locate {nameof(UIVendorView)} prefab at Resources/{_vendorViewResourcePath}.");
				return;
			}

			var vendorView = Instantiate(vendorPrefab, Canvas.transform, false);
			vendorView.gameObject.name = vendorPrefab.gameObject.name;

			if (RegisterView(vendorView) == false)
			{
				Destroy(vendorView.gameObject);
			}
		}

	}
}
