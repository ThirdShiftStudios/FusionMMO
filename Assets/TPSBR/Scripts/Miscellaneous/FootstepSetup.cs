using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
        public readonly struct FootstepSurface
        {
                public readonly int          TagHash;
                public readonly TerrainLayer TerrainLayer;
                public readonly Texture2D    TerrainTexture;

                public FootstepSurface(int tagHash, TerrainLayer terrainLayer, Texture2D terrainTexture)
                {
                        TagHash        = tagHash;
                        TerrainLayer   = terrainLayer;
                        TerrainTexture = terrainTexture;
                }

                public bool HasTerrain => TerrainLayer != null || TerrainTexture != null;
        }

        [CreateAssetMenu(menuName = "TPSBR/Footstep Setup")]
        public class FootstepSetup : ScriptableObject
        {
                // PRIVATE MEMBERS

		[SerializeField]
		private AudioSetup _fallbackWalkSound;
                [SerializeField]
                private AudioSetup _fallbackRunSound;
                [SerializeField]
                private FootstepData[] _footsteps;
                [SerializeField]
                private TerrainFootstepData[] _terrainFootsteps;

                [NonSerialized]
                private bool _initialized;
                [NonSerialized]
                private int _untaggedHash;
                [NonSerialized]
                private Dictionary<TerrainLayer, TerrainFootstepData> _terrainLayerLookup;
                [NonSerialized]
                private Dictionary<Texture2D, TerrainFootstepData> _terrainTextureLookup;

                // PUBLIC METHODS

                public AudioSetup GetSound(FootstepSurface surface, bool isRunning)
                {
                        if (_initialized == false)
                        {
                                _untaggedHash = "Untagged".GetHashCode();

                                for (int i = 0; i < _footsteps.Length; i++)
                                {
                                        _footsteps[i].TagHash = _footsteps[i].Tag.GetHashCode();
                                }

                                InitializeTerrainLookups();

                                _initialized = true;
                        }

                        if (surface.HasTerrain == true)
                        {
                                var terrainSetup = GetTerrainSound(surface, isRunning);
                                if (terrainSetup != null)
                                        return terrainSetup;
                        }

                        return GetTagSound(surface.TagHash, isRunning);
                }

                // HELPERS

                [Serializable]
		private class FootstepData
		{
			public string Tag;
			public AudioSetup SoundWalk;
                        public AudioSetup SoundRun;

                        [NonSerialized]
                        public int TagHash;
                }

                [Serializable]
                private class TerrainFootstepData
                {
                        public TerrainLayer TerrainLayer;
                        public bool         SerializeTexture;
                        public Texture2D    SerializedTexture;
                        public AudioSetup   SoundWalk;
                        public AudioSetup   SoundRun;

                        public Texture2D GetTexture()
                        {
                                if (SerializeTexture == true)
                                {
                                        if (SerializedTexture != null)
                                                return SerializedTexture;

                                        if (TerrainLayer != null && TerrainLayer.diffuseTexture != null)
                                                return TerrainLayer.diffuseTexture;

                                        return null;
                                }

                                if (TerrainLayer != null && TerrainLayer.diffuseTexture != null)
                                        return TerrainLayer.diffuseTexture;

                                return null;
                        }
                }

                private void InitializeTerrainLookups()
                {
                        if (_terrainFootsteps == null || _terrainFootsteps.Length == 0)
                                return;

                        if (_terrainLayerLookup == null)
                                _terrainLayerLookup = new Dictionary<TerrainLayer, TerrainFootstepData>();
                        else
                                _terrainLayerLookup.Clear();

                        if (_terrainTextureLookup == null)
                                _terrainTextureLookup = new Dictionary<Texture2D, TerrainFootstepData>();
                        else
                                _terrainTextureLookup.Clear();

                        for (int i = 0; i < _terrainFootsteps.Length; i++)
                        {
                                var data = _terrainFootsteps[i];
                                if (data == null)
                                        continue;

                                if (data.TerrainLayer != null && _terrainLayerLookup.ContainsKey(data.TerrainLayer) == false)
                                {
                                        _terrainLayerLookup.Add(data.TerrainLayer, data);
                                }

                                var texture = data.GetTexture();
                                if (texture != null && _terrainTextureLookup.ContainsKey(texture) == false)
                                {
                                        _terrainTextureLookup.Add(texture, data);
                                }
                        }
                }

                private AudioSetup GetTerrainSound(FootstepSurface surface, bool isRunning)
                {
                        if (_terrainFootsteps == null || _terrainFootsteps.Length == 0)
                                return null;

                        TerrainFootstepData terrainData = null;

                        if (surface.TerrainLayer != null && _terrainLayerLookup != null)
                                _terrainLayerLookup.TryGetValue(surface.TerrainLayer, out terrainData);

                        if (terrainData == null && surface.TerrainTexture != null && _terrainTextureLookup != null)
                                _terrainTextureLookup.TryGetValue(surface.TerrainTexture, out terrainData);

                        if (terrainData == null)
                                return null;

                        return isRunning == true ? terrainData.SoundRun : terrainData.SoundWalk;
                }

                private AudioSetup GetTagSound(int tagHash, bool isRunning)
                {
                        if (tagHash == 0 || tagHash == _untaggedHash)
                                return isRunning == true ? _fallbackRunSound : _fallbackWalkSound;

                        for (int i = 0; i < _footsteps.Length; i++)
                        {
                                if (_footsteps[i].TagHash == tagHash)
                                        return isRunning == true ? _footsteps[i].SoundRun : _footsteps[i].SoundWalk;
                        }

                        return isRunning == true ? _fallbackRunSound : _fallbackWalkSound;
                }
        }
}
