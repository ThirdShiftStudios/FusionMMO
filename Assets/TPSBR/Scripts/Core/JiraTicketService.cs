using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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
            _ = SubmitTicketAsync(record);
        }

        private async Task SubmitTicketAsync(ErrorRecord record)
        {
            try
            {
                using (var request = BuildCreateIssueRequest(record))
                {
                    var result = await SendRequestAsync(request);

                    if (result.IsSuccess == true)
                    {
                        record.SubmittedToJira = true;
                        Debug.Log(LogPrefix + $"Created Jira ticket {result.TicketKey} for error: {record.Condition}");
                    }
                    else
                    {
                        Debug.LogWarning(LogPrefix + $"Failed to create Jira ticket (HTTP {result.StatusCode}): {result.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + $"Unexpected error submitting Jira ticket: {ex}");
            }
        }

        private UnityWebRequest BuildCreateIssueRequest(ErrorRecord record)
        {
            var issueUrl = new Uri(new Uri(Configuration.BaseUrl.TrimEnd('/')), "/rest/api/3/issue");

            var payload = new JiraIssuePayload
            {
                fields = new JiraIssueFields
                {
                    project = new JiraProject { key = Configuration.ProjectKey },
                    summary = BuildSummary(record),
                    description = BuildDescription(record),
                    issuetype = new JiraIssueType { name = "Bug" },
                    labels = string.IsNullOrWhiteSpace(Configuration.Label) ? null : new[] { Configuration.Label }
                }
            };

            var json = JsonUtility.ToJson(payload);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(issueUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(jsonBytes),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Basic " + BuildAuthToken());

            return request;
        }

        private string BuildSummary(ErrorRecord record)
        {
            if (string.IsNullOrWhiteSpace(record.Condition) == true)
                return "Unity Error";

            const int maxLength = 120;
            return record.Condition.Length <= maxLength ? record.Condition : record.Condition.Substring(0, maxLength);
        }

        private string BuildDescription(ErrorRecord record)
        {
            var builder = new StringBuilder();
            builder.AppendLine("h2. Automated Error Capture");
            builder.AppendLine($"*Log Type:* {record.LogType}");
            builder.AppendLine($"*Condition:* {record.Condition}");

            if (string.IsNullOrWhiteSpace(record.StackTrace) == false)
            {
                builder.AppendLine("h3. Stack Trace");
                builder.AppendLine("{code}");
                builder.AppendLine(record.StackTrace);
                builder.AppendLine("{code}");
            }

            return builder.ToString();
        }

        private string BuildAuthToken()
        {
            var token = $"{Configuration.Username}:{Configuration.ApiToken}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        }

        private async Task<JiraSubmissionResult> SendRequestAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();

            while (operation.isDone == false)
            {
                await Task.Yield();
            }

            var statusCode = (long)request.responseCode;
            var isSuccess = request.result == UnityWebRequest.Result.Success && statusCode >= 200 && statusCode < 300;
            var ticketKey = ExtractTicketKey(request.downloadHandler?.text);
            var error = request.error ?? request.downloadHandler?.text;

            return new JiraSubmissionResult
            {
                IsSuccess = isSuccess,
                StatusCode = statusCode,
                TicketKey = ticketKey,
                Error = error
            };
        }

        private string ExtractTicketKey(string responseText)
        {
            if (string.IsNullOrEmpty(responseText) == true)
                return string.Empty;

            try
            {
                var response = JsonUtility.FromJson<JiraIssueResponse>(responseText);
                return response?.key ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        [Serializable]
        private class JiraIssuePayload
        {
            public JiraIssueFields fields;
        }

        [Serializable]
        private class JiraIssueFields
        {
            public JiraProject project;
            public string summary;
            public string description;
            public JiraIssueType issuetype;
            public string[] labels;
        }

        [Serializable]
        private class JiraProject
        {
            public string key;
        }

        [Serializable]
        private class JiraIssueType
        {
            public string name;
        }

        [Serializable]
        private class JiraIssueResponse
        {
            public string key;
        }

        private struct JiraSubmissionResult
        {
            public bool IsSuccess;
            public long StatusCode;
            public string TicketKey;
            public string Error;
        }
    }
}
