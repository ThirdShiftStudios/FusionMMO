using System;
using UnityEngine;

namespace TPSBR
{
    public partial class JiraTicketService
    {
        [Serializable]
        [CreateAssetMenu(fileName = "JiraTicketSettings", menuName = "TPSBR/Jira Ticket Settings")]
        public class JiraSettings : ScriptableObject
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

            [Tooltip("Issue type id to use when creating Jira tickets. Overrides IssueTypeName when provided.")]
            public string IssueTypeId;

            [Tooltip("Issue type to use when creating Jira tickets, e.g. Bug, Task")]
            public string IssueTypeName = "Bug";
        }
    }
}
