using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class LevelExitPortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string destinationScene = "Town";

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool saveBeforeExit = true;
    [SerializeField] private float saveTimeout = 5f;

    private UI uiRoot;
    private bool isPlayerNearby;
    private bool isLoadingScene;
    private bool isBlockedByLivingEnemies;

    private void Awake()
    {
        uiRoot = UI.instance != null ? UI.instance : FindObjectOfType<UI>();
    }

    private void Update()
    {
        if (!isPlayerNearby || isLoadingScene)
            return;

        if (PlayerManager.instance != null && PlayerManager.instance.isInMenu)
            return;

        if (isBlockedByLivingEnemies && !HasLivingEnemiesOnMap())
        {
            isBlockedByLivingEnemies = false;
            uiRoot?.SetNpcInteractionPrompt(true, NpcFeatureType.Portal, interactKey);
        }

        if (Input.GetKeyDown(interactKey))
            StartCoroutine(ExitLevel());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponentInParent<Player>();
        if (player == null || player.statCtrl.IsDeath())
            return;

        isPlayerNearby = true;
        uiRoot?.SetNpcInteractionPrompt(true, NpcFeatureType.Portal, interactKey);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
            return;

        isPlayerNearby = false;
        isBlockedByLivingEnemies = false;
        uiRoot?.SetNpcInteractionPrompt(false, NpcFeatureType.Portal, interactKey);
    }

    private IEnumerator ExitLevel()
    {
        if (HasLivingEnemiesOnMap())
        {
            isBlockedByLivingEnemies = true;
            uiRoot?.SetNpcInteractionMessage(true, "Defeat all enemies before leaving");
            yield break;
        }

        if (string.IsNullOrEmpty(destinationScene))
        {
            Debug.LogWarning("Level exit portal has no destination scene.", this);
            yield break;
        }

        isLoadingScene = true;
        uiRoot?.SetNpcInteractionPrompt(false, NpcFeatureType.Portal, interactKey);

        if (uiRoot != null && uiRoot.fadeScreen != null)
            uiRoot.fadeScreen.FadeOut();

        if (saveBeforeExit && SaveManager.instance != null)
            yield return SaveGameBeforeSceneLoad();

        SceneManager.LoadScene(destinationScene);
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

    private IEnumerator SaveGameBeforeSceneLoad()
    {
        bool saveFinished = false;
        void HandleSaveFinished() => saveFinished = true;

        SaveManager.OnGameDataSaved += HandleSaveFinished;
        SaveManager.instance.SaveGame();

        float timer = 0f;
        while (ShouldWaitForSave(saveFinished, timer))
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        SaveManager.OnGameDataSaved -= HandleSaveFinished;
    }

    private bool ShouldWaitForSave(bool saveFinished, float timer)
    {
        bool waitForSave = !saveFinished && timer < saveTimeout;

#if UNITY_WEBGL && !UNITY_EDITOR
        return waitForSave;
#else
        return false;
#endif
    }

    private void OnValidate()
    {
        Collider2D portalCollider = GetComponent<Collider2D>();
        if (portalCollider != null)
            portalCollider.isTrigger = true;
    }
}
