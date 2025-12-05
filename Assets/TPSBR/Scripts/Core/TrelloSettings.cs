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
            [Tooltip("Base URL for the Trello API, e.g. https://api.trello.com")]
            public string BaseUrl = "https://api.trello.com";

            [Tooltip("API key issued by Trello")]
            public string ApiKey;

            [Tooltip("API token issued by Trello")]
            public string ApiToken;

            [Tooltip("ID of the Trello list where new cards should be created")]
            public string ListId;

            [Tooltip("Optional comma-separated Trello label IDs to attach to the created card")]
            public string LabelIds;
        }
    }
}
