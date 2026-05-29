using UnityEngine.EventSystems;

public class UI_WizardNFTInventorySlot : UI_InventorySlot
{
    private UI_WizardNFTPanel wizardPanel;

    public void Setup(UI_WizardNFTPanel panel)
    {
        wizardPanel = panel;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (itemData == null || itemData.data == null || !itemData.data.canConvertToNFT)
            return;

        wizardPanel?.SelectItem(itemData);
    }
}
