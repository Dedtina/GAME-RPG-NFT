using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UIImage = UnityEngine.UI;

public class UI_InventorySlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] protected UIImage.Image itemImage;
    [SerializeField] protected TextMeshProUGUI itemText;
    public InventoryItem itemData;
    protected UI ui;

    protected virtual void Start()
    {
        ResolveUI();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (itemData != null && itemData.data != null)
        {
            InventoryItem selectedInventoryItem = itemData;
            ItemData selectedItem = selectedInventoryItem.data;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (Input.GetKey(KeyCode.LeftControl))
                Inventory.instance.RemoveItem(selectedInventoryItem);
            else if (selectedItem.itemType == ItemType.Equipment)
            {
                Inventory.instance.RemoveItem(selectedInventoryItem);
                Inventory.instance.EquipItem(selectedInventoryItem);
            }

            HideEquipmentTooltips();
        }
    }

    public void UpdateInventorySlot(InventoryItem _newItem)
    {
        itemData = _newItem;
        itemImage.color = Color.white;

        if (itemData != null)
        {
            itemImage.sprite = itemData.data.icon;

            if (itemData.IsEquipment() && itemData.enhanceLevel > 0)
                itemText.text = "+" + itemData.enhanceLevel;
            else if (itemData.stack > 1)
                itemText.text = itemData.stack.ToString();
            else itemText.text = "";
        }
        else
        {
            Debug.Log("itemData is null");
            itemImage.sprite = null;
            itemImage.color = Color.clear;
            itemText.text = "";
        }

    }

    public virtual void ClearSlot()
    {
        itemImage.sprite = null;
        itemImage.color = Color.clear;
        itemText.text = "";
        itemData = null;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (
            itemData != null &&
            itemData.data != null
        )
        {
            UI root = ResolveUI();
            if (root != null && root.equipmentTooltips != null)
                root.equipmentTooltips.EnableTooltips(itemData);
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (itemData != null)
            HideEquipmentTooltips();
    }

    protected UI ResolveUI()
    {
        if (ui == null)
            ui = GetComponentInParent<UI>();

        if (ui == null)
            ui = UI.instance;

        return ui;
    }

    protected void HideEquipmentTooltips()
    {
        UI root = ResolveUI();
        if (root != null && root.equipmentTooltips != null)
            root.equipmentTooltips.DisableTooltips();
    }
}
