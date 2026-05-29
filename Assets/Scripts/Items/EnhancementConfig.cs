using System.Collections.Generic;
using UnityEngine;

public enum EnhancementOutcomeType
{
    Success,
    Downgrade,
    Reset,
    NoChange
}

public class EnhancementOutcomeResult
{
    public EnhancementOutcomeType outcomeType;
    public int downgradeLevels;

    public EnhancementOutcomeResult(EnhancementOutcomeType outcomeType, int downgradeLevels = 1)
    {
        this.outcomeType = outcomeType;
        this.downgradeLevels = downgradeLevels;
    }
}

[System.Serializable]
public class EnhancementLevelData
{
    private const float RequiredTotalChance = 100f;
    private const float ChanceTolerance = .001f;

    [Min(1)] public int level = 1;
    [Min(0)] public float statMultiplier = 1f;
    [Min(0)] public int currencyCost;
    public List<InventoryItem> requiredMaterials = new();

    [Header("Outcome Chances")]
    [Range(0, 100)] public float successChance = 100;
    [Range(0, 100)] public float downgradeChance;
    [Range(0, 100)] public float resetChance;
    [Range(0, 100)] public float noChangeChance;
    [Min(1)] public int downgradeLevels = 1;

    public EnhancementOutcomeResult RollOutcome()
    {
        float totalChance = GetTotalOutcomeChance();

        if (!HasValidOutcomeChanceTotal())
        {
            Debug.LogError($"Enhancement level {level} outcomes must total exactly {RequiredTotalChance}. Current total: {totalChance}");
            return new EnhancementOutcomeResult(EnhancementOutcomeType.NoChange);
        }

        float roll = Random.Range(0, RequiredTotalChance);
        float currentChance = successChance;
        if (roll < currentChance)
            return new EnhancementOutcomeResult(EnhancementOutcomeType.Success);

        currentChance += downgradeChance;
        if (roll < currentChance)
            return new EnhancementOutcomeResult(EnhancementOutcomeType.Downgrade, downgradeLevels);

        currentChance += resetChance;
        if (roll < currentChance)
            return new EnhancementOutcomeResult(EnhancementOutcomeType.Reset);

        return new EnhancementOutcomeResult(EnhancementOutcomeType.NoChange);
    }

    public float GetTotalOutcomeChance()
    {
        return successChance + downgradeChance + resetChance + noChangeChance;
    }

    public bool HasValidOutcomeChanceTotal()
    {
        return Mathf.Abs(GetTotalOutcomeChance() - RequiredTotalChance) <= ChanceTolerance;
    }
}

[CreateAssetMenu(fileName = "Enhancement Config", menuName = "Data/Enhancement Config")]
public class EnhancementConfig : ScriptableObject
{
    [SerializeField] private List<EnhancementLevelData> levels = new();

    public EnhancementLevelData GetLevelData(int targetLevel)
    {
        return levels.Find(level => level.level == targetLevel);
    }

    public float GetStatMultiplier(int enhanceLevel)
    {
        if (enhanceLevel <= 0)
            return 1f;

        EnhancementLevelData levelData = GetLevelData(enhanceLevel);
        return levelData != null ? levelData.statMultiplier : 1f;
    }

    public bool CanEnhanceTo(int targetLevel)
    {
        EnhancementLevelData levelData = GetLevelData(targetLevel);
        return levelData != null && levelData.HasValidOutcomeChanceTotal();
    }

    private void OnValidate()
    {
        foreach (EnhancementLevelData levelData in levels)
        {
            if (levelData == null || levelData.HasValidOutcomeChanceTotal())
                continue;

            Debug.LogError($"Enhancement Config '{name}' level {levelData.level} outcomes must total exactly 100. Current total: {levelData.GetTotalOutcomeChance()}", this);
        }
    }
}
