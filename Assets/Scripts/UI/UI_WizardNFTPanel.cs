using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_WizardNFTPanel : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform convertibleInventorySlotParent;
    [SerializeField] private UI_WizardNFTSelectedSlot selectedItemSlot;

    [Header("Details")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button convertButton;

    private UI_WizardNFTInventorySlot[] convertibleInventorySlots;
    private InventoryItem selectedItem;

    private void Awake()
    {
        if (convertibleInventorySlotParent != null)
            convertibleInventorySlots = convertibleInventorySlotParent.GetComponentsInChildren<UI_WizardNFTInventorySlot>(true);

        if (selectedItemSlot != null)
            selectedItemSlot.Setup(this);

        if (convertibleInventorySlots != null)
        {
            foreach (UI_WizardNFTInventorySlot slot in convertibleInventorySlots)
                slot.Setup(this);
        }

        if (convertButton != null)
            convertButton.onClick.AddListener(ConvertSelectedItem);
    }

    private void OnEnable()
    {
        if (Inventory.instance != null)
            Inventory.instance.OnInventoryChanged += Refresh;

        NFTItemConverter.OnConvertSucceeded += HandleConvertSucceeded;
        NFTItemConverter.OnConvertFailed += HandleConvertFailed;

        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.instance != null)
            Inventory.instance.OnInventoryChanged -= Refresh;

        NFTItemConverter.OnConvertSucceeded -= HandleConvertSucceeded;
        NFTItemConverter.OnConvertFailed -= HandleConvertFailed;
    }

    public void SelectItem(InventoryItem item)
    {
        selectedItem = item;
        SetStatus("");
        RefreshSelectedItem();
    }

    public void ClearSelectedItem()
    {
        selectedItem = null;
        SetStatus("");
        RefreshSelectedItem();
    }

    private void ConvertSelectedItem()
    {
        if (selectedItem == null || selectedItem.data == null)
        {
            SetStatus("Select an item first.");
            return;
        }

        if (NFTItemConverter.IsConverting)
        {
            SetStatus("Conversion is already running.");
            return;
        }

        SetStatus("Converting item to NFT...");
        SetConvertButton(false);
        NFTItemConverter.ConvertInventoryItemToNFT(selectedItem);
    }

    private void HandleConvertSucceeded(string message)
    {
        selectedItem = null;
        SetStatus("Convert succeeded. NFT has been added to your wallet.");
        Refresh();
    }

    private void HandleConvertFailed(string error)
    {
        SetStatus(error);
        RefreshSelectedItem();
    }

    private void Refresh()
    {
        RefreshConvertibleInventory();

        if (Inventory.instance != null && selectedItem != null && !Inventory.instance.GetInventoryItems().Contains(selectedItem))
            selectedItem = null;

        RefreshSelectedItem();
    }

    private void RefreshConvertibleInventory()
    {
        if (convertibleInventorySlots == null || Inventory.instance == null)
            return;

        List<InventoryItem> convertibleItems = Inventory.instance.GetInventoryItems()
            .FindAll(item => item != null && item.data != null && item.data.canConvertToNFT);

        for (int i = 0; i < convertibleInventorySlots.Length; i++)
        {
            if (i < convertibleItems.Count)
                convertibleInventorySlots[i].UpdateInventorySlot(convertibleItems[i]);
            else
                convertibleInventorySlots[i].ClearSlot();
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

        if (selectedItem == null || selectedItem.data == null)
        {
            if (itemNameText != null)
                itemNameText.text = "Select NFT Item";
            if (itemDescriptionText != null)
                itemDescriptionText.text = "";

            SetConvertButton(false);
            return;
        }

        ItemData item = selectedItem.data;
        if (itemNameText != null)
            itemNameText.text = selectedItem.IsEquipment() && selectedItem.enhanceLevel > 0 ? $"{item.itemName} +{selectedItem.enhanceLevel}" : item.itemName;

        if (itemDescriptionText != null)
        {
            EquipmentItemData equipmentData = item as EquipmentItemData;
            itemDescriptionText.text = equipmentData != null ? equipmentData.GetDescription(selectedItem.enhanceLevel) : item.GetDescription();
        }

        SetConvertButton(item.canConvertToNFT && !NFTItemConverter.IsConverting);
    }

    private void SetConvertButton(bool interactable)
    {
        if (convertButton != null)
            convertButton.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
