using System;

[Serializable]
public class InventoryItem
{
    public string instanceId;
    public ItemData data;
    public int stack;
    public int enhanceLevel;

    public InventoryItem(ItemData _item)
    {
        this.data = _item;
        AddStack();
        EnsureInstanceId();
    }

    public void AddStack(int amount = 1) => this.stack += amount;
    public void RemoveStack(int amount = 1) => this.stack -= amount;

    public bool IsEquipment()
    {
        return data != null && data.itemType == ItemType.Equipment;
    }

    public void EnsureInstanceId()
    {
        if (IsEquipment() && string.IsNullOrEmpty(instanceId))
            instanceId = Guid.NewGuid().ToString("N");
    }
}
