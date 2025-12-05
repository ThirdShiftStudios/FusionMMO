using System;
using UnityEngine;

namespace TPSBR
{
    public class JiraTicketService
    {
        private const string LogPrefix = "[<color=magenta>JiraTicketService</color>] ";

        private readonly object _sync = new object();
        private readonly ErrorRecorder _errorRecorder;

        [Serializable]
        public class Settings
        {
            [Tooltip("Base URL for your Jira instance, e.g. https://yourcompany.atlassian.net")]
            public string BaseUrl;

            [Tooltip("Project key for new tickets, e.g. GAME")]
            public string ProjectKey;

            [Tooltip("Email or username used for authentication")]
            public string Username;

            [Tooltip("API token or password used for authentication")]
            public string ApiToken;

            [Tooltip("Optional label to attach to auto-created tickets")]
            public string Label = "auto-error";
        }

        public Settings Configuration { get; }

        public JiraTicketService(ErrorRecorder errorRecorder, Settings configuration = null)
        {
            _errorRecorder = errorRecorder;
            Configuration = configuration ?? new Settings();

            if (_errorRecorder != null)
            {
                _errorRecorder.ErrorRecorded -= OnErrorRecorded;
                _errorRecorder.ErrorRecorded += OnErrorRecorded;
            }
        }

        private void OnErrorRecorded(ErrorRecord record)
        {
            if (record == null)
                return;

            lock (_sync)
            {
                if (record.SubmittedToJira == true)
                    return;

                if (IsConfigurationValid() == false)
                {
                    Debug.LogWarning(LogPrefix + "Jira configuration missing. Populate JiraTicketService.Settings to enable auto submission.");
                    return;
                }

                SubmitTicket(record);
            }
        }

        private bool IsConfigurationValid()
        {
            return string.IsNullOrWhiteSpace(Configuration.BaseUrl) == false
                   && string.IsNullOrWhiteSpace(Configuration.ProjectKey) == false
                   && string.IsNullOrWhiteSpace(Configuration.Username) == false
                   && string.IsNullOrWhiteSpace(Configuration.ApiToken) == false;
        }

        private void SubmitTicket(ErrorRecord record)
        {
            // Placeholder for actual Jira integration. Replace this with an HTTP client implementation
            // that creates issues using the configured BaseUrl, ProjectKey, Username, and ApiToken.
            Debug.Log(LogPrefix + $"Submitting Jira ticket for error: {record.Condition}");

            record.SubmittedToJira = true;
        }
    }
}
