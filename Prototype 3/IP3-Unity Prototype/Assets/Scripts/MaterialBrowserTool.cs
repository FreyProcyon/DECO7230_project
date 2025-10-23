using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialBrowserTool : MonoBehaviour
{
    [Header("Deps (Required)")]
    public XRInputBridge inputBridge;
    public SelectionManager selectionManager;
    public ToolbarManager toolbar;

    [Header("UI (Top Panel)")]
    [Tooltip("材质浏览面板：只在浏览阶段显示")]
    public GameObject materialPanel;
    [Tooltip("等待选择提示面板：只在选择阶段显示（可选）")]
    public GameObject selectHintPanel;
    public TextMeshProUGUI label;   // 显示当前材质名
    public Image iconImage;         // 显示当前材质图标（可选）

    [Header("Material Library")]
    public Material[] materialOptions;  // 候选材质
    public Sprite[]   materialIcons;    // 与材质对齐的图标（可空）

    [Header("Stick Browse")]
    public float stickThreshold = 0.5f;
    public float repeatInterval = 0.25f;

    [Header("Outline (no color change)")]
    public Color outlineColor = new(1, 1, 1, 0.9f);
    public float outlineWidth = 0.004f;

    private enum State { Idle, ArmingSelect, Browsing }
    private State state = State.Idle;

    // 运行时
    private GameObject target;
    private readonly Dictionary<Renderer, Material[]> originals = new();
    private int currentIndex = 0;

    // 摇杆重复控制
    private float holdLeftT = 0f, holdRightT = 0f;
    private bool leftActive = false, rightActive = false;

    // 描边
    private BoundsOutline outline;

    void Awake()
    {
        if (!selectionManager) selectionManager = FindObjectOfType<SelectionManager>();
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
        if (materialPanel)   materialPanel.SetActive(false);
        if (selectHintPanel) selectHintPanel.SetActive(false);
    }

    void Update()
    {
        switch (state)
        {
            case State.ArmingSelect:
                UpdateArming();
                break;
            case State.Browsing:
                UpdateBrowsing();
                break;
        }
    }

    // =============== UI按钮入口（绑定到“Material”） ===============
    public void StartMaterialFlow()
    {
        if (materialOptions == null || materialOptions.Length == 0)
        {
            Debug.LogWarning("MaterialBrowserTool: No material options assigned.");
            return;
        }

        // 1) 工具进入：严格使用 ToolbarManager 的标准入口——关闭折叠面板
        if (toolbar) toolbar.OnToolEnter();

        // 2) 如果已经有选中 → 直接进入浏览；否则进入选择模式
        var cur = selectionManager ? selectionManager.Current : null;
        if (cur)
        {
            // 先退出选择模式（会清掉 SelectionManager 的高亮与射线），再以该目标进入浏览
            selectionManager.ExitSelectionMode();
            BeginBrowsing(cur);
        }
        else
        {
            BeginArming(); // 进入选择阶段（显示射线由 SelectionManager 内部负责）
        }
    }

    // =============== 阶段1：等待选择（使用 SelectionManager 标准接口） ===============
    void BeginArming()
    {
        state = State.ArmingSelect;

        if (materialPanel)   materialPanel.SetActive(false);
        if (selectHintPanel) selectHintPanel.SetActive(true);

        // 用 SelectionManager 的公开接口进入选择模式（会显示右手可视射线）
        selectionManager?.EnterSelectionMode();
        // 注：若你在 SelectionManager 勾了 showRayInSelection=true，射线会立刻出现
    }

    void UpdateArming()
    {
        if (inputBridge && inputBridge.CancelPressedThisFrame())
        {
            ExitAll();
            return;
        }

        // 一旦被选中 → 立刻转浏览阶段
        var cur = selectionManager ? selectionManager.Current : null;
        if (cur)
        {
            // 先退出选择模式（清理其高亮与射线），避免与本工具的预览/描边冲突
            selectionManager.ExitSelectionMode();
            BeginBrowsing(cur);
        }
    }

    // =============== 阶段2：浏览/预览/确认 ===============
    void BeginBrowsing(GameObject selected)
    {
        target = selected;
        state  = State.Browsing;

        if (selectHintPanel) selectHintPanel.SetActive(false);
        if (materialPanel)   materialPanel.SetActive(true);

        // 记录原材质（以便 Cancel 还原）
        originals.Clear();
        foreach (var r in target.GetComponentsInChildren<Renderer>(false))
        {
            if (!r) continue;
            if (!originals.ContainsKey(r))
                originals[r] = r.sharedMaterials; // 记录共享材质引用，快速还原
        }

        // 描边（不改物体颜色）
        if (!outline) outline = BoundsOutline.AttachTo(target, outlineColor, outlineWidth);
        else outline.Bind(target, outlineColor, outlineWidth);

        currentIndex = Mathf.Clamp(currentIndex, 0, materialOptions.Length - 1);
        ApplyPreview(currentIndex);

        ResetStickRepeat();
        UpdateLabelAndIcon();
    }

    void UpdateBrowsing()
    {
        if (!inputBridge) return;

        // Cancel：还原并退出整个流程
        if (inputBridge.CancelPressedThisFrame())
        {
            RevertPreview();
            ExitAll();
            return;
        }

        // 右摇杆左右切换（支持长按重复）
        Vector2 s = inputBridge.RightStick();
        float dt = Time.unscaledDeltaTime;

        if (s.x <= -stickThreshold)
        {
            if (!leftActive || holdLeftT <= 0f)
            {
                Step(-1);
                leftActive = true;
                holdLeftT  = repeatInterval;
            }
        }
        else leftActive = false;

        if (s.x >= stickThreshold)
        {
            if (!rightActive || holdRightT <= 0f)
            {
                Step(+1);
                rightActive = true;
                holdRightT  = repeatInterval;
            }
        }
        else rightActive = false;

        if (leftActive)  holdLeftT  -= dt;
        if (rightActive) holdRightT -= dt;

        // Confirm：保留预览并退出
        if (inputBridge.ConfirmPressedThisFrame())
        {
            ConfirmAndExit();
        }
    }

    // =============== 预览/确认/还原 ===============
    void Step(int dir)
    {
        if (materialOptions.Length == 0) return;
        currentIndex = (currentIndex + dir + materialOptions.Length) % materialOptions.Length;
        ApplyPreview(currentIndex);
        UpdateLabelAndIcon();
    }

    // ====== 工具：拿到真实 submesh 数（兼容 MeshRenderer / SkinnedMeshRenderer）======
int GetRealSubmeshCount(Renderer r)
{
    if (r is SkinnedMeshRenderer smr && smr.sharedMesh)
        return smr.sharedMesh.subMeshCount;

    var mf = r.GetComponent<MeshFilter>();
    if (mf && mf.sharedMesh) 
        return mf.sharedMesh.subMeshCount;

    // 兜底：没有网格时就用已有材质槽数量
    var mats = r.sharedMaterials;
    return (mats != null && mats.Length > 0) ? mats.Length : 1;
}

// ====== 套预览材质（覆盖所有 submesh）======
void ApplyPreview(int index)
{
    var m = materialOptions[Mathf.Clamp(index, 0, materialOptions.Length - 1)];
    if (!target || !m) return;

    foreach (var r in target.GetComponentsInChildren<Renderer>(false))
    {
        if (!r) continue;

        int subCount = GetRealSubmeshCount(r);

        // 准备正确长度的数组；避免只改到部分槽位
        var arr = r.sharedMaterials;
        if (arr == null || arr.Length != subCount)
            arr = new Material[subCount];

        for (int i = 0; i < subCount; i++)
            arr[i] = m;

        r.sharedMaterials = arr;
    }
}


    void RevertPreview()
    {
        if (!target) return;
        foreach (var r in target.GetComponentsInChildren<Renderer>(false))
        {
            if (!r) continue;
            if (originals.TryGetValue(r, out var mats))
                r.sharedMaterials = mats;
        }
        originals.Clear();
    }

    void ConfirmAndExit()
    {
        originals.Clear(); // 预览结果生效
        ExitAll();
    }

    void ExitAll()
    {
        state = State.Idle;

        if (materialPanel)   materialPanel.SetActive(false);
        if (selectHintPanel) selectHintPanel.SetActive(false);

        if (outline) outline.UnbindOrDisable();

        // 工具退出：按你的接口调用 ToolbarManager 的退出
        if (toolbar) toolbar.OnToolExit();

        // 确保选择器处于干净状态（不保留选择/射线）
        selectionManager?.ExitSelectionMode();

        target = null;
        ResetStickRepeat();
    }

    void ResetStickRepeat()
    {
        holdLeftT = holdRightT = 0f;
        leftActive = rightActive = false;
    }

    void UpdateLabelAndIcon()
    {
        if (label)
        {
            string matName = (materialOptions != null && materialOptions.Length > 0 &&
                              currentIndex >= 0 && currentIndex < materialOptions.Length)
                ? materialOptions[currentIndex].name : "-";
            label.text = $"Material: {matName} ({currentIndex+1}/{(materialOptions!=null?materialOptions.Length:0)})";
        }

        if (iconImage)
        {
            Sprite icon = null;

            // 1) 优先用手工图标
            if (materialIcons != null && currentIndex >= 0 && currentIndex < materialIcons.Length)
                icon = materialIcons[currentIndex];

            // 2) 没图标就尝试从材质纹理生成
            if (!icon && materialOptions != null && currentIndex >= 0 && currentIndex < materialOptions.Length)
            {
                var mat = materialOptions[currentIndex];
                if (mat)
                {
                    Texture2D tex = null;
                    if (mat.HasProperty("_BaseMap"))  tex = mat.GetTexture("_BaseMap") as Texture2D;
                    if (!tex && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex") as Texture2D;

                    if (tex)
                    {
                        icon = Sprite.Create(tex,
                            new Rect(0,0,tex.width,tex.height),
                            new Vector2(0.5f,0.5f), 100f);
                    }
                }
            }

            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }
    }
}

// ==============================
// BoundsOutline：LineRenderer画包围盒（不改物体材质）
// ==============================
public class BoundsOutline : MonoBehaviour
{
    const int EDGE_COUNT = 12;
    LineRenderer[] edges;
    Transform target;
    Color color = Color.white;
    float width = 0.004f;
    Material lineMat;

    public static BoundsOutline AttachTo(GameObject target, Color c, float w)
    {
        var host = new GameObject("[Outline] Bounds").AddComponent<BoundsOutline>();
        host.Bind(target, c, w);
        return host;
    }

    public void Bind(GameObject newTarget, Color c, float w)
    {
        target = newTarget ? newTarget.transform : null;
        color = c; width = w;

        if (edges == null || edges.Length != EDGE_COUNT)
        {
            edges = new LineRenderer[EDGE_COUNT];
            for (int i = 0; i < EDGE_COUNT; i++)
            {
                var go = new GameObject($"edge_{i}");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.startWidth = width;
                lr.endWidth   = width;
                lr.numCapVertices = 2;

                if (lineMat == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                 Shader.Find("Unlit/Color") ??
                                 Shader.Find("Sprites/Default") ??
                                 Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                    lineMat = new Material(shader);
                    if (lineMat.HasProperty("_Color")) lineMat.color = color;
                }
                lr.material = lineMat;
                if (lr.material.HasProperty("_Color")) lr.material.color = color;

                edges[i] = lr;
            }
        }
        else
        {
            foreach (var lr in edges)
            {
                if (!lr) continue;
                lr.startWidth = width; lr.endWidth = width;
                if (lr.material && lr.material.HasProperty("_Color")) lr.material.color = color;
            }
        }
        enabled = target != null;
    }

    public void UnbindOrDisable()
    {
        enabled = false;
        if (edges != null)
            foreach (var e in edges) if (e) e.enabled = false;
    }

    void LateUpdate()
    {
        if (!target) { UnbindOrDisable(); return; }

        var rends = target.GetComponentsInChildren<Renderer>(false);
        if (rends.Length == 0) { UnbindOrDisable(); return; }

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        Vector3 c = b.center;
        Vector3 e = b.extents;
        Vector3[] p = new Vector3[8];
        p[0] = c + new Vector3(-e.x, -e.y, -e.z);
        p[1] = c + new Vector3(+e.x, -e.y, -e.z);
        p[2] = c + new Vector3(+e.x, -e.y, +e.z);
        p[3] = c + new Vector3(-e.x, -e.y, +e.z);
        p[4] = c + new Vector3(-e.x, +e.y, -e.z);
        p[5] = c + new Vector3(+e.x, +e.y, -e.z);
        p[6] = c + new Vector3(+e.x, +e.y, +e.z);
        p[7] = c + new Vector3(-e.x, +e.y, +e.z);

        int[,] edge = new int[,]
        {
            {0,1},{1,2},{2,3},{3,0}, // 下
            {4,5},{5,6},{6,7},{7,4}, // 上
            {0,4},{1,5},{2,6},{3,7}  // 竖
        };

        for (int i = 0; i < EDGE_COUNT; i++)
        {
            var lr = edges[i];
            if (!lr) continue;
            lr.enabled = true;
            lr.SetPosition(0, p[edge[i,0]]);
            lr.SetPosition(1, p[edge[i,1]]);
        }
    }
}
