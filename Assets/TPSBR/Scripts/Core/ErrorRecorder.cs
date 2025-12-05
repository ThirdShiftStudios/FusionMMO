using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class ErrorRecorder
    {
        private const string LogPrefix = "[<color=red>ErrorRecorder</color>] ";

        private readonly object _sync = new object();
        private readonly HashSet<string> _recordedErrors = new HashSet<string>();

        public IReadOnlyCollection<string> RecordedErrors => _recordedErrors;

        public ErrorRecorder()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
                return;

            string formattedMessage = string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}";
            RecordError(formattedMessage);
        }

        private void RecordError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage) == true)
                return;

            bool wasAdded;
            lock (_sync)
            {
                wasAdded = _recordedErrors.Add(errorMessage);
            }

            if (wasAdded == false)
                return;

            Debug.Log(LogPrefix + errorMessage);
        }
    }
}
