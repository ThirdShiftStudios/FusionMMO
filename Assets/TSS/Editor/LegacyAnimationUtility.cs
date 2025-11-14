using UnityEditor;
using UnityEngine;

namespace TPSBR
{
    public static class LegacyAnimationUtility
    {
        [MenuItem("Tools/Convert Selected Clips To Legacy")]
        private static void Convert()
        {
            foreach (var obj in Selection.objects)
            {
                var clip = obj as AnimationClip;
                if (clip == null) continue;
                var serialized = new SerializedObject(clip);
                serialized.FindProperty("m_Legacy").boolValue = true;
                serialized.ApplyModifiedProperties();
                Debug.Log($"Marked {clip.name} as Legacy");
            }
        }
    }

}
