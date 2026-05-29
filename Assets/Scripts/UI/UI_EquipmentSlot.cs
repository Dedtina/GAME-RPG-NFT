using UnityEngine.EventSystems;

public class UI_EquipmentSlot : UI_InventorySlot
{
    public EquipmentType slotType;

    private void OnValidate()
    {
        gameObject.name = "Equipment Slot - " + slotType.ToString();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (
            itemData != null &&
            itemData.data != null &&
            itemData.data.itemType == ItemType.Equipment
        )
        {
            EquipmentItemData selectedItemData = itemData.data as EquipmentItemData;
            InventoryItem unequippedItem = Inventory.instance.Unequip(selectedItemData);
            Inventory.instance.AddItem(unequippedItem);
            HideEquipmentTooltips();
        }
    }
}
