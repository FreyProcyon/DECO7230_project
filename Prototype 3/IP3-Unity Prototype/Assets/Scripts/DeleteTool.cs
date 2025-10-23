using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeleteTool : MonoBehaviour
{
    [Header("XR Input (Required)")] 
    public XRInputBridge inputBridge;

    [Header("UI Top Panel")] 
    public GameObject topPanel; 
    public Button deleteButton; 
    public Button cancelButton;

    [Header("Colors")] 
    public Color deleteColor = Color.red; 
    [Range(0f,2f)] public float emissionStrength = 0.5f;

    [Header("Managers")] 
    public ToolbarManager toolbar;

    [Header("Raycast")] 
    [Tooltip("只勾可删除对象的层（不要勾 UI）")]
    public LayerMask selectableMask = ~0; 
    public float maxRayDistance = 30f;

    // ==== 可见射线设置（与 SelectionManager 一致风格） ====
    [Header("Delete Ray Visual")]
    [Tooltip("进入删除模式时显示右手可见射线")]
    public bool showRayInDelete = true;
    [Tooltip("完成一次点选/反选后是否自动隐藏射线")]
    public bool hideRayAfterPick = false; // 删除模式通常持续选择，默认不隐藏
    [Tooltip("射线最大可视长度")]
    public float rayVisualMaxLength = 30f;
    [Tooltip("射线宽度（米）")]
    public float rayWidth = 0.003f;
    [Tooltip("射线起止颜色")]
    public Color rayStartColor = new(1, 1, 1, 0.9f);
    public Color rayEndColor   = new(1, 1, 1, 0.15f);
    [Tooltip("射线材质（可空，自动创建简单材质）")]
    public Material rayMaterial;

    private readonly HashSet<GameObject> marked = new(); 
    private readonly Dictionary<Renderer, Material[]> originals = new();

    bool deleting = false;

    // 可视射线
    LineRenderer rayLR;
    bool rayVisible = false;

    void Awake()
    {
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
        if (deleteButton) deleteButton.onClick.AddListener(DeleteSelected);
        if (cancelButton) cancelButton.onClick.AddListener(CancelDelete);
        if (topPanel) topPanel.SetActive(false);
    }

    void Update()
    {
        if (!deleting || !inputBridge)
        {
            UpdateRayOff();
            return;
        }

        // 每帧更新射线可视（如果开启）
        UpdateRayVisual();

        // XR 全局取消
        if (inputBridge.CancelPressedThisFrame())
        {
            CancelDelete();
            return;
        }

        // 触发键（右手）进行“标红/取消标红”
        if (inputBridge.SelectPressedThisFrame() && !IsPointerOverUI())
        {
            if (inputBridge.RayFromRight(out var ray) &&
                Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, selectableMask, QueryTriggerInteraction.Ignore))
            {
                var go = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;

                if (marked.Contains(go)) { Restore(go); marked.Remove(go); }
                else                     { Mark(go);    marked.Add(go); }

                if (hideRayAfterPick) UpdateRayOff();
            }
        }
    }

    public void StartDeleteMode()
    {
        if (toolbar){ toolbar.OnToolEnter(); toolbar.ShowDeleteTopPanel(true); }
        if (topPanel) topPanel.SetActive(true);

        deleting = true; 
        marked.Clear(); 
        originals.Clear();

        if (showRayInDelete) EnsureRayOn();
    }

    void DeleteSelected()
    {
        foreach (var go in marked) if (go) RemoveOriginalsFor(go);
        foreach (var go in marked) if (go) Destroy(go);
        marked.Clear(); originals.Clear();
        Exit();
    }

    void CancelDelete()
    {
        foreach (var go in marked) Restore(go);
        marked.Clear(); originals.Clear();
        Exit();
    }

    void Exit()
    {
        deleting = false;
        if (toolbar) toolbar.ShowDeleteTopPanel(false);
        if (topPanel) topPanel.SetActive(false);
        UpdateRayOff();
    }

    // ====== 高亮控制 ======
    void Mark(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(false))
        {
            if (!r) continue;
            if (!originals.ContainsKey(r)) originals[r] = r.materials;

            var src = r.materials; 
            var arr = new Material[src.Length];
            for (int i=0;i<src.Length;i++)
            {
                var m = new Material(src[i]);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", deleteColor);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", deleteColor);

                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", deleteColor * emissionStrength);
                }
                arr[i] = m;
            }
            r.materials = arr;
        }
    }

    void Restore(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(false))
            if (r && originals.TryGetValue(r, out var mats)) { r.materials = mats; originals.Remove(r); }
    }

    void RemoveOriginalsFor(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(false))
            if (r && originals.ContainsKey(r)) originals.Remove(r);
    }

    // ====== 可视射线（LineRenderer）======
    void EnsureRayOn()
    {
        if (rayLR == null)
        {
            var go = new GameObject("[Delete] RightHand Ray");
            go.transform.SetParent(transform, false);
            rayLR = go.AddComponent<LineRenderer>();
            rayLR.positionCount = 2;
            rayLR.useWorldSpace = true;
            rayLR.startWidth = rayWidth;
            rayLR.endWidth   = rayWidth;

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
        if (!showRayInDelete || !rayVisible) return;
        if (inputBridge == null || inputBridge.rightHand == null) return;

        Vector3 start = inputBridge.rightHand.position;
        Vector3 end   = start + inputBridge.rightHand.forward * rayVisualMaxLength;

        // 为视觉友好：不限制层做一次命中，末端放到命中点
        if (Physics.Raycast(start, inputBridge.rightHand.forward, out RaycastHit hit, rayVisualMaxLength, ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
        }

        rayLR.SetPosition(0, start);
        rayLR.SetPosition(1, end);
    }

    // ====== 杂项 ======
    bool IsPointerOverUI(){ return EventSystem.current && EventSystem.current.IsPointerOverGameObject(); }
}
