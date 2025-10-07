namespace TPSBR
{
	using UnityEngine;
	using UnityEngine.Playables;
	using Fusion.Addons.KCC;
	using Fusion.Addons.AnimationController;

	public sealed class ShootState : MultiClipState
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _animationPower = 1.0f;

		private KCC     _kcc;
		private Inventory _inventory;

		// MultiClipState INTERFACE

                protected override int GetClipID()
                {
                        WeaponSize currentSize = _inventory.CurrentWeaponSize;
                        int stanceIndex = currentSize.ToStanceIndex();

                        int setCount = Nodes != null && Nodes.Length > 0 ? Mathf.Max(1, Nodes.Length / 2) : 1;
                        stanceIndex = Mathf.Clamp(stanceIndex, 0, setCount - 1);

                        return stanceIndex * 2 + 1;
                }

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_kcc     = Controller.GetComponentNoAlloc<KCC>();
			_inventory = Controller.GetComponentNoAlloc<Inventory>();
		}

		protected override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			int clipID = GetClipID();
			int idleID = clipID - 1;

			Mixer.SetInputWeight(idleID, 1.0f - _animationPower);
			Mixer.SetInputWeight(clipID, _animationPower);

			Nodes[idleID].PlayableClip.SetTime(AnimationTime);
		}

		protected override void OnInterpolate()
		{
			base.OnInterpolate();

			int clipID = GetClipID();
			int idleID = clipID - 1;

			Mixer.SetInputWeight(idleID, 1.0f - _animationPower);
			Mixer.SetInputWeight(clipID, _animationPower);

			Nodes[idleID].PlayableClip.SetTime(InterpolatedAnimationTime);
		}
	}
}
