using UnityEngine;

namespace TPSBR.UI
{
        public struct KillFeedData : IFeedData
        {
                public string    Killer;
                public string    Victim;
                public bool      IsHeadshot;
                public EHitType  DamageType;
                public bool      KillerIsLocal;
                public bool      VictimIsLocal;
        }

        public struct JoinedLeftFeedData : IFeedData
        {
                public string Nickname;
                public bool   Joined;
        }

        public struct EliminationFeedData : IFeedData
        {
                public string Nickname;
                public bool   IsLocal;
        }

        public struct AnnouncementFeedData : IFeedData
        {
                public string Announcement;
                public Color  Color;
        }

        public class UIKillFeed : UIFeedBase
        {
                protected override UIFeedItemBase[] GetFeedItems()
                {
                        return GetComponentsInChildren<UIKillFeedItem>();
                }
        }
}
