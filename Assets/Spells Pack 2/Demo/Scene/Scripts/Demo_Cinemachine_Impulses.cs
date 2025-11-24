using UnityEngine;
using Unity.Cinemachine;
using System.Globalization;

namespace ZakhanSpellsPack2
{
	public class Demo_Cinemachine_Impulses : MonoBehaviour
    {
        CinemachineImpulseSource Source;
		CinemachineImpulseListener Listener;
		private void Start()
		{
			Source = GetComponent<CinemachineImpulseSource>();
			Listener = GetComponent<CinemachineImpulseListener>();
		}
        public void Impulse (string Impulse)
        {
			
			// 0.1, 0.1, 0.1, Duration
			Vector4 RImpulse = StringToVector4(Impulse);
			float RDuration = RImpulse.w;

			Vector3 RVelocity = new Vector3(RImpulse.x, RImpulse.y, RImpulse.z);

			if(Source && Listener)
			{
				Listener.ReactionSettings.Duration = RDuration;
				Source.ImpulseDefinition.ImpulseDuration = RDuration;

				Source.GenerateImpulseWithVelocity(RVelocity);
            }
			
		}

		public void StopImpulse() 
		{
			if (Source && Listener)
			{
				Listener.ReactionSettings.Duration = 0.0001f;
				Source.ImpulseDefinition.ImpulseDuration = 0.0001f;
			}
		}

		Vector4 StringToVector4(string sVector)
		{

			if (sVector.StartsWith("(") && sVector.EndsWith(")"))
			{
				sVector = sVector.Substring(1, sVector.Length - 2);
			}

			string[] sArray = sVector.Split(',');

			Vector4 result = new Vector4(
				float.Parse(sArray[0], CultureInfo.InvariantCulture),
				float.Parse(sArray[1], CultureInfo.InvariantCulture),
				float.Parse(sArray[2], CultureInfo.InvariantCulture),
				float.Parse(sArray[3], CultureInfo.InvariantCulture));

			return result;
		}
	}
}
