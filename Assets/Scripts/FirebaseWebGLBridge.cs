using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseWebGLBridge : MonoBehaviour
{
    private const string LoginSceneName = "LoginScene";
    private const string PostLoginSceneName = "MainMenu";

    public static FirebaseWebGLBridge Instance { get; private set; }
    public static string CurrentUserId { get; private set; }
    public static bool HasCurrentUser => !string.IsNullOrEmpty(CurrentUserId);

    public static void ClearCurrentUser()
    {
        CurrentUserId = null;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FirebaseCreateOrLoadUser(string walletAddress, string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void FirebaseLoadGameData(string userId, string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void FirebaseSaveGameData(string userId, string gameDataJson, string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void FirebaseDeleteGameData(string userId, string callbackObjectName);

    [DllImport("__Internal")]
    private static extern void FirebaseConvertEquipmentToNFT(
        string walletAddress,
        string itemId,
        string instanceId,
        int enhanceLevel,
        string itemName,
        string itemDescription,
        string itemImageUri,
        string contractAddress,
        string callbackObjectName
    );
#endif

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void CreateOrLoadUserForWallet(string walletAddress)
    {
        if (Instance == null)
        {
            var bridgeObject = new GameObject(nameof(FirebaseWebGLBridge));
            bridgeObject.AddComponent<FirebaseWebGLBridge>();
        }

        Instance.CreateOrLoadUser(walletAddress);
    }

    public void CreateOrLoadUser(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogError("Wallet address is empty.");
            return;
        }

        CurrentUserId = walletAddress.ToLowerInvariant();

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseCreateOrLoadUser(CurrentUserId, gameObject.name);
#else
        Debug.Log($"Firebase WebGL skipped outside WebGL build. Wallet: {CurrentUserId}");
        OnFirebaseUserLoaded("{}");
#endif
    }

    public static void LoadGameData(string callbackObjectName)
    {
        if (!HasCurrentUser)
        {
            Debug.LogWarning("Firebase user is not ready. Cannot load cloud save.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseLoadGameData(CurrentUserId, callbackObjectName);
#else
        Debug.Log("Firebase cloud load skipped outside WebGL build.");
#endif
    }

    public static void SaveGameData(string gameDataJson, string callbackObjectName)
    {
        if (!HasCurrentUser)
        {
            Debug.LogWarning("Firebase user is not ready. Cannot save cloud data.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseSaveGameData(CurrentUserId, gameDataJson, callbackObjectName);
#else
        Debug.Log("Firebase cloud save skipped outside WebGL build.");
#endif
    }

    public static void DeleteGameData(string callbackObjectName)
    {
        if (!HasCurrentUser)
        {
            Debug.LogWarning("Firebase user is not ready. Cannot delete cloud save.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseDeleteGameData(CurrentUserId, callbackObjectName);
#else
        Debug.Log("Firebase cloud delete skipped outside WebGL build.");
#endif
    }

    public static void ConvertEquipmentToNFT(
        string itemId,
        string instanceId,
        int enhanceLevel,
        string itemName,
        string itemDescription,
        string itemImageUri,
        string contractAddress
    )
    {
        if (Instance == null)
        {
            Debug.LogError("Firebase bridge is not available. Cannot convert equipment to NFT.");
            NFTItemConverter.OnEquipmentConvertFailed("Firebase bridge is not available.");
            return;
        }

        if (!HasCurrentUser)
        {
            Debug.LogError("Firebase user is not ready. Cannot convert equipment to NFT.");
            NFTItemConverter.OnEquipmentConvertFailed("Firebase user is not ready.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseConvertEquipmentToNFT(
            CurrentUserId,
            itemId,
            instanceId,
            enhanceLevel,
            itemName,
            itemDescription,
            itemImageUri,
            contractAddress,
            Instance.gameObject.name
        );
#else
        Debug.Log("Firebase equipment NFT conversion skipped outside WebGL build.");
        NFTItemConverter.OnEquipmentConvertFailed("Equipment NFT conversion only runs in WebGL build.");
#endif
    }

    public void OnFirebaseUserLoaded(string json)
    {
        Debug.Log($"Firebase user loaded: {json}");

        if (SceneManager.GetActiveScene().name == LoginSceneName)
        {
            SceneManager.LoadScene(PostLoginSceneName);
            return;
        }

        if (SaveManager.instance != null)
            SaveManager.instance.LoadGame();
    }

    public void OnFirebaseUserError(string error)
    {
        Debug.LogError($"Firebase error: {error}");
    }

    public void OnFirebaseEquipmentConverted(string json)
    {
        Debug.Log($"Firebase equipment converted to NFT: {json}");
        NFTItemConverter.OnEquipmentConvertSucceeded(json);
    }

    public void OnFirebaseEquipmentConvertError(string error)
    {
        Debug.LogError($"Firebase equipment NFT conversion error: {error}");
        NFTItemConverter.OnEquipmentConvertFailed(error);
    }
}
