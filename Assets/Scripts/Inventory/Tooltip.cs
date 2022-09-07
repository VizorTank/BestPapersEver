using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private UnityEngine.Camera uiCamera;
    private Text tooltipText;
    private RectTransform background;
    PlayerInput playerInput;
    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    private void Awake()
    {
        background = transform.Find("background").GetComponent<RectTransform>();
        tooltipText = transform.Find("Text").GetComponent<Text>();
        ShowTooltip("Testing area in progress");
        playerInput = new PlayerInput();
        playerInput.Enable();
    }

    private void Update()
    {
        transform.position = playerInput.UI.Point.ReadValue<Vector2>();
        if (CheckForSlot() != null && CheckForSlot().itemSlot.HasItem)
        {

            ShowTooltip(CheckForSlot().itemSlot.stack.Item.ItemName);
        }
        else HideTooltip();

        

    }

    private UIItemSlot CheckForSlot()
    {

        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = playerInput.UI.Point.ReadValue<Vector2>();

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results)
        {

            if (result.gameObject.tag == "UIItemSlot")
                return result.gameObject.GetComponent<UIItemSlot>();
        }

        return null;

    }

    private void ShowTooltip(string tooltipstring)
    {
        gameObject.SetActive(true);

        tooltipText.text = tooltipstring;
        
        float textpaddingsize = 4f;

        Vector2 backgroundSize = new Vector2(tooltipText.preferredWidth + textpaddingsize*2f, tooltipText.preferredHeight + textpaddingsize * 2f);
        background.sizeDelta = backgroundSize;
        
    }

    private void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}
