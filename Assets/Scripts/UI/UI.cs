using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    public static UI instance;

    [SerializeField] private string mainMenuScene = "MainMenu";

    public GameObject characterUI;
    public GameObject skillTreeUI;
    public GameObject craftUI;
    public GameObject optionsUI;
    public GameObject inGameUI;

    [Header("Town NPC UI")]
    [SerializeField] private GameObject blacksmithUI;
    [SerializeField] private GameObject wizardUI;
    [SerializeField] private GameObject wizardConvertUI;
    [SerializeField] private GameObject wizardRedeemUI;
    [SerializeField] private GameObject portalUI;
    [SerializeField] private GameObject summonerUI;
    [SerializeField] private UI_SummonerPanel summonerPanel;
    [SerializeField] private GameObject npcInteractionPrompt;
    [SerializeField] private TextMeshProUGUI npcInteractionPromptText;

    [SerializeField] private float saveBeforeExitTimeout = 5f;
    public UI_Tooltips_Equipment equipmentTooltips;
    public UI_Tooltips_Stat statTooltips;
    public UI_Tooltips_Skill skillTooltips;
    public UI_CraftWindow craftWindow;
    public UI_Fade fadeScreen;
    public UI_DieScreen dieScreen;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        MoveEquipmentTooltipsToOverlay();

        // Awake skill tree functions to setup skills' events
        SwitchTo(skillTreeUI);
    }
    private void Start()
    {
        SwitchTo(inGameUI);
        fadeScreen.gameObject.SetActive(true);
        equipmentTooltips.gameObject.SetActive(false);
        statTooltips.gameObject.SetActive(false);
        SetNpcInteractionPrompt(false, NpcFeatureType.None, KeyCode.None);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            SwitchToUsingKeyboard(characterUI);
        if (Input.GetKeyDown(KeyCode.B))
            SwitchToUsingKeyboard(craftUI);
        if (Input.GetKeyDown(KeyCode.K))
            SwitchToUsingKeyboard(skillTreeUI);
        if (Input.GetKeyDown(KeyCode.O))
            SwitchToUsingKeyboard(optionsUI);
    }

    public void SwitchTo(GameObject _menu)
    {
        HideAllTooltips();

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
            if (PlayerManager.instance)
                PlayerManager.instance.isInMenu = false;
        }

        if (_menu != null)
        {
            _menu.SetActive(true);

            if (PlayerManager.instance != null)
            {
                PlayerManager.instance.isInMenu = _menu != inGameUI;
                Debug.Log("Is In Menu: " + PlayerManager.instance.isInMenu);
            }

            // Set default value 
            UI_CraftList craftList = _menu.GetComponentInChildren<UI_CraftList>();
            if (craftList != null)
            {
                craftList.InitCraftList();
            }
        }
    }

    public void OpenNpcFeature(NpcFeatureType featureType)
    {
        GameObject npcUI = GetNpcFeatureUI(featureType);
        if (npcUI == null)
        {
            Debug.LogWarning($"No UI panel assigned for NPC feature: {featureType}", this);
            return;
        }

        SetNpcInteractionPrompt(false, featureType, KeyCode.None);
        SwitchTo(npcUI);

        if (featureType == NpcFeatureType.Wizard)
            OpenWizardConvertUI();
    }

    public void ToggleNpcFeature(NpcFeatureType featureType)
    {
        if (IsNpcFeatureOpen(featureType))
            CloseNpcFeature(featureType);
        else
            OpenNpcFeature(featureType);
    }

    public bool IsNpcFeatureOpen(NpcFeatureType featureType)
    {
        GameObject npcUI = GetNpcFeatureUI(featureType);
        return npcUI != null && npcUI.activeInHierarchy;
    }

    public void CloseNpcFeature(NpcFeatureType featureType)
    {
        GameObject npcUI = GetNpcFeatureUI(featureType);
        if (npcUI == null || !npcUI.activeInHierarchy)
            return;

        NPCEnemySummoner closedSummoner = null;
        if (featureType == NpcFeatureType.Summoner && summonerPanel != null)
            closedSummoner = summonerPanel.ClearSummoner();

        HideAllTooltips();
        npcUI.SetActive(false);

        if (inGameUI != null)
            inGameUI.SetActive(true);

        if (PlayerManager.instance != null)
            PlayerManager.instance.isInMenu = false;

        closedSummoner?.HandleSummonerPanelClosed();
    }

    public void OpenWizardConvertUI()
    {
        SwitchWizardUI(wizardConvertUI);
    }

    public void OpenWizardRedeemUI()
    {
        SwitchWizardUI(wizardRedeemUI);
    }

    public bool OpenSummonerPanel(NPCEnemySummoner summoner)
    {
        if (summonerUI == null || summoner == null)
        {
            Debug.LogWarning("No summoner UI panel or summoner NPC assigned.", this);
            return false;
        }

        if (summonerPanel == null)
            summonerPanel = summonerUI.GetComponentInChildren<UI_SummonerPanel>(true);

        if (summonerPanel == null)
        {
            Debug.LogWarning("Summoner UI does not have a UI_SummonerPanel component.", this);
            return false;
        }

        SwitchTo(summonerUI);
        summonerPanel.OpenFor(summoner);
        return true;
    }

    public void SetNpcInteractionPrompt(bool active, NpcFeatureType featureType, KeyCode interactKey)
    {
        if (npcInteractionPrompt == null)
            return;

        npcInteractionPrompt.SetActive(active);

        if (!active || npcInteractionPromptText == null)
            return;

        npcInteractionPromptText.text = $"Press {interactKey} - {GetNpcFeatureLabel(featureType)}";
    }

    public void SetNpcInteractionMessage(bool active, string message)
    {
        if (npcInteractionPrompt == null)
            return;

        npcInteractionPrompt.SetActive(active);

        if (active && npcInteractionPromptText != null)
            npcInteractionPromptText.text = message;
    }

    private GameObject GetNpcFeatureUI(NpcFeatureType featureType)
    {
        switch (featureType)
        {
            case NpcFeatureType.Blacksmith:
                return blacksmithUI;
            case NpcFeatureType.Wizard:
                return wizardUI;
            case NpcFeatureType.Portal:
                return portalUI;
            case NpcFeatureType.Summoner:
                return summonerUI;
            default:
                return null;
        }
    }

    private string GetNpcFeatureLabel(NpcFeatureType featureType)
    {
        switch (featureType)
        {
            case NpcFeatureType.Blacksmith:
                return "Blacksmith";
            case NpcFeatureType.Wizard:
                return "Wizard";
            case NpcFeatureType.Portal:
                return "Portal";
            case NpcFeatureType.Summoner:
                return "Summon enemies";
            default:
                return "Interact";
        }
    }

    private void SwitchWizardUI(GameObject wizardFeatureUI)
    {
        if (wizardFeatureUI == null)
            return;

        HideAllTooltips();

        if (wizardUI != null && !wizardUI.activeInHierarchy)
            OpenNpcFeature(NpcFeatureType.Wizard);

        if (wizardConvertUI != null)
            wizardConvertUI.SetActive(wizardConvertUI == wizardFeatureUI);

        if (wizardRedeemUI != null)
            wizardRedeemUI.SetActive(wizardRedeemUI == wizardFeatureUI);
    }

    private void SwitchToUsingKeyboard(GameObject _menu)
    {
        if (_menu != null && _menu.activeInHierarchy)
        {
            HideAllTooltips();
            _menu.SetActive(false);
            inGameUI.SetActive(true);
            if (PlayerManager.instance != null)
            {
                PlayerManager.instance.isInMenu = false;
                Debug.Log("Is In Menu: " + PlayerManager.instance.isInMenu);

            }
            return;
        }


        SwitchTo(_menu);
    }

    public void SwitchToDieUI()
    {
        fadeScreen.FadeOut();
        StartCoroutine(ShowDieMsgDelay(1f));
    }

    public void SaveAndExit()
    {
        StartCoroutine(SaveAndLoadMainMenu());
    }

    private IEnumerator SaveAndLoadMainMenu()
    {
        bool saveFinished = false;
        void HandleSaveFinished() => saveFinished = true;

        SaveManager.OnGameDataSaved += HandleSaveFinished;
        SaveManager.instance?.SaveGame();

        if (fadeScreen != null)
            fadeScreen.FadeOut();

        float timer = 0f;
        while (!saveFinished && timer < saveBeforeExitTimeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        SaveManager.OnGameDataSaved -= HandleSaveFinished;
        SceneManager.LoadScene(mainMenuScene);
    }

    private IEnumerator ShowDieMsgDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        dieScreen.ShowDieMessage();
    }

    public void RestartThisScene() => GameManager.instance.RestartScene();

    private void MoveEquipmentTooltipsToOverlay()
    {
        if (equipmentTooltips == null || equipmentTooltips.transform.parent == transform)
            return;

        equipmentTooltips.transform.SetParent(transform, false);
        equipmentTooltips.transform.SetAsLastSibling();
    }

    private void HideAllTooltips()
    {
        equipmentTooltips?.DisableTooltips();
        statTooltips?.DisableTooltips();
        skillTooltips?.DisableTooltips();
    }
}
