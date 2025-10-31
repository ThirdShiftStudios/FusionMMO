using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TPSBR
{
        public class SceneAudio : SceneService
        {
                public readonly struct AudioHandle
                {
                        internal readonly SceneAudio Manager;
                        internal readonly int        Id;

                        internal AudioHandle(SceneAudio manager, int id)
                        {
                                Manager = manager;
                                Id      = id;
                        }

                        public bool IsValid   => Manager != null && Manager.HasHandle(Id);
                        public bool IsPlaying => Manager != null && Manager.IsHandlePlaying(Id);

                        public void Stop(float fadeOut = -1f)
                        {
                                Manager?.StopHandle(Id, fadeOut);
                        }
                }

                [System.Serializable]
                public struct AudioRequest
                {
                        public AudioClip       Clip;
                        public bool            Loop;
                        public float           Volume;
                        public float           Pitch;
                        public float           SpatialBlend;
                        public float           FadeIn;
                        public float           FadeOut;
                        public AudioMixerGroup OutputMixer;
                        public Transform       FollowTarget;
                        public Vector3         Position;
                        public bool            UsePosition;
                }

                private sealed class ManagedAudio
                {
                        public int           Id;
                        public string        Key;
                        public AudioSource   Source;
                        public AudioRequest  Request;
                        public bool          AutoRelease;
                        public Transform     FollowTarget;
                        public Coroutine     StopRoutine;
                }

                [SerializeField]
                private AudioMixer _masterMixer;
                [SerializeField]
                private AudioMixerGroup _musicGroup;
                [SerializeField]
                private AudioMixerGroup _effectsGroup;
                [SerializeField]
                private int _initialPoolSize = 4;

                private readonly Dictionary<int, ManagedAudio>    _handles = new Dictionary<int, ManagedAudio>();
                private readonly Dictionary<string, ManagedAudio> _tracks  = new Dictionary<string, ManagedAudio>();
                private readonly List<ManagedAudio>               _active  = new List<ManagedAudio>();
                private readonly Stack<AudioSource>               _pool    = new Stack<AudioSource>();

                private Transform _poolRoot;
                private int       _nextHandleId = 1;

                public void UpdateVolume()
                {
                        if (_masterMixer == null)
                                return;

                        _masterMixer.SetFloat("MusicVolume", Mathf.Log10(Context.RuntimeSettings.MusicVolume) * 20);
                        _masterMixer.SetFloat("EffectsVolume", Mathf.Log10(Context.RuntimeSettings.EffectsVolume) * 20);
                }

                public AudioHandle PlayTrack(string trackId, AudioClip clip, bool loop = true, float volume = 1f, float fadeIn = 0f, float fadeOut = 0f, AudioMixerGroup output = null)
                {
                        var request = new AudioRequest
                        {
                                Clip         = clip,
                                Loop         = loop,
                                Volume       = Mathf.Max(0f, volume),
                                Pitch        = 1f,
                                SpatialBlend = 0f,
                                FadeIn       = Mathf.Max(0f, fadeIn),
                                FadeOut      = Mathf.Max(0f, fadeOut),
                                OutputMixer  = output,
                                UsePosition  = false,
                                FollowTarget = null,
                        };

                        return PlayInternal(trackId, request, treatAsMusic: true, autoReleaseWhenFinished: loop == false);
                }

                public AudioHandle PlayTrack(string trackId, AudioRequest request)
                {
                        return PlayInternal(trackId, request, treatAsMusic: true, autoReleaseWhenFinished: request.Loop == false);
                }

                public AudioHandle PlayEffect(AudioClip clip, float volume = 1f, float pitch = 1f, AudioMixerGroup output = null)
                {
                        var request = new AudioRequest
                        {
                                Clip         = clip,
                                Loop         = false,
                                Volume       = Mathf.Max(0f, volume),
                                Pitch        = pitch,
                                SpatialBlend = 0f,
                                FadeIn       = 0f,
                                FadeOut      = 0f,
                                OutputMixer  = output,
                                UsePosition  = false,
                        };

                        return PlayInternal(null, request, treatAsMusic: false, autoReleaseWhenFinished: true);
                }

                public AudioHandle PlayEffect(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f, AudioMixerGroup output = null)
                {
                        var request = new AudioRequest
                        {
                                Clip         = clip,
                                Loop         = false,
                                Volume       = Mathf.Max(0f, volume),
                                Pitch        = pitch,
                                SpatialBlend = Mathf.Clamp01(spatialBlend),
                                FadeIn       = 0f,
                                FadeOut      = 0f,
                                OutputMixer  = output,
                                Position     = position,
                                UsePosition  = true,
                        };

                        return PlayInternal(null, request, treatAsMusic: false, autoReleaseWhenFinished: true);
                }

                public AudioHandle PlayEffect(AudioClip clip, Transform followTarget, float volume = 1f, float pitch = 1f, float spatialBlend = 1f, AudioMixerGroup output = null)
                {
                        var request = new AudioRequest
                        {
                                Clip         = clip,
                                Loop         = false,
                                Volume       = Mathf.Max(0f, volume),
                                Pitch        = pitch,
                                SpatialBlend = Mathf.Clamp01(spatialBlend),
                                FadeIn       = 0f,
                                FadeOut      = 0f,
                                OutputMixer  = output,
                                FollowTarget = followTarget,
                                UsePosition  = false,
                        };

                        return PlayInternal(null, request, treatAsMusic: false, autoReleaseWhenFinished: true);
                }

                public AudioHandle PlayEffect(AudioRequest request)
                {
                        request.Loop   = false;
                        request.FadeIn = Mathf.Max(0f, request.FadeIn);
                        request.FadeOut = Mathf.Max(0f, request.FadeOut);

                        return PlayInternal(null, request, treatAsMusic: false, autoReleaseWhenFinished: true);
                }

                public void StopTrack(string trackId, float fadeOut = -1f)
                {
                        if (string.IsNullOrEmpty(trackId) == true)
                                return;

                        if (_tracks.TryGetValue(trackId, out var managed) == false)
                                return;

                        StopManaged(managed, fadeOut);
                }

                public void Stop(AudioHandle handle, float fadeOut = -1f)
                {
                        if (handle.Manager != this)
                                return;

                        StopHandle(handle.Id, fadeOut);
                }

                // SceneService

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        CreatePool();
                }

                protected override void OnActivate()
                {
                        base.OnActivate();

                        UpdateVolume();
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        UpdateManagedSources();
                }

                protected override void OnDeactivate()
                {
                        base.OnDeactivate();

                        StopAll();
                }

                protected override void OnDeinitialize()
                {
                        base.OnDeinitialize();

                        StopAll(immediate: true);
                        ClearPool();
                }

                internal bool HasHandle(int id)
                {
                        return _handles.ContainsKey(id);
                }

                internal bool IsHandlePlaying(int id)
                {
                        if (_handles.TryGetValue(id, out var managed) == false)
                                return false;

                        return managed.Source != null && managed.Source.isPlaying;
                }

                internal void StopHandle(int id, float fadeOut)
                {
                        if (_handles.TryGetValue(id, out var managed) == false)
                                return;

                        StopManaged(managed, fadeOut);
                }

                private AudioHandle PlayInternal(string trackId, AudioRequest request, bool treatAsMusic, bool autoReleaseWhenFinished)
                {
                        if (request.Clip == null)
                        {
                                Debug.LogWarning("Missing audio clip for playback request.");
                                return default;
                        }

                        request.Volume       = Mathf.Max(0f, request.Volume);
                        request.Pitch        = Mathf.Approximately(request.Pitch, 0f) ? 1f : request.Pitch;
                        request.SpatialBlend = Mathf.Clamp01(request.SpatialBlend);
                        request.FadeIn       = Mathf.Max(0f, request.FadeIn);
                        request.FadeOut      = Mathf.Max(0f, request.FadeOut);

                        ManagedAudio managed = null;

                        if (string.IsNullOrEmpty(trackId) == false)
                        {
                                _tracks.TryGetValue(trackId, out managed);
                        }

                        bool isNew = managed == null;

                        if (isNew == true)
                        {
                                managed = CreateManaged(trackId);
                        }
                        else if (managed.StopRoutine != null)
                        {
                                StopCoroutine(managed.StopRoutine);
                                managed.StopRoutine = null;
                        }

                        managed.Request      = request;
                        managed.AutoRelease  = autoReleaseWhenFinished;
                        managed.FollowTarget = request.FollowTarget;

                        SetupSource(managed, request, treatAsMusic, isNew);

                        return new AudioHandle(this, managed.Id);
                }

                private void SetupSource(ManagedAudio managed, AudioRequest request, bool treatAsMusic, bool isNew)
                {
                        var source = managed.Source;

                        if (source == null)
                                return;

                        bool wasPlaying   = source.isPlaying;
                        AudioClip previousClip = source.clip;
                        bool hasPreviousClip = previousClip != null;

                        source.loop = request.Loop;
                        source.pitch = Mathf.Approximately(request.Pitch, 0f) ? 1f : request.Pitch;
                        source.spatialBlend = request.SpatialBlend;
                        source.outputAudioMixerGroup = request.OutputMixer ?? (treatAsMusic == true ? _musicGroup : _effectsGroup);

                        if (request.FollowTarget != null)
                        {
                                source.transform.SetParent(request.FollowTarget, false);
                                source.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                                source.transform.SetParent(transform, false);

                                if (request.UsePosition == true)
                                {
                                        source.transform.position = request.Position;
                                }
                                else
                                {
                                        source.transform.localPosition = Vector3.zero;
                                }
                        }

                        if (hasPreviousClip == true && previousClip == request.Clip && wasPlaying == true)
                        {
                                source.volume = request.Volume;
                                return;
                        }

                        if (wasPlaying == true && hasPreviousClip == true && previousClip != request.Clip && request.FadeOut > 0f && request.FadeIn > 0f)
                        {
                                source.CrossFade(this, request.Clip, request.FadeOut, request.FadeIn, volume: request.Volume);
                        }
                        else
                        {
                                if (wasPlaying == true)
                                {
                                        source.Stop();
                                }

                                source.clip = request.Clip;

                                if (request.FadeIn > 0f && (isNew == true || hasPreviousClip == false || previousClip != request.Clip))
                                {
                                        source.FadeIn(this, request.FadeIn, volume: request.Volume);
                                }
                                else
                                {
                                        source.volume = request.Volume;
                                        source.Play();
                                }
                        }
                }

                private ManagedAudio CreateManaged(string trackId)
                {
                        var source = GetSource();

                        var managed = new ManagedAudio
                        {
                                Id          = _nextHandleId++,
                                Key         = trackId,
                                Source      = source,
                                AutoRelease = false,
                        };

                        _handles.Add(managed.Id, managed);
                        _active.Add(managed);

                        if (string.IsNullOrEmpty(trackId) == false)
                        {
                                _tracks[trackId] = managed;
                        }

                        return managed;
                }

                private AudioSource GetSource()
                {
                        if (_poolRoot == null)
                        {
                                CreatePool();
                        }

                        AudioSource source;

                        if (_pool.Count > 0)
                        {
                                source = _pool.Pop();
                                source.gameObject.SetActive(true);
                        }
                        else
                        {
                                source = CreateSource();
                        }

                        source.transform.SetParent(transform, false);
                        source.transform.localPosition = Vector3.zero;
                        source.clip = null;
                        source.volume = 1f;
                        source.loop = false;

                        return source;
                }

                private AudioSource CreateSource()
                {
                        var sourceGO = new GameObject("ManagedAudioSource");
                        sourceGO.transform.SetParent(_poolRoot, false);

                        var source = sourceGO.AddComponent<AudioSource>();
                        source.playOnAwake = false;
                        source.spatialBlend = 0f;
                        source.rolloffMode = AudioRolloffMode.Linear;
                        source.minDistance = 1f;
                        source.maxDistance = 25f;

                        return source;
                }

                private void ReturnSource(ManagedAudio managed)
                {
                        var source = managed.Source;

                        if (source == null)
                                return;

                        source.Stop();
                        source.clip = null;
                        source.outputAudioMixerGroup = null;
                        source.transform.SetParent(_poolRoot, false);
                        source.transform.localPosition = Vector3.zero;
                        source.gameObject.SetActive(false);

                        _pool.Push(source);
                }

                private void StopManaged(ManagedAudio managed, float fadeOutOverride)
                {
                        if (managed == null)
                                return;

                        if (managed.StopRoutine != null)
                        {
                                StopCoroutine(managed.StopRoutine);
                                managed.StopRoutine = null;
                        }

                        float fadeOut = fadeOutOverride >= 0f ? fadeOutOverride : managed.Request.FadeOut;

                        if (fadeOut > 0f && managed.Source != null && managed.Source.isPlaying == true)
                        {
                                managed.StopRoutine = StartCoroutine(StopAfterFade(managed, fadeOut));
                        }
                        else
                        {
                                ReleaseManaged(managed);
                        }
                }

                private IEnumerator StopAfterFade(ManagedAudio managed, float fadeOut)
                {
                        managed.Source?.FadeOut(this, fadeOut);

                        yield return new WaitForSeconds(fadeOut);

                        ReleaseManaged(managed);
                }

                private void ReleaseManaged(ManagedAudio managed)
                {
                        if (managed == null)
                                return;

                        if (managed.StopRoutine != null)
                        {
                                StopCoroutine(managed.StopRoutine);
                                managed.StopRoutine = null;
                        }

                        if (managed.Source != null)
                        {
                                managed.Source.Stop();
                        }

                        if (string.IsNullOrEmpty(managed.Key) == false && _tracks.TryGetValue(managed.Key, out var track) == true && track == managed)
                        {
                                _tracks.Remove(managed.Key);
                        }

                        _handles.Remove(managed.Id);
                        _active.Remove(managed);

                        if (managed.Source != null)
                        {
                                ReturnSource(managed);
                        }
                }

                private void UpdateManagedSources()
                {
                        for (int i = _active.Count - 1; i >= 0; i--)
                        {
                                var managed = _active[i];

                                if (managed.FollowTarget != null && managed.Source != null)
                                {
                                        managed.Source.transform.position = managed.FollowTarget.position;
                                }

                                if (managed.AutoRelease == true && managed.Source != null && managed.Source.loop == false && managed.Source.isPlaying == false && managed.StopRoutine == null)
                                {
                                        ReleaseManaged(managed);
                                }
                        }
                }

                private void StopAll(bool immediate = false)
                {
                        if (_active.Count == 0)
                                return;

                        var managedArray = _active.ToArray();

                        for (int i = 0; i < managedArray.Length; i++)
                        {
                                if (immediate == true)
                                {
                                        ReleaseManaged(managedArray[i]);
                                }
                                else
                                {
                                        StopManaged(managedArray[i], managedArray[i].Request.FadeOut);
                                }
                        }
                }

                private void CreatePool()
                {
                        if (_poolRoot != null)
                                return;

                        var poolGO = new GameObject("AudioPool");
                        poolGO.transform.SetParent(transform, false);
                        poolGO.hideFlags = HideFlags.HideInHierarchy;

                        _poolRoot = poolGO.transform;

                        for (int i = 0; i < _initialPoolSize; i++)
                        {
                                var source = CreateSource();
                                source.gameObject.SetActive(false);
                                _pool.Push(source);
                        }
                }

                private void ClearPool()
                {
                        foreach (var source in _pool)
                        {
                                if (source == null)
                                        continue;

                                if (Application.isPlaying == true)
                                {
                                        Destroy(source.gameObject);
                                }
                                else
                                {
                                        DestroyImmediate(source.gameObject);
                                }
                        }

                        _pool.Clear();

                        if (_poolRoot != null)
                        {
                                if (Application.isPlaying == true)
                                {
                                        Destroy(_poolRoot.gameObject);
                                }
                                else
                                {
                                        DestroyImmediate(_poolRoot.gameObject);
                                }

                                _poolRoot = null;
                        }
                }
        }
}
