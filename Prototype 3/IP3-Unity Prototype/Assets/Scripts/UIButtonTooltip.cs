using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipMessage;
    public TMP_Text tooltipText; // 拖到 Inspector

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipText) tooltipText.text = tooltipMessage;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipText) tooltipText.text = "";
    }
}
