using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Networking;

namespace TPSBR
{
    public partial class TrelloTicketService
    {
        private const string LogPrefix = "[<color=magenta>TrelloTicketService</color>] ";

        private readonly object _sync = new object();
        private readonly ErrorRecorder _errorRecorder;
        private static readonly string[] _errorBlacklist = new[]
        {
            "A scripted object (probably TerrainToMapBaker?) has a different serialization layout when loading. (Read 32 bytes but expected 256 bytes)"
        };

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

                if (IsBlacklisted(record))
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

        private bool IsBlacklisted(ErrorRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.Condition) == true)
                return false;

            for (int i = 0; i < _errorBlacklist.Length; i++)
            {
                if (record.Condition.IndexOf(_errorBlacklist[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private void SubmitTicket(ErrorRecord record)
        {
            _ = SubmitTicketAsync(record);
        }

        private async Task SubmitTicketAsync(ErrorRecord record)
        {
            try
            {
                var summary = BuildSummary(record);

                if (await CardAlreadyExistsAsync(summary) == true)
                {
                    record.SubmittedToTrello = true;
                    Debug.Log(LogPrefix + $"Skipped Trello card creation because a card named '{summary}' already exists.");
                    return;
                }

                var description = BuildDescription(record);

                using (var request = BuildCreateCardRequest(summary, description))
                {
                    var result = await SendRequestAsync(request);

                    if (result.IsSuccess == true)
                    {
                        record.SubmittedToTrello = true;
                        Debug.Log(LogPrefix + $"Created Trello card {result.TicketKey} for error: {record.Condition}");

                        await TryAttachScreenshotAsync(result.CardId);
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

        private UnityWebRequest BuildCreateCardRequest(string summary, string description)
        {
            const string baseUrl = "https://api.trello.com/1/cards";

            // Trim everything defensive-style
            string listId = Configuration.ListId?.Trim();
            string labelIds = Configuration.LabelIds?.Trim();
            string memberIds = Configuration.MemberIds?.Trim();

            var name = UnityWebRequest.EscapeURL(summary);
            var desc = UnityWebRequest.EscapeURL(description);

            var sb = new StringBuilder();
            sb.Append($"{baseUrl}?key={Configuration.ApiKey}");
            sb.Append($"&token={Configuration.ApiToken}");
            sb.Append($"&idList={listId}");
            sb.Append($"&name={name}");
            sb.Append($"&desc={desc}");

            // Labels: split, trim each, then re-join so we guarantee no hidden spaces
            if (!string.IsNullOrWhiteSpace(labelIds))
            {
                var parts = labelIds.Split(',');
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = parts[i].Trim();

                var cleaned = string.Join(",", parts);
                sb.Append($"&idLabels={UnityWebRequest.EscapeURL(cleaned)}");
            }

            // Members: same deal
            if (!string.IsNullOrWhiteSpace(memberIds))
            {
                var parts = memberIds.Split(',');
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = parts[i].Trim();

                var cleaned = string.Join(",", parts);
                sb.Append($"&idMembers={UnityWebRequest.EscapeURL(cleaned)}");
            }

            var url = sb.ToString();

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Array.Empty<byte>()),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log(LogPrefix + "Trello URL: " + url);

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
            builder.AppendLine($"Steam User Name: {GetSteamUserName()}");
            builder.AppendLine($"Steam User ID: {GetSteamUserId()}");

            if (string.IsNullOrWhiteSpace(record.StackTrace) == false)
            {
                builder.AppendLine();
                builder.AppendLine("Stack Trace:");
                builder.AppendLine(record.StackTrace);
            }

            builder.AppendLine();
            builder.AppendLine("Screenshot attached at submission time.");

            return builder.ToString();
        }

        private IEnumerable<TrelloCardInfo> ExtractCardList(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText) == true)
                return Array.Empty<TrelloCardInfo>();

            try
            {
                var wrapped = $"{{\"cards\":{responseText}}}";
                var response = JsonUtility.FromJson<TrelloCardListResponse>(wrapped);
                return response?.cards ?? Array.Empty<TrelloCardInfo>();
            }
            catch
            {
                return Array.Empty<TrelloCardInfo>();
            }
        }

        private async Task<bool> CardAlreadyExistsAsync(string summary)
        {
            try
            {
                var listId = Configuration.ListId?.Trim();

                if (string.IsNullOrWhiteSpace(listId) == true)
                    return false;

                var url = $"https://api.trello.com/1/lists/{listId}/cards?fields=name&key={Configuration.ApiKey}&token={Configuration.ApiToken}";

                using (var request = UnityWebRequest.Get(url))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();

                    var operation = request.SendWebRequest();

                    while (operation.isDone == false)
                    {
                        await Task.Yield();
                    }

                    var statusCode = (long)request.responseCode;
                    var isSuccess = request.result == UnityWebRequest.Result.Success && statusCode >= 200 && statusCode < 300;

                    if (isSuccess == false)
                    {
                        Debug.LogWarning(LogPrefix + $"Failed to check existing Trello cards (HTTP {statusCode}): {request.error}");
                        return false;
                    }

                    var cards = ExtractCardList(request.downloadHandler?.text);

                    foreach (var card in cards)
                    {
                        if (string.Equals(card.name, summary, StringComparison.OrdinalIgnoreCase) == true)
                            return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + $"Error while checking for duplicate Trello card: {exception}");
            }

            return false;
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
            var responseBody = request.downloadHandler?.text;
            var response = ExtractCardResponse(responseBody);
            var ticketKey = string.IsNullOrWhiteSpace(response?.shortUrl) ? response?.id ?? string.Empty : response.shortUrl;
            var error = string.IsNullOrWhiteSpace(responseBody) ? request.error : responseBody;

            return new TrelloSubmissionResult
            {
                IsSuccess = isSuccess,
                StatusCode = statusCode,
                CardId = response?.id ?? string.Empty,
                TicketKey = ticketKey,
                Error = error
            };
        }

        private TrelloCardResponse ExtractCardResponse(string responseText)
        {
            if (string.IsNullOrEmpty(responseText) == true)
                return new TrelloCardResponse();

            try
            {
                var response = JsonUtility.FromJson<TrelloCardResponse>(responseText);
                return response ?? new TrelloCardResponse();
            }
            catch
            {
                return new TrelloCardResponse();
            }
        }

        private async Task TryAttachScreenshotAsync(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId) == true)
            {
                Debug.LogWarning(LogPrefix + "Cannot attach screenshot because Trello card ID is missing.");
                return;
            }

            if (Application.isBatchMode == true || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogWarning(LogPrefix + "Screenshot capture is unavailable because graphics are disabled.");
                return;
            }

            Texture2D screenshot = null;

            try
            {
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + $"Failed to capture screenshot for Trello card {cardId}: {exception}");
            }

            if (screenshot == null)
            {
                Debug.LogWarning(LogPrefix + "Screenshot capture returned null. Skipping attachment upload.");
                return;
            }

            byte[] pngData = null;

            try
            {
                pngData = screenshot.EncodeToPNG();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + $"Failed to encode screenshot for Trello card {cardId}: {exception}");
            }
            finally
            {
                UnityEngine.Object.Destroy(screenshot);
            }

            if (pngData == null || pngData.Length == 0)
            {
                Debug.LogWarning(LogPrefix + "Screenshot data was empty. Skipping attachment upload.");
                return;
            }

            using (var request = BuildAttachmentRequest(cardId, pngData))
            {
                var attachmentResult = await SendRequestAsync(request);

                if (attachmentResult.IsSuccess == false)
                {
                    Debug.LogWarning(LogPrefix + $"Failed to attach screenshot to Trello card {cardId} (HTTP {attachmentResult.StatusCode}): {attachmentResult.Error}");
                }
            }
        }

        private UnityWebRequest BuildAttachmentRequest(string cardId, byte[] pngData)
        {
            var url = $"https://api.trello.com/1/cards/{cardId}/attachments";
            var form = new WWWForm();

            form.AddField("key", Configuration.ApiKey);
            form.AddField("token", Configuration.ApiToken);
            form.AddBinaryData("file", pngData, "bug-screenshot.png", "image/png");

            var request = UnityWebRequest.Post(url, form);
            request.downloadHandler = new DownloadHandlerBuffer();

            return request;
        }

        private string GetSteamUserName()
        {
            try
            {
                if (SteamAPI.IsSteamRunning() == false)
                {
                    return "Unknown Steam Name";
                }

                var personaName = SteamFriends.GetPersonaName();
                return string.IsNullOrWhiteSpace(personaName) ? "Unknown Steam Name" : personaName;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + $"Failed to retrieve Steam user name: {exception}");
                return "Unknown Steam Name";
            }
        }

        private string GetSteamUserId()
        {
            try
            {
                if (SteamAPI.IsSteamRunning() == false)
                {
                    return "Unknown Steam ID";
                }

                var steamId = SteamUser.GetSteamID().ToString();
                return string.IsNullOrWhiteSpace(steamId) ? "Unknown Steam ID" : steamId;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + $"Failed to retrieve Steam user ID: {exception}");
                return "Unknown Steam ID";
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

        [Serializable]
        private class TrelloCardListResponse
        {
            public TrelloCardInfo[] cards;
        }

        [Serializable]
        private class TrelloCardInfo
        {
            public string name;
        }

        private struct TrelloSubmissionResult
        {
            public bool IsSuccess;
            public long StatusCode;
            public string CardId;
            public string TicketKey;
            public string Error;
        }
    }
}
