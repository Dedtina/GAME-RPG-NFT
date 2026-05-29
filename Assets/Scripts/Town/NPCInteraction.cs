using UnityEngine;

public enum NpcFeatureType
{
    None,
    Blacksmith,
    Wizard,
    Portal,
    Summoner
}

public class NPCInteraction : MonoBehaviour
{
    public static bool HasNearbyInteractable => currentInteraction != null;

    private static NPCInteraction currentInteraction;

    [Header("Interaction")]
    [SerializeField] private NpcFeatureType featureType = NpcFeatureType.None;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool closeFeatureOnExit = true;

    private UI uiRoot;
    private bool isPlayerNearby;

    private void Awake()
    {
        uiRoot = UI.instance != null ? UI.instance : FindObjectOfType<UI>();
    }

    private void Update()
    {
        if (!isPlayerNearby)
            return;

        if (Input.GetKeyDown(interactKey))
            ToggleFeature();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponentInParent<Player>();
        if (player == null || player.statCtrl.IsDeath())
            return;

        isPlayerNearby = true;
        currentInteraction = this;
        uiRoot?.SetNpcInteractionPrompt(true, featureType, interactKey);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Player>() == null)
            return;

        isPlayerNearby = false;
        if (currentInteraction == this)
            currentInteraction = null;

        uiRoot?.SetNpcInteractionPrompt(false, featureType, interactKey);

        if (closeFeatureOnExit)
            uiRoot?.CloseNpcFeature(featureType);
    }

    private void ToggleFeature()
    {
        if (featureType == NpcFeatureType.None)
        {
            Debug.LogWarning($"{name} has no NPC feature type assigned.", this);
            return;
        }

        if (uiRoot == null)
            return;

        bool isCurrentFeatureOpen = uiRoot.IsNpcFeatureOpen(featureType);
        if (PlayerManager.instance != null && PlayerManager.instance.isInMenu && !isCurrentFeatureOpen)
            return;

        uiRoot.ToggleNpcFeature(featureType);
        uiRoot.SetNpcInteractionPrompt(!uiRoot.IsNpcFeatureOpen(featureType), featureType, interactKey);
    }
}
