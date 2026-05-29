using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SummonEnemyOption
{
    public string displayName;
    public GameObject enemyPrefab;

    [Header("Summon Limit")]
    [Tooltip("Set to 0 to use the NPC default maximum.")]
    [Min(0)] public int maxQuantity;

    [Header("Summon Cost")]
    public ItemData requiredItem;
    [Min(1)] public int requiredItemAmount = 1;
    public bool consumeCostPerEnemy = true;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName))
            return displayName;

        return enemyPrefab != null ? enemyPrefab.name : "Missing Enemy Prefab";
    }

    public int GetRequiredItemAmount(int enemyQuantity)
    {
        if (requiredItem == null)
            return 0;

        int costMultiplier = consumeCostPerEnemy ? Mathf.Max(1, enemyQuantity) : 1;
        return Mathf.Max(1, requiredItemAmount) * costMultiplier;
    }

    public int GetMaxQuantity(int npcMaxQuantity)
    {
        int defaultMaximum = Mathf.Max(1, npcMaxQuantity);
        return maxQuantity > 0 ? Mathf.Min(maxQuantity, defaultMaximum) : defaultMaximum;
    }
}

[RequireComponent(typeof(Collider2D))]
public class NPCEnemySummoner : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool summonOnlyOnce = true;
    [SerializeField] private bool hideUntilMapIsCleared = true;

    [Header("Enemies")]
    [SerializeField] private SummonEnemyOption[] enemyOptions;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(1)] private int maxEnemiesPerSummon = 10;
    [SerializeField, Min(0)] private float repeatedSpawnSpacing = .75f;

    private UI uiRoot;
    private Collider2D interactionCollider;
    private Renderer[] npcRenderers;
    private bool isPlayerNearby;
    private bool hasSummoned;
    private bool isWaitingForMapClear;

    public IReadOnlyList<SummonEnemyOption> EnemyOptions => enemyOptions;
    public int MaxEnemiesPerSummon => Mathf.Max(1, maxEnemiesPerSummon);

    private bool CanSummon => !isWaitingForMapClear && (!summonOnlyOnce || !hasSummoned);

    private void Awake()
    {
        uiRoot = UI.instance != null ? UI.instance : FindObjectOfType<UI>();
        interactionCollider = GetComponent<Collider2D>();
        npcRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        if (isWaitingForMapClear)
        {
            if (!HasLivingEnemiesOnMap())
                ShowAfterMapCleared();

            return;
        }

        if (!isPlayerNearby || !CanSummon)
            return;

        if (PlayerManager.instance != null && PlayerManager.instance.isInMenu)
            return;

        if (Input.GetKeyDown(interactKey))
            OpenSummonerPanel();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponentInParent<Player>();
        if (player == null || player.statCtrl.IsDeath() || !CanSummon)
            return;

        isPlayerNearby = true;
        uiRoot?.SetNpcInteractionPrompt(true, NpcFeatureType.Summoner, interactKey);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
            return;

        isPlayerNearby = false;
        uiRoot?.SetNpcInteractionPrompt(false, NpcFeatureType.Summoner, interactKey);
        uiRoot?.CloseNpcFeature(NpcFeatureType.Summoner);
    }

    private void OnDisable()
    {
        if (isPlayerNearby)
            uiRoot?.SetNpcInteractionPrompt(false, NpcFeatureType.Summoner, interactKey);
    }

    public string GetSummonCostDescription(int enemyOptionIndex, int quantity)
    {
        if (!TryGetSummonRequirement(enemyOptionIndex, quantity, out ItemData requiredItem, out int requiredAmount, out int ownedAmount))
            return "Cost: Free";

        return $"Cost: {requiredAmount} {requiredItem.itemName} (Owned: {ownedAmount})";
    }

    public bool TryGetSummonRequirement(int enemyOptionIndex, int quantity, out ItemData requiredItem, out int requiredAmount, out int ownedAmount)
    {
        requiredItem = null;
        requiredAmount = 0;
        ownedAmount = 0;

        if (!TryGetEnemyOption(enemyOptionIndex, out SummonEnemyOption option) || option.requiredItem == null)
            return false;

        requiredItem = option.requiredItem;
        requiredAmount = option.GetRequiredItemAmount(quantity);
        ownedAmount = Inventory.instance != null ? Inventory.instance.GetItemAmount(requiredItem) : 0;
        return true;
    }

    public int GetMaxQuantityForOption(int enemyOptionIndex)
    {
        if (!TryGetEnemyOption(enemyOptionIndex, out SummonEnemyOption option))
            return 1;

        return option.GetMaxQuantity(MaxEnemiesPerSummon);
    }

    public bool TrySummonEnemies(int enemyOptionIndex, int quantity, out string failureMessage)
    {
        failureMessage = "";

        if (!CanSummon)
        {
            failureMessage = "This NPC cannot summon again.";
            return false;
        }

        if (!TryGetEnemyOption(enemyOptionIndex, out SummonEnemyOption option))
        {
            failureMessage = "Selected enemy is not configured.";
            return false;
        }

        int maximumQuantity = option.GetMaxQuantity(MaxEnemiesPerSummon);
        if (quantity < 1 || quantity > maximumQuantity)
        {
            failureMessage = $"Amount must be between 1 and {maximumQuantity}.";
            Debug.LogWarning($"{name} cannot summon {quantity} enemies of type {option.GetDisplayName()}. Valid range: 1-{maximumQuantity}.", this);
            return false;
        }

        if (!TryConsumeSummonCost(option, quantity, out failureMessage))
            return false;

        for (int i = 0; i < quantity; i++)
        {
            Transform spawnPoint = GetSpawnPoint(i);
            Vector3 spawnPosition = spawnPoint.position + Vector3.right * repeatedSpawnSpacing * GetRepeatedSpawnIndex(i);
            Instantiate(option.enemyPrefab, spawnPosition, spawnPoint.rotation);
        }

        hasSummoned = true;

        if (hideUntilMapIsCleared)
            HideUntilMapCleared();

        return true;
    }

    public void HandleSummonerPanelClosed()
    {
        if (isPlayerNearby && CanSummon)
            uiRoot?.SetNpcInteractionPrompt(true, NpcFeatureType.Summoner, interactKey);
    }

    private void OpenSummonerPanel()
    {
        if (uiRoot == null)
            return;

        if (uiRoot.OpenSummonerPanel(this))
            uiRoot.SetNpcInteractionPrompt(false, NpcFeatureType.Summoner, interactKey);
    }

    private bool TryGetEnemyOption(int enemyOptionIndex, out SummonEnemyOption option)
    {
        option = null;

        if (enemyOptions == null || enemyOptionIndex < 0 || enemyOptionIndex >= enemyOptions.Length)
            return false;

        option = enemyOptions[enemyOptionIndex];
        if (option == null || option.enemyPrefab == null)
        {
            Debug.LogWarning($"{name} has an invalid enemy option at index {enemyOptionIndex}.", this);
            return false;
        }

        return true;
    }

    private bool TryConsumeSummonCost(SummonEnemyOption option, int quantity, out string failureMessage)
    {
        failureMessage = "";

        if (option.requiredItem == null)
            return true;

        if (Inventory.instance == null)
        {
            failureMessage = "Inventory is not available.";
            return false;
        }

        int requiredAmount = option.GetRequiredItemAmount(quantity);
        if (!Inventory.instance.TryConsumeItem(option.requiredItem, requiredAmount))
        {
            failureMessage = $"Requires {requiredAmount} {option.requiredItem.itemName}.";
            return false;
        }

        return true;
    }

    private Transform GetSpawnPoint(int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform;

        Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];
        return spawnPoint != null ? spawnPoint : transform;
    }

    private int GetRepeatedSpawnIndex(int spawnIndex)
    {
        int spawnPointCount = spawnPoints == null || spawnPoints.Length == 0 ? 1 : spawnPoints.Length;
        return spawnIndex / spawnPointCount;
    }

    private void HideUntilMapCleared()
    {
        isWaitingForMapClear = true;
        isPlayerNearby = false;
        uiRoot?.SetNpcInteractionPrompt(false, NpcFeatureType.Summoner, interactKey);

        if (interactionCollider != null)
            interactionCollider.enabled = false;

        SetRenderersVisible(false);
    }

    private void ShowAfterMapCleared()
    {
        isWaitingForMapClear = false;
        hasSummoned = false;

        if (interactionCollider != null)
            interactionCollider.enabled = true;

        SetRenderersVisible(true);
    }

    private bool HasLivingEnemiesOnMap()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && enemy.statCtrl != null && !enemy.statCtrl.IsDeath())
                return true;
        }

        return false;
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer npcRenderer in npcRenderers)
        {
            if (npcRenderer != null)
                npcRenderer.enabled = visible;
        }
    }

    private void OnValidate()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;

        maxEnemiesPerSummon = Mathf.Max(1, maxEnemiesPerSummon);
        repeatedSpawnSpacing = Mathf.Max(0, repeatedSpawnSpacing);
    }
}
