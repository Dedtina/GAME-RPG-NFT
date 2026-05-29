using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_BlacksmithPanel : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform equipmentInventorySlotParent;
    [SerializeField] private UI_BlacksmithSelectedSlot selectedItemSlot;

    [Header("Details")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [FormerlySerializedAs("statPreviewText")]
    [SerializeField] private TextMeshProUGUI currentStatPreviewText;
    [SerializeField] private TextMeshProUGUI enhancedStatPreviewText;
    [SerializeField] private TextMeshProUGUI chanceText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button enhanceButton;

    [Header("Required Materials")]
    [SerializeField] private Image[] materialImages;

    private readonly StringBuilder sb = new();
    private UI_BlacksmithInventorySlot[] equipmentInventorySlots;
    private InventoryItem selectedItem;

    private void Awake()
    {
        if (equipmentInventorySlotParent != null)
            equipmentInventorySlots = equipmentInventorySlotParent.GetComponentsInChildren<UI_BlacksmithInventorySlot>(true);

        if (selectedItemSlot != null)
            selectedItemSlot.Setup(this);

        if (equipmentInventorySlots != null)
        {
            foreach (UI_BlacksmithInventorySlot slot in equipmentInventorySlots)
                slot.Setup(this);
        }

        if (enhanceButton != null)
            enhanceButton.onClick.AddListener(EnhanceSelectedItem);
    }

    private void OnEnable()
    {
        if (Inventory.instance != null)
            Inventory.instance.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.instance != null)
            Inventory.instance.OnInventoryChanged -= Refresh;
    }

    public void SelectItem(InventoryItem item)
    {
        selectedItem = item;
        resultText?.SetText("");
        RefreshSelectedItem();
    }

    public void ClearSelectedItem()
    {
        selectedItem = null;
        resultText?.SetText("");
        RefreshSelectedItem();
    }

    private void EnhanceSelectedItem()
    {
        if (Inventory.instance == null || selectedItem == null)
            return;

        EnhancementAttemptResult result = Inventory.instance.TryEnhanceItemOnce(selectedItem);

        if (resultText != null)
            resultText.text = result.message;

        Refresh();
    }

    private void Refresh()
    {
        RefreshEquipmentInventory();

        if (selectedItem != null && !Inventory.instance.GetInventoryItems().Contains(selectedItem) && !Inventory.instance.GetEquipmentItems().Contains(selectedItem))
            selectedItem = null;

        RefreshSelectedItem();
    }

    private void RefreshEquipmentInventory()
    {
        if (equipmentInventorySlots == null || Inventory.instance == null)
            return;

        List<InventoryItem> equipmentItems = Inventory.instance.GetInventoryItems().FindAll(item => item != null && item.IsEquipment());
        for (int i = 0; i < equipmentInventorySlots.Length; i++)
        {
            if (i < equipmentItems.Count)
                equipmentInventorySlots[i].UpdateInventorySlot(equipmentItems[i]);
            else
                equipmentInventorySlots[i].ClearSlot();
        }
    }

    private void RefreshSelectedItem()
    {
        if (selectedItemSlot != null)
        {
            if (selectedItem != null)
                selectedItemSlot.UpdateInventorySlot(selectedItem);
            else
                selectedItemSlot.ClearSlot();
        }

        if (selectedItem == null || !selectedItem.IsEquipment())
        {
            SetEmptyDetails();
            return;
        }

        EquipmentItemData equipmentData = selectedItem.data as EquipmentItemData;
        int currentLevel = selectedItem.enhanceLevel;
        int targetLevel = currentLevel + 1;
        EnhancementLevelData levelData = equipmentData.GetEnhancementLevelData(targetLevel);

        if (itemNameText != null)
            itemNameText.text = currentLevel > 0 ? $"{equipmentData.itemName} +{currentLevel}" : equipmentData.itemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = equipmentData.GetDescription(currentLevel);

        if (currentStatPreviewText != null)
            currentStatPreviewText.text = BuildCurrentStatPreview(equipmentData, currentLevel);

        if (enhancedStatPreviewText != null)
            enhancedStatPreviewText.text = BuildEnhancedStatPreview(equipmentData, targetLevel, levelData);

        if (chanceText != null)
            chanceText.text = BuildChanceText(levelData);

        if (costText != null)
            costText.text = BuildCostText(levelData);

        UpdateRequiredMaterials(levelData);

        if (enhanceButton != null)
            enhanceButton.interactable = levelData != null && levelData.HasValidOutcomeChanceTotal();
    }

    private void SetEmptyDetails()
    {
        if (itemNameText != null)
            itemNameText.text = "Select Equipment";
        if (itemDescriptionText != null)
            itemDescriptionText.text = "";
        if (currentStatPreviewText != null)
            currentStatPreviewText.text = "";
        if (enhancedStatPreviewText != null)
            enhancedStatPreviewText.text = "";
        if (chanceText != null)
            chanceText.text = "";
        if (costText != null)
            costText.text = "";
        ClearRequiredMaterials();
        if (enhanceButton != null)
            enhanceButton.interactable = false;
    }

    private string BuildCurrentStatPreview(EquipmentItemData equipmentData, int currentLevel)
    {
        sb.Length = 0;
        sb.AppendLine($"Current +{currentLevel}");
        sb.Append(equipmentData.GetDescription(currentLevel));
        return sb.ToString();
    }

    private string BuildEnhancedStatPreview(EquipmentItemData equipmentData, int targetLevel, EnhancementLevelData levelData)
    {
        if (levelData == null)
            return $"No config for +{targetLevel}.";

        sb.Length = 0;
        sb.AppendLine($"On Success +{targetLevel}");
        sb.Append(equipmentData.GetDescription(targetLevel));
        return sb.ToString();
    }

    private string BuildChanceText(EnhancementLevelData levelData)
    {
        if (levelData == null)
            return "Can not enhance.";

        sb.Length = 0;
        sb.AppendLine($"Success: {levelData.successChance:0.##}%");
        sb.AppendLine($"No Change: {levelData.noChangeChance:0.##}%");
        sb.AppendLine($"Downgrade: {levelData.downgradeChance:0.##}%");
        sb.AppendLine($"Reset: {levelData.resetChance:0.##}%");
        return sb.ToString();
    }

    private string BuildCostText(EnhancementLevelData levelData)
    {
        if (levelData == null)
            return "";

        sb.Length = 0;
        sb.AppendLine($"Gold: {levelData.currencyCost}");

        return sb.ToString();
    }

    private void UpdateRequiredMaterials(EnhancementLevelData levelData)
    {
        ClearRequiredMaterials();

        if (materialImages == null || levelData == null || levelData.requiredMaterials == null)
            return;

        if (levelData.requiredMaterials.Count > materialImages.Length)
            Debug.LogWarning($"Blacksmith material UI has {materialImages.Length} slots, but +{levelData.level} requires {levelData.requiredMaterials.Count} materials.", this);

        int max = Mathf.Min(levelData.requiredMaterials.Count, materialImages.Length);
        for (int i = 0; i < max; i++)
        {
            InventoryItem requiredMaterial = levelData.requiredMaterials[i];
            if (requiredMaterial == null || requiredMaterial.data == null)
                continue;

            Image materialImage = materialImages[i];
            materialImage.sprite = requiredMaterial.data.icon;
            materialImage.color = Color.white;

            TextMeshProUGUI amountText = materialImage.GetComponentInChildren<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.text = requiredMaterial.stack.ToString();
                amountText.color = Color.white;
            }
        }
    }

    private void ClearRequiredMaterials()
    {
        if (materialImages == null)
            return;

        foreach (Image materialImage in materialImages)
        {
            if (materialImage == null)
                continue;

            materialImage.sprite = null;
            materialImage.color = Color.clear;

            TextMeshProUGUI amountText = materialImage.GetComponentInChildren<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.text = "";
                amountText.color = Color.clear;
            }
        }
    }
}
