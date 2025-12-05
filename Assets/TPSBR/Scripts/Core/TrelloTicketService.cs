using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TPSBR
{
    public partial class TrelloTicketService
    {
        private const string LogPrefix = "[<color=magenta>TrelloTicketService</color>] ";

        private readonly object _sync = new object();
        private readonly ErrorRecorder _errorRecorder;

        public TrelloSettings Configuration { get; }

        public TrelloTicketService(ErrorRecorder errorRecorder, TrelloSettings configuration = null)
        {
            _errorRecorder = errorRecorder;
            Configuration = configuration ?? LoadConfiguration();

            if (_errorRecorder != null)
            {
                _errorRecorder.ErrorRecorded -= OnErrorRecorded;
                _errorRecorder.ErrorRecorded += OnErrorRecorded;
            }
        }

        private TrelloSettings LoadConfiguration()
        {
            const string resourcePath = "TrelloTicketSettings";

            var settings = Resources.Load<TrelloSettings>(resourcePath);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TrelloSettings>();
                Debug.LogWarning(LogPrefix + $"No TrelloTicketSettings asset found at Resources/{resourcePath}. Using an in-memory configuration. Create one via Assets → Create → TPSBR → Trello Ticket Settings and keep API secrets out of version control.");
            }

            return settings;
        }

        private void OnErrorRecorded(ErrorRecord record)
        {
            if (record == null)
                return;

            lock (_sync)
            {
                if (record.SubmittedToTrello == true)
                    return;

                if (IsConfigurationValid() == false)
                {
                    Debug.LogWarning(LogPrefix + "Trello configuration missing. Populate TrelloTicketService.Settings to enable auto submission.");
                    return;
                }

                SubmitTicket(record);
            }
        }

        private bool IsConfigurationValid()
        {
            return string.IsNullOrWhiteSpace(Configuration.ApiKey) == false
                   && string.IsNullOrWhiteSpace(Configuration.ApiToken) == false
                   && string.IsNullOrWhiteSpace(Configuration.ListId) == false;
        }

        private void SubmitTicket(ErrorRecord record)
        {
            _ = SubmitTicketAsync(record);
        }

        private async Task SubmitTicketAsync(ErrorRecord record)
        {
            try
            {
                using (var request = BuildCreateCardRequest(record))
                {
                    var result = await SendRequestAsync(request);

                    if (result.IsSuccess == true)
                    {
                        record.SubmittedToTrello = true;
                        Debug.Log(LogPrefix + $"Created Trello card {result.TicketKey} for error: {record.Condition}");
                    }
                    else
                    {
                        Debug.LogWarning(LogPrefix + $"Failed to create Trello card (HTTP {result.StatusCode}): {result.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + $"Unexpected error submitting Trello card: {ex}");
            }
        }

        private UnityWebRequest BuildCreateCardRequest(ErrorRecord record)
        {
            var baseUrl = string.IsNullOrWhiteSpace(Configuration.BaseUrl) ? "https://api.trello.com/1/" : Configuration.BaseUrl;
            var issueUrl = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "cards");

            var payload = new TrelloCreateCardPayload
            {
                name = BuildSummary(record),
                desc = BuildDescription(record),
                idBoard = string.IsNullOrWhiteSpace(Configuration.BoardId) ? null : Configuration.BoardId,
                idList = Configuration.ListId,
                idLabels = string.IsNullOrWhiteSpace(Configuration.LabelIds) ? null : Configuration.LabelIds,
                idMembers = string.IsNullOrWhiteSpace(Configuration.MemberIds) ? null : Configuration.MemberIds
            };

            var json = JsonUtility.ToJson(payload);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            var request = new UnityWebRequest(issueUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(jsonBytes),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            var query = $"key={Configuration.ApiKey}&token={Configuration.ApiToken}";
            request.url = issueUrl + (issueUrl.Query.Length > 0 ? "&" : "?") + query;

            return request;
        }

        private string BuildSummary(ErrorRecord record)
        {
            var prefix = string.IsNullOrWhiteSpace(Configuration.CardNamePrefix) ? string.Empty : Configuration.CardNamePrefix.Trim();
            var baseSummary = string.IsNullOrWhiteSpace(record.Condition) ? "Unity Error" : record.Condition;

            const int maxLength = 120;
            var summary = baseSummary.Length <= maxLength ? baseSummary : baseSummary.Substring(0, maxLength);

            return string.IsNullOrEmpty(prefix) ? summary : $"{prefix} {summary}";
        }

        private string BuildDescription(ErrorRecord record)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Automated Error Capture");
            builder.AppendLine($"Log Type: {record.LogType}");
            builder.AppendLine($"Condition: {record.Condition}");

            if (string.IsNullOrWhiteSpace(record.StackTrace) == false)
            {
                builder.AppendLine();
                builder.AppendLine("Stack Trace:");
                builder.AppendLine(record.StackTrace);
            }

            return builder.ToString();
        }

        private async Task<TrelloSubmissionResult> SendRequestAsync(UnityWebRequest request)
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

            return new TrelloSubmissionResult
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
                var response = JsonUtility.FromJson<TrelloCardResponse>(responseText);
                return string.IsNullOrWhiteSpace(response?.shortUrl) ? response?.id ?? string.Empty : response.shortUrl;
            }
            catch
            {
                return string.Empty;
            }
        }

        [Serializable]
        private class TrelloCreateCardPayload
        {
            public string name;
            public string desc;
            public string idBoard;
            public string idList;
            public string idLabels;
            public string idMembers;
        }

        [Serializable]
        private class TrelloCardResponse
        {
            public string id;
            public string shortUrl;
        }

        private struct TrelloSubmissionResult
        {
            public bool IsSuccess;
            public long StatusCode;
            public string TicketKey;
            public string Error;
        }
    }
}
