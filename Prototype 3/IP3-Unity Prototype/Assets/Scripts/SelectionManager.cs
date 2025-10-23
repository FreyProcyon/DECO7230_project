using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [Header("XR Input (Required)")]
    public XRInputBridge inputBridge;
    [Tooltip("工具需要选择时置 true；退出置 false")]
    public bool selectionEnabled = false;

    [Header("Raycast")]
    [Tooltip("可被选择的对象层（不要勾 UI）")]
    public LayerMask selectableMask = ~0;
    [Tooltip("地面层（指到地面清除选择）")]
    public LayerMask groundMask = 0;
    public float maxRayDistance = 30f;

    [Header("Highlight")]
    public Color highlightColor = new(1f, 0.85f, 0.1f, 1f);
    [Range(0f, 1f)] public float highlightEmission = 0.25f;
    [Tooltip("是否直接改 sharedMaterials（谨慎）。默认 false：改实例材质。")]
    public bool useSharedMaterial = false;

    [Header("Managers (Optional)")]
    public ToolbarManager toolbar;

    // === 可见射线设置 ===
    [Header("Selection Ray Visual")]
    [Tooltip("进入选择模式时显示右手可见射线")]
    public bool showRayInSelection = true;
    [Tooltip("完成一次选择/清除后自动隐藏射线")]
    public bool hideRayAfterPick = true;
    [Tooltip("射线最大可视长度（与射线检测距离一致就好）")]
    public float rayVisualMaxLength = 30f;
    [Tooltip("射线宽度（米）")]
    public float rayWidth = 0.003f;
    [Tooltip("射线起止颜色（起点→终点）")]
    public Color rayStartColor = new(1, 1, 1, 0.9f);
    public Color rayEndColor   = new(1, 1, 1, 0.15f);
    [Tooltip("射线材质（可空，自动创建简单材质）")]
    public Material rayMaterial;

    private GameObject current;
    public GameObject Current => current;

    private struct OriginalMat { public Renderer r; public Material[] mats; }
    private readonly List<OriginalMat> originals = new();

    // 可视射线
    LineRenderer rayLR;
    bool rayVisible = false;

    void Awake()
    {
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
    }

    void Update()
    {
        if (!selectionEnabled || inputBridge == null) { UpdateRayOff(); return; }

        // 每帧更新射线可视（如果开启）
        UpdateRayVisual();

        // 全局取消
        if (inputBridge.CancelPressedThisFrame())
        {
            ClearSelection();
            if (hideRayAfterPick) UpdateRayOff();
            return;
        }

        // 只有按下选择键才进行点选逻辑
        if (!inputBridge.SelectPressedThisFrame()) return;
        if (!inputBridge.RayFromRight(out var ray)) return;

        // ✅ 只检测 selectableMask ∪ groundMask，避免 UI 层阻挡
        int effectiveMask = selectableMask | groundMask;
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, effectiveMask, QueryTriggerInteraction.Ignore))
        {
            int hitLayer = hit.collider.gameObject.layer;

            if (IsInLayerMask(hitLayer, selectableMask))
            {
                var go = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;
                Select(go);
                if (hideRayAfterPick) UpdateRayOff();
            }
            else if (IsInLayerMask(hitLayer, groundMask))
            {
                ClearSelection();
                if (hideRayAfterPick) UpdateRayOff();
            }
        }
        else
        {
            // 没命中（或只命中被排除层）→ 清除
            ClearSelection();
            if (hideRayAfterPick) UpdateRayOff();
        }
    }

    // ===== 模式控制 =====
    public void EnterSelectionMode()
    {
        selectionEnabled = true;
        if (toolbar)
        {
            toolbar.OnToolEnter();
            toolbar.ShowDeleteTopPanel(false);
        }
        if (showRayInSelection) EnsureRayOn();
    }

    public void ExitSelectionMode()
    {
        selectionEnabled = false;
        ClearSelection();
        UpdateRayOff();
    }

    // ===== 选择/清理 =====
    public void Select(GameObject go)
    {
        if (go == current) return;
        ClearSelection();
        current = go;
        ApplyHighlight(current);
    }

    public void ClearSelection()
    {
        if (!current && originals.Count == 0) return;

        foreach (var e in originals)
        {
            if (!e.r) continue;
            if (useSharedMaterial) e.r.sharedMaterials = e.mats;
            else e.r.materials = Clone(e.mats);
        }
        originals.Clear();
        current = null;
    }

    // ===== 高亮 =====
    void ApplyHighlight(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(false))
        {
            if (!r) continue;

            originals.Add(new OriginalMat
            {
                r = r,
                mats = useSharedMaterial ? r.sharedMaterials : Clone(r.materials)
            });

            var mats = useSharedMaterial ? r.sharedMaterials : r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i]; if (!m) continue;

                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", highlightColor);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", highlightColor);

                if (highlightEmission > 0f && m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", highlightColor * highlightEmission);
                }
            }
            if (!useSharedMaterial) r.materials = mats; else r.sharedMaterials = mats;
        }
    }

    // ===== 可视射线（LineRenderer）=====
    void EnsureRayOn()
    {
        if (rayLR == null)
        {
            // 创建一个“跟随右手”的对象挂 LineRenderer
            var go = new GameObject("[Selection] RightHand Ray");
            go.transform.SetParent(transform, false); // 归到管理器下，不跟随缩放
            rayLR = go.AddComponent<LineRenderer>();
            rayLR.positionCount = 2;
            rayLR.useWorldSpace = true;
            rayLR.startWidth = rayWidth;
            rayLR.endWidth   = rayWidth;

            // 材质
            if (!rayMaterial)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Unlit/Color") ??
                             Shader.Find("Sprites/Default") ??
                             Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                rayMaterial = new Material(shader);
                if (rayMaterial.HasProperty("_Color")) rayMaterial.color = Color.white;
            }
            rayLR.material = rayMaterial;

            // 颜色渐变
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(rayStartColor, 0f), new GradientColorKey(rayEndColor, 1f) },
                new[] { new GradientAlphaKey(rayStartColor.a, 0f), new GradientAlphaKey(rayEndColor.a, 1f) }
            );
            rayLR.colorGradient = grad;
        }
        rayVisible = true;
        rayLR.enabled = true;
    }

    void UpdateRayOff()
    {
        if (rayLR != null) rayLR.enabled = false;
        rayVisible = false;
    }

    void UpdateRayVisual()
    {
        if (!showRayInSelection || !rayVisible) return;
        if (inputBridge == null || inputBridge.rightHand == null) return;

        // 起点
        Vector3 start = inputBridge.rightHand.position;
        Vector3 end   = start + inputBridge.rightHand.forward * rayVisualMaxLength;

        // 为了可视化反馈更直观，这里尝试一次射线（不限制层），若命中则把末端放到命中点
        if (Physics.Raycast(start, inputBridge.rightHand.forward, out RaycastHit hit, rayVisualMaxLength, ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
        }

        rayLR.SetPosition(0, start);
        rayLR.SetPosition(1, end);
    }

    // ===== 杂项 =====
    Material[] Clone(Material[] src)
    {
        var arr = new Material[src.Length];
        for (int i = 0; i < src.Length; i++) arr[i] = src[i] ? new Material(src[i]) : null;
        return arr;
    }

    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    // 仅为兼容旧逻辑保留；物理选择不依赖它
    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
