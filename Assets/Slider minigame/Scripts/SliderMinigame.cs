using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // NEW: Input System
using Random = UnityEngine.Random;

public class SliderMinigame : MonoBehaviour
{
    public enum BarEndReachAction
    {
        RetunToStart,
        Fail
    }

    public enum SuccessHitAction
    {
        EndMinigame,
        IncreaseSpeedAndReset
    }

    /// <summary>
    /// This is directly used as NAME of the animation that will be played !
    /// It is parsed to string and played by animator.
    /// </summary>
    public enum WinCounterAnimations
    {
        None,
        ScaleDown,
        Slide
    }

    [Header("Dependencies")]
    [SerializeField] Image SuccessRangeDisplay;
    [SerializeField] Slider Bar;
    [SerializeField] Animator BarAnimator;
    [SerializeField] Animator MasterAnimator;
    /// <summary>
    /// Animator of an Item displayed above the Slider game
    /// </summary>
    [SerializeField] Animator MainItemAnimator;
    [SerializeField] Image MainPicture;

    [Header("Options")]
    public bool ForceStart;
    /// <summary>
    /// If Position should be always randomized on next try
    /// </summary>
    public bool RandomizePosition;
    [Range(0.1f, 100f), SerializeField] float minPosition = 0f, maxPosition = 100f;
    [SerializeField] BarEndReachAction onBarEndReachAction;
    [SerializeField] SuccessHitAction onSuccessHitAction;
    [SerializeField] string successHitBarAnim;
    [SerializeField] int maxWins = 99;
    [SerializeField] bool resetToStartOnSuccessHit = true;

    [Header("Interactions")]
    public bool TriggerByLeftMouseButton;
    public Key TriggerKey = Key.Space; // NEW: Input System Key enum

    [Header("Delay"), SerializeField] float StartDelay;
    [SerializeField] float CloseDelay;

    [Header("Chance")]
    public bool RandomizeChance;
    public float MinChance, MaxChance;
    /// <summary>
    /// If you game have Bonus chance stat, or Luck stat or something like that,
    /// then you can use this property to increase win chance (extend Success bar width).
    /// Range should be between 0-50.
    /// </summary>
    public static float BonusChanceStat = 0f;

    [Header("Init Settings")]
    [Range(0.1f, 100f), SerializeField] float Chance = 15;
    [Range(0, 100), SerializeField] float Position = 50;

    [Header("Speed")]
    [Range(10f, 120f), SerializeField] float Speed = 90f;
    [Range(1.01f, 3f), SerializeField] float onSuccessHitSpeedIncrease = 1.5f;
    [SerializeField] float maximumSpeed = 120f;

    [Header("Success area Text")]
    [SerializeField] bool DisplaySuccessAreaText;
    [SerializeField] Text SuccessAreaTextComponent;
    [SerializeField] string InSuccessAreaText, OutsideSuccessAreaText;
    [SerializeField] Color InSuccessAreaColor, OutsideSuccessAreaColor;

    [Header("Win counter display")]
    [SerializeField] bool HaveWinCounter;
    [SerializeField] Text WinCounterTextComponent, WinCounterOldTextComponent;
    [SerializeField] Animator WinCounterAnim;
    [SerializeField] string WinAnimationName, WinAmountAfterText;
    [SerializeField] WinCounterAnimations winCounterAnimation;

    [Header("Rewards")]
    [SerializeField] GameObject RewardItemDisplayPrefab;
    /// <summary>
    /// Destination where will RewardItemDisplayPrefab spawn for each of the reward.
    /// </summary>
    [SerializeField] GameObject RewardDisplay;

    [Header("Result screen")]
    [SerializeField] bool ShowResultScreen;
    [SerializeField] bool ShowScore;
    [SerializeField] Text ResultScreenTextComponent, ScoreTextComponent;
    [SerializeField] Animator ResultScreenAnimator, ItemDisplayAnimator;
    [SerializeField] string AnyWinsText, ZeroWinsText, MaxWinsText;
    [SerializeField] float ItemOffset = 250f;

    [Header("Particles")]
    [SerializeField] bool ShowParticles;
    [SerializeField] ParticleSystem SuccessAreaHitParticles, FailedAreaHitParticles, IdleParticles;

    [Header("Sounds")]
    [SerializeField] bool PlaySounds;
    [SerializeField] bool PlaySuccessAreaEnterSfx;
    [SerializeField] AudioSource SuccessAreaHitSfx, FailedAreaHitSfx, SuccessAreaEnterSfx;

    RectTransform successRangeTransform, barTransform;

    static Action _winCallback;
    static _winCallbackResults _winCallbackWithResults;
    public delegate void _winCallbackResults(int resultCount);
    public delegate void ItemsWin(List<WonReward> wonRewards);
    /// <summary>
    /// Register to this event to receive retrieved items from all of the minigames
    /// </summary>
    public static event ItemsWin OnAnyMinigameItemsWin;
    /// <summary>
    /// Register to this event to receive retrieved items from this minigame
    /// </summary>
    public event ItemsWin OnMinigameItemsWin;
    static Image g_MainPicture;

    /// <summary>
    /// How many times player hit the success range
    /// </summary>
    int successHits;
    float barWidth, succesRangeWidth, successRangeXOnBar, timer, currentSpeed, closingTimer;
    bool sliderDirectionDown, isOnTextShown, isOnSuccessArea, isStartDelayed, result, isResultScreenShown, startCloseDelay, isSuccessAreaEnterSfxPlayed;

    /// <summary>
    /// True if this minigame is currently in progress
    /// </summary>
    bool isMinigameRunning;

    /// <summary>
    /// Switched to True when all tries are depleted, unsuccessfull hit
    /// or when end of the bar is reached with mode set to OnBarEndReachAction.
    /// Will show Item Display if checked and start closing delay afterwards.
    /// </summary>
    bool isMinigameFinished;

    /// <summary>
    /// Indicates end of the minigame.
    /// </summary>
    bool isFinished = true;
    /// <summary>
    /// Hard reset of all properties. Start all anew.
    /// </summary>
    bool Initialize;
    static bool InitializeAllMinigames;

    public event Action<bool> MinigameFinished;

    bool resultNotified;

    static List<SliderMinigameReward> _possibleRewards = new List<SliderMinigameReward>();
    List<GameObject> CachedRewards = new List<GameObject>();

    void Start()
    {
        barTransform = Bar.gameObject.GetComponent<RectTransform>();
        barWidth = barTransform.rect.width;
        successRangeTransform = SuccessRangeDisplay.GetComponent<RectTransform>();

        // If maximum speed is lesser then speed, set it to double the speed as default
        if (maximumSpeed < Speed)
            maximumSpeed = Speed * 2;

        g_MainPicture = MainPicture;

        currentSpeed = Speed;
    }


    void Update()
    {
        if (ForceStart)
        {
            // This is example on how to start Slider minigame with specified rewards
            var rewards = new List<SliderMinigameReward>();

            // REPLACE THIS BY YOUR ITEM PICTURES
            rewards.Add(new SliderMinigameReward("Test item 1", ItemPicturesLibrary.Sprites[0], 2, 1));
            rewards.Add(new SliderMinigameReward("Test item 2", ItemPicturesLibrary.Sprites[1], 1, 2));
            rewards.Add(new SliderMinigameReward("Test item 3", ItemPicturesLibrary.Sprites[2], 1, 5));

            Begin(rewards);

            ForceStart = false;
        }

        if (Initialize || InitializeAllMinigames)
        {
            ResetStates();
            ResetTimers();
            ResetBar();
            Randomization();
            successHits = 0;
            Debug.Log(gameObject.name + " started minigame");
            SetSuccessHitArea();

            resultNotified = false;

            if (HaveWinCounter)
                WinCounterTextComponent.text = "0" + WinAmountAfterText;

            MasterAnimator.Play("Show");

            if (IdleParticles != null)
                IdleParticles.Play();

            // Clear last rewards
            foreach (var previousReward in CachedRewards)
            {
                Destroy(previousReward);
            }
            CachedRewards.Clear();

            MainItemAnimator.SetBool("Finished", false);
            MainItemAnimator.Play("SlightUpDownMovement");

            currentSpeed = Speed;
            InitializeAllMinigames = Initialize = false;
        }

        if (isFinished)
            return;

        // Delay before playing Hide animation on Master animator
        if (startCloseDelay)
        {
            closingTimer += Time.unscaledDeltaTime;
            if (closingTimer < CloseDelay)
                return;

            ResultScreenAnimator.Play("Hide");
            ItemDisplayAnimator.Play("Hide");
            MasterAnimator.Play("Hide");

            isFinished = true;
            isMinigameRunning = false;
            return;
        }

        // When all tries are depleted or on failed hit, start closing delay
        if (isMinigameFinished)
        {
            NotifyMinigameFinished(result);

            if (ShowResultScreen)
            {
                MainItemAnimator.SetBool("Finished", true);

                if (!isResultScreenShown)
                    ResultScreen();

                if (CheckInputs())
                    startCloseDelay = true;

            }
            else
                startCloseDelay = true;

            if (IdleParticles != null)
                IdleParticles.Stop();

            return;
        }

        // Delay start by 'StartDelay' value
        if (!isStartDelayed)
        {
            timer += Time.unscaledDeltaTime;
            if (timer < StartDelay)
                return;

            isStartDelayed = true;
        }

        MoveBar();

        isOnSuccessArea = IsOnSuccessArea();

        if (isOnSuccessArea && PlaySuccessAreaEnterSfx && !isSuccessAreaEnterSfxPlayed)
        {
            if (SuccessAreaEnterSfx is not null)
                SuccessAreaEnterSfx.Play();
            else
                Debug.LogWarning($"Missing SuccessAreaEnterSfx in {gameObject.name}!");

            isSuccessAreaEnterSfxPlayed = true;
        }

        if (CheckInputs())
        {
            result = isOnSuccessArea;

            if (result)
                OnSuccessResult();
            else
                OnFailedResult();

            if (DisplaySuccessAreaText)
                ShowSuccessAreaText(false);

            isSuccessAreaEnterSfxPlayed = false;
        }

        if (DisplaySuccessAreaText)
            CheckAndSetDisplayText();
    }


    #region Public API - Start minigame with these functions


    /// <summary>
    /// Begin this minigame. Recommended usage for multiple minigames.
    /// </summary>
    public void Begin(List<SliderMinigameReward> rewards)
    {
        if (isMinigameRunning)
        {
            Debug.LogWarning($"Tried to start alrady running minigame {gameObject.name}!");
            return;
        }

        _possibleRewards = rewards;

        isMinigameRunning = Initialize = true;
    }

    /// <summary>
    /// Begin this minigame. Recommended usage for multiple minigames if you have only 1 reward.
    /// </summary>
    public void Begin(SliderMinigameReward reward)
    {
        if (isMinigameRunning)
        {
            Debug.LogWarning($"Tried to start alrady running minigame {gameObject.name}!");
            return;
        }

        _possibleRewards.Clear();
        _possibleRewards.Add(reward);

        isMinigameRunning = Initialize = true;
    }

    public void ForceStop()
    {
        isMinigameRunning = false;
        startCloseDelay = false;
        isMinigameFinished = false;
        isFinished = true;
        Initialize = false;
        InitializeAllMinigames = false;
        timer = 0f;
        closingTimer = 0f;
        resultNotified = false;
    }

    public static void G_Begin(SliderMinigameReward reward = null)
    {
        _possibleRewards.Clear();

        if (reward != null)
            _possibleRewards.Add(reward);

        InitializeAllMinigames = true;
    }

    /// <summary>
    /// Begin minigame WARNING: Will begin all minigames if you have more then 1!
    /// Use Refference and Public function Begin to start specific minigame.
    /// </summary>
    public static void G_Begin(List<SliderMinigameReward> rewards = null)
    {
        if (rewards != null)
            _possibleRewards = rewards;
        else
            _possibleRewards.Clear();

        InitializeAllMinigames = true;
    }

    /// <summary>
    /// Recommended usage if you have 1 minigame, Begin minigame with win action (function that is called on win)
    /// </summary>
    /// <param name="winCallback"></param>
    public static void G_Begin(Action winCallback, SliderMinigameReward reward = null)
    {
        _possibleRewards.Clear();

        if (reward != null)
            _possibleRewards.Add(reward);

        _winCallback = winCallback;
        InitializeAllMinigames = true;
    }

    /// <summary>
    /// Recommended usage if you have 1 minigame, Begin minigame with win action (function that is called on win)
    /// </summary>
    /// <param name="winCallback"></param>
    public static void G_Begin(Action winCallback, List<SliderMinigameReward> rewards = null)
    {
        if (rewards != null)
            _possibleRewards = rewards;
        else
            _possibleRewards.Clear();

        _winCallback = winCallback;
        InitializeAllMinigames = true;
    }

    /// <summary>
    /// Use in case you want to handle different reward per wins and if you have 1 Minigame.
    /// </summary>
    /// <param name="winCallback"></param>
    public static void G_Begin(_winCallbackResults winCallback, List<SliderMinigameReward> rewards = null)
    {
        if (rewards != null)
            _possibleRewards = rewards;
        else
            _possibleRewards.Clear();

        _winCallbackWithResults = winCallback;
        InitializeAllMinigames = true;
    }

    public void SetMainPicture(Sprite sprite)
    {
        MainPicture.sprite = sprite;
    }

    public static void G_SetMainPicture(Sprite sprite)
    {
        g_MainPicture.sprite = sprite;
    }

    #endregion


    /// <summary>
    /// Set Success hit area width and position
    /// </summary>
    void SetSuccessHitArea()
    {
        // Calculate and Set width
        succesRangeWidth = (Chance / 100) * barWidth;
        var height = barTransform.sizeDelta.y / 2;
        successRangeTransform.sizeDelta = new Vector2(succesRangeWidth, height);

        // Set correct position to success range display
        var currentPosition = successRangeTransform.localPosition;

        // Store value to further calculation
        successRangeXOnBar = SuccessBarPosition();

        // Apply
        currentPosition.x = successRangeXOnBar;
        successRangeTransform.localPosition = currentPosition;
    }

    /// <summary>
    /// Calculate correct position for success range display
    /// </summary>
    /// <returns>X position for success range display</returns>
    float SuccessBarPosition()
    {
        var percentagePosition = Position / 100;
        var positionOnBar = percentagePosition * barWidth;
        var widthAdjustedPosition = positionOnBar - (successRangeTransform.rect.width / 2);

        // Check overflows
        if (widthAdjustedPosition < 0)
            widthAdjustedPosition = 0;

        var maxPosition = barWidth - succesRangeWidth;
        if (widthAdjustedPosition > maxPosition)
            widthAdjustedPosition = maxPosition;

        return widthAdjustedPosition;
    }


    #region Result actions

    void OnSuccessResult()
    {
        successHits++;

        switch (onSuccessHitAction)
        {
            case SuccessHitAction.EndMinigame:
                isMinigameFinished = true;
                break;

            case SuccessHitAction.IncreaseSpeedAndReset:
                // Increase speed
                currentSpeed *= onSuccessHitSpeedIncrease;

                if (currentSpeed > maximumSpeed)
                    currentSpeed = maximumSpeed;

                // Reset and randomize
                ResetBar();
                ResetStates();
                Randomization();

                // Add win
                if (HaveWinCounter)
                {
                    TriggerWinCounter();

                    // End minigame if max wins count reached
                    if (successHits == maxWins)
                    {
                        isMinigameFinished = true;
                    }
                }

                break;

            default:
                break;
        }

        if (_winCallback != null)
            _winCallback.Invoke();

        if (_winCallbackWithResults != null)
            _winCallbackWithResults.Invoke(successHits);

        NotifyMinigameFinished(true);

        if (PlaySounds && SuccessAreaHitSfx != null)
            SuccessAreaHitSfx.Play();

        if (ShowParticles && SuccessAreaHitParticles != null)
            SuccessAreaHitParticles.Play();

        if (successHitBarAnim != "")
            BarAnimator.Play(successHitBarAnim);
    }

    void OnFailedResult()
    {
        isMinigameFinished = true;

        if (PlaySounds && FailedAreaHitSfx != null)
            FailedAreaHitSfx.Play();

        if (ShowParticles && FailedAreaHitParticles != null)
            FailedAreaHitParticles.Play();

        NotifyMinigameFinished(false);
    }

    #endregion

    /// <summary>
    /// Trigger Animator and display current Win count
    /// </summary>
    void TriggerWinCounter()
    {
        if (WinCounterAnim == null)
            return;

        switch (winCounterAnimation)
        {
            case WinCounterAnimations.None:
                break;
            case WinCounterAnimations.ScaleDown:
                WinCounterTextComponent.text = successHits.ToString() + WinAmountAfterText;
                break;
            case WinCounterAnimations.Slide:
                WinCounterTextComponent.text = successHits.ToString() + WinAmountAfterText;
                WinCounterOldTextComponent.text = (successHits - 1).ToString() + WinAmountAfterText;

                break;
            default:
                break;
        }

        // WARNING! Value of the 'winCounterAnimation' is directly used as NAME of the animation that will be played.
        if (WinCounterAnim != null)
            WinCounterAnim.Play(winCounterAnimation.ToString());
    }

    void ResultScreen()
    {
        if (successHits == 0)
            ResultScreenTextComponent.text = ZeroWinsText;
        else if (successHits == maxWins)
        {
            ShowWonItems();
            ResultScreenTextComponent.text = MaxWinsText;
        }
        else
        {
            ShowWonItems();
            ResultScreenTextComponent.text = AnyWinsText;
        }

        if (ShowScore && ScoreTextComponent != null)
            ScoreTextComponent.text = $"{successHits} / {maxWins}";

        ResultScreenAnimator.Play("Show");

        isResultScreenShown = true;
    }

    #region ItemDisplay

    void ShowWonItems()
    {
        List<WonReward> wonRewards = new List<WonReward>();

        for (int i = 0; i < _possibleRewards.Count; i++)
        {
            var requiredWins = _possibleRewards[i].RequiredWins;
            Debug.Log($"Req wins: {requiredWins} Current wins: {successHits} ");
            // Check required wins
            if (_possibleRewards[i].RequiredWins > successHits)
                continue;

            int wonAmount = (int)Math.Round((successHits * _possibleRewards[i].ItemsPerSuccessHit) - (requiredWins - 1), 0);    // Round amount to whole number
            var reward = new WonReward() { Amount = wonAmount, ItemName = _possibleRewards[i].Name };
            wonRewards.Add(reward);

            // Create reward
            var rewardDisplay = Instantiate(RewardItemDisplayPrefab, RewardDisplay.transform);

            // Set correct image and amount
            var displayScript = rewardDisplay.GetComponent<SliderMinigameWonItemDisplay>();

            displayScript.Set(_possibleRewards[i].Sprite, wonAmount.ToString(), _possibleRewards[i].Name);
            CachedRewards.Add(rewardDisplay);
        }


        // Offset won rewards
        var rewardsOffsets = GetDisplayOffsets(CachedRewards.Count);
        for (int i = 0; i < CachedRewards.Count; i++)
        {
            var rewardDisplay = CachedRewards[i];

            // Offset item
            var thisRewardXoffset = rewardsOffsets[i];
            rewardDisplay.transform.localPosition = new Vector3(thisRewardXoffset, 0f, 0f);
        }

        ItemDisplayAnimator.Play("Show");

        DistributeRewards(wonRewards);
    }

    void DistributeRewards(List<WonReward> wonRewards)
    {
        bool distributed = false;

        // If any function is registered to global reward receiver, invoke (call) it with rewards.
        if (OnAnyMinigameItemsWin != null && OnAnyMinigameItemsWin.GetInvocationList().Length > 0)
        {
            OnAnyMinigameItemsWin.Invoke(wonRewards);
            distributed = true;
        }

        // If any function is registered to this reward receiver event, invoke (call) it with rewards.
        if (OnMinigameItemsWin != null && OnMinigameItemsWin.GetInvocationList().Length > 0)
        {
            OnMinigameItemsWin.Invoke(wonRewards);
            distributed = true;
        }

        if (!distributed)
            Debug.LogWarning("No receivers for Slider minigame rewards! Consider registering to 'SliderMinigame.OnItemsWin' event to receive and handle rewards");
    }

    float[] GetDisplayOffsets(int itemCount)
    {
        float[] offsets = new float[10];

        switch (itemCount)
        {
            case 1:
                offsets[0] = 0f;
                break;

            case 2:
                offsets[0] = -ItemOffset / 2;
                offsets[1] = ItemOffset / 2;
                break;

            case 3:
                offsets[0] = -ItemOffset;
                offsets[1] = 0f;
                offsets[2] = ItemOffset;
                break;

            case 4:
                offsets[0] = -ItemOffset * 1.5f;
                offsets[1] = -ItemOffset / 2;
                offsets[2] = ItemOffset / 2;
                offsets[3] = ItemOffset * 1.5f;
                break;

            case 5:
                offsets[0] = -ItemOffset * 2;
                offsets[1] = -ItemOffset;
                offsets[2] = 0f;
                offsets[3] = ItemOffset;
                offsets[4] = ItemOffset * 2;
                break;

            default:
                break;
        }

        return offsets;
    }

    #endregion

    #region Resets

    void Randomization()
    {
        if (RandomizePosition)
            Position = Random.Range(minPosition, maxPosition);
        if (RandomizeChance)
            Chance = Random.Range(MinChance, MaxChance);
    }

    void ResetTimers() => timer = closingTimer = 0f;

    void ResetStates() =>
        isMinigameFinished = startCloseDelay = isResultScreenShown = sliderDirectionDown = isOnTextShown = isOnSuccessArea = isStartDelayed = isFinished = resultNotified = false;

    void ResetBar()
    {
        if (resetToStartOnSuccessHit)
            Bar.value = 0;

        SetSuccessHitArea();
    }

    #endregion


    #region Checks

    /// <summary>
    /// Check for input trigger using the New Input System.
    /// </summary>
    /// <returns>True if any of the inputs were triggered</returns>
    bool CheckInputs()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        bool mousePressed = false;
        bool keyPressed = false;

        if (TriggerByLeftMouseButton && mouse != null)
            mousePressed = mouse.leftButton.wasPressedThisFrame;

        if (TriggerKey != Key.None && keyboard != null)
            keyPressed = keyboard[TriggerKey].wasPressedThisFrame;

        // (Optional) enable gamepad A button too:
        // bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        return mousePressed || keyPressed /* || gamepadPressed */;
    }

    void CheckAndSetDisplayText()
    {
        if (isOnSuccessArea && !isOnTextShown)
        {
            ShowSuccessAreaText(true);
        }
        else if (!isOnSuccessArea && isOnTextShown)
        {
            ShowSuccessAreaText(false);
        }
    }

    void ShowSuccessAreaText(bool show)
    {
        if (show)
        {
            SuccessAreaTextComponent.text = InSuccessAreaText;
            SuccessAreaTextComponent.color = InSuccessAreaColor;
            isOnTextShown = true;
        }
        else
        {
            SuccessAreaTextComponent.text = OutsideSuccessAreaText;
            SuccessAreaTextComponent.color = OutsideSuccessAreaColor;
            isOnTextShown = false;
        }
    }

    bool IsOnSuccessArea()
    {
        var successStart = (successRangeXOnBar / barWidth) * 100;
        var successEnd = ((successRangeXOnBar + succesRangeWidth) / barWidth) * 100;

        return Bar.value > successStart && Bar.value < successEnd;
    }

    #endregion



    void MoveBar()
    {
        if (!sliderDirectionDown)
        {
            if (Bar.value < Bar.maxValue)
                Bar.value += Time.unscaledDeltaTime * currentSpeed;
            else
            {
                if (onBarEndReachAction == BarEndReachAction.Fail)
                {
                    isMinigameFinished = true;
                    result = false;
                    return;
                }
                else
                    sliderDirectionDown = true;
            }
        }
        else
        {
            if (Bar.value < 1)
                sliderDirectionDown = false;
            else
                Bar.value -= Time.unscaledDeltaTime * currentSpeed;
        }
    }

    void NotifyMinigameFinished(bool didSucceed)
    {
        if (resultNotified)
            return;

        resultNotified = true;
        MinigameFinished?.Invoke(didSucceed);
    }
}
