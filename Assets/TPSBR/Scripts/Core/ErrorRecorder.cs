using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class ErrorRecorder
    {
        private const string LogPrefix = "[<color=red>ErrorRecorder</color>] ";

        private readonly object _sync = new object();
        private readonly Dictionary<ErrorKey, ErrorRecord> _recordedErrors = new Dictionary<ErrorKey, ErrorRecord>();

        public event Action<ErrorRecord> ErrorRecorded;

        public IReadOnlyCollection<ErrorRecord> RecordedErrors => _recordedErrors.Values;

        public ErrorRecorder()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
                return;

            RecordError(condition, stackTrace, type);
        }

        private void RecordError(string condition, string stackTrace, LogType logType)
        {
            if (string.IsNullOrEmpty(condition) == true)
                return;

            var key = new ErrorKey(condition, stackTrace, logType);
            ErrorRecord record;
            bool wasAdded;

            lock (_sync)
            {
                if (_recordedErrors.TryGetValue(key, out record) == false)
                {
                    record = new ErrorRecord(condition, stackTrace, logType);
                    _recordedErrors.Add(key, record);
                    wasAdded = true;
                }
                else
                {
                    wasAdded = false;
                }
            }

            if (wasAdded == false)
                return;

            Debug.Log(LogPrefix + record.FormattedMessage);
            ErrorRecorded?.Invoke(record);
        }
    }

    public readonly struct ErrorKey : IEquatable<ErrorKey>
    {
        public readonly string Condition;
        public readonly string StackTrace;
        public readonly LogType LogType;

        public ErrorKey(string condition, string stackTrace, LogType logType)
        {
            Condition = condition ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            LogType = logType;
        }

        public bool Equals(ErrorKey other)
        {
            return Condition == other.Condition && StackTrace == other.StackTrace && LogType == other.LogType;
        }

        public override bool Equals(object obj)
        {
            return obj is ErrorKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Condition.GetHashCode();
                hash = (hash * 397) ^ StackTrace.GetHashCode();
                hash = (hash * 397) ^ (int)LogType;
                return hash;
            }
        }
    }

    public class ErrorRecord
    {
        public string Condition { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }
        public bool SubmittedToTrello { get; set; }
        public string FormattedMessage => string.IsNullOrEmpty(StackTrace) ? Condition : $"{Condition}\n{StackTrace}";

        public ErrorRecord(string condition, string stackTrace, LogType logType)
        {
            Condition = condition ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            LogType = logType;
        }
    }
}
