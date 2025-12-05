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
            [Tooltip("Base URL for Trello API (defaults to https://api.trello.com/1)")]
            public string BaseUrl = "https://api.trello.com/1";

            [Tooltip("Trello API key")]
            public string ApiKey;

            [Tooltip("Trello API token")]
            public string ApiToken;

            [Tooltip("ID of the Trello list where cards should be created")]
            public string ListId;

            [Tooltip("Optional Trello label IDs to attach to created cards")]
            public string[] LabelIds;
        }
    }
}
