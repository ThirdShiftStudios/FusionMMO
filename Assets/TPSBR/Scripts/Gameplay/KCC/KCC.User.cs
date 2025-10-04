namespace Fusion.Addons.KCC
{
	using UnityEngine;
	using System.Collections.Generic;
	using TPSBR;

	public partial class KCC
	{
		public MoveState MoveState { get; set; }

		partial void InitializeUserNetworkProperties(KCCNetworkContext networkContext, List<IKCCNetworkProperty> networkProperties)
		{
			networkProperties.Add(new KCCNetworkVector2<KCCNetworkContext>(networkContext, 0.0f, (context, value) => context.Data.Recoil     = value, (context) => context.Data.Recoil,     null));
			networkProperties.Add(new KCCNetworkBool   <KCCNetworkContext>(networkContext,       (context, value) => context.Data.IsGrounded = value, (context) => context.Data.IsGrounded, null));
			networkProperties.Add(new KCCNetworkBool   <KCCNetworkContext>(networkContext,       (context, value) => context.Data.Aim        = value, (context) => context.Data.Aim,        null));
		}
	}

	public partial class KCCData
	{
		public bool    Aim;
		public Vector2 Recoil;

		partial void CopyUserDataFromOther(KCCData other)
		{
			Aim    = other.Aim;
			Recoil = other.Recoil;
		}
	}
}
