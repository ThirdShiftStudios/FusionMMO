using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

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
		private int       _lastClipIndex = -1;

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

			InitializeFootAudio(_leftFootData, _leftFootEffect);
			InitializeFootAudio(_rightFootData, _rightFootEffect);
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
				CancelFootstepDelay(_leftFootData);
				CancelFootstepDelay(_rightFootData);
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

				PlayFootstepSound(foot);

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

		private void PlayFootstepSound(FootData foot)
		{
			var sceneAudio = _agent?.Context?.Audio;
			if (sceneAudio == null)
				return;

			var setup = foot.Setup;
			if (setup == null || setup.Clips == null || setup.Clips.Length == 0)
				return;

			int clipIndex = Random.Range(0, setup.Clips.Length);

			if (setup.Clips.Length > 1 && clipIndex == _lastClipIndex)
			{
				clipIndex = (clipIndex + 1) % setup.Clips.Length;
			}

			var clip = setup.Clips[clipIndex];
			if (clip == null)
				return;

			_lastClipIndex = clipIndex;

			float pitch = foot.BasePitch + setup.PitchShift + Random.Range(-setup.MaxPitchChange, setup.MaxPitchChange);
			if (pitch <= 0f)
			{
				pitch = 0.01f;
			}

			var request = new SceneAudio.AudioRequest
			{
				Clip         = clip,
				Loop         = setup.Loop,
				Volume       = Mathf.Max(0f, setup.Volume),
				Pitch        = pitch,
				SpatialBlend = Mathf.Clamp01(foot.SpatialBlend),
				FadeIn       = Mathf.Max(0f, setup.FadeIn),
				FadeOut      = Mathf.Max(0f, setup.FadeOut),
				OutputMixer  = foot.OutputMixer,
				FollowTarget = foot.Transform,
				UsePosition  = false,
			};

			CancelFootstepDelay(foot);

			if (setup.Delay > 0.01f)
			{
				foot.DelayRoutine = StartCoroutine(PlayFootstepDelayed(sceneAudio, request, setup.Delay, foot));
			}
			else
			{
				sceneAudio.PlayEffect(request);
			}
		}

		private IEnumerator PlayFootstepDelayed(SceneAudio sceneAudio, SceneAudio.AudioRequest request, float delay, FootData foot)
		{
			if (delay > 0.01f)
			{
				yield return new WaitForSeconds(delay);
			}

			sceneAudio?.PlayEffect(request);
			foot.DelayRoutine = null;
		}

		private void CancelFootstepDelay(FootData foot)
		{
			if (foot.DelayRoutine != null)
			{
				StopCoroutine(foot.DelayRoutine);
				foot.DelayRoutine = null;
			}
		}

		private void InitializeFootAudio(FootData foot, AudioEffect effect)
		{
			if (effect == null)
				return;

			var source = effect.AudioSource;
			if (source != null)
			{
				foot.BasePitch    = effect.BasePitch;
				foot.SpatialBlend = source.spatialBlend;
				foot.OutputMixer  = source.outputAudioMixerGroup;

				source.enabled = false;
			}

			effect.enabled = false;
		}

		// HELPERS

		private class FootData
		{
			public bool        IsUp;
			public float       Cooldown;
			public Transform   Transform;
			public AudioSetup  Setup = new AudioSetup();
			public AudioSetup  SetupSource;
			public float       BasePitch     = 1f;
			public float       SpatialBlend  = 1f;
			public AudioMixerGroup OutputMixer;
			public Coroutine   DelayRoutine;
		}
	}
}
