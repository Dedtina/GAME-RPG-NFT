using Thirdweb;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_WizardNFTRedeemSlot : UI_InventorySlot
{
    private UI_WizardNFTRedeemPanel redeemPanel;
    private NFT? nft;

    public void Setup(UI_WizardNFTRedeemPanel panel)
    {
        redeemPanel = panel;
        ClearSlot();
    }

    public void LoadNFT(NFT ownedNft)
    {
        nft = ownedNft;
        itemData = null;

        if (itemText != null)
        {
            if (IsERC1155(ownedNft))
            {
                int quantity = Mathf.Max(1, ownedNft.quantityOwned);
                itemText.text = $"x{quantity}";
            }
            else
            {
                int enhanceLevel = NFTItemRedeemer.GetEquipmentEnhanceLevel(ownedNft);
                itemText.text = enhanceLevel > 0 ? $"+{enhanceLevel}" : "";
            }
        }

        LoadNFTImage(ownedNft);
    }

    public override void ClearSlot()
    {
        nft = null;
        base.ClearSlot();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && nft.HasValue)
            redeemPanel?.SelectNFT(nft.Value);
    }

    private async void LoadNFTImage(NFT ownedNft)
    {
        if (itemImage == null)
            return;

        itemImage.sprite = null;
        itemImage.color = Color.clear;

        if (ThirdwebManager.Instance == null || string.IsNullOrEmpty(ownedNft.metadata.image))
            return;

        Sprite image = await ThirdwebManager.Instance.SDK.storage.DownloadImage(ownedNft.metadata.image);
        if (this == null || itemImage == null || !nft.HasValue || nft.Value.metadata.id != ownedNft.metadata.id)
            return;

        itemImage.sprite = image;
        itemImage.color = image != null ? Color.white : Color.clear;
    }

    private bool IsERC1155(NFT ownedNft)
    {
        return ownedNft.quantityOwned > 0 ||
               string.Equals(ownedNft.type, "ERC1155", System.StringComparison.OrdinalIgnoreCase);
    }
}
