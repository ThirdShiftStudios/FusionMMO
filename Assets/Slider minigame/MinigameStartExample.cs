using System.Collections.Generic;
using UnityEngine;

public class MinigameStartExample : MonoBehaviour
{
    public SliderMinigame FishingMinigame, CookingMinigame, MiningMinigame;

    /// <summary>
    /// Example with multiple possible rewards
    /// </summary>
    public void StartMiningMinigame()
    {
        List<SliderMinigameReward> rewards = new();

        // Declare all possible rewards
        rewards.Add(new SliderMinigameReward("Iron ore", ItemPicturesLibrary.Sprites[0]));
        rewards.Add(new SliderMinigameReward("Amethyst", ItemPicturesLibrary.Sprites[1], 1, 3));
        rewards.Add(new SliderMinigameReward("Pink Topaz", ItemPicturesLibrary.Sprites[2], 1, 5));

        // Subscribte to reward notify
        MiningMinigame.OnMinigameItemsWin += WonItemsReceiver;

        // Start minigame
        MiningMinigame.Begin(rewards);
    }

    /// <summary>
    /// Example with only 1 reward
    /// </summary>
    public void StartCookingMinigame()
    {
        // Subscribte to reward notify and start
        CookingMinigame.OnMinigameItemsWin += WonItemsReceiver;
        CookingMinigame.Begin(new SliderMinigameReward("Hot Strawberry", ItemPicturesLibrary.Sprites[4]));
    }

    /// <summary>
    /// Example where you get 1 item only after 3 success hits
    /// </summary>
    public void StartFishingMinigame()
    {
        List<SliderMinigameReward> rewards = new();

        // Create new reward
        var reward = new SliderMinigameReward("Trout", ItemPicturesLibrary.Sprites[5], 1, 3);

        rewards.Add(reward);

        // Subscribte to reward notify and start
        FishingMinigame.OnMinigameItemsWin += WonItemsReceiver;
        FishingMinigame.Begin(rewards);
    }

    public void WonItemsReceiver(List<WonReward> wonRewards)
    {
        Debug.Log($"You won: {wonRewards.Count} items");

        // You can Add items to your inventory here
        //foreach (var reward in wonRewards)
        //{
        //    reward.ItemName;
        //    reward.Amount;
        //}

        ResetSubscriptions();
    }

    void ResetSubscriptions()
    {
        FishingMinigame.OnMinigameItemsWin -= WonItemsReceiver;
        CookingMinigame.OnMinigameItemsWin -= WonItemsReceiver;
        MiningMinigame.OnMinigameItemsWin -= WonItemsReceiver;
    }
}
