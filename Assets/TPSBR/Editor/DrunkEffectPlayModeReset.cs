#if UNITY_EDITOR
using FronkonGames.SpiceUp.Drunk;
using UnityEditor;

namespace TPSBR.Editor
{
    [InitializeOnLoad]
    public static class DrunkEffectPlayModeReset
    {
        static DrunkEffectPlayModeReset()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (EditorApplication.isPlaying == false)
            {
                ResetEffect();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ResetEffect();
            }
        }

        private static void ResetEffect()
        {
            Drunk effect = Drunk.Instance;
            if (effect == null)
            {
                return;
            }

            var settings = effect.settings;
            if (settings == null)
            {
                return;
            }

            settings.drunkenness = 0f;
            settings.intensity = 0f;
        }
    }
}
#endif
