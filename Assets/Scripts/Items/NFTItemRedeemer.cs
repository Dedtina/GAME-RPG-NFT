using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Thirdweb;
using UnityEngine;

public class RedeemableNFTItem
{
    public NFT nft;
    public string contractAddress;
    public NFTItemStandard standard;
}

public static class NFTItemRedeemer
{
    private const string GameItemIdTrait = "Game Item ID";
    private const string EnhanceLevelTrait = "Enhance Level";

    public static event Action<string> OnRedeemSucceeded;
    public static event Action<string> OnRedeemFailed;
    public static bool IsRedeeming => isRedeeming;

    private static bool isRedeeming;

    public static async Task<List<NFT>> GetOwnedEquipmentNFTs(string contractAddress)
    {
        if (string.IsNullOrEmpty(contractAddress))
            throw new ArgumentException("NFT contract address is required.", nameof(contractAddress));

        string ownerAddress = await GetConnectedWalletAddress();
        Contract contract = ThirdwebManager.Instance.SDK.GetContract(contractAddress);
        return await contract.ERC721.GetOwned(ownerAddress);
    }

    public static async Task<List<NFT>> GetOwnedMaterialNFTs(string contractAddress)
    {
        if (string.IsNullOrEmpty(contractAddress))
            throw new ArgumentException("NFT contract address is required.", nameof(contractAddress));

        string ownerAddress = await GetConnectedWalletAddress();
        Contract contract = ThirdwebManager.Instance.SDK.GetContract(contractAddress);
        return await contract.ERC1155.GetOwned(ownerAddress);
    }

    public static int GetEquipmentEnhanceLevel(NFT nft)
    {
        return GetEnhanceLevel(nft.metadata.attributes);
    }

    public static void RedeemNFT(string contractAddress, string tokenId, NFTItemStandard standard, int amount = 1)
    {
        switch (standard)
        {
            case NFTItemStandard.ERC721:
                RedeemERC721Equipment(contractAddress, tokenId);
                break;
            case NFTItemStandard.ERC1155:
                RedeemERC1155Item(contractAddress, tokenId, amount);
                break;
            default:
                OnRedeemFailed?.Invoke("Redeem NFT failed: NFT standard is not supported for redeem.");
                break;
        }
    }

    public static async void RedeemERC721Equipment(string contractAddress, string tokenId)
    {
        if (isRedeeming)
            return;

        isRedeeming = true;

        try
        {
            RedeemItemData equipmentData = await ValidateERC721RedeemRequest(contractAddress, tokenId);
            Contract contract = ThirdwebManager.Instance.SDK.GetContract(contractAddress);

            await contract.ERC721.Burn(tokenId);

            Inventory.instance.AddItem(new InventoryItem(equipmentData.itemData)
            {
                stack = 1,
                enhanceLevel = equipmentData.enhanceLevel
            });
            SaveManager.instance?.SaveGame();

            string levelSuffix = equipmentData.enhanceLevel > 0 ? $" +{equipmentData.enhanceLevel}" : "";
            string message = $"Redeemed NFT #{tokenId} into {equipmentData.itemData.itemName}{levelSuffix}.";
            Debug.Log(message);
            OnRedeemSucceeded?.Invoke(message);
        }
        catch (Exception e)
        {
            string error = $"Redeem NFT failed: {e.Message}";
            Debug.LogError(error);
            OnRedeemFailed?.Invoke(error);
        }
        finally
        {
            isRedeeming = false;
        }
    }

    public static async void RedeemERC1155Item(string contractAddress, string tokenId, int amount = 1)
    {
        if (isRedeeming)
            return;

        isRedeeming = true;

        try
        {
            RedeemItemData itemData = await ValidateERC1155RedeemRequest(contractAddress, tokenId, amount);
            Contract contract = ThirdwebManager.Instance.SDK.GetContract(contractAddress);

            await contract.ERC1155.Burn(tokenId, itemData.amount);

            Inventory.instance.AddItem(new InventoryItem(itemData.itemData)
            {
                stack = itemData.amount,
                enhanceLevel = itemData.enhanceLevel
            });
            SaveManager.instance?.SaveGame();

            string amountPrefix = itemData.amount > 1 ? $"{itemData.amount}x " : "";
            string levelSuffix = itemData.enhanceLevel > 0 ? $" +{itemData.enhanceLevel}" : "";
            string message = $"Redeemed ERC1155 NFT #{tokenId} into {amountPrefix}{itemData.itemData.itemName}{levelSuffix}.";
            Debug.Log(message);
            OnRedeemSucceeded?.Invoke(message);
        }
        catch (Exception e)
        {
            string error = $"Redeem NFT failed: {e.Message}";
            Debug.LogError(error);
            OnRedeemFailed?.Invoke(error);
        }
        finally
        {
            isRedeeming = false;
        }
    }

    private static async Task<RedeemItemData> ValidateERC721RedeemRequest(string contractAddress, string tokenId)
    {
        ValidateCommonRedeemRequest(contractAddress, tokenId);

        Contract contract = ThirdwebManager.Instance.SDK.GetContract(contractAddress);
        string connectedWalletAddress = await GetConnectedWalletAddress();
        string ownerAddress = await contract.ERC721.OwnerOf(tokenId);

        if (!string.Equals(ownerAddress, connectedWalletAddress, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Connected wallet does not own this NFT.");

        NFT nft = await contract.ERC721.Get(tokenId);
        ItemData itemData = ResolveRedeemItemData(nft, contractAddress, tokenId, NFTItemStandard.ERC721);

        if (itemData.itemType != ItemType.Equipment)
            throw new InvalidOperationException($"{itemData.itemName} is not an ERC721 equipment item.");

        if (Inventory.instance.IsInventoryFull())
            throw new InvalidOperationException("Inventory is full.");

        return new RedeemItemData
        {
            itemData = itemData,
            enhanceLevel = GetEnhanceLevel(nft.metadata.attributes),
            amount = 1
        };
    }

    private static async Task<RedeemItemData> ValidateERC1155RedeemRequest(string contractAddress, string tokenId, int amount)
    {
        ValidateCommonRedeemRequest(contractAddress, tokenId);

        if (amount <= 0)
            throw new InvalidOperationException("Redeem amount must be greater than zero.");

        Contract contract = ThirdwebManager.Instance.SDK.GetContract(contractAddress);
        string connectedWalletAddress = await GetConnectedWalletAddress();
        string balanceValue = await contract.ERC1155.BalanceOf(connectedWalletAddress, tokenId);

        if (!BigInteger.TryParse(balanceValue, out BigInteger balance) || balance < amount)
            throw new InvalidOperationException("Connected wallet does not own enough of this ERC1155 NFT.");

        NFT nft = await contract.ERC1155.Get(tokenId);
        ItemData itemData = ResolveRedeemItemData(nft, contractAddress, tokenId, NFTItemStandard.ERC1155);

        if (itemData.itemType == ItemType.Equipment && Inventory.instance.IsInventoryFull())
            throw new InvalidOperationException("Inventory is full.");

        return new RedeemItemData
        {
            itemData = itemData,
            enhanceLevel = GetEnhanceLevel(nft.metadata.attributes),
            amount = amount
        };
    }

    private static void ValidateCommonRedeemRequest(string contractAddress, string tokenId)
    {
        if (Inventory.instance == null)
            throw new InvalidOperationException("Inventory is not available.");

        if (ThirdwebManager.Instance == null)
            throw new InvalidOperationException("Thirdweb manager is not available.");

        if (string.IsNullOrEmpty(contractAddress))
            throw new InvalidOperationException("NFT contract address is required.");

        if (string.IsNullOrEmpty(tokenId))
            throw new InvalidOperationException("NFT token id is required.");
    }

    private static ItemData ResolveRedeemItemData(NFT nft, string contractAddress, string tokenId, NFTItemStandard standard)
    {
        string itemId = GetMetadataTraitValue(nft.metadata.attributes, GameItemIdTrait);

        if (!string.IsNullOrEmpty(itemId))
        {
            if (!Inventory.instance.TryGetItemData(itemId, out ItemData itemData))
                throw new InvalidOperationException($"No in-game item matches NFT Game Item ID: {itemId}.");

            return itemData;
        }

        if (Inventory.instance.TryGetNFTItemData(contractAddress, tokenId, standard, out ItemData mappedItemData))
            return mappedItemData;

        throw new InvalidOperationException("NFT metadata is missing the Game Item ID trait and no in-game item is mapped to this NFT token.");
    }

    private static async Task<string> GetConnectedWalletAddress()
    {
        if (FirebaseWebGLBridge.HasCurrentUser)
            return FirebaseWebGLBridge.CurrentUserId;

        if (ThirdwebManager.Instance == null)
            throw new InvalidOperationException("Wallet is not connected.");

        string walletAddress = await ThirdwebManager.Instance.SDK.wallet.GetAddress();
        if (string.IsNullOrEmpty(walletAddress))
            throw new InvalidOperationException("Wallet is not connected.");

        return walletAddress;
    }

    private static int GetEnhanceLevel(object attributes)
    {
        string enhanceLevelValue = GetMetadataTraitValue(attributes, EnhanceLevelTrait);
        if (!int.TryParse(enhanceLevelValue, out int enhanceLevel))
            return 0;

        return Mathf.Max(0, enhanceLevel);
    }

    private static string GetMetadataTraitValue(object attributes, string traitName)
    {
        if (attributes == null)
            return null;

        JToken attributesToken = attributes as JToken ?? JToken.FromObject(attributes);
        if (attributesToken.Type != JTokenType.Array)
            return null;

        foreach (JToken attribute in attributesToken)
        {
            string traitType = attribute["trait_type"]?.Value<string>();
            if (!string.Equals(traitType, traitName, StringComparison.Ordinal))
                continue;

            return attribute["value"]?.Value<string>();
        }

        return null;
    }

    private struct RedeemItemData
    {
        public ItemData itemData;
        public int enhanceLevel;
        public int amount;
    }
}
