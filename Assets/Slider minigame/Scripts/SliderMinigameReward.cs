using UnityEngine;

public class SliderMinigameReward : MonoBehaviour
{
    public readonly string Name;
    public readonly Sprite Sprite;

    /// <summary>
    /// Example: Success hits: 3  Required hits: 2  => Won items: 1
    /// <para>
    /// Success hits: 6  Required hits: 3 ItemsPerSuccessHit: 0.34f  => Won items: 1
    /// </para>
    /// </summary>
    public readonly float ItemsPerSuccessHit;

    /// <summary>
    /// Example: Success hits: 3  Required hits: 2  => Won items: 1
    /// <para>
    /// Success hits: 6  Required hits: 3 ItemsPerSuccessHit: 1  => Won items: 3
    /// </para>
    /// </summary>
    public readonly int RequiredWins;

    /// <summary>
    /// Create new POSSIBLE Reward. Reward will be distributed by 'SliderMinigam.OnItemsWin' event IF wins > or equal to requiredWins.
    /// </summary>
    /// <param name="name">Name if reward - usually item name. You can use this to parse reward to your item type.</param>
    /// <param name="sprite">Image which will be displayed for this reward </param>
    /// <param name="itemsPerSuccessHit">How many items will be distributed for each success hit</param>
    /// <param name="requiredWins">How many successfull hits are required to obtain this item</param>
    /// <returns>Possible Slider Minigame reward</returns>
    public SliderMinigameReward(string name, Sprite sprite, float itemsPerSuccessHit = 1, int requiredWins = 1)
    {
        Name = name;
        Sprite = sprite;
        ItemsPerSuccessHit = itemsPerSuccessHit;
        RequiredWins = requiredWins;
    }

    /// <summary>
    /// Create new POSSIBLE Reward. Reward will be distributed by 'SliderMinigam.OnItemsWin' event IF wins > or equal to requiredWins.
    /// </summary>
    /// <param name="name">Name if reward - usually item name. You can use this to parse reward to your item type.</param>
    /// <param name="sprite">Image which will be displayed for this reward </param>
    /// <param name="itemsPerSuccessHit">How many items will be distributed for each success hit</param>
    /// <param name="requiredWins">How many successfull hits are required to obtain this item</param>
    /// <returns>Possible Slider Minigame reward</returns>
    public static SliderMinigameReward Create(string name, Sprite sprite, float itemsPerSuccessHit = 1, int requiredWins = 1) =>
        new SliderMinigameReward(name, sprite, itemsPerSuccessHit, requiredWins);
}

public class WonReward
{
    public string ItemName;
    public int Amount;
}