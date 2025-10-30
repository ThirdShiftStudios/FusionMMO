using UnityEngine;
using UnityEngine.UI;

public class SliderMinigameWonItemDisplay : MonoBehaviour
{
    public Image image;
    public Text amountText, nameText;

    public void Set(Sprite sprite, string amount, string name)
    {
        image.sprite = sprite;
        amountText.text = $"{amount}x";
        nameText.text = name;
    }
}
