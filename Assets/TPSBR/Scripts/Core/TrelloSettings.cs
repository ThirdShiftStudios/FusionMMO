using System;
using UnityEngine;

namespace TPSBR
{
    [Serializable]
    [CreateAssetMenu(fileName = "TrelloTicketSettings", menuName = "TPSBR/Trello Ticket Settings")]
    public class TrelloSettings : ScriptableObject
    {
        [Tooltip("Base URL for Trello API. Leave empty to use https://api.trello.com/1/")]
        public string BaseUrl = "https://api.trello.com/1/";

        [Tooltip("API key from your Trello developer settings")]
        public string ApiKey;

        [Tooltip("API token generated for the Trello user that will create cards")]
        public string ApiToken;

        [Tooltip("ID of the Trello board where error cards will be created")]
        public string BoardId;

        [Tooltip("ID of the Trello list that will contain the created cards")]
        public string ListId;

        [Tooltip("Optional space separated Trello label IDs to apply to the card")]
        public string LabelIds;

        [Tooltip("Optional comma separated Trello member IDs to assign to the card")]
        public string MemberIds;

        [Tooltip("Optional prefix applied to the Trello card name")]
        public string CardNamePrefix = "[Auto Error]";
    }
    
}
