using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private string mainScene = "Town";
    [SerializeField] private string loginScene = "LoginScene";
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject deleteSavePanel;
    [SerializeField, Range(0f, 1f)] private float deleteSaveBackdropAlpha = 0.7f;
    [SerializeField] private UI_Fade fade;
    [SerializeField] private float newGameSaveTimeout = 5f;
    private GameObject deleteSaveBackdrop;
    private bool isTransitioning;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.interactable = false;

        CreateDeleteSaveBackdrop();
        SetDeleteSavePanelVisible(false);
    }

    private void Start()
    {
        RefreshContinueButton();
        SaveManager.OnGameDataLoadStateChanged += RefreshContinueButton;
    }

    private void OnDestroy()
    {
        SaveManager.OnGameDataLoadStateChanged -= RefreshContinueButton;
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.interactable = !isTransitioning && SaveManager.instance != null && SaveManager.instance.HasGameData();
    }

    public void ContinueGame()
    {
        if (isTransitioning || SaveManager.instance == null || !SaveManager.instance.HasGameData())
        {
            RefreshContinueButton();
            return;
        }

        isTransitioning = true;
        RefreshContinueButton();
        StartCoroutine(LoadSceneDelay(mainScene, 1.5f));
    }

    public void NewGame()
    {
        if (isTransitioning)
            return;

        if (SaveManager.instance != null && SaveManager.instance.HasGameData())
        {
            SetDeleteSavePanelVisible(true);
            return;
        }

        BeginNewGame();
    }

    public void ConfirmNewGame()
    {
        if (isTransitioning)
            return;

        SetDeleteSavePanelVisible(false);
        StartCoroutine(DeleteSaveAndCreateNewGame());
    }

    public void CancelNewGame()
    {
        if (isTransitioning)
            return;

        SetDeleteSavePanelVisible(false);
    }

    private void CreateDeleteSaveBackdrop()
    {
        if (deleteSavePanel == null || deleteSavePanel.transform.parent == null)
            return;

        deleteSaveBackdrop = new GameObject("DeleteSaveBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        RectTransform backdropRect = deleteSaveBackdrop.GetComponent<RectTransform>();
        backdropRect.SetParent(deleteSavePanel.transform.parent, false);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.anchoredPosition = Vector2.zero;
        backdropRect.sizeDelta = Vector2.zero;

        Image backdropImage = deleteSaveBackdrop.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, deleteSaveBackdropAlpha);
        backdropImage.raycastTarget = true;

        deleteSavePanel.transform.SetAsLastSibling();
        deleteSaveBackdrop.transform.SetSiblingIndex(deleteSavePanel.transform.GetSiblingIndex() - 1);
    }

    private void SetDeleteSavePanelVisible(bool isVisible)
    {
        if (deleteSavePanel == null)
            return;

        if (isVisible && deleteSaveBackdrop == null)
            CreateDeleteSaveBackdrop();

        deleteSaveBackdrop?.SetActive(isVisible);
        deleteSavePanel.SetActive(isVisible);

        if (isVisible)
        {
            deleteSaveBackdrop?.transform.SetAsLastSibling();
            deleteSavePanel.transform.SetAsLastSibling();
        }
    }

    private void BeginNewGame()
    {
        isTransitioning = true;
        RefreshContinueButton();
        StartCoroutine(CreateNewGameAndLoadScene(1.5f));
    }

    public async void Logout()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        RefreshContinueButton();

        try
        {
            if (ThirdwebManager.Instance != null && ThirdwebManager.Instance.SDK != null)
                await ThirdwebManager.Instance.SDK.wallet.Disconnect();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Wallet disconnect failed: {exception.Message}");
        }
        finally
        {
            FirebaseWebGLBridge.ClearCurrentUser();
            StartCoroutine(LoadSceneDelay(loginScene, 1.5f));
        }
    }

    private IEnumerator LoadSceneDelay(string sceneName, float delay)
    {
        fade?.FadeOut();
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator CreateNewGameAndLoadScene(float transitionDelay)
    {
        bool saveFinished = false;
        void HandleSaveFinished() => saveFinished = true;

        SaveManager.OnGameDataSaved += HandleSaveFinished;
        SaveManager.instance?.CreateNewGameSave();
        fade?.FadeOut();

        float timer = 0f;
        while (timer < transitionDelay || (!saveFinished && timer < newGameSaveTimeout))
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        SaveManager.OnGameDataSaved -= HandleSaveFinished;
        SceneManager.LoadScene(mainScene);
    }

    private IEnumerator DeleteSaveAndCreateNewGame()
    {
        isTransitioning = true;
        RefreshContinueButton();

        bool deleteFinished = false;
        void HandleDeleteFinished() => deleteFinished = true;

        SaveManager.OnGameDataDeleted += HandleDeleteFinished;
        SaveManager.instance?.Delete();

        float timer = 0f;
        while (!deleteFinished && timer < newGameSaveTimeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        SaveManager.OnGameDataDeleted -= HandleDeleteFinished;
        yield return CreateNewGameAndLoadScene(1.5f);
    }
}
