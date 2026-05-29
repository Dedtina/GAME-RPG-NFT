using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StartedItem
{
    public ItemData item;
    public int stack;
}

public enum EnhancementAttemptStatus
{
    NotAttempted,
    Success,
    Downgrade,
    Reset,
    NoChange
}

public struct EnhancementAttemptResult
{
    public bool attempted;
    public EnhancementAttemptStatus status;
    public int previousLevel;
    public int currentLevel;
    public string message;
}

public class Inventory : MonoBehaviour, IGameData
{
    public static Inventory instance;
    public event Action OnInventoryChanged;
    [Header("Started Pack")]
    [SerializeField] private List<StartedItem> startedPack;

    [Header("Inventory")]
    [SerializeField] private List<InventoryItem> inventoryItems;
    [SerializeField] private Dictionary<ItemData, InventoryItem> inventoryDict;
    [SerializeField] private List<InventoryItem> stashItems;
    [SerializeField] private Dictionary<ItemData, InventoryItem> stashDict;
    [SerializeField] private List<InventoryItem> equippedItems;
    [SerializeField] private Dictionary<EquipmentItemData, InventoryItem> equippedDict;

    [Header("Inventory UI")]
    [SerializeField] private GameObject inventorySlotParent;
    private UI_InventorySlot[] inventorySlots;
    [SerializeField] private GameObject stashSlotParent;
    private UI_InventorySlot[] stashSlots;
    [SerializeField] private GameObject equipmentSlotParent;
    private UI_EquipmentSlot[] equipmentSlots;
    [SerializeField] private GameObject statSlotsParent;
    private UI_StatSlot[] statSlots;

    private float lastTimeUsedFlask = 0;
    private float flaskCooldown = 0;
    public Action<float> OnFlaskCooldownUpdated;

    private float lastTimeUsedArmor = 0;
    private float armorCooldown = 0;

    [NonSerialized] public Dictionary<string, ItemData> assetDict;
    private List<InventoryItem> loadedItems = new();
    private List<InventoryItem> loadedEquipItems = new();
    private bool hasStarted;
    private bool suppressInventoryChanged;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        inventoryItems = new List<InventoryItem>();
        inventoryDict = new Dictionary<ItemData, InventoryItem>();
        stashItems = new List<InventoryItem>();
        stashDict = new Dictionary<ItemData, InventoryItem>();
        equippedItems = new List<InventoryItem>();
        equippedDict = new Dictionary<EquipmentItemData, InventoryItem>();
        BuildItemDatabase();
    }

    private void Start()
    {
        BuildItemDatabase();

        inventorySlots = inventorySlotParent.GetComponentsInChildren<UI_InventorySlot>();
        stashSlots = stashSlotParent.GetComponentsInChildren<UI_InventorySlot>();
        equipmentSlots = equipmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>();
        statSlots = statSlotsParent.GetComponentsInChildren<UI_StatSlot>();

        foreach (StartedItem item in startedPack)
            AddItem(item.item, item.stack);

        foreach (InventoryItem item in loadedItems)
            AddItem(item);

        foreach (InventoryItem item in loadedEquipItems)
            EquipItem(item);

        hasStarted = true;
    }

    public void EquipItem(ItemData _newItem)
    {
        EquipItem(new InventoryItem(_newItem));
    }

    public void EquipItem(InventoryItem inventoryItem)
    {
        EquipmentItemData _newEquipmentItemData = inventoryItem.data as EquipmentItemData;
        if (_newEquipmentItemData == null)
            return;

        inventoryItem.stack = 1;
        inventoryItem.EnsureInstanceId();

        EquipmentItemData existingData = null;
        foreach (KeyValuePair<EquipmentItemData, InventoryItem> item in equippedDict)
        {
            if (item.Key.equipmentType == _newEquipmentItemData.equipmentType)
            {
                existingData = item.Key;
                break;
            }
        }

        if (existingData != null)
        {
            Debug.Log(">> I THINK SOMETHIGN ISBROKEN");
            InventoryItem unequippedItem = Unequip(existingData);
            AddItem(unequippedItem);
        }

        _newEquipmentItemData.AddModifier(inventoryItem.enhanceLevel);
        equippedItems.Add(inventoryItem);
        equippedDict.Add(_newEquipmentItemData, inventoryItem);

        UpdateInventory();
        NotifyInventoryChanged();
    }

    public InventoryItem Unequip(EquipmentItemData existingData)
    {
        if (equippedDict.TryGetValue(existingData, out InventoryItem value))
        {
            existingData.RemoveModifier(value.enhanceLevel);
            equippedItems.Remove(value);
            equippedDict.Remove(existingData);
            UpdateInventory();
            NotifyInventoryChanged();
            return value;
        }

        return null;
    }

    private void UpdateInventory()
    {
        foreach (UI_EquipmentSlot slot in equipmentSlots)
        {
            bool flag = false;
            foreach (KeyValuePair<EquipmentItemData, InventoryItem> item in equippedDict)
            {
                if (item.Key.equipmentType == slot.slotType)
                {
                    slot.UpdateInventorySlot(item.Value);
                    flag = true;
                    break;
                }
            }
            if (!flag) slot.ClearSlot();
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i < inventoryItems.Count)
                inventorySlots[i].UpdateInventorySlot(inventoryItems[i]);
            else
                inventorySlots[i].ClearSlot();
        }

        for (int i = 0; i < stashSlots.Length; i++)
        {
            if (i < stashItems.Count)
                stashSlots[i].UpdateInventorySlot(stashItems[i]);
            else
                stashSlots[i].ClearSlot();
        }

        for (int i = 0; i < statSlots.Length; i++)
        {
            statSlots[i].UpdateStatValue();
        }
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item.itemType == ItemType.Equipment && !IsInventoryFull())
            AddToInventory(item, amount);
        else if (item.itemType == ItemType.Material)
            AddToStash(item, amount);

        UpdateInventory();
        NotifyInventoryChanged();
    }

    public void AddItem(InventoryItem item)
    {
        if (item == null || item.data == null)
            return;

        if (item.data.itemType == ItemType.Equipment && !IsInventoryFull())
        {
            item.stack = 1;
            item.EnsureInstanceId();
            inventoryItems.Add(item);
        }
        else if (item.data.itemType == ItemType.Material)
        {
            AddToStash(item.data, item.stack);
        }

        UpdateInventory();
        NotifyInventoryChanged();
    }

    private void AddToInventory(ItemData item, int amount = 1)
    {
        if (item.itemType == ItemType.Equipment)
        {
            for (int i = 0; i < amount && !IsInventoryFull(); i++)
            {
                Debug.Log("AddToInventory, name: " + item.name + "; value: 1");
                InventoryItem inventoryItem = new(item);
                inventoryItem.stack = 1;
                inventoryItem.EnsureInstanceId();
                inventoryItems.Add(inventoryItem);
            }
        }
        else if (inventoryDict.TryGetValue(item, out InventoryItem inventoryItem))
        {
            inventoryItem.AddStack(amount);
        }
        else
        {
            Debug.Log("AddToInventory, name: " + item.name + "; value: " + amount);
            inventoryItem = new InventoryItem(item);
            inventoryItem.stack = amount;
            inventoryItems.Add(inventoryItem);
            inventoryDict.Add(item, inventoryItem);
        }
    }

    private void AddToStash(ItemData item, int amount = 1)
    {
        if (stashDict.TryGetValue(item, out InventoryItem stashItem))
        {
            stashItem.AddStack(amount);
        }
        else
        {
            stashItem = new InventoryItem(item);
            stashItem.stack = amount;
            stashItems.Add(stashItem);
            stashDict.Add(item, stashItem);
        }
    }

    public void RemoveItem(ItemData item, int amount = 1)
    {
        if (item.itemType == ItemType.Equipment)
            RemoveInventoryItem(item, amount);
        else if (item.itemType == ItemType.Material)
            RemoveStashItem(item, amount);

        UpdateInventory();
        NotifyInventoryChanged();
    }

    public void RemoveItem(InventoryItem item, int amount = 1)
    {
        if (item == null || item.data == null)
            return;

        if (item.data.itemType == ItemType.Equipment)
            inventoryItems.Remove(item);
        else if (item.data.itemType == ItemType.Material)
            RemoveStashItem(item.data, amount);

        UpdateInventory();
        NotifyInventoryChanged();
    }

    private void RemoveInventoryItem(ItemData item, int amount = 1)
    {
        if (item.itemType == ItemType.Equipment)
        {
            for (int i = inventoryItems.Count - 1; i >= 0 && amount > 0; i--)
            {
                InventoryItem equipmentItem = inventoryItems[i];
                if (equipmentItem.data == item)
                {
                    inventoryItems.RemoveAt(i);
                    amount--;
                }
            }

            return;
        }

        if (inventoryDict.TryGetValue(item, out InventoryItem inventoryItem))
        {
            if (inventoryItem.stack == amount)
            {
                inventoryItems.Remove(inventoryItem);
                inventoryDict.Remove(inventoryItem.data);
            }
            else
                inventoryItem.RemoveStack(amount);
        }
    }

    private void RemoveStashItem(ItemData item, int amount = 1)
    {
        if (stashDict.TryGetValue(item, out InventoryItem stashItem))
        {
            if (stashItem.stack == amount)
            {
                stashItems.Remove(stashItem);
                stashDict.Remove(stashItem.data);
            }
            else
                stashItem.RemoveStack(amount);
        }
    }

    public bool Craft(EquipmentItemData _itemToCraft)
    {
        List<InventoryItem> requiredMaterials = _itemToCraft.requiredMaterials;
        foreach (InventoryItem item in requiredMaterials)
        {
            if (stashDict.TryGetValue(item.data, out InventoryItem inventoryItem))
            {
                if (inventoryItem.stack < item.stack)
                    return false;
            }
            else return false;
        }

        foreach (InventoryItem item in requiredMaterials)
        {
            if (stashDict.TryGetValue(item.data, out InventoryItem inventoryItem))
            {
                if (inventoryItem.stack < item.stack)
                    return false;

                RemoveItem(inventoryItem.data, item.stack);
            }
            else return false;
        }

        AddItem(_itemToCraft);

        Debug.Log("Craft successfully!");

        return true;
    }

    public List<InventoryItem> GetEquipmentItems() => equippedItems;
    public List<InventoryItem> GetStashItems() => stashItems;
    public List<InventoryItem> GetInventoryItems() => inventoryItems;

    public int GetItemAmount(ItemData item)
    {
        if (item == null)
            return 0;

        if (item.itemType == ItemType.Material)
            return stashDict.TryGetValue(item, out InventoryItem stashItem) ? stashItem.stack : 0;

        int amount = 0;
        foreach (InventoryItem inventoryItem in inventoryItems)
        {
            if (inventoryItem != null && inventoryItem.data == item)
                amount += Mathf.Max(1, inventoryItem.stack);
        }

        return amount;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return item != null && amount > 0 && GetItemAmount(item) >= amount;
    }

    public bool TryConsumeItem(ItemData item, int amount = 1)
    {
        if (!HasItem(item, amount))
            return false;

        RemoveItem(item, amount);
        return true;
    }

    public bool TryGetItemData(string itemId, out ItemData itemData)
    {
        if (assetDict == null || assetDict.Count == 0)
            BuildItemDatabase();

        return assetDict.TryGetValue(itemId, out itemData);
    }

    public bool TryGetNFTItemData(string contractAddress, string tokenId, NFTItemStandard standard, out ItemData itemData)
    {
        itemData = null;

        if (string.IsNullOrWhiteSpace(contractAddress) || string.IsNullOrWhiteSpace(tokenId))
            return false;

        if (assetDict == null || assetDict.Count == 0)
            BuildItemDatabase();

        foreach (ItemData item in assetDict.Values)
        {
            if (item == null || !item.canConvertToNFT)
                continue;

            NFTItemStandard itemStandard = item.nftStandard == NFTItemStandard.Auto
                ? (item.itemType == ItemType.Material ? NFTItemStandard.ERC1155 : NFTItemStandard.ERC721)
                : item.nftStandard;

            if (itemStandard != standard)
                continue;

            if (!string.Equals(item.nftContractAddress, contractAddress, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(item.nftTokenId, tokenId, StringComparison.Ordinal))
                continue;

            itemData = item;
            return true;
        }

        return false;
    }

    public EquipmentItemData GetEquipmentByType(EquipmentType _type)
    {
        foreach (InventoryItem item in equippedItems)
        {
            if (((EquipmentItemData)item.data).equipmentType == _type)
                return item.data as EquipmentItemData;
        }

        return null;
    }

    public bool TryEnhanceItem(InventoryItem item, int levelIncrease = 1)
    {
        if (item == null || !item.IsEquipment() || levelIncrease <= 0)
            return false;

        bool enhanced = false;
        for (int i = 0; i < levelIncrease; i++)
        {
            EnhancementAttemptResult result = TryEnhanceItemOnce(item);
            if (result.status != EnhancementAttemptStatus.Success)
                return enhanced;

            enhanced = true;
        }

        return enhanced;
    }

    public EnhancementAttemptResult TryEnhanceItemOnce(InventoryItem item)
    {
        EnhancementAttemptResult result = new()
        {
            attempted = false,
            status = EnhancementAttemptStatus.NotAttempted,
            previousLevel = item != null ? item.enhanceLevel : 0,
            currentLevel = item != null ? item.enhanceLevel : 0
        };

        if (item == null || !item.IsEquipment())
        {
            result.message = "Select an equipment item.";
            return result;
        }

        EquipmentItemData equipmentData = item.data as EquipmentItemData;
        int targetLevel = item.enhanceLevel + 1;
        EnhancementLevelData levelData = equipmentData.GetEnhancementLevelData(targetLevel);

        if (levelData == null)
        {
            result.message = $"No enhancement config for +{targetLevel}.";
            return result;
        }

        if (!levelData.HasValidOutcomeChanceTotal())
        {
            Debug.LogError($"Cannot enhance {item.data.itemName} to +{targetLevel}. Enhancement outcome chances must total exactly 100.");
            result.message = $"Enhancement config for +{targetLevel} is invalid.";
            return result;
        }

        if (!HasMaterials(levelData.requiredMaterials))
        {
            result.message = "Not enough materials.";
            return result;
        }

        if (PlayerManager.instance != null && !PlayerManager.instance.SpendCurrency(levelData.currencyCost))
        {
            result.message = "Not enough currency.";
            return result;
        }

        ConsumeMaterials(levelData.requiredMaterials);

        bool isEquipped = equippedItems.Contains(item);

        if (isEquipped)
            equipmentData.RemoveModifier(item.enhanceLevel);

        EnhancementOutcomeResult outcome = levelData.RollOutcome();
        ApplyEnhancementOutcome(item, targetLevel, outcome);

        if (isEquipped)
            equipmentData.AddModifier(item.enhanceLevel);

        result.attempted = true;
        result.currentLevel = item.enhanceLevel;
        result.status = GetEnhancementAttemptStatus(outcome);
        result.message = GetEnhancementAttemptMessage(result.status, result.previousLevel, result.currentLevel);

        UpdateInventory();
        NotifyInventoryChanged();
        return result;
    }

    private bool TryEnhanceOneLevel(InventoryItem item)
    {
        return TryEnhanceItemOnce(item).status == EnhancementAttemptStatus.Success;
    }

    private bool HasMaterials(List<InventoryItem> requiredMaterials)
    {
        foreach (InventoryItem requiredItem in requiredMaterials)
        {
            if (requiredItem == null || requiredItem.data == null)
                continue;

            if (!stashDict.TryGetValue(requiredItem.data, out InventoryItem ownedItem))
                return false;

            if (ownedItem.stack < requiredItem.stack)
                return false;
        }

        return true;
    }

    private void ConsumeMaterials(List<InventoryItem> requiredMaterials)
    {
        foreach (InventoryItem requiredItem in requiredMaterials)
        {
            if (requiredItem == null || requiredItem.data == null)
                continue;

            RemoveStashItem(requiredItem.data, requiredItem.stack);
        }
    }

    private void ApplyEnhancementOutcome(InventoryItem item, int targetLevel, EnhancementOutcomeResult outcome)
    {
        if (outcome == null)
            return;

        switch (outcome.outcomeType)
        {
            case EnhancementOutcomeType.Success:
                item.enhanceLevel = targetLevel;
                break;
            case EnhancementOutcomeType.Downgrade:
                item.enhanceLevel = Mathf.Max(0, item.enhanceLevel - outcome.downgradeLevels);
                break;
            case EnhancementOutcomeType.Reset:
                item.enhanceLevel = 0;
                break;
            case EnhancementOutcomeType.NoChange:
                break;
        }
    }

    private EnhancementAttemptStatus GetEnhancementAttemptStatus(EnhancementOutcomeResult outcome)
    {
        if (outcome == null)
            return EnhancementAttemptStatus.NoChange;

        switch (outcome.outcomeType)
        {
            case EnhancementOutcomeType.Success:
                return EnhancementAttemptStatus.Success;
            case EnhancementOutcomeType.Downgrade:
                return EnhancementAttemptStatus.Downgrade;
            case EnhancementOutcomeType.Reset:
                return EnhancementAttemptStatus.Reset;
            default:
                return EnhancementAttemptStatus.NoChange;
        }
    }

    private string GetEnhancementAttemptMessage(EnhancementAttemptStatus status, int previousLevel, int currentLevel)
    {
        switch (status)
        {
            case EnhancementAttemptStatus.Success:
                return $"Success: +{previousLevel} -> +{currentLevel}";
            case EnhancementAttemptStatus.Downgrade:
                return $"Downgrade: +{previousLevel} -> +{currentLevel}";
            case EnhancementAttemptStatus.Reset:
                return $"Reset: +{previousLevel} -> +0";
            case EnhancementAttemptStatus.NoChange:
                return $"No Change: still +{currentLevel}";
            default:
                return "Enhancement did not start.";
        }
    }

    public void TryUseFlask()
    {
        foreach (KeyValuePair<EquipmentItemData, InventoryItem> item in equippedDict)
        {
            if (item.Key.equipmentType == EquipmentType.Flask)
            {
                if (Time.time > (lastTimeUsedFlask + flaskCooldown))
                {
                    flaskCooldown = item.Key.itemCooldown;
                    lastTimeUsedFlask = Time.time;
                    OnFlaskCooldownUpdated?.Invoke(flaskCooldown);
                    item.Key.ExecuteEffects(null);
                }
            }
        }
    }

    public void TryUseArmor()
    {
        foreach (KeyValuePair<EquipmentItemData, InventoryItem> item in equippedDict)
        {
            if (item.Key.equipmentType == EquipmentType.Armor)
            {
                if (Time.time > (lastTimeUsedArmor + armorCooldown))
                {
                    armorCooldown = item.Key.itemCooldown;
                    lastTimeUsedArmor = Time.time;
                    item.Key.ExecuteEffects(PlayerManager.instance.player.transform);
                }
            }
        }
    }

    public bool IsInventoryFull()
    {
        return inventoryItems.Count >= inventorySlots.Length;
    }

    public void SaveData(ref GameData gameData)
    {
        gameData.inventory ??= new();
        gameData.stash ??= new();
        gameData.equipments ??= new();
        gameData.inventoryEquipments ??= new();
        gameData.equippedEquipments ??= new();

        gameData.inventory.Clear();
        gameData.inventoryEquipments.Clear();
        foreach (InventoryItem item in inventoryItems)
            gameData.inventoryEquipments.Add(CreateEquipmentSaveData(item));

        gameData.stash.Clear();
        foreach (InventoryItem item in stashItems)
            gameData.stash.Add(item.data.itemID, item.stack);

        gameData.equipments.Clear();
        gameData.equippedEquipments.Clear();
        foreach (InventoryItem item in equippedItems)
        {
            gameData.equipments.Add(item.data.itemID);
            gameData.equippedEquipments.Add(CreateEquipmentSaveData(item));
        }

        Debug.Log(
            $"Inventory saved. Inventory: {inventoryItems.Count}, Stash: {stashItems.Count}, Equipped: {equippedItems.Count}"
        );
    }

    public void LoadData(GameData gameData)
    {
        BuildItemDatabase();

        loadedItems.Clear();
        loadedEquipItems.Clear();

        if (gameData.inventoryEquipments != null && gameData.inventoryEquipments.Count > 0)
            LoadEquipmentItems(gameData.inventoryEquipments, loadedItems);
        else if (gameData.inventory != null)
            LoadItemsFromSavedDictionary(gameData.inventory, true);

        if (gameData.stash != null)
            LoadItemsFromSavedDictionary(gameData.stash, false);

        if (gameData.equippedEquipments != null && gameData.equippedEquipments.Count > 0)
        {
            LoadEquipmentItems(gameData.equippedEquipments, loadedEquipItems);
        }
        else foreach (string id in gameData.equipments)
        {
            if (!assetDict.ContainsKey(id))
            {
                Debug.LogError("Invalid Item ID: " + id);
                return;
            }

            loadedEquipItems.Add(new InventoryItem(assetDict[id]));
        }

        if (!hasStarted)
            return;

        suppressInventoryChanged = true;

        try
        {
            ClearRuntimeInventory();

            foreach (InventoryItem item in loadedItems)
                AddItem(item);

            foreach (InventoryItem item in loadedEquipItems)
                EquipItem(item);

            UpdateInventory();
        }
        finally
        {
            suppressInventoryChanged = false;
        }
    }

    private void LoadItemsFromSavedDictionary(SerializableDictionary<string, int> savedItems, bool isInventory)
    {
        foreach (KeyValuePair<String, int> item in savedItems)
        {
            string itemId = GetItemIdFromSavedKey(item.Key);
            if (!assetDict.ContainsKey(itemId))
            {
                Debug.LogError("Invalid Item ID: " + itemId);
                continue;
            }

            ItemData itemData = assetDict[itemId];
            int stack = item.Value;

            if (isInventory && itemData.itemType == ItemType.Equipment)
            {
                for (int i = 0; i < stack; i++)
                    loadedItems.Add(new(itemData) { stack = 1 });
            }
            else
            {
                loadedItems.Add(new(itemData) { stack = stack });
            }
        }
    }

    private void ClearRuntimeInventory()
    {
        foreach (InventoryItem item in equippedItems)
        {
            EquipmentItemData equipmentData = item.data as EquipmentItemData;
            if (equipmentData != null)
                equipmentData.RemoveModifier(item.enhanceLevel);
        }

        inventoryItems.Clear();
        inventoryDict.Clear();
        stashItems.Clear();
        stashDict.Clear();
        equippedItems.Clear();
        equippedDict.Clear();
    }

    private void BuildItemDatabase()
    {
        assetDict = new Dictionary<string, ItemData>();

        foreach (ItemData item in Resources.LoadAll<ItemData>("Data/Items"))
            RegisterItem(item);

        foreach (StartedItem item in startedPack)
            RegisterItem(item.item);
    }

    private void RegisterItem(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID))
            return;

        if (!assetDict.ContainsKey(item.itemID))
            assetDict.Add(item.itemID, item);
    }

    private string GetItemIdFromSavedKey(string savedKey)
    {
        int separatorIndex = savedKey.LastIndexOf("#", StringComparison.Ordinal);
        if (separatorIndex < 0)
            return savedKey;

        return savedKey[..separatorIndex];
    }

    private EquipmentSaveData CreateEquipmentSaveData(InventoryItem item)
    {
        item.EnsureInstanceId();

        return new EquipmentSaveData
        {
            instanceId = item.instanceId,
            itemID = item.data.itemID,
            enhanceLevel = item.enhanceLevel
        };
    }

    private void LoadEquipmentItems(List<EquipmentSaveData> savedItems, List<InventoryItem> target)
    {
        foreach (EquipmentSaveData savedItem in savedItems)
        {
            if (savedItem == null || !assetDict.ContainsKey(savedItem.itemID))
            {
                Debug.LogError("Invalid Item ID: " + savedItem?.itemID);
                continue;
            }

            target.Add(new InventoryItem(assetDict[savedItem.itemID])
            {
                instanceId = savedItem.instanceId,
                stack = 1,
                enhanceLevel = savedItem.enhanceLevel
            });
        }
    }

    private void NotifyInventoryChanged()
    {
        if (!hasStarted || suppressInventoryChanged)
            return;

        OnInventoryChanged?.Invoke();
    }
}
