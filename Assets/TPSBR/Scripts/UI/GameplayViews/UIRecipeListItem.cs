using System;
using System.Collections.Generic;
using TMPro;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public sealed class UIRecipeListItem : MonoBehaviour
        {
                [Header("Recipe")]
                [SerializeField]
                private TextMeshProUGUI _nameLabel;
                [SerializeField]
                private Image _iconImage;

                [Header("Contents")]
                [SerializeField]
                private RectTransform _inputContainer;
                [SerializeField]
                private RectTransform _outputContainer;
                [SerializeField]
                private UIRecipeItemIcon _itemIconPrefab;

                [Header("Buttons")]
                [SerializeField]
                private Button _craftButton;
                [SerializeField]
                private Button _craftAllButton;
                [SerializeField]
                private TextMeshProUGUI _craftButtonLabel;
                [SerializeField]
                private TextMeshProUGUI _craftAllButtonLabel;
                [SerializeField]
                private string _craftButtonText = "Craft";
                [SerializeField]
                private string _craftAllButtonText = "Craft All";
                [SerializeField]
                private string _craftAllButtonCountFormat = "Craft All ({0})";

                [Header("Crafting")]
                [SerializeField]
                private GameObject _craftingProgressRoot;
                [SerializeField]
                private Image _craftingProgressImage;
                [SerializeField]
                private Button _cancelButton;
                [SerializeField]
                private TextMeshProUGUI _cancelButtonLabel;
                [SerializeField]
                private string _cancelButtonText = "Cancel";

                private readonly List<UIRecipeItemIcon> _inputIcons = new List<UIRecipeItemIcon>();
                private readonly List<UIRecipeItemIcon> _outputIcons = new List<UIRecipeItemIcon>();

                private RecipeDefinition _recipe;
                private int _currentCraftableCount;
                private bool _isCrafting;
                private float _craftStartTime;
                private float _craftDuration;

                public event Action<RecipeDefinition> CraftRequested;
                public event Action<RecipeDefinition> CraftAllRequested;
                public event Action<RecipeDefinition> CancelRequested;

                private void Awake()
                {
                        if (_craftButton != null)
                        {
                                _craftButton.onClick.AddListener(HandleCraftClicked);
                        }

                        if (_craftAllButton != null)
                        {
                                _craftAllButton.onClick.AddListener(HandleCraftAllClicked);
                        }

                        if (_cancelButton != null)
                        {
                                _cancelButton.onClick.AddListener(HandleCancelClicked);
                                _cancelButton.gameObject.SetActive(false);
                        }

                        if (_craftingProgressRoot != null)
                        {
                                _craftingProgressRoot.SetActive(false);
                        }
                }

                private void OnDestroy()
                {
                        if (_craftButton != null)
                        {
                                _craftButton.onClick.RemoveListener(HandleCraftClicked);
                        }

                        if (_craftAllButton != null)
                        {
                                _craftAllButton.onClick.RemoveListener(HandleCraftAllClicked);
                        }

                        if (_cancelButton != null)
                        {
                                _cancelButton.onClick.RemoveListener(HandleCancelClicked);
                        }
                }

                private void Update()
                {
                        if (_isCrafting == true)
                        {
                                UpdateCraftingProgress();
                        }
                }

                public void Configure(RecipeDefinition recipe, int craftableCount)
                {
                        _recipe = recipe;

                        UpdateRecipeHeader(recipe);
                        PopulateIcons(recipe != null ? recipe.Inputs : Array.Empty<RecipeDefinition.ItemQuantity>(), _inputContainer, _inputIcons);
                        PopulateIcons(recipe != null ? recipe.Outputs : Array.Empty<RecipeDefinition.ItemQuantity>(), _outputContainer, _outputIcons);
                        UpdateCraftButtons(craftableCount);
                }

                public void ApplyCraftingState(bool isCrafting, float startTime, float duration)
                {
                        _isCrafting = isCrafting;

                        if (_isCrafting == true)
                        {
                                _craftStartTime = startTime;
                                _craftDuration = Mathf.Max(0.0001f, duration);
                        }
                        else
                        {
                                _craftStartTime = 0f;
                                _craftDuration = 0f;
                        }

                        UpdateCraftButtons(_currentCraftableCount);
                }

                private void UpdateRecipeHeader(RecipeDefinition recipe)
                {
                        if (_nameLabel != null)
                        {
                                _nameLabel.text = recipe != null ? recipe.Name : string.Empty;
                        }

                        if (_iconImage != null)
                        {
                                Sprite icon = recipe != null ? recipe.IconSprite : null;
                                if (icon != null)
                                {
                                        _iconImage.sprite = icon;
                                        _iconImage.enabled = true;
                                        _iconImage.color = Color.white;
                                }
                                else
                                {
                                        _iconImage.sprite = null;
                                        _iconImage.enabled = false;
                                }
                        }
                }

                private void PopulateIcons(IReadOnlyList<RecipeDefinition.ItemQuantity> data, RectTransform container, List<UIRecipeItemIcon> pool)
                {
                        if (container == null || pool == null)
                                return;

                        int requiredCount = data != null ? data.Count : 0;

                        if (_itemIconPrefab == null)
                        {
                                ToggleExistingIcons(pool, requiredCount);
                                if (container.gameObject.activeSelf != (requiredCount > 0))
                                {
                                        container.gameObject.SetActive(requiredCount > 0);
                                }
                                return;
                        }

                        EnsureIconPool(pool, container, requiredCount);

                        for (int i = 0; i < pool.Count; ++i)
                        {
                                bool shouldBeActive = i < requiredCount;
                                UIRecipeItemIcon iconView = pool[i];
                                if (iconView == null)
                                        continue;

                                iconView.gameObject.SetActive(shouldBeActive);

                                if (shouldBeActive == false)
                                        continue;

                                RecipeDefinition.ItemQuantity entry = data[i];
                                iconView.Configure(entry.Item, entry.Quantity);
                        }

                        if (container.gameObject.activeSelf != (requiredCount > 0))
                        {
                                container.gameObject.SetActive(requiredCount > 0);
                        }
                }

                private void ToggleExistingIcons(List<UIRecipeItemIcon> pool, int requiredCount)
                {
                        for (int i = 0; i < pool.Count; ++i)
                        {
                                UIRecipeItemIcon iconView = pool[i];
                                if (iconView == null)
                                        continue;

                                bool shouldBeActive = i < requiredCount;
                                iconView.gameObject.SetActive(shouldBeActive);
                        }
                }

                private void EnsureIconPool(List<UIRecipeItemIcon> pool, RectTransform container, int requiredCount)
                {
                        if (requiredCount < 0)
                        {
                                requiredCount = 0;
                        }

                        while (pool.Count < requiredCount)
                        {
                                UIRecipeItemIcon iconInstance = Instantiate(_itemIconPrefab, container);
                                RectTransform iconRect = iconInstance.transform as RectTransform;
                                if (iconRect != null)
                                {
                                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                                        iconRect.anchoredPosition3D = Vector3.zero;
                                        iconRect.localScale = Vector3.one;
                                }

                                pool.Add(iconInstance);
                        }
                }

                private void UpdateCraftButtons(int craftableCount)
                {
                        _currentCraftableCount = craftableCount;

                        bool canCraft = craftableCount > 0 && _isCrafting == false;
                        bool canCraftAll = craftableCount > 0 && _isCrafting == false;

                        if (_craftButton != null)
                        {
                                _craftButton.interactable = canCraft;
                        }

                        if (_craftAllButton != null)
                        {
                                _craftAllButton.interactable = canCraftAll;
                        }

                        if (_craftButtonLabel != null)
                        {
                                _craftButtonLabel.text = _craftButtonText;
                        }

                        if (_craftAllButtonLabel != null)
                        {
                                if (craftableCount > 1)
                                {
                                        _craftAllButtonLabel.text = string.Format(_craftAllButtonCountFormat, craftableCount);
                                }
                                else
                                {
                                        _craftAllButtonLabel.text = _craftAllButtonText;
                                }
                        }

                        if (_cancelButtonLabel != null)
                        {
                                _cancelButtonLabel.text = _cancelButtonText;
                        }

                        if (_cancelButton != null)
                        {
                                _cancelButton.gameObject.SetActive(_isCrafting);
                                _cancelButton.interactable = true;
                        }

                        if (_craftingProgressRoot != null)
                        {
                                _craftingProgressRoot.SetActive(_isCrafting);
                        }

                        UpdateCraftingProgress();
                }

                private void HandleCraftClicked()
                {
                        if (_recipe == null)
                                return;

                        CraftRequested?.Invoke(_recipe);
                }

                private void HandleCraftAllClicked()
                {
                        if (_recipe == null)
                                return;

                        CraftAllRequested?.Invoke(_recipe);
                }

                private void HandleCancelClicked()
                {
                        if (_recipe == null || _isCrafting == false)
                                return;

                        CancelRequested?.Invoke(_recipe);
                }

                private void UpdateCraftingProgress()
                {
                        if (_craftingProgressImage == null)
                                return;

                        if (_isCrafting == false)
                        {
                                _craftingProgressImage.fillAmount = 0f;
                                return;
                        }

                        float duration = Mathf.Max(0.0001f, _craftDuration);
                        float elapsed = Time.unscaledTime - _craftStartTime;
                        float progress = Mathf.Clamp01(duration > 0f ? (elapsed / duration) : 1f);

                        _craftingProgressImage.fillAmount = progress;
                }
        }
}
