using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    Weapon,
    Armor,
    Amulet,
    Flask
}

[CreateAssetMenu(fileName = "Create New Item", menuName = "Data/Equipment")]
public class EquipmentItemData : ItemData
{
    public EquipmentType equipmentType;
    public float itemCooldown;

    [Header("Enhancement")]
    public EnhancementConfig enhancementConfig;

    [Header("Item Effects")]
    public ItemEffect[] itemEffects;

    [Header("Major Stats")]
    public float strength;    // 1 point --> +1 damage; +1% crit.damage
    public float agility;     // 1 point --> +1 evasion; +1% crit.chance
    public float intelligent; // 1 point --> +1 magic damage; +3% magic resistance
    public float vitality;    // 1 point --> +3 or +5 heath points

    [Header("Defensive Stats")]
    public float maxHP;
    public float armor;
    public float evasion;

    [Header("Offensive Stats")]
    public float damage;
    public float critChance; // percent
    public float critPower; // e.g. 150 --> 150% * total_damage

    [Header("Magic Stats")]
    public float fireDamage;
    public float iceDamage;
    public float lightingDamage;

    [Header("Craft Materials")]
    public List<InventoryItem> requiredMaterials;

    private int line;

    public override void AddModifier()
    {
        AddModifier(0);
    }

    public void AddModifier(int enhanceLevel)
    {
        Player player = PlayerManager.instance.player;
        if (strength != 0)
            player.statCtrl.strength.AddModifier(GetEnhancedValue(strength, enhanceLevel));
        if (agility != 0)
            player.statCtrl.agility.AddModifier(GetEnhancedValue(agility, enhanceLevel));
        if (intelligent != 0)
            player.statCtrl.intelligent.AddModifier(GetEnhancedValue(intelligent, enhanceLevel));
        if (vitality != 0)
            player.statCtrl.vitality.AddModifier(GetEnhancedValue(vitality, enhanceLevel));
        if (maxHP != 0)
            player.statCtrl.maxHP.AddModifier(GetEnhancedValue(maxHP, enhanceLevel));
        if (armor != 0)
            player.statCtrl.armor.AddModifier(GetEnhancedValue(armor, enhanceLevel));
        if (evasion != 0)
            player.statCtrl.evasion.AddModifier(GetEnhancedValue(evasion, enhanceLevel));
        if (damage != 0)
            player.statCtrl.damage.AddModifier(GetEnhancedValue(damage, enhanceLevel));
        if (critChance != 0)
            player.statCtrl.critChance.AddModifier(GetEnhancedValue(critChance, enhanceLevel));
        if (critPower != 0)
            player.statCtrl.critPower.AddModifier(GetEnhancedValue(critPower, enhanceLevel));
        if (fireDamage != 0)
            player.statCtrl.fireDamage.AddModifier(GetEnhancedValue(fireDamage, enhanceLevel));
        if (iceDamage != 0)
            player.statCtrl.iceDamage.AddModifier(GetEnhancedValue(iceDamage, enhanceLevel));
        if (lightingDamage != 0)
            player.statCtrl.lightingDamage.AddModifier(GetEnhancedValue(lightingDamage, enhanceLevel));
    }

    public override void RemoveModifier()
    {
        RemoveModifier(0);
    }

    public void RemoveModifier(int enhanceLevel)
    {
        Player player = PlayerManager.instance.player;
        player.statCtrl.strength.RemoveModifier(GetEnhancedValue(strength, enhanceLevel));
        player.statCtrl.agility.RemoveModifier(GetEnhancedValue(agility, enhanceLevel));
        player.statCtrl.intelligent.RemoveModifier(GetEnhancedValue(intelligent, enhanceLevel));
        player.statCtrl.vitality.RemoveModifier(GetEnhancedValue(vitality, enhanceLevel));
        player.statCtrl.maxHP.RemoveModifier(GetEnhancedValue(maxHP, enhanceLevel));
        player.statCtrl.armor.RemoveModifier(GetEnhancedValue(armor, enhanceLevel));
        player.statCtrl.evasion.RemoveModifier(GetEnhancedValue(evasion, enhanceLevel));
        player.statCtrl.damage.RemoveModifier(GetEnhancedValue(damage, enhanceLevel));
        player.statCtrl.critChance.RemoveModifier(GetEnhancedValue(critChance, enhanceLevel));
        player.statCtrl.critPower.RemoveModifier(GetEnhancedValue(critPower, enhanceLevel));
        player.statCtrl.fireDamage.RemoveModifier(GetEnhancedValue(fireDamage, enhanceLevel));
        player.statCtrl.iceDamage.RemoveModifier(GetEnhancedValue(iceDamage, enhanceLevel));
        player.statCtrl.lightingDamage.RemoveModifier(GetEnhancedValue(lightingDamage, enhanceLevel));
    }

    private float GetEnhancedValue(float baseValue, int enhanceLevel)
    {
        if (baseValue == 0 || enhanceLevel <= 0)
            return baseValue;

        return baseValue * GetEnhancementMultiplier(enhanceLevel);
    }

    public bool CanEnhanceTo(int targetLevel)
    {
        return enhancementConfig != null && enhancementConfig.CanEnhanceTo(targetLevel);
    }

    public EnhancementLevelData GetEnhancementLevelData(int targetLevel)
    {
        if (enhancementConfig == null)
            return null;

        return enhancementConfig.GetLevelData(targetLevel);
    }

    private float GetEnhancementMultiplier(int enhanceLevel)
    {
        if (enhancementConfig == null)
            return 1f;

        return enhancementConfig.GetStatMultiplier(enhanceLevel);
    }
    public virtual void ExecuteEffects(Transform _enemyTrans)
    {
        foreach (ItemEffect ie in itemEffects)
        {
            ie.ExecuteEffect(_enemyTrans);
        }
    }

    // Item Description
    public override string GetDescription()
    {
        return GetDescription(0);
    }

    public string GetDescription(int enhanceLevel)
    {
        sb.Length = 0;
        line = 0;

        AddItemDescription(GetEnhancedValue(strength, enhanceLevel), "Strength");
        AddItemDescription(GetEnhancedValue(agility, enhanceLevel), "Agility");
        AddItemDescription(GetEnhancedValue(intelligent, enhanceLevel), "Intelligent");
        AddItemDescription(GetEnhancedValue(vitality, enhanceLevel), "Vitality");
        AddItemDescription(GetEnhancedValue(maxHP, enhanceLevel), "Health");
        AddItemDescription(GetEnhancedValue(armor, enhanceLevel), "Armor");
        AddItemDescription(GetEnhancedValue(evasion, enhanceLevel), "Evasion");
        AddItemDescription(GetEnhancedValue(damage, enhanceLevel), "Damage");
        AddItemDescription(GetEnhancedValue(critChance, enhanceLevel), "Crit. Chance");
        AddItemDescription(GetEnhancedValue(critPower, enhanceLevel), "Crit. Power");
        AddItemDescription(GetEnhancedValue(fireDamage, enhanceLevel), "Fire Damage");
        AddItemDescription(GetEnhancedValue(iceDamage, enhanceLevel), "Ice Damage");
        AddItemDescription(GetEnhancedValue(lightingDamage, enhanceLevel), "Lighting Damage");

        while (line < 5)
        {
            sb.AppendLine();
            line++;
        }

        return sb.ToString();

    }
    public void AddItemDescription(float val, string name)
    {
        if (val != 0)
        {
            if (sb.Length > 0)
                sb.AppendLine();

            if (val > 0) sb.Append(" + ");
            else sb.Append(" - ");
            float _val = val < 0 ? -val : val;
            sb.Append(_val + " " + name);
            line++;
        }
    }
}
