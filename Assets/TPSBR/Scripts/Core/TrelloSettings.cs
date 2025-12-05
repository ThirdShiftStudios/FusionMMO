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
            [Tooltip("Base URL for the Trello API. Defaults to https://api.trello.com/1 if left blank.")]
            public string BaseUrl = "https://api.trello.com/1";

            [Tooltip("Trello API key used for authentication")]
            public string ApiKey;

            [Tooltip("Trello API token used for authentication")]
            public string ApiToken;

            [Tooltip("ID of the Trello list where new cards should be created")]
            public string ListId;

            [Tooltip("Optional Trello label ID to attach to auto-created cards")]
            public string LabelId;
        }
    }
}
