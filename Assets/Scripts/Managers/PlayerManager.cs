using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour, IGameData
{
    public static PlayerManager instance;
    public Player player;
    public Action OnCurrencyUpdated;
    public bool isInMenu = false;

    public int currency;
    public void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(instance.gameObject);
    }

    public void LoadData(GameData gameData)
    {
        currency = gameData.currency;
        OnCurrencyUpdated?.Invoke();
    }

    public void SaveData(ref GameData gameData)
    {
        gameData.currency = currency;
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
            return;

        currency += amount;
        OnCurrencyUpdated?.Invoke();
    }

    public bool SpendCurrency(int _price)
    {
        if (currency < _price)
            return false;

        currency -= _price;
        OnCurrencyUpdated?.Invoke();
        return true;
    }
}
