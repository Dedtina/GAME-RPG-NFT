using System;
using Thirdweb;
using UnityEngine;

public static class NFTItemConverter
{
    public static event Action<string> OnConvertSucceeded;
    public static event Action<string> OnConvertFailed;
    public static bool IsConverting => isConverting;

    private static bool isConverting;
    private static InventoryItem pendingEquipmentInventoryItem;
    private static ItemData pendingEquipmentItem;
    private static int pendingEquipmentAmount;

    public static void ConvertInventoryItemToNFT(ItemData item, int amount = 1)
    {
        ConvertInventoryItemToNFT(item, amount, null);
    }

    public static void ConvertInventoryItemToNFT(InventoryItem inventoryItem, int amount = 1)
    {
        if (inventoryItem == null || inventoryItem.data == null)
        {
            FailConversion("Cannot convert a null inventory item to NFT.");
            return;
        }

        ConvertInventoryItemToNFT(inventoryItem.data, amount, inventoryItem);
    }

    private static async void ConvertInventoryItemToNFT(ItemData item, int amount, InventoryItem inventoryItem)
    {
        if (isConverting)
            return;

        if (item == null)
        {
            FailConversion("Cannot convert a null item to NFT.");
            return;
        }

        if (!item.canConvertToNFT)
        {
            FailConversion($"{item.itemName} is not configured for NFT conversion.");
            return;
        }

        if (string.IsNullOrEmpty(item.nftContractAddress))
        {
            FailConversion($"{item.itemName} is missing NFT contract address.");
            return;
        }

        if (!FirebaseWebGLBridge.HasCurrentUser)
        {
            FailConversion("Wallet is not logged in. Cannot convert item to NFT.");
            return;
        }

        if (Inventory.instance == null)
        {
            FailConversion("Inventory is not available. Cannot convert item to NFT.");
            return;
        }

        isConverting = true;

        try
        {
            NFTItemStandard standard = ResolveStandard(item);
            if (standard == NFTItemStandard.ERC721)
            {
                ConvertEquipmentToERC721(item, inventoryItem, amount);
                return;
            }

            TransactionResult result = await ConvertMaterialToERC1155(item, amount);

            if (inventoryItem != null)
                Inventory.instance.RemoveItem(inventoryItem, amount);
            else
                Inventory.instance.RemoveItem(item, amount);

            SaveManager.instance?.SaveGame();

            string message = $"Converted {item.itemName} to {standard}. Transaction: {result}";
            Debug.Log(message);
            OnConvertSucceeded?.Invoke(message);
        }
        catch (Exception e)
        {
            FailConversion($"Convert {item.itemName} to NFT failed: {e.Message}");
        }
        finally
        {
            if (pendingEquipmentItem == null)
                isConverting = false;
        }
    }

    private static NFTItemStandard ResolveStandard(ItemData item)
    {
        if (item.nftStandard != NFTItemStandard.Auto)
            return item.nftStandard;

        return item.itemType == ItemType.Material ? NFTItemStandard.ERC1155 : NFTItemStandard.ERC721;
    }

    private static async System.Threading.Tasks.Task<TransactionResult> ConvertMaterialToERC1155(ItemData item, int amount)
    {
        if (string.IsNullOrEmpty(item.nftTokenId))
            throw new InvalidOperationException($"{item.itemName} is a material NFT and requires nftTokenId.");

        Contract contract = ThirdwebManager.Instance.SDK.GetContract(item.nftContractAddress);
        return await contract.ERC1155.Claim(item.nftTokenId, amount);
    }

    private static void ConvertEquipmentToERC721(ItemData item, InventoryItem inventoryItem, int amount)
    {
        if (inventoryItem == null)
        {
            FailConversion("Equipment NFT conversion requires a specific inventory item instance.");
            return;
        }

        pendingEquipmentInventoryItem = inventoryItem;
        pendingEquipmentItem = item;
        pendingEquipmentAmount = amount;
        string instanceId = inventoryItem.instanceId;
        int enhanceLevel = inventoryItem.enhanceLevel;

        inventoryItem.EnsureInstanceId();
        instanceId = inventoryItem.instanceId;

        FirebaseWebGLBridge.ConvertEquipmentToNFT(
            item.itemID,
            instanceId,
            enhanceLevel,
            item.itemName,
            GetEquipmentDescription(item, enhanceLevel),
            item.nftImageUri,
            item.nftContractAddress
        );
    }

    private static string GetEquipmentDescription(ItemData item, int enhanceLevel)
    {
        EquipmentItemData equipmentData = item as EquipmentItemData;
        return equipmentData != null ? equipmentData.GetDescription(enhanceLevel) : item.GetDescription();
    }

    public static void OnEquipmentConvertSucceeded(string resultJson)
    {
        if (pendingEquipmentInventoryItem != null)
            Inventory.instance.RemoveItem(pendingEquipmentInventoryItem, pendingEquipmentAmount);
        else if (pendingEquipmentItem != null)
            Inventory.instance.RemoveItem(pendingEquipmentItem, pendingEquipmentAmount);

        SaveManager.instance?.SaveGame();

        string message = $"Converted equipment to ERC721 NFT. Result: {resultJson}";
        Debug.Log(message);
        OnConvertSucceeded?.Invoke(message);
        ClearPendingEquipmentConversion();
    }

    public static void OnEquipmentConvertFailed(string error)
    {
        FailConversion($"Convert equipment to NFT failed: {error}");
        ClearPendingEquipmentConversion();
    }

    private static void FailConversion(string error)
    {
        isConverting = false;
        Debug.LogError(error);
        OnConvertFailed?.Invoke(error);
    }

    private static void ClearPendingEquipmentConversion()
    {
        pendingEquipmentInventoryItem = null;
        pendingEquipmentItem = null;
        pendingEquipmentAmount = 0;
        isConverting = false;
    }
}
