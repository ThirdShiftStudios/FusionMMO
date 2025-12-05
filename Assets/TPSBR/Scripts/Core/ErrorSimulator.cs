using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TPSBR
{
    public class ErrorSimulator : MonoBehaviour
    {
        public void ThrowNullReferenceException()
        {
            throw new NullReferenceException("Simulated NullReferenceException from ErrorSimulator");
        }

        public void ThrowInvalidOperationException()
        {
            throw new InvalidOperationException("Simulated InvalidOperationException from ErrorSimulator");
        }

        public void LogError()
        {
            Debug.LogError("Simulated LogError from ErrorSimulator");
        }

        public void LogException()
        {
            Debug.LogException(new Exception("Simulated Debug.LogException from ErrorSimulator"));
        }
    }
}

#if UNITY_EDITOR
namespace TPSBR.Editor
{
    [CustomEditor(typeof(ErrorSimulator))]
    public class ErrorSimulatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var simulator = (ErrorSimulator)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulated Errors", EditorStyles.boldLabel);

            if (GUILayout.Button("Throw NullReferenceException"))
            {
                simulator.ThrowNullReferenceException();
            }

            if (GUILayout.Button("Throw InvalidOperationException"))
            {
                simulator.ThrowInvalidOperationException();
            }

            if (GUILayout.Button("Log Error"))
            {
                simulator.LogError();
            }

            if (GUILayout.Button("Log Exception"))
            {
                simulator.LogException();
            }
        }
    }
}
#endif
