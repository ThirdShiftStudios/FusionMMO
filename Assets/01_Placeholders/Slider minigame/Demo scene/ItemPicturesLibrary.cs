using UnityEngine;

/// <summary>
/// EXAMPLE sprites library. Replace with your sprites source. If it is not static, then 
/// just make refference to it using FindObjectOfType<YOUR_LIBRARY>() or refference it via
/// public variable. This differs per project.
/// </summary>
public class ItemPicturesLibrary : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;
    public static Sprite[] Sprites;

    void Start() => Sprites = sprites;
    
}
