using TMPro;
using UnityEngine;

public class UI_Tooltips_Skill : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI skillName;
    [SerializeField] TextMeshProUGUI skillDescription;
    [SerializeField] TextMeshProUGUI skillCost;
    [SerializeField] private Vector2 pointerOffset = new(12f, -12f);

    private void Start()
    {
        DisableTooltips();
    }

    public void EnableTooltips(UI_SkillSlot skillSlot)
    {
        if (skillSlot != null)
        {
            skillName.text = skillSlot.skillName;
            skillDescription.text = skillSlot.skillDescription;
            skillCost.text = "Cost: " + skillSlot.skillPrice;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            CorrectPosition();
        }
    }

    private void CorrectPosition()
    {
        RectTransform tooltipRect = transform as RectTransform;
        RectTransform parentRect = transform.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (tooltipRect == null || parentRect == null || canvas == null)
            return;

        bool showOnLeft = Input.mousePosition.x > Screen.width * 0.5f;
        tooltipRect.pivot = showOnLeft ? new Vector2(1f, 1f) : new Vector2(0f, 1f);

        Vector2 offset = pointerOffset;
        if (showOnLeft)
            offset.x *= -1f;

        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPosition = (Vector2)Input.mousePosition + offset;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, camera, out Vector2 localPosition))
            return;

        float minX = parentRect.rect.xMin + tooltipRect.rect.width * tooltipRect.pivot.x;
        float maxX = parentRect.rect.xMax - tooltipRect.rect.width * (1f - tooltipRect.pivot.x);
        float minY = parentRect.rect.yMin + tooltipRect.rect.height * tooltipRect.pivot.y;
        float maxY = parentRect.rect.yMax - tooltipRect.rect.height * (1f - tooltipRect.pivot.y);

        localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
        localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
        tooltipRect.anchoredPosition = localPosition;
    }

    public void DisableTooltips()
    {
        gameObject.SetActive(false);
    }
}
