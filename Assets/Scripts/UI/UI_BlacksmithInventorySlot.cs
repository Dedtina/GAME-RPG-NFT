using UnityEngine.EventSystems;

public class UI_BlacksmithInventorySlot : UI_InventorySlot
{
    private UI_BlacksmithPanel blacksmithPanel;

    public void Setup(UI_BlacksmithPanel panel)
    {
        blacksmithPanel = panel;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (itemData == null || !itemData.IsEquipment())
            return;

        blacksmithPanel?.SelectItem(itemData);
    }
}
