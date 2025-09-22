using UnityEngine;

public class ToolbarManager : MonoBehaviour
{
    [Header("Main Buttons")]
    public GameObject selectButton;
    public GameObject createButton;

    [Header("Option Panels")]
    public GameObject selectOptionsPanel;
    public GameObject createOptionsPanel;

    void Start()
    {
        // Start hidden (idempotent)
        if (selectOptionsPanel) selectOptionsPanel.SetActive(false);
        if (createOptionsPanel) createOptionsPanel.SetActive(false);
    }

    // Called by Select main button (OnClick)
    public void ToggleSelectPanel()
    {
        if (!selectOptionsPanel || !createOptionsPanel)
        {
            Debug.LogWarning("ToolbarManager: Panels not assigned.");
            return;
        }

        // derive state from activeSelf (no separate bools)
        bool newState = !selectOptionsPanel.activeSelf;
        selectOptionsPanel.SetActive(newState);

        // ensure mutual-exclusive
        if (newState) createOptionsPanel.SetActive(false);
    }

    // Called by Create main button (OnClick)
    public void ToggleCreatePanel()
    {
        if (!selectOptionsPanel || !createOptionsPanel)
        {
            Debug.LogWarning("ToolbarManager: Panels not assigned.");
            return;
        }

        bool newState = !createOptionsPanel.activeSelf;
        createOptionsPanel.SetActive(newState);

        if (newState) selectOptionsPanel.SetActive(false);
    }

    // Optional helper: close all (可在别处调用)
    public void CloseAll()
    {
        if (selectOptionsPanel) selectOptionsPanel.SetActive(false);
        if (createOptionsPanel) createOptionsPanel.SetActive(false);
    }
}
