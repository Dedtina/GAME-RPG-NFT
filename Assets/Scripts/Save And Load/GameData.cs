using System.Collections.Generic;

[System.Serializable]
public class EquipmentSaveData
{
    public string instanceId;
    public string itemID;
    public int enhanceLevel;
}

[System.Serializable]
public class GameData
{
    public int currency;
    public SerializableDictionary<string, int> inventory;
    public SerializableDictionary<string, int> stash;
    public List<string> equipments;
    public List<EquipmentSaveData> inventoryEquipments;
    public List<EquipmentSaveData> equippedEquipments;
    public SerializableDictionary<string, bool> skills;
    public SerializableDictionary<string, bool> collectedWorldItems;

    public GameData()
    {
        currency = 0;
        inventory = new();
        stash = new();
        equipments = new();
        inventoryEquipments = new();
        equippedEquipments = new();
        skills = new();
        collectedWorldItems = new();
    }

    public void EnsureInitialized()
    {
        inventory ??= new();
        stash ??= new();
        equipments ??= new();
        inventoryEquipments ??= new();
        equippedEquipments ??= new();
        skills ??= new();
        collectedWorldItems ??= new();
    }
}
