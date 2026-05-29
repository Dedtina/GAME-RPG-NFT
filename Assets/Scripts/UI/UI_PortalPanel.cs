using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class PortalLevelOption
{
    public string displayName;
    public string sceneName;
    public Button button;
}

public class UI_PortalPanel : MonoBehaviour
{
    [SerializeField] private List<PortalLevelOption> levelOptions = new();
    [SerializeField] private TextMeshProUGUI selectedLevelText;
    [SerializeField] private Button enterButton;
    [SerializeField] private bool saveBeforeEnter = true;
    [SerializeField] private float saveTimeout = 5f;

    private PortalLevelOption selectedLevel;

    private void Awake()
    {
        foreach (PortalLevelOption option in levelOptions)
        {
            if (option == null || option.button == null)
                continue;

            PortalLevelOption capturedOption = option;
            option.button.onClick.AddListener(() => SelectLevel(capturedOption));
        }

        if (enterButton != null)
            enterButton.onClick.AddListener(EnterSelectedLevel);
    }

    private void OnEnable()
    {
        if (selectedLevel == null && levelOptions.Count > 0)
            SelectLevel(levelOptions[0]);
        else
            RefreshSelectedLevel();
    }

    public void SelectLevel(PortalLevelOption option)
    {
        selectedLevel = option;
        RefreshSelectedLevel();
    }

    private void EnterSelectedLevel()
    {
        if (selectedLevel == null || string.IsNullOrEmpty(selectedLevel.sceneName))
        {
            Debug.LogWarning("Select a level first.");
            return;
        }

        if (saveBeforeEnter)
            StartCoroutine(SaveAndLoadSelectedLevel());
        else
            LoadSelectedLevel();
    }

    private IEnumerator SaveAndLoadSelectedLevel()
    {
        SetEnterButton(false);

        bool saveFinished = false;
        void HandleSaveFinished() => saveFinished = true;

        SaveManager.OnGameDataSaved += HandleSaveFinished;
        SaveManager.instance?.SaveGame();

        float timer = 0f;
        while (!saveFinished && timer < saveTimeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        SaveManager.OnGameDataSaved -= HandleSaveFinished;
        LoadSelectedLevel();
    }

    private void LoadSelectedLevel()
    {
        SceneManager.LoadScene(selectedLevel.sceneName);
    }

    private void RefreshSelectedLevel()
    {
        if (selectedLevelText != null)
            selectedLevelText.text = selectedLevel != null ? GetSelectedDisplayName() : "No Level Selected";

        SetEnterButton(selectedLevel != null && !string.IsNullOrEmpty(selectedLevel.sceneName));
    }

    private string GetSelectedDisplayName()
    {
        if (selectedLevel == null)
            return "";

        return string.IsNullOrEmpty(selectedLevel.displayName) ? selectedLevel.sceneName : selectedLevel.displayName;
    }

    private void SetEnterButton(bool interactable)
    {
        if (enterButton != null)
            enterButton.interactable = interactable;
    }
}
