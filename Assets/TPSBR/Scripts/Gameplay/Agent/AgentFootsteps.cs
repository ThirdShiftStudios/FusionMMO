using UnityEngine;

namespace TPSBR
{
	[DefaultExecutionOrder(10000)]
	public sealed class AgentFootsteps : MonoBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private AudioEffect _leftFootEffect;
		[SerializeField]
		private AudioEffect _rightFootEffect;
		[SerializeField]
		private FootstepSetup _footstepSetup;
		[SerializeField]
		private float _defaultDelay = 0.03f;
		[SerializeField]
		private float _minFootstepTime = 0.2f;
		[SerializeField]
		private float _checkDistanceUp = 0.1f;
		[SerializeField]
		private float _checkDistanceDown = 0.05f;
		[SerializeField]
		private float _maxStepCorrectionTime = 0.7f;
		[SerializeField]
		private float _minSpeed = 0.1f;
		[SerializeField]
		private float _runSpeedTreshold = 2f;
		[SerializeField]
		private LayerMask _hitMask;

		private FootData _leftFootData = new FootData();
		private FootData _rightFootData = new FootData();

		private float _lastStepTime;

		private Agent     _agent;
		private Character _character;

		// PUBLIC METHODS

		public void OnAgentRender()
		{
			PlayFootsteps();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_agent     = GetComponent<Agent>();
			_character = GetComponent<Character>();

			_leftFootData.Transform = _character.ThirdPersonView.LeftFoot;
			_rightFootData.Transform = _character.ThirdPersonView.RightFoot;

			_leftFootData.Effect = _leftFootEffect;
			_rightFootData.Effect = _rightFootEffect;
		}

		// PRIVATE METHODS

		private void PlayFootsteps()
		{
			_leftFootData.Cooldown -= Time.deltaTime;
			_rightFootData.Cooldown -= Time.deltaTime;

			if (_character.CharacterController.FixedData.IsGrounded == false)
			{
				_leftFootData.IsUp = true;
				_rightFootData.IsUp = true;
				return;
			}

			if (_character.CharacterController.FixedData.RealSpeed < _minSpeed)
				return;

			float timeFromLastStep = Time.time - _lastStepTime;

			CheckFoot(_leftFootData, timeFromLastStep);
			CheckFoot(_rightFootData, timeFromLastStep);
		}

		private void CheckFoot(FootData foot, float timeFromAnyLastStep)
		{
			if (foot.IsUp == true && foot.Cooldown > 0f)
				return;

			float distanceFromBottom = (foot.Transform.position - transform.position).y;

			if (foot.IsUp == false && distanceFromBottom > _checkDistanceUp)
			{
				foot.IsUp = true;
			}
			else if (foot.IsUp == true && distanceFromBottom < _checkDistanceDown)
			{
                                var surface = GetSurface(foot.Transform);
                                var newSetupSource = _footstepSetup.GetSound(surface, _character.CharacterController.FixedData.RealSpeed > _runSpeedTreshold);

				if (foot.SetupSource != newSetupSource)
				{
					foot.Setup.CopyFrom(newSetupSource);
					foot.SetupSource = newSetupSource;
				}

				float timeFromThisLastStep = -foot.Cooldown + _minFootstepTime;

				if (timeFromAnyLastStep < _maxStepCorrectionTime && timeFromThisLastStep * 0.5f < _maxStepCorrectionTime)
				{
					// Pace correction
					foot.Setup.Delay = (timeFromThisLastStep * 0.5f - timeFromAnyLastStep) * 0.75f;
				}
				else
				{
					foot.Setup.Delay = _defaultDelay;
				}

				foot.Effect.Play(foot.Setup, EForceBehaviour.ForceAny);

				// Make sure same sound is not played twice in a row
				_leftFootData.Effect.LastPlayedClipIndex = foot.Effect.LastPlayedClipIndex;
				_rightFootData.Effect.LastPlayedClipIndex = foot.Effect.LastPlayedClipIndex;

				_lastStepTime = Time.time + foot.Setup.Delay;

				foot.IsUp = false;
				foot.Cooldown = _minFootstepTime;
			}
		}

                private FootstepSurface GetSurface(Transform foot)
                {
                        var physicsScene = _agent.Runner.SimulationUnityScene.GetPhysicsScene();
                        if (physicsScene.Raycast(foot.position + Vector3.up, Vector3.down, out RaycastHit hit, 1.5f, _hitMask, QueryTriggerInteraction.Collide) == true)
                        {
                                var collider = hit.collider;
                                if (collider != null)
                                {
                                        Terrain terrain = collider.GetComponent<Terrain>();
                                        if (terrain == null)
                                        {
                                                terrain = collider.GetComponentInParent<Terrain>();
                                        }

                                        TerrainLayer terrainLayer = null;
                                        Texture2D terrainTexture = null;

                                        if (terrain != null)
                                        {
                                                var terrainData = terrain.terrainData;
                                                if (terrainData != null && terrainData.alphamapLayers > 0 && terrainData.alphamapWidth > 0 && terrainData.alphamapHeight > 0)
                                                {
                                                        Vector3 terrainLocalPos = terrain.transform.InverseTransformPoint(hit.point);
                                                        Vector3 terrainSize = terrainData.size;

                                                        float normalizedX = terrainSize.x > 0f ? Mathf.Clamp01(terrainLocalPos.x / terrainSize.x) : 0f;
                                                        float normalizedZ = terrainSize.z > 0f ? Mathf.Clamp01(terrainLocalPos.z / terrainSize.z) : 0f;

                                                        int mapX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (terrainData.alphamapWidth - 1)), 0, terrainData.alphamapWidth - 1);
                                                        int mapZ = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (terrainData.alphamapHeight - 1)), 0, terrainData.alphamapHeight - 1);

                                                        var alphamaps = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
                                                        int dominantLayer = 0;
                                                        float maxWeight = 0f;

                                                        for (int i = 0; i < terrainData.alphamapLayers; i++)
                                                        {
                                                                float weight = alphamaps[0, 0, i];
                                                                if (weight > maxWeight)
                                                                {
                                                                        maxWeight = weight;
                                                                        dominantLayer = i;
                                                                }
                                                        }

                                                        var layers = terrainData.terrainLayers;
                                                        if (layers != null && dominantLayer >= 0 && dominantLayer < layers.Length)
                                                        {
                                                                terrainLayer = layers[dominantLayer];
                                                                if (terrainLayer != null)
                                                                {
                                                                        terrainTexture = terrainLayer.diffuseTexture;
                                                                }
                                                        }
                                                }
                                        }

                                        int tagHash = collider.tag.GetHashCode();
                                        return new FootstepSurface(tagHash, terrainLayer, terrainTexture);
                                }
                        }

                        return new FootstepSurface(0, null, null);
                }

		// HELPERS

		private class FootData
		{
			public bool        IsUp;
			public float       Cooldown;
			public Transform   Transform;
			public AudioEffect Effect;
			public AudioSetup  Setup = new AudioSetup();
			public AudioSetup  SetupSource;
		}
	}
}
