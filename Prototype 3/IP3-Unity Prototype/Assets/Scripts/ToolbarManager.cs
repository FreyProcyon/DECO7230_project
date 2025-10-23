using UnityEngine;
using UnityEngine.UI;

public class ToolbarManager : MonoBehaviour
{
    [Header("Main Buttons (optional)")]
    public GameObject selectButton;
    public GameObject createButton;

    [Header("Foldable Option Panels")]
    public GameObject selectOptionsPanel; // 选择主按钮展开的面板
    public GameObject createOptionsPanel; // 创建主按钮展开的面板

    [Header("Transient Panels (shown only in a tool)")]
    [Tooltip("删除模式的顶栏（含 Delete / Cancel 按钮），进入删除模式时显示，退出时隐藏")]
    public GameObject deleteTopPanel;

    void Start()
    {
        // 初始收起可折叠面板；临时面板默认隐藏
        if (selectOptionsPanel) selectOptionsPanel.SetActive(false);
        if (createOptionsPanel) createOptionsPanel.SetActive(false);
        if (deleteTopPanel)     deleteTopPanel.SetActive(false);
    }

    // ================== 主按钮（UI OnClick） ==================
    public void ToggleSelectPanel()
    {
        if (!selectOptionsPanel || !createOptionsPanel)
        {
            Debug.LogWarning("ToolbarManager: Panels not assigned.");
            return;
        }
        bool newState = !selectOptionsPanel.activeSelf;
        selectOptionsPanel.SetActive(newState);
        if (newState) createOptionsPanel.SetActive(false);
        // 打开任一折叠面板时，确保临时面板（如删除顶栏）不干扰
        if (newState && deleteTopPanel) deleteTopPanel.SetActive(false);
    }

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
        if (newState && deleteTopPanel) deleteTopPanel.SetActive(false);
    }

    // ================== 工具调用的标准入口 ==================
    /// <summary>
    /// 任意工具进入时请调用。会收起 Select/Create 的折叠面板，避免挡住或误触。
    /// </summary>
    public void OnToolEnter()
    {
        CloseFoldables();
        // 不主动隐藏 deleteTopPanel，因为删除工具可能需要显示它（由工具显式控制）
    }

    /// <summary>
    /// 工具退出时可调用（目前只是对称接口，保留扩展位）。
    /// </summary>
    public void OnToolExit()
    {
        // 这里暂不自动展开任何面板；保持“退出后都收起”的干净状态
        CloseFoldables();
        HideDeleteTopPanel();
    }

    /// <summary>
    /// 删除工具开始/结束时调用，用于显示/隐藏顶部 Delete 面板。
    /// </summary>
    public void ShowDeleteTopPanel(bool show)
    {
        if (!deleteTopPanel) return;
        // 显示删除顶栏前，先收起其它折叠面板，避免视觉与点击冲突
        if (show) CloseFoldables();
        deleteTopPanel.SetActive(show);
    }

    // ================== Helpers ==================
    public void CloseAll()
    {
        CloseFoldables();
        HideDeleteTopPanel();
    }

    void CloseFoldables()
    {
        if (selectOptionsPanel) selectOptionsPanel.SetActive(false);
        if (createOptionsPanel) createOptionsPanel.SetActive(false);
    }

    void HideDeleteTopPanel()
    {
        if (deleteTopPanel) deleteTopPanel.SetActive(false);
    }
}
