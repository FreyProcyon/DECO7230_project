using System.Collections.Generic;
using UnityEngine;
// 保留 EventSystems 可选；XR 下不依赖鼠标指针，但不影响编译
using UnityEngine.EventSystems;

/// <summary>
/// XR 版选择管理：
/// - 由 XRInputBridge 提供“选择/取消”按钮与右手射线
/// - 命中 selectableMask 时选中对象并高亮；命中 groundMask 或按 Cancel 时清除选择
/// - 仍保留 selectionEnabled 开关，便于工具（Move/Rotate/Scale 等）控制何时允许点选
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("XR Input (Required)")]
    public XRInputBridge inputBridge;          // 拖 Managers 上的 XRInputBridge
    [Tooltip("Enable/disable picking by tools. Set true when a tool needs selection.")]
    public bool selectionEnabled = false;

    [Header("Raycast")]
    [Tooltip("Selectable objects layers (e.g., Default | Placeable)")]
    public LayerMask selectableMask = ~0;
    [Tooltip("Ground layers (clicking ground clears selection)")]
    public LayerMask groundMask = ~0;
    public float maxRayDistance = 30f;

    [Header("Highlight")]
    public Color highlightColor = new Color(1f, 0.85f, 0.1f, 1f); // 暖黄
    [Range(0f, 1f)] public float highlightEmission = 0.25f;
    [Tooltip("Use sharedMaterials (dangerous). Default false to work on instance materials.")]
    public bool useSharedMaterial = false;

    // —— 当前选中（单选）——
    private GameObject current;
    public GameObject Current => current;

    // 还原材质所需缓存
    private struct OriginalMat
    {
        public Renderer renderer;
        public Material[] mats;
    }
    private readonly List<OriginalMat> originalMats = new();

    void Update()
    {
        if (!selectionEnabled) return;
        if (!inputBridge || !inputBridge.rightHand) return;

        // 选择：按下 Select（通常是右手 Trigger / 你在 Action 里绑定的键）
        if (inputBridge.SelectPressedThisFrame())
        {
            // 从右手 forward 发射射线
            var ray = new Ray(inputBridge.rightHand.position, inputBridge.rightHand.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, selectableMask, QueryTriggerInteraction.Ignore))
            {
                var go = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;
                Select(go);
            }
            else if (Physics.Raycast(ray, out hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                // 指向地面：清除选择
                ClearSelection();
            }
            else
            {
                // 指向其它不可选层：也清除
                ClearSelection();
            }
        }

        // 取消键（B/Y 或你自定义的 Cancel）
        if (inputBridge.CancelPressedThisFrame())
        {
            ClearSelection();
        }
    }

    /// <summary>设置当前选中并应用高亮</summary>
    public void Select(GameObject go)
    {
        if (go == current) return;

        ClearSelection();

        current = go;
        ApplyHighlight(current);
    }

    /// <summary>清除当前选中并恢复原材质</summary>
    public void ClearSelection()
    {
        if (!current && originalMats.Count == 0) return;

        // 还原材质
        foreach (var entry in originalMats)
        {
            if (!entry.renderer) continue;
            if (useSharedMaterial)
                entry.renderer.sharedMaterials = entry.mats;
            else
                entry.renderer.materials = Clone(entry.mats); // 防止引用污染
        }
        originalMats.Clear();
        current = null;
    }

    // —— 高亮实现：逐个 Renderer 改材质颜色/Emission（兼容内置/URP/HDRP 常见属性）——
    void ApplyHighlight(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
        foreach (var r in renderers)
        {
            if (!r) continue;

            // 记录原材质阵列
            var record = new OriginalMat
            {
                renderer = r,
                mats = useSharedMaterial ? r.sharedMaterials : Clone(r.materials)
            };
            originalMats.Add(record);

            // 复制或取共享材质并改色
            var mats = useSharedMaterial ? r.sharedMaterials : r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;

                // Base/Color
                if (mats[i].HasProperty("_BaseColor"))       mats[i].SetColor("_BaseColor", highlightColor);
                else if (mats[i].HasProperty("_Color"))      mats[i].SetColor("_Color", highlightColor);

                // 轻微自发光
                if (highlightEmission > 0f && mats[i].HasProperty("_EmissionColor"))
                {
                    mats[i].EnableKeyword("_EMISSION");
                    mats[i].SetColor("_EmissionColor", highlightColor * highlightEmission);
                }
            }

            if (!useSharedMaterial) r.materials = mats;
            else r.sharedMaterials = mats;
        }
    }

    // —— 工具：克隆材质数组（避免实例互相污染）——
    Material[] Clone(Material[] src)
    {
        var arr = new Material[src.Length];
        for (int i = 0; i < src.Length; i++)
            arr[i] = src[i] ? new Material(src[i]) : null;
        return arr;
    }

    // XR 下通常不再需要“鼠标是否悬停在 UI 上”的判断；保留函数以兼容旧引用
    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
