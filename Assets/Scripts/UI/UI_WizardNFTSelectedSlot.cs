using UnityEngine.EventSystems;

public class UI_WizardNFTSelectedSlot : UI_InventorySlot
{
    private UI_WizardNFTPanel wizardPanel;

    public void Setup(UI_WizardNFTPanel panel)
    {
        wizardPanel = panel;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            wizardPanel?.ClearSelectedItem();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        HideEquipmentTooltips();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        HideEquipmentTooltips();
    }
}
