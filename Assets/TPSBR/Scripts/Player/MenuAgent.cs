namespace TPSBR
{
    using UnityEngine;

    public sealed class MenuAgent : MonoBehaviour
    {
        [SerializeField]
        private EquipmentVisualsManager _equipmentVisuals;

        private void Awake()
        {
            if (_equipmentVisuals == null)
            {
                _equipmentVisuals = GetComponentInChildren<EquipmentVisualsManager>(true);
            }
        }

        private void OnEnable()
        {
            SubscribeToCloud();
            RefreshActiveCharacterVisuals();
        }

        private void OnDisable()
        {
            UnsubscribeFromCloud();
        }

        private void SubscribeToCloud()
        {
            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null)
                return;

            cloud.ActiveCharacterChanged += OnActiveCharacterChanged;
            cloud.CharactersChanged += OnCharactersChanged;
        }

        private void UnsubscribeFromCloud()
        {
            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null)
                return;

            cloud.ActiveCharacterChanged -= OnActiveCharacterChanged;
            cloud.CharactersChanged -= OnCharactersChanged;
        }

        private void OnActiveCharacterChanged(string characterId)
        {
            RefreshActiveCharacterVisuals();
        }

        private void OnCharactersChanged()
        {
            RefreshActiveCharacterVisuals();
        }

        private void RefreshActiveCharacterVisuals()
        {
            if (_equipmentVisuals == null)
                return;

            var cloud = Global.PlayerCloudSaveService;
            if (cloud == null || cloud.IsInitialized == false)
            {
                _equipmentVisuals.RefreshFromInventoryData(null);
                return;
            }

            var inventory = cloud.GetActiveCharacterInventorySnapshot();
            _equipmentVisuals.RefreshFromInventoryData(inventory);
        }
    }
}
