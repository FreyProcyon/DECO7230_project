using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// XR 版删除工具：
/// - StartDeleteMode() 后，显示顶部 Delete/Cancel 面板；
/// - 右手触发（Select）射线命中可选层：切换“标记为待删（红色高亮）/取消标记”；
/// - 点 Delete 按钮：删除所有标记对象；
/// - 点 Cancel 按钮或 XR 的 Cancel：全部还原并退出；
/// - 依赖 XRInputBridge 提供按钮与右手射线；
/// </summary>
public class DeleteTool : MonoBehaviour
{
    [Header("XR Input (Required)")]
    public XRInputBridge inputBridge;                 // 拖 Managers 上的 XRInputBridge

    [Header("UI Buttons")]
    public GameObject topPanel;                       // 顶部的 UI 面板（包含 Delete 和 Cancel 按钮）
    public Button deleteButton;
    public Button cancelButton;

    [Header("Highlight Colors")]
    public Color deleteColor = Color.red;
    [Range(0f, 2f)] public float emissionStrength = 0.5f;

    [Header("Managers")]
    public ToolbarManager toolbar;                    // 用于进入模式时关闭其它面板（可空）

    [Header("Raycast")]
    public LayerMask selectableMask = ~0;             // 可以删除的层
    public float maxRayDistance = 30f;

    // 选中集合
    private readonly HashSet<GameObject> markedForDelete = new HashSet<GameObject>();
    // 记录每个 Renderer 的原材质，用于还原
    private readonly Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();

    private bool deleteMode = false;

    void Awake()
    {
        if (deleteButton) deleteButton.onClick.AddListener(DeleteSelected);
        if (cancelButton) cancelButton.onClick.AddListener(CancelDelete);
        if (topPanel) topPanel.SetActive(false);
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
    }

    void Update()
    {
        if (!deleteMode || inputBridge == null || inputBridge.rightHand == null) return;

        // XR Cancel 快捷键：等同 Cancel 按钮
        if (inputBridge.CancelPressedThisFrame())
        {
            CancelDelete();
            return;
        }

        // XR Select（Trigger/A）：从右手 forward 发射射线，命中可选对象则切换标记
        if (inputBridge.SelectPressedThisFrame() && !IsPointerOverUI())
        {
            var ray = new Ray(inputBridge.rightHand.position, inputBridge.rightHand.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, selectableMask, QueryTriggerInteraction.Ignore))
            {
                var go = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;

                if (markedForDelete.Contains(go))
                {
                    // 已经选中 → 取消选中
                    Restore(go);
                    markedForDelete.Remove(go);
                }
                else
                {
                    // 新选中 → 标红
                    Mark(go);
                    markedForDelete.Add(go);
                }
            }
            // 指到空白（未命中或非 selectableMask）→ 不做操作
        }
    }

    // ====== 外部入口 ======
    public void StartDeleteMode()
    {
        // 关闭其他 UI 面板，打开顶部 Delete/Cancel
        if (toolbar) toolbar.CloseAll();
        deleteMode = true;
        if (topPanel) topPanel.SetActive(true);

        // 清空旧状态
        // （不强制还原旧记录，进入模式即为新一轮）
        markedForDelete.Clear();
        originalMats.Clear();
    }

    // ====== 顶部按钮：删除 & 取消 ======
    void DeleteSelected()
    {
        // 删除所有标记对象前，清理它们的 Renderer 记录
        foreach (var go in markedForDelete)
        {
            if (go) RemoveOriginalsFor(go);
        }

        foreach (var go in markedForDelete)
        {
            if (go) Destroy(go);
        }
        markedForDelete.Clear();
        originalMats.Clear();

        ExitDeleteMode();
    }

    void CancelDelete()
    {
        // 还原所有标红物体
        foreach (var go in markedForDelete)
        {
            Restore(go);
        }
        markedForDelete.Clear();
        originalMats.Clear();

        ExitDeleteMode();
    }

    void ExitDeleteMode()
    {
        deleteMode = false;
        if (topPanel) topPanel.SetActive(false);
    }

    // ====== 高亮控制 ======
    void Mark(GameObject go)
    {
        // 处理自身与所有子 Renderer
        var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: false);
        if (renderers.Length == 0) return;

        foreach (var r in renderers)
        {
            if (!r) continue;

            // 首次记录原材质
            if (!originalMats.ContainsKey(r))
            {
                // 注意：这里存引用用于还原
                originalMats[r] = r.materials;
            }

            // 克隆并着色
            Material[] src = r.materials; // 实例化数组
            Material[] colored = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var m = new Material(src[i]);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", deleteColor);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", deleteColor);

                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", deleteColor * emissionStrength);
                }
                colored[i] = m;
            }
            r.materials = colored;
        }
    }

    void Restore(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: false);
        foreach (var r in renderers)
        {
            if (!r) continue;
            if (originalMats.TryGetValue(r, out var mats))
            {
                r.materials = mats;
                originalMats.Remove(r);
            }
        }
    }

    void RemoveOriginalsFor(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: false);
        foreach (var r in renderers)
        {
            if (!r) continue;
            if (originalMats.ContainsKey(r))
                originalMats.Remove(r);
        }
    }

    // XR 下通常不需要鼠标 UI 命中，但保留以兼容在 Editor 模式同时测试鼠标的情况
    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
