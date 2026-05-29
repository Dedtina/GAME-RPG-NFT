using System;
using System.Collections.Generic;
using TMPro;
using Thirdweb;
using UnityEngine;
using UnityEngine.UI;

public class UI_WizardNFTRedeemPanel : MonoBehaviour
{
    [Header("Contract")]
    [SerializeField] private string equipmentContractAddress;
    [SerializeField] private string materialContractAddress;

    [Header("NFT Stash")]
    [SerializeField] private Transform nftStashSlotParent;
    [SerializeField] private UI_WizardNFTRedeemSlot nftStashSlotPrefab;

    [Header("Selected NFT")]
    [SerializeField] private Image selectedNftImage;
    [SerializeField] private TextMeshProUGUI selectedNftNameText;
    [SerializeField] private TextMeshProUGUI selectedNftDescriptionText;

    [Header("Actions")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button redeemButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<UI_WizardNFTRedeemSlot> nftStashSlots = new();
    private readonly List<RedeemableNFTItem> ownedRedeemableNfts = new();
    private RedeemableNFTItem selectedNft;
    private int refreshVersion;
    private bool isLoading;

    private void Awake()
    {
        HideSceneSlotTemplate();

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshOwnedNFTs);

        if (redeemButton != null)
            redeemButton.onClick.AddListener(RedeemSelectedNFT);
    }

    private void OnEnable()
    {
        NFTItemRedeemer.OnRedeemSucceeded += HandleRedeemSucceeded;
        NFTItemRedeemer.OnRedeemFailed += HandleRedeemFailed;
        RefreshOwnedNFTs();
    }

    private void OnDisable()
    {
        NFTItemRedeemer.OnRedeemSucceeded -= HandleRedeemSucceeded;
        NFTItemRedeemer.OnRedeemFailed -= HandleRedeemFailed;
        refreshVersion++;
        isLoading = false;
    }

    public void SelectNFT(NFT nft)
    {
        selectedNft = FindRedeemableNFT(nft);
        RefreshSelectedNFT();
    }

    public async void RefreshOwnedNFTs()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(equipmentContractAddress) && string.IsNullOrWhiteSpace(materialContractAddress))
        {
            ClearNFTStash();
            ClearSelectedNFT();
            SetStatus("NFT contract address is missing.");
            return;
        }

        int currentRefreshVersion = ++refreshVersion;
        isLoading = true;
        SetButtonsInteractable(false);
        SetStatus("Loading NFTs from wallet...");

        try
        {
            List<RedeemableNFTItem> ownedNfts = await GetOwnedRedeemableNFTs();
            if (!CanApplyRefresh(currentRefreshVersion))
                return;

            if (!RefreshNFTStash(ownedNfts))
                return;

            ClearSelectedNFT();
            SetStatus(GetLoadStatus(ownedNfts.Count));
        }
        catch (Exception e)
        {
            if (!CanApplyRefresh(currentRefreshVersion))
                return;

            ClearNFTStash();
            ClearSelectedNFT();
            SetStatus($"Load NFT items failed: {e.Message}");
        }
        finally
        {
            if (currentRefreshVersion == refreshVersion)
            {
                isLoading = false;
                SetButtonsInteractable(true);
            }
        }
    }

    private bool CanApplyRefresh(int currentRefreshVersion)
    {
        return this != null && isActiveAndEnabled && currentRefreshVersion == refreshVersion;
    }

    private async System.Threading.Tasks.Task<List<RedeemableNFTItem>> GetOwnedRedeemableNFTs()
    {
        List<RedeemableNFTItem> ownedNfts = new();

        if (!string.IsNullOrWhiteSpace(equipmentContractAddress))
        {
            List<NFT> equipmentNfts = await NFTItemRedeemer.GetOwnedEquipmentNFTs(equipmentContractAddress);
            foreach (NFT nft in equipmentNfts)
            {
                ownedNfts.Add(new RedeemableNFTItem
                {
                    nft = nft,
                    contractAddress = equipmentContractAddress,
                    standard = NFTItemStandard.ERC721
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(materialContractAddress))
        {
            List<NFT> materialNfts = await NFTItemRedeemer.GetOwnedMaterialNFTs(materialContractAddress);
            foreach (NFT nft in materialNfts)
            {
                ownedNfts.Add(new RedeemableNFTItem
                {
                    nft = nft,
                    contractAddress = materialContractAddress,
                    standard = NFTItemStandard.ERC1155
                });
            }
        }

        return ownedNfts;
    }

    private bool RefreshNFTStash(List<RedeemableNFTItem> ownedNfts)
    {
        ClearNFTStash();
        ownedRedeemableNfts.Clear();
        ownedRedeemableNfts.AddRange(ownedNfts);

        if (nftStashSlotParent == null || nftStashSlotPrefab == null)
        {
            SetStatus("NFT stash parent or slot prefab is missing.");
            return false;
        }

        foreach (RedeemableNFTItem nft in ownedNfts)
        {
            UI_WizardNFTRedeemSlot slot = Instantiate(nftStashSlotPrefab, nftStashSlotParent);
            slot.gameObject.SetActive(true);
            slot.Setup(this);
            slot.LoadNFT(nft.nft);
            nftStashSlots.Add(slot);
        }

        RebuildNFTStashLayout();
        return true;
    }

    private void RedeemSelectedNFT()
    {
        if (selectedNft == null)
        {
            SetStatus("Select an NFT item first.");
            return;
        }

        if (NFTItemRedeemer.IsRedeeming)
        {
            SetStatus("Redeem is already running.");
            return;
        }

        NFT nft = selectedNft.nft;
        SetButtonsInteractable(false);
        SetStatus($"Redeeming NFT #{nft.metadata.id}...");
        NFTItemRedeemer.RedeemNFT(selectedNft.contractAddress, nft.metadata.id, selectedNft.standard, 1);
    }

    private void HandleRedeemSucceeded(string message)
    {
        SetStatus(message);
        RefreshOwnedNFTs();
    }

    private void HandleRedeemFailed(string error)
    {
        SetStatus(error);
        SetButtonsInteractable(true);
    }

    private void RefreshSelectedNFT()
    {
        if (selectedNft == null)
        {
            ClearSelectedNFT();
            return;
        }

        NFT nft = selectedNft.nft;
        if (selectedNftNameText != null)
            selectedNftNameText.text = GetDisplayName(selectedNft);

        if (selectedNftDescriptionText != null)
            selectedNftDescriptionText.text = GetDescription(selectedNft);

        LoadSelectedNFTImage(selectedNft);
        SetRedeemButtonInteractable(!NFTItemRedeemer.IsRedeeming);
    }

    private async void LoadSelectedNFTImage(RedeemableNFTItem nftItem)
    {
        if (selectedNftImage == null)
            return;

        selectedNftImage.sprite = null;
        selectedNftImage.color = Color.clear;

        NFT nft = nftItem.nft;
        if (ThirdwebManager.Instance == null || string.IsNullOrEmpty(nft.metadata.image))
            return;

        Sprite image = await ThirdwebManager.Instance.SDK.storage.DownloadImage(nft.metadata.image);
        if (!IsSelectedNFT(nftItem) || selectedNftImage == null)
            return;

        selectedNftImage.sprite = image;
        selectedNftImage.color = image != null ? Color.white : Color.clear;
    }

    private void ClearSelectedNFT()
    {
        selectedNft = null;

        if (selectedNftImage != null)
        {
            selectedNftImage.sprite = null;
            selectedNftImage.color = Color.clear;
        }

        if (selectedNftNameText != null)
            selectedNftNameText.text = "Select NFT Item";

        if (selectedNftDescriptionText != null)
            selectedNftDescriptionText.text = "";

        SetRedeemButtonInteractable(false);
    }

    private void ClearNFTStash()
    {
        foreach (UI_WizardNFTRedeemSlot slot in nftStashSlots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(false);
                Destroy(slot.gameObject);
            }
        }

        nftStashSlots.Clear();
        ownedRedeemableNfts.Clear();
        RebuildNFTStashLayout();
    }

    private string GetDisplayName(RedeemableNFTItem nftItem)
    {
        NFT nft = nftItem.nft;
        string name = string.IsNullOrEmpty(nft.metadata.name) ? "NFT Item" : nft.metadata.name;
        if (nftItem.standard == NFTItemStandard.ERC1155)
        {
            int quantity = Mathf.Max(1, nft.quantityOwned);
            return $"{name} x{quantity}";
        }

        int enhanceLevel = NFTItemRedeemer.GetEquipmentEnhanceLevel(nft);
        return enhanceLevel > 0 ? $"{name} +{enhanceLevel}" : name;
    }

    private string GetDescription(RedeemableNFTItem nftItem)
    {
        NFT nft = nftItem.nft;
        string description = nft.metadata.description ?? "";
        string standardText = nftItem.standard == NFTItemStandard.ERC1155 ? "ERC1155" : "ERC721";
        string tokenLine = $"{standardText} Token #{nft.metadata.id}";

        if (nftItem.standard == NFTItemStandard.ERC1155)
            tokenLine += $"\nOwned: {Mathf.Max(1, nft.quantityOwned)}";

        if (string.IsNullOrWhiteSpace(description))
            return tokenLine;

        return $"{description}\n{tokenLine}";
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (refreshButton != null)
            refreshButton.interactable = interactable;

        SetRedeemButtonInteractable(interactable && selectedNft != null && !NFTItemRedeemer.IsRedeeming);
    }

    private void SetRedeemButtonInteractable(bool interactable)
    {
        if (redeemButton != null)
            redeemButton.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private string GetLoadStatus(int nftCount)
    {
        if (nftCount == 0)
            return "No NFT items found in this wallet.";

        return $"Loaded {nftCount} NFT item(s).";
    }

    private bool IsSelectedNFT(RedeemableNFTItem nftItem)
    {
        return selectedNft != null &&
               selectedNft.standard == nftItem.standard &&
               string.Equals(selectedNft.contractAddress, nftItem.contractAddress, StringComparison.OrdinalIgnoreCase) &&
               selectedNft.nft.metadata.id == nftItem.nft.metadata.id;
    }

    private RedeemableNFTItem FindRedeemableNFT(NFT nft)
    {
        NFTItemStandard standard = ResolveNFTStandard(nft);

        foreach (RedeemableNFTItem item in ownedRedeemableNfts)
        {
            if (item.standard == standard && item.nft.metadata.id == nft.metadata.id)
                return item;
        }

        return null;
    }

    private NFTItemStandard ResolveNFTStandard(NFT nft)
    {
        if (nft.quantityOwned > 0 || string.Equals(nft.type, "ERC1155", StringComparison.OrdinalIgnoreCase))
            return NFTItemStandard.ERC1155;

        return NFTItemStandard.ERC721;
    }

    private void HideSceneSlotTemplate()
    {
        if (nftStashSlotParent == null || nftStashSlotPrefab == null)
            return;

        if (nftStashSlotPrefab.transform.parent == nftStashSlotParent)
            nftStashSlotPrefab.gameObject.SetActive(false);
    }

    private void RebuildNFTStashLayout()
    {
        RectTransform stashRect = nftStashSlotParent as RectTransform;
        if (stashRect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(stashRect);
    }
}
