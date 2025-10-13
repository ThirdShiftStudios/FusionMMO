using Fusion;
using UnityEngine;

namespace TPSBR
{
    public interface IInventoryItemDetails
    {
        string DisplayName { get; }
        Sprite Icon { get; }
        string GetDisplayName(NetworkString<_32> configurationHash);
        string GetDescription();
        string GetDescription(NetworkString<_32> configurationHash);
    }
}
