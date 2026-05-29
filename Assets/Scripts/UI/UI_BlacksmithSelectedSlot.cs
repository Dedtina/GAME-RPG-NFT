using UnityEngine.EventSystems;

public class UI_BlacksmithSelectedSlot : UI_InventorySlot
{
    private UI_BlacksmithPanel blacksmithPanel;

    public void Setup(UI_BlacksmithPanel panel)
    {
        blacksmithPanel = panel;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            blacksmithPanel?.ClearSelectedItem();
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
