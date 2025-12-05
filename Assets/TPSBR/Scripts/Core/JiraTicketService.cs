using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TPSBR
{
    public partial class JiraTicketService
    {
        private const string LogPrefix = "[<color=magenta>JiraTicketService</color>] ";

        private readonly object _sync = new object();
        private readonly ErrorRecorder _errorRecorder;

        public JiraSettings Configuration { get; }

        public JiraTicketService(ErrorRecorder errorRecorder, JiraSettings configuration = null)
        {
            _errorRecorder = errorRecorder;
            Configuration = configuration ?? LoadConfiguration();

            if (_errorRecorder != null)
            {
                _errorRecorder.ErrorRecorded -= OnErrorRecorded;
                _errorRecorder.ErrorRecorded += OnErrorRecorded;
            }
        }

        private JiraSettings LoadConfiguration()
        {
            const string resourcePath = "JiraTicketSettings";

            var settings = Resources.Load<JiraSettings>(resourcePath);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<JiraSettings>();
                Debug.LogWarning(LogPrefix + $"No JiraTicketSettings asset found at Resources/{resourcePath}. Using an in-memory configuration. Create one via Assets → Create → TPSBR → Jira Ticket Settings and keep API secrets out of version control.");
            }

            return settings;
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
                    description = BuildDescriptionDocument(record),
                    issuetype = BuildIssueType(),
                    labels = string.IsNullOrWhiteSpace(Configuration.Label) ? Array.Empty<string>() : new[] { Configuration.Label }
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

        private JiraDocument BuildDescriptionDocument(ErrorRecord record)
        {
            var blocks = new System.Collections.Generic.List<JiraBlock>
            {
                new JiraBlock
                {
                    type = "heading",
                    attrs = new JiraAttributes { level = 2 },
                    content = new[] { new JiraText { text = "Automated Error Capture" } }
                },
                new JiraBlock
                {
                    type = "paragraph",
                    content = new[] { new JiraText { text = $"Log Type: {record.LogType}" } }
                },
                new JiraBlock
                {
                    type = "paragraph",
                    content = new[] { new JiraText { text = $"Condition: {record.Condition}" } }
                }
            };

            if (string.IsNullOrWhiteSpace(record.StackTrace) == false)
            {
                blocks.Add(new JiraBlock
                {
                    type = "heading",
                    attrs = new JiraAttributes { level = 3 },
                    content = new[] { new JiraText { text = "Stack Trace" } }
                });

                blocks.Add(new JiraBlock
                {
                    type = "codeBlock",
                    attrs = new JiraAttributes { language = string.Empty },
                    content = new[] { new JiraText { text = record.StackTrace } }
                });
            }

            return new JiraDocument
            {
                content = blocks.ToArray()
            };
        }

        private JiraIssueType BuildIssueType()
        {
            if (string.IsNullOrWhiteSpace(Configuration.IssueTypeId) == false)
            {
                return new JiraIssueType { id = Configuration.IssueTypeId };
            }

            return new JiraIssueType
            {
                name = string.IsNullOrWhiteSpace(Configuration.IssueTypeName) ? "Bug" : Configuration.IssueTypeName
            };
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
            var responseBody = request.downloadHandler?.text;
            var error = string.IsNullOrWhiteSpace(responseBody) ? request.error : responseBody;

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
            public JiraDocument description;
            public JiraIssueType issuetype;
            public string[] labels;
        }

        [Serializable]
        private class JiraDocument
        {
            public string type = "doc";
            public int version = 1;
            public JiraBlock[] content;
        }

        [Serializable]
        private class JiraBlock
        {
            public string type;
            public JiraAttributes attrs;
            public JiraText[] content;
        }

        [Serializable]
        private class JiraAttributes
        {
            public int level;
            public string language;
        }

        [Serializable]
        private class JiraText
        {
            public string type = "text";
            public string text;
        }

        [Serializable]
        private class JiraProject
        {
            public string key;
        }

        [Serializable]
        private class JiraIssueType
        {
            public string id;
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
