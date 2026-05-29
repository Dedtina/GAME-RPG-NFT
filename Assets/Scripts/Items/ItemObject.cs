using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour, IGameData
{
    private static readonly HashSet<string> collectedWorldItemsThisSession = new();

    [SerializeField] private ItemData itemData;
    [SerializeField] private Rigidbody2D rb;

    [Header("Persistence")]
    [SerializeField] private bool isOneTimePickup;
    [SerializeField] private string persistentId;

    private bool wasPickedUp;

    private void Awake()
    {
        if (IsCollectedThisSession())
            HideCollectedItem();
    }

    private void OnValidate()
    {
        if (itemData == null)
            return;

        GetComponent<SpriteRenderer>().sprite = itemData.icon;
        gameObject.name = "Item Object - " + itemData.name;

#if UNITY_EDITOR
        if (isOneTimePickup && string.IsNullOrEmpty(persistentId))
            persistentId = Guid.NewGuid().ToString("N");
#endif
    }

    public void SetupItem(ItemData _itemData, Vector2 _dropVelocity)
    {
        itemData = _itemData;
        rb.velocity = _dropVelocity;
        OnValidate();
    }

    public void PickUpItem()
    {
        if (wasPickedUp)
            return;

        if (Inventory.instance.IsInventoryFull())
        {
            Debug.Log("The inventory is full");
            rb.velocity = new Vector2(0, 5);
            return;
        }

        Inventory.instance.AddItem(itemData);

        if (CanPersistCollectionState())
        {
            wasPickedUp = true;
            collectedWorldItemsThisSession.Add(persistentId);
            HideCollectedItem();
        }
        else
            Destroy(gameObject);
    }

    public void SaveData(ref GameData gameData)
    {
        if (!CanPersistCollectionState())
            return;

        gameData.collectedWorldItems ??= new();

        if (gameData.collectedWorldItems.ContainsKey(persistentId))
            gameData.collectedWorldItems[persistentId] = wasPickedUp;
        else
            gameData.collectedWorldItems.Add(persistentId, wasPickedUp);
    }

    public void LoadData(GameData gameData)
    {
        if (!CanPersistCollectionState())
            return;

        gameData.collectedWorldItems ??= new();
        bool wasSavedAsCollected = gameData.collectedWorldItems.TryGetValue(persistentId, out bool collected) && collected;
        wasPickedUp = wasSavedAsCollected || IsCollectedThisSession();

        if (wasPickedUp)
        {
            collectedWorldItemsThisSession.Add(persistentId);
            HideCollectedItem();
        }
    }

    public static void ClearSessionCollectedWorldItems()
    {
        collectedWorldItemsThisSession.Clear();
    }

    private bool CanPersistCollectionState()
    {
        if (!isOneTimePickup)
            return false;

        if (!string.IsNullOrEmpty(persistentId))
            return true;

        Debug.LogWarning($"{name} is a one-time pickup without a persistent ID.", this);
        return false;
    }

    private bool IsCollectedThisSession()
    {
        return isOneTimePickup &&
               !string.IsNullOrEmpty(persistentId) &&
               collectedWorldItemsThisSession.Contains(persistentId);
    }

    private void HideCollectedItem()
    {
        wasPickedUp = true;
        gameObject.SetActive(false);
    }
}
