using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    public static event Action OnGameDataLoadStateChanged;
    public static event Action OnGameDataSaved;
    public static event Action OnGameDataDeleted;
    [SerializeField] private float autoSaveDelay = .5f;
    private GameData gameData;
    private List<IGameData> gameDataList;
    private bool hasLoadedGameData;
    private bool hasExistingGameData;
    private Coroutine autoSaveRoutine;
    private Inventory subscribedInventory;

    // Singleton Pattern
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(instance.gameObject);
    }

    private void Start()
    {
        gameDataList = FindAllData();
        SubscribeToInventoryChanges();
        LoadGame();
    }

    private void OnDestroy()
    {
        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= RequestAutoSave;
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private List<IGameData> FindAllData()
    {
        IEnumerable<IGameData> data = FindObjectsOfType<MonoBehaviour>(true).OfType<IGameData>();
        return new List<IGameData>(data);
    }

    public void Delete()
    {
        ItemObject.ClearSessionCollectedWorldItems();
        FirebaseWebGLBridge.DeleteGameData(gameObject.name);
    }

    public bool HasGameData()
    {
        return hasExistingGameData;
    }

    public void NewGame()
    {
        gameData = new GameData();
    }

    public void CreateNewGameSave()
    {
        ItemObject.ClearSessionCollectedWorldItems();
        NewGame();
        hasLoadedGameData = true;
        hasExistingGameData = false;
        SaveGame(true);
    }

    public void LoadGame()
    {
        Debug.Log("Load Game");

        if (!FirebaseWebGLBridge.HasCurrentUser)
        {
            Debug.LogWarning("Waiting for wallet login before loading Firebase game data.");
            return;
        }

        hasLoadedGameData = false;
        hasExistingGameData = false;
        OnGameDataLoadStateChanged?.Invoke();
        FirebaseWebGLBridge.LoadGameData(gameObject.name);
    }

    public void SaveGame()
    {
        SaveGame(false);
    }

    private void SaveGame(bool allowEmptySceneData)
    {
        Debug.Log("Saved game data");
        gameDataList = FindAllData();
        Debug.Log($"Saving {gameDataList.Count} game data components: {string.Join(", ", gameDataList.Select(data => data.GetType().Name))}");

        if (!FirebaseWebGLBridge.HasCurrentUser)
        {
            Debug.LogWarning("Firebase save skipped because wallet login is not ready.");
            return;
        }

        if (!allowEmptySceneData && (gameDataList == null || gameDataList.Count == 0))
        {
            Debug.Log("Firebase save skipped because this scene has no game data components.");
            return;
        }

        if (gameData == null)
            NewGame();

        gameData.EnsureInitialized();

        foreach (IGameData data in gameDataList)
            data.SaveData(ref gameData);

        string jsonData = JsonUtility.ToJson(gameData);
        FirebaseWebGLBridge.SaveGameData(jsonData, gameObject.name);
    }

    private void SubscribeToInventoryChanges()
    {
        subscribedInventory = Inventory.instance != null
            ? Inventory.instance
            : FindObjectOfType<Inventory>(true);

        if (subscribedInventory == null)
            return;

        subscribedInventory.OnInventoryChanged += RequestAutoSave;
    }

    private void RequestAutoSave()
    {
        if (!hasLoadedGameData)
        {
            Debug.LogWarning("Firebase autosave skipped because game data has not loaded yet.");
            return;
        }

        if (autoSaveRoutine != null)
            StopCoroutine(autoSaveRoutine);

        autoSaveRoutine = StartCoroutine(AutoSaveAfterDelay());
    }

    private IEnumerator AutoSaveAfterDelay()
    {
        yield return new WaitForSeconds(autoSaveDelay);
        autoSaveRoutine = null;
        SaveGame();
    }

    public void OnFirebaseGameDataLoaded(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Firebase save data is empty. Waiting for New Game.");
            OnFirebaseGameDataMissing(FirebaseWebGLBridge.CurrentUserId);
            return;
        }

        gameData = JsonUtility.FromJson<GameData>(json);
        if (gameData == null)
        {
            Debug.LogWarning("Firebase save data could not be parsed. Waiting for New Game.");
            OnFirebaseGameDataMissing(FirebaseWebGLBridge.CurrentUserId);
            return;
        }

        gameData.EnsureInitialized();
        hasLoadedGameData = true;
        hasExistingGameData = true;
        ApplyGameData();
        OnGameDataLoadStateChanged?.Invoke();
        Debug.Log("Firebase game data loaded.");
    }

    public void OnFirebaseGameDataMissing(string userId)
    {
        Debug.Log($"No Firebase save found for {userId}. Waiting for New Game.");
        NewGame();
        hasLoadedGameData = false;
        hasExistingGameData = false;
        OnGameDataLoadStateChanged?.Invoke();
    }

    public void OnFirebaseGameDataSaved(string userId)
    {
        Debug.Log($"Firebase game data saved for {userId}.");
        hasExistingGameData = true;
        OnGameDataLoadStateChanged?.Invoke();
        OnGameDataSaved?.Invoke();
    }

    public void OnFirebaseGameDataDeleted(string userId)
    {
        Debug.Log($"Firebase game data deleted for {userId}.");
        NewGame();
        hasLoadedGameData = false;
        hasExistingGameData = false;
        OnGameDataLoadStateChanged?.Invoke();
        OnGameDataDeleted?.Invoke();
    }

    public void OnFirebaseGameDataError(string error)
    {
        Debug.LogError($"Firebase game data error: {error}");
    }

    private void ApplyGameData()
    {
        foreach (IGameData data in gameDataList)
            data.LoadData(gameData);
    }
}
