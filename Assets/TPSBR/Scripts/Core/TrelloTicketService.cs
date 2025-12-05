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
            return string.IsNullOrWhiteSpace(Configuration.BaseUrl) == false
                   && string.IsNullOrWhiteSpace(Configuration.ApiKey) == false
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
                        Debug.Log(LogPrefix + $"Created Trello card {result.TicketId} for error: {record.Condition}");
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
            var baseUri = Configuration.BaseUrl.TrimEnd('/') + "/";
            var cardsUrl = new Uri(new Uri(baseUri), "cards");

            var form = new WWWForm();
            form.AddField("idList", Configuration.ListId);
            form.AddField("key", Configuration.ApiKey);
            form.AddField("token", Configuration.ApiToken);
            form.AddField("name", BuildSummary(record));
            form.AddField("desc", BuildDescription(record));

            if (Configuration.LabelIds != null && Configuration.LabelIds.Length > 0)
            {
                foreach (var labelId in Configuration.LabelIds)
                {
                    if (string.IsNullOrWhiteSpace(labelId) == false)
                    {
                        form.AddField("idLabels", labelId);
                    }
                }
            }

            return UnityWebRequest.Post(cardsUrl, form);
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
            var ticketId = ExtractTicketId(request.downloadHandler?.text);
            var responseBody = request.downloadHandler?.text;
            var error = string.IsNullOrWhiteSpace(responseBody) ? request.error : responseBody;

            return new TrelloSubmissionResult
            {
                IsSuccess = isSuccess,
                StatusCode = statusCode,
                TicketId = ticketId,
                Error = error
            };
        }

        private string ExtractTicketId(string responseText)
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
        private class TrelloCardResponse
        {
            public string id;
            public string shortUrl;
            public string url;
            public string name;
        }

        private struct TrelloSubmissionResult
        {
            public bool IsSuccess;
            public long StatusCode;
            public string TicketId;
            public string Error;
        }
    }
}
