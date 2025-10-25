using UnityEngine;
using Fusion;
using TMPro;

namespace TPSBR.UI
{
    public class UIGameplayView : UIView
    {
        // PRIVATE MEMBERS

        [SerializeField] private UIPlayer _player;
        [SerializeField] private UIHealth _health;
        [SerializeField] private UIGameplayInteractions _interactions;
        [SerializeField] private UIAgentEffects _effects;
        [SerializeField] private UIGameplayEvents _events;
        [SerializeField] private UIKillFeed _killFeed;
        [SerializeField] private UIGoldFeed _goldFeed;
        [SerializeField] private UIInventoryFeed _inventoryFeed;
        [SerializeField] private UIBehaviour _spectatingGroup;
        [SerializeField] private TextMeshProUGUI _spectatingText;
        [SerializeField] private UIHitDamageIndicator _hitDamage;
        [SerializeField] private UIExperienceNumberIndicator _experienceIndicator;
        [SerializeField] private UIButton _menuButton;

        [Header("Events Setup")] [SerializeField]
        private Color _enemyKilledColor = Color.red;

        [SerializeField] private Color _playerDeathColor = Color.yellow;
        [SerializeField] private AudioSetup _enemyKilledSound;
        [SerializeField] private AudioSetup _playerDeathSound;
        [SerializeField] private Color _interactionFailedColor = Color.white;
        [SerializeField] private AudioSetup _interactionFailedSound;
        [SerializeField] private Color _levelUpColor = new Color(1f, 0.8431373f, 0f);

        [SerializeField] private UITeamPlayerPanels _teamPlayerPanels;

        private UIMana _mana;
        private UIStamina _stamina;
        private UIFishingView _fishingView;
        private Agent _localAgent;
        private NetworkBehaviourId _localAgentId;
        private bool _localAgentIsLocalPlayer;
        private int _lastKnownPlayerLevel = -1;
        private bool _isExperienceSubscribed;

        // UIView INTERFACE

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_health != null)
            {
                var healthGameObject = _health.gameObject;

                _mana = healthGameObject.GetComponent<UIMana>();
                _stamina = healthGameObject.GetComponent<UIStamina>();
            }

            ClearLocalAgent();

            if (Context.PlayerData != null)
            {
                _lastKnownPlayerLevel = Context.PlayerData.Level;
            }

            Context.GameplayMode.OnAgentDeath += OnAgentDeath;
            Context.GameplayMode.OnPlayerEliminated += OnPlayerEliminated;
            Context.GameplayMode.OnPlayerJoinedGame += OnPlayerJoined;
            Context.GameplayMode.OnPlayerLeftGame += OnPlayerLeft;

            if (Context.Announcer != null)
            {
                Context.Announcer.Announce += OnAnnounce;
            }


            if ((Application.isMobilePlatform == false || Application.isEditor == true) &&
                Context.Settings.SimulateMobileInput == false)
            {
                _menuButton.SetActive(false);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(OnMenuButton);
            }
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();

            Context.GameplayMode.OnAgentDeath -= OnAgentDeath;
            Context.GameplayMode.OnPlayerEliminated -= OnPlayerEliminated;
            Context.GameplayMode.OnPlayerJoinedGame -= OnPlayerJoined;
            Context.GameplayMode.OnPlayerLeftGame -= OnPlayerLeft;

            if (Context.Announcer != null)
            {
                Context.Announcer.Announce -= OnAnnounce;
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveListener(OnMenuButton);
            }

            var fishingView = GetFishingView();
            if (fishingView != null)
            {
                fishingView.Bind(null);
            }

            _localAgentIsLocalPlayer = false;
            _lastKnownPlayerLevel = -1;

            UnsubscribeExperienceEvents();

            _fishingView = null;
        }

        protected override void OnTick()
        {
            base.OnTick();

            if (Context.Runner == null || Context.Runner.IsRunning == false)
                return;
            if (Context.GameplayMode == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
                return;

            if (_localAgent != Context.ObservedAgent ||
                (Context.ObservedAgent != null && _localAgentId != Context.ObservedAgent.Id))
            {
                if (Context.ObservedAgent == null)
                {
                    ClearLocalAgent();
                }
                else
                {
                    var player = Context.NetworkGame.GetPlayer(Context.ObservedPlayerRef);
                    if (player == null)
                    {
                        ClearLocalAgent();
                    }
                    else
                    {
                        SetLocalAgent(SceneUI.Context.ObservedAgent, player,
                            Context.LocalPlayerRef == Context.ObservedPlayerRef);
                    }
                }
            }

            if (_localAgent == null)
                return;

            CheckLevelUpEvent();

            _health.UpdateHealth(_localAgent.Health);
            _mana?.UpdateMana(_localAgent.Mana);
            _stamina?.UpdateStamina(_localAgent.Stamina);
            _effects.UpdateEffects(_localAgent);
            _interactions.UpdateInteractions(Context, _localAgent);
            _teamPlayerPanels.UpdateTeamPlayerPanels(Context, _localAgent);
        }

        // PRIVATE MEMBERS

        private void OnHitPerformed(HitData hitData)
        {
            _hitDamage.HitPerformed(hitData);
        }

        private void OnAgentDeath(KillData killData)
        {
            var victimPlayer = Context.NetworkGame.GetPlayer(killData.VictimRef);
            var killerPlayer = Context.NetworkGame.GetPlayer(killData.KillerRef);

            _killFeed.ShowFeed(new KillFeedData
            {
                Killer = killerPlayer != null ? killerPlayer.Nickname : "",
                Victim = victimPlayer != null ? victimPlayer.Nickname : "",
                IsHeadshot = killData.Headshot,
                DamageType = killData.HitType,
                VictimIsLocal = killData.VictimRef != PlayerRef.None && killData.VictimRef == Context.LocalPlayerRef,
                KillerIsLocal = killData.KillerRef != PlayerRef.None && killData.KillerRef == Context.LocalPlayerRef,
            });

            if (killData.VictimRef == Context.ObservedPlayerRef)
            {
                bool eliminated = victimPlayer != null ? victimPlayer.Statistics.IsEliminated : false;

                _events.ShowEvent(new GameplayEventData
                {
                    Name = eliminated == true ? "YOU WERE ELIMINATED" : "YOU WERE KILLED",
                    Description = killerPlayer != null ? $"Eliminated by {killerPlayer.Nickname}" : "",
                    Color = _playerDeathColor,
                    Sound = _playerDeathSound,
                });
            }
            else if (killData.KillerRef == Context.ObservedPlayerRef)
            {
                bool eliminated = killerPlayer != null ? killerPlayer.Statistics.IsEliminated : false;

                _events.ShowEvent(new GameplayEventData
                {
                    Name = eliminated == true ? "ENEMY ELIMINATED" : "ENEMY KILLED",
                    Description = victimPlayer != null ? victimPlayer.Nickname : "",
                    Color = _enemyKilledColor,
                    Sound = _enemyKilledSound,
                });
            }
        }

        private void OnPlayerEliminated(PlayerRef playerRef)
        {
            var player = Context.NetworkGame.GetPlayer(playerRef);
            if (player == null)
                return;

            _killFeed.ShowFeed(new EliminationFeedData
            {
                Nickname = player.Nickname,
            });
        }

        private void OnPlayerJoined(PlayerRef playerRef)
        {
            var player = Context.NetworkGame.GetPlayer(playerRef);
            if (player == null)
                return;

            _killFeed.ShowFeed(new JoinedLeftFeedData
            {
                Joined = true,
                Nickname = player.Nickname,
            });
        }

        private void OnPlayerLeft(string nickname)
        {
            _killFeed.ShowFeed(new JoinedLeftFeedData
            {
                Joined = false,
                Nickname = nickname,
            });
        }

        private void OnAnnounce(AnnouncementData announcement)
        {
            if (announcement.FeedMessage.HasValue() == false)
                return;

            _killFeed.ShowFeed(new AnnouncementFeedData
            {
                Announcement = announcement.FeedMessage,
                Color = announcement.Color,
            });
        }

        private void OnMenuButton()
        {
            Context.Input.TrigggerBackAction();
        }

        private void SetLocalAgent(Agent agent, Player player, bool isLocalPlayer)
        {
            var fishingView = GetFishingView();

            if (_localAgent != null)
            {
                _localAgent.Health.HitPerformed -= OnHitPerformed;
                _localAgent.Health.HitTaken -= OnHitTaken;

                _localAgent.Interactions.InteractionFailed -= OnInteractionFailed;

                _inventoryFeed?.Bind(null);
                _goldFeed?.Bind(null);

                fishingView?.Bind(null);
            }

            _localAgent = agent;
            _localAgentId = agent.Id;
            _localAgentIsLocalPlayer = isLocalPlayer;

            _health.SetActive(true);
            _mana?.SetActive(true);
            _stamina?.SetActive(true);
            _interactions.SetActive(true);
            _effects.SetActive(true);
            _spectatingGroup.SetActive(isLocalPlayer == false);

            _player.SetData(Context, player);
            
            
            if (isLocalPlayer == false)
            {
                _spectatingText.text = player.Nickname;
            }

            if (isLocalPlayer == true)
            {
                _inventoryFeed?.Bind(agent.Inventory);
                _goldFeed?.Bind(agent.Inventory);
                _lastKnownPlayerLevel = Context.PlayerData != null ? Context.PlayerData.Level : -1;

                fishingView?.Bind(agent.Inventory);

                SubscribeExperienceEvents();
            }
            else
            {
                _inventoryFeed?.Bind(null);
                _goldFeed?.Bind(null);
                _lastKnownPlayerLevel = -1;

                fishingView?.Bind(null);

                UnsubscribeExperienceEvents();
            }

            _mana?.UpdateMana(agent.Mana);
            _stamina?.UpdateStamina(agent.Stamina);

            agent.Health.HitPerformed += OnHitPerformed;
            agent.Health.HitTaken += OnHitTaken;
            agent.Interactions.InteractionFailed += OnInteractionFailed;
        }

        private void ClearLocalAgent()
        {
            _health.SetActive(false);
            _mana?.SetActive(false);
            _stamina?.SetActive(false);
            _interactions.SetActive(false);
            _effects.SetActive(false);
            _spectatingGroup.SetActive(false);

            _inventoryFeed?.Bind(null);
            _goldFeed?.Bind(null);

            GetFishingView()?.Bind(null);

            UnsubscribeExperienceEvents();

            _mana?.UpdateMana(null);
            _stamina?.UpdateStamina(null);

            if (_localAgent != null)
            {
                _localAgent.Health.HitPerformed -= OnHitPerformed;
                _localAgent.Health.HitTaken -= OnHitTaken;
                _localAgent.Interactions.InteractionFailed -= OnInteractionFailed;

                _localAgent = null;
                _localAgentId = default;
            }

            _localAgentIsLocalPlayer = false;
            _lastKnownPlayerLevel = -1;
        }

        private void OnHitTaken(HitData hitData)
        {
            _effects.OnHitTaken(hitData);
        }

        private void OnInteractionFailed(string reason)
        {
            _events.ShowEvent(new GameplayEventData
            {
                Name = string.Empty,
                Description = reason,
                Color = _interactionFailedColor,
                Sound = _interactionFailedSound,
            }, false, true);
        }

        private UIFishingView GetFishingView()
        {
            if (_fishingView == null && SceneUI != null)
            {
                _fishingView = SceneUI.Get<UIFishingView>();
            }

            return _fishingView;
        }

        private void SubscribeExperienceEvents()
        {
            if (_isExperienceSubscribed == true)
                return;

            var playerData = Context.PlayerData;
            if (playerData == null)
                return;

            playerData.ExperienceAdded += OnExperienceAdded;
            _isExperienceSubscribed = true;
        }

        private void UnsubscribeExperienceEvents()
        {
            if (_isExperienceSubscribed == false)
                return;

            var playerData = Context.PlayerData;
            if (playerData != null)
            {
                playerData.ExperienceAdded -= OnExperienceAdded;
            }

            _isExperienceSubscribed = false;
        }

        private void OnExperienceAdded(int amount, IExperienceGiver experienceGiver)
        {
            if (_experienceIndicator == null)
                return;

            if (amount <= 0)
                return;

            IExperienceGiver provider = experienceGiver;

            if (provider == null && _localAgent != null)
            {
                provider = _localAgent.Health as IExperienceGiver;

                if (provider == null)
                {
                    provider = new StaticExperienceGiver(_localAgent.transform.position);
                }
            }

            if (provider == null)
            {
                provider = new StaticExperienceGiver(Vector3.zero);
            }

            _experienceIndicator.ExperienceAdded(amount, provider);
        }

        private void CheckLevelUpEvent()
        {
            if (_localAgentIsLocalPlayer == false)
                return;

            if (_events == null)
                return;

            var playerData = Context.PlayerData;
            if (playerData == null)
                return;

            int currentLevel = playerData.Level;

            if (_lastKnownPlayerLevel < 0)
            {
                _lastKnownPlayerLevel = currentLevel;
                return;
            }

            if (currentLevel > _lastKnownPlayerLevel)
            {
                _events.ShowEvent(new GameplayEventData
                {
                    Name = "Level Up",
                    Description = currentLevel.ToString(),
                    Color = _levelUpColor,
                });
            }

            _lastKnownPlayerLevel = currentLevel;
        }
    }
}
