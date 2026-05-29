using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SummonerPanel : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private TMP_Dropdown enemyDropdown;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI quantityLimitText;
    [SerializeField] private TextMeshProUGUI summonCostText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Required Item")]
    [SerializeField] private GameObject requiredItemContainer;
    [SerializeField] private Image requiredItemImage;
    [SerializeField] private TextMeshProUGUI requiredItemNameText;
    [SerializeField] private TextMeshProUGUI requiredItemAmountText;
    [SerializeField] private TextMeshProUGUI ownedItemAmountText;

    [Header("Actions")]
    [SerializeField] private Button decreaseQuantityButton;
    [SerializeField] private Button increaseQuantityButton;
    [SerializeField] private Button summonButton;
    [SerializeField] private Button closeButton;

    private readonly List<int> optionIndices = new();
    private NPCEnemySummoner activeSummoner;
    private int quantity = 1;

    private void Awake()
    {
        enemyDropdown?.onValueChanged.AddListener(_ => RefreshSelectedEnemy());
        decreaseQuantityButton?.onClick.AddListener(DecreaseQuantity);
        increaseQuantityButton?.onClick.AddListener(IncreaseQuantity);
        summonButton?.onClick.AddListener(SummonSelectedEnemy);
        closeButton?.onClick.AddListener(ClosePanel);
    }

    public void OpenFor(NPCEnemySummoner summoner)
    {
        activeSummoner = summoner;
        SetStatus("");
        PopulateEnemyOptions();

        quantity = 1;
        RefreshQuantity();
        RefreshSummonCost();
    }

    public NPCEnemySummoner ClearSummoner()
    {
        NPCEnemySummoner previousSummoner = activeSummoner;
        activeSummoner = null;
        return previousSummoner;
    }

    public void ClosePanel()
    {
        UI.instance?.CloseNpcFeature(NpcFeatureType.Summoner);
    }

    public void DecreaseQuantity()
    {
        quantity = Mathf.Max(1, quantity - 1);
        RefreshQuantity();
        RefreshSummonCost();
    }

    public void IncreaseQuantity()
    {
        if (activeSummoner == null)
            return;

        quantity = Mathf.Min(GetSelectedMaximumQuantity(), quantity + 1);
        RefreshQuantity();
        RefreshSummonCost();
    }

    private void PopulateEnemyOptions()
    {
        optionIndices.Clear();

        if (enemyDropdown == null)
        {
            SetSummonButton(false);
            SetStatus("Enemy dropdown is not assigned.");
            return;
        }

        enemyDropdown.ClearOptions();
        List<string> optionNames = new();

        if (activeSummoner != null && activeSummoner.EnemyOptions != null)
        {
            for (int i = 0; i < activeSummoner.EnemyOptions.Count; i++)
            {
                SummonEnemyOption option = activeSummoner.EnemyOptions[i];
                if (option == null || option.enemyPrefab == null)
                    continue;

                optionIndices.Add(i);
                optionNames.Add(option.GetDisplayName());
            }
        }

        enemyDropdown.AddOptions(optionNames);
        enemyDropdown.SetValueWithoutNotify(0);
        SetSummonButton(optionIndices.Count > 0);

        if (optionIndices.Count == 0)
            SetStatus("This NPC has no enemy prefabs assigned.");
    }

    private void SummonSelectedEnemy()
    {
        if (activeSummoner == null || optionIndices.Count == 0)
        {
            SetStatus("No enemy available to summon.");
            return;
        }

        int maximumQuantity = GetSelectedMaximumQuantity();
        if (quantity < 1 || quantity > maximumQuantity)
        {
            SetStatus($"Amount must be between 1 and {maximumQuantity}.");
            return;
        }

        int dropdownIndex = Mathf.Clamp(enemyDropdown.value, 0, optionIndices.Count - 1);
        if (!activeSummoner.TrySummonEnemies(optionIndices[dropdownIndex], quantity, out string failureMessage))
        {
            SetStatus(failureMessage);
            RefreshSummonCost();
            return;
        }

        UI.instance?.CloseNpcFeature(NpcFeatureType.Summoner);
    }

    private void RefreshQuantity()
    {
        int maxQuantity = GetSelectedMaximumQuantity();
        quantity = Mathf.Clamp(quantity, 1, maxQuantity);

        if (quantityText != null)
            quantityText.text = quantity.ToString();

        if (quantityLimitText != null)
            quantityLimitText.text = $"Amount (1-{maxQuantity})";

        if (decreaseQuantityButton != null)
            decreaseQuantityButton.interactable = quantity > 1;

        if (increaseQuantityButton != null)
            increaseQuantityButton.interactable = quantity < maxQuantity;
    }

    private void RefreshSelectedEnemy()
    {
        RefreshQuantity();
        RefreshSummonCost();
        SetStatus("");
    }

    private int GetSelectedMaximumQuantity()
    {
        if (activeSummoner == null || enemyDropdown == null || optionIndices.Count == 0)
            return 1;

        int dropdownIndex = Mathf.Clamp(enemyDropdown.value, 0, optionIndices.Count - 1);
        return activeSummoner.GetMaxQuantityForOption(optionIndices[dropdownIndex]);
    }

    private void RefreshSummonCost()
    {
        if (activeSummoner == null || enemyDropdown == null || optionIndices.Count == 0)
        {
            if (summonCostText != null)
                summonCostText.text = "";

            ClearRequiredItem();
            return;
        }

        int dropdownIndex = Mathf.Clamp(enemyDropdown.value, 0, optionIndices.Count - 1);
        int optionIndex = optionIndices[dropdownIndex];

        if (summonCostText != null)
            summonCostText.text = activeSummoner.GetSummonCostDescription(optionIndex, quantity);

        if (!activeSummoner.TryGetSummonRequirement(optionIndex, quantity, out ItemData requiredItem, out int requiredAmount, out int ownedAmount))
        {
            ClearRequiredItem();
            return;
        }

        if (requiredItemContainer != null)
            requiredItemContainer.SetActive(true);

        if (requiredItemImage != null)
        {
            requiredItemImage.sprite = requiredItem.icon;
            requiredItemImage.color = requiredItem.icon != null ? Color.white : Color.clear;
        }

        if (requiredItemNameText != null)
            requiredItemNameText.text = requiredItem.itemName;

        if (requiredItemAmountText != null)
            requiredItemAmountText.text = $"{requiredAmount}";

        if (ownedItemAmountText != null)
            ownedItemAmountText.text = $"Owned: {ownedAmount}";
    }

    private void ClearRequiredItem()
    {
        if (requiredItemContainer != null)
            requiredItemContainer.SetActive(false);

        if (requiredItemImage != null)
        {
            requiredItemImage.sprite = null;
            requiredItemImage.color = Color.clear;
        }

        if (requiredItemNameText != null)
            requiredItemNameText.text = "";

        if (requiredItemAmountText != null)
            requiredItemAmountText.text = "";

        if (ownedItemAmountText != null)
            ownedItemAmountText.text = "";
    }

    private void SetSummonButton(bool interactable)
    {
        if (summonButton != null)
            summonButton.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
