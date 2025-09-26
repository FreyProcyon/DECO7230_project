using UnityEngine;
// XR 版放置工具：右手射线预览与放置，摇杆缩放，Confirm 放置，Cancel 取消
public class PlacementTool : MonoBehaviour
{
    [Header("XR Input (Required)")]
    public XRInputBridge inputBridge;          // 拖 Managers 上的 XRInputBridge

    [Header("Ground & Snapping")]
    public LayerMask groundMask;               // Ground 层；命中它 = 底面对齐
    public LayerMask placementMask;            // 射线可命中的层（Ground + 可堆叠对象）
    public bool snapToGrid = true;
    public float gridSize = 0.2f;
    public float hoverOffset = 0.01f;          // 轻微抬起避免 z-fighting

    [Header("Preview Style")]
    public Material ghostMaterial;             // 半透明材质（可空，Awake 自动生成）
    public Color ghostColor = new Color(1, 0, 0, 0.5f);

    [Header("Defaults")]
    public Transform parentContainer;          // 放置后的父物体（可空）
    public Vector3 defaultSize = Vector3.one;  // 初始缩放（非 Plane）
    public float planeSize = 2f;               // Plane 的目标边长（Unity Plane 基于10）

    [Header("Scale by Thumbstick (Y)")]
    public float scaleSpeed = 0.8f;            // 摇杆缩放速度
    public float minUniformScale = 0.1f;
    public float maxUniformScale = 5f;

    [Header("Panels (Optional)")]
    public ToolbarManager toolbar;             // 进入放置模式时自动关闭面板（可空）

    enum Shape { None, Cube, Sphere, Cylinder, Plane }
    Shape currentShape = Shape.None;

    Camera cam;                                // 仅兜底；XR 流程不使用
    GameObject preview;                        // 预览体（ghost 实例）
    Renderer previewRenderer;                  // 用于计算底面对齐
    bool inPlacement = false;

    void Awake()
    {
        cam = Camera.main ?? FindObjectOfType<Camera>();

        if (!ghostMaterial)
        {
            // 自动创建半透明 Unlit 材质
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (!sh) sh = Shader.Find("Unlit/Color");
            if (!sh) sh = Shader.Find("Sprites/Default");
            if (!sh) sh = Shader.Find("Standard");
            ghostMaterial = new Material(sh);
            if (sh.name.Contains("Unlit") || sh.name.Contains("Sprites"))
                ghostMaterial.color = ghostColor;
            else
            {
                // Standard/URP Lit：尽量设透明
                if (ghostMaterial.HasProperty("_Color"))
                    ghostMaterial.color = ghostColor;
                ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
    }

    // ===== 按钮入口（UI 直接绑定） =====
    public void StartPlaceCube()     => StartPlacement(Shape.Cube);
    public void StartPlaceSphere()   => StartPlacement(Shape.Sphere);
    public void StartPlaceCylinder() => StartPlacement(Shape.Cylinder);
    public void StartPlacePlane()    => StartPlacement(Shape.Plane);

    void StartPlacement(Shape shape)
    {
        CancelPlacement();                    // 清理旧的
        if (toolbar) toolbar.CloseAll();      // 自动关闭 UI 面板

        currentShape = shape;
        inPlacement  = true;

        // 创建预览体（ghost）
        preview = CreatePrimitive(shape, isPreview: true);
        previewRenderer = preview.GetComponentInChildren<Renderer>();

        // 预览体禁用碰撞/设置半透明材质
        foreach (var col in preview.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (previewRenderer)
        {
            var mats = previewRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
            previewRenderer.sharedMaterials = mats;
        }
    }

    void Update()
    {
        if (!inPlacement || currentShape == Shape.None) return;
        if (!inputBridge || !inputBridge.rightHand) return;

        // === 1) 右手射线命中位置（地面 or 物体顶部） ===
        if (Physics.Raycast(new Ray(inputBridge.rightHand.position, inputBridge.rightHand.forward),
                            out RaycastHit hit, 1000f, placementMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos;

            // 命中地面？→ 底面对齐
            if (IsInLayerMask(hit.collider.gameObject.layer, groundMask))
                pos = GetBottomAlignedPosition(hit, previewRenderer, preview.transform);
            else
                // 命中的是已有物体 → 顶面对齐（堆叠）
                pos = GetStackTopPosition(hit, previewRenderer, preview.transform);

            if (snapToGrid) pos = Snap(pos, gridSize);

            preview.SetActive(true);
            preview.transform.position = pos;
            preview.transform.rotation = Quaternion.identity;

            // === 2) Confirm：放置 ===
            if (inputBridge.ConfirmPressedThisFrame())
            {
                PlaceAt(pos, preview.transform.localScale);
                return; // 放置后本帧不再处理缩放等
            }
        }
        else
        {
            preview.SetActive(false);
        }

        // === 3) 摇杆缩放（Y 轴） ===
        float y = inputBridge.Stick().y;
        if (Mathf.Abs(y) > 0.2f && preview) // 加阈值避免漂移
        {
            float k = 1f + y * scaleSpeed * Time.deltaTime;
            Vector3 s = preview.transform.localScale * k;

            float uni = Mathf.Clamp(s.x, minUniformScale, maxUniformScale);
            preview.transform.localScale = new Vector3(uni, uni, uni);
        }

        // === 4) Cancel：取消放置模式 ===
        if (inputBridge.CancelPressedThisFrame())
        {
            CancelPlacement();
        }
    }

    // 计算“底面对齐”的落点（命中地面）
    Vector3 GetBottomAlignedPosition(RaycastHit hit, Renderer rend, Transform t)
    {
        Vector3 groundPoint = hit.point;

        if (!rend)
            return groundPoint + hit.normal * hoverOffset;

        Bounds localBounds = rend.localBounds; // Unity 2021+；较早版本可用 sharedMesh.bounds
        Vector3 scale = t.lossyScale;
        float halfHeight = Mathf.Abs(localBounds.extents.y * scale.y);

        return groundPoint + hit.normal * (halfHeight + hoverOffset);
    }

    // 顶面对齐：把新物体“底面”放在被命中物体的“顶部”上方 hoverOffset
    Vector3 GetStackTopPosition(RaycastHit hit, Renderer newObjRenderer, Transform newObj)
    {
        float topY;

        var hitRend = hit.collider.GetComponentInParent<Renderer>();
        if (hitRend != null) topY = hitRend.bounds.max.y;
        else                 topY = hit.collider.bounds.max.y;

        float halfHeight = 0.0f;
        if (newObjRenderer != null)
        {
            var lb = newObjRenderer.localBounds;
            halfHeight = Mathf.Abs(lb.extents.y * newObj.lossyScale.y);
        }

        Vector3 pos = hit.point;
        pos.y = Mathf.Max(topY + halfHeight + hoverOffset, pos.y);
        return pos;
    }

    void PlaceAt(Vector3 pos, Vector3 scale)
    {
        var placed = CreatePrimitive(currentShape, isPreview: false);
        placed.transform.position   = pos;
        placed.transform.rotation   = Quaternion.identity;
        placed.transform.localScale = scale;

        if (parentContainer) placed.transform.SetParent(parentContainer, true);

        CancelPlacement();
    }

    void CancelPlacement()
    {
        inPlacement   = false;
        currentShape  = Shape.None;
        if (preview) Destroy(preview);
        preview = null; previewRenderer = null;
    }

    GameObject CreatePrimitive(Shape shape, bool isPreview)
    {
        GameObject go;
        switch (shape)
        {
            case Shape.Cube:     go = GameObject.CreatePrimitive(PrimitiveType.Cube); break;
            case Shape.Sphere:   go = GameObject.CreatePrimitive(PrimitiveType.Sphere); break;
            case Shape.Cylinder: go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); break;
            case Shape.Plane:    go = GameObject.CreatePrimitive(PrimitiveType.Plane); break;
            default:             go = new GameObject("Unknown"); break;
        }

        // 缩放：Plane 以 10 为基；其他按 defaultSize
        if (shape == Shape.Plane)
        {
            float s = Mathf.Max(0.01f, planeSize / 10f);
            go.transform.localScale = new Vector3(s, 1f, s);
        }
        else
        {
            go.transform.localScale = defaultSize;
        }

        // 预览体禁用碰撞
        if (isPreview)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        return go;
    }

    static Vector3 Snap(Vector3 v, float step)
    {
        if (step <= 0f) return v;
        return new Vector3(
            Mathf.Round(v.x / step) * step,
            Mathf.Round(v.y / step) * step,
            Mathf.Round(v.z / step) * step
        );
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
