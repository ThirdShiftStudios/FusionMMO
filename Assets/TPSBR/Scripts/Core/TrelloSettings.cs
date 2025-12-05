using System;
using UnityEngine;

namespace TPSBR
{
    public partial class TrelloTicketService
    {
        [Serializable]
        [CreateAssetMenu(fileName = "TrelloTicketSettings", menuName = "TPSBR/Trello Ticket Settings")]
        public class TrelloSettings : ScriptableObject
        {
            [Tooltip("Base URL for Trello API requests")]
            public string BaseUrl = "https://api.trello.com/1";

            [Tooltip("API key issued by Trello")]
            public string ApiKey;

            [Tooltip("API token generated for the Trello user")]
            public string ApiToken;

            [Tooltip("Target Trello board identifier (optional, for documentation)")]
            public string BoardId;

            [Tooltip("Identifier of the Trello list where new cards should be created")]
            public string ListId;

            [Tooltip("Optional comma-separated Trello label IDs to attach to auto-created cards")]
            public string LabelIds;

            [Tooltip("Optional prefix applied to created card titles")]
            public string CardNamePrefix = "Auto Error";
        }
    }
}
