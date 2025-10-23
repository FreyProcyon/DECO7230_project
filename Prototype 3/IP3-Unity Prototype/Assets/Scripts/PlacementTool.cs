using UnityEngine;

public class PlacementTool : MonoBehaviour
{
    [Header("XR Input (Required)")] public XRInputBridge inputBridge;

    [Header("Masks")]
    [Tooltip("仅 Ground 层，用于识别地面")]
    public LayerMask groundMask;                 // 仅 Ground
    [Tooltip("Ground + 可堆叠对象层（如 Default/Placeable），用于一次射线最近命中")]
    public LayerMask placementMask;              // Ground + 可堆叠对象层

    [Header("Grid")]
    public bool  snapToGrid   = true;
    public float gridSize     = 0.2f;
    [Tooltip("除 Plane 外的通用微抬高度，避免与表面粘连")]
    public float hoverOffset  = 0.01f;

    [Header("Preview")]
    public Material ghostMaterial;
    public Color    ghostColor = new(1,0,0,0.5f);

    [Header("Defaults")]
    public Transform parentContainer;
    public Vector3   defaultSize = Vector3.one;
    [Tooltip("Unity 内置 Plane 以 10 为边长基准，这里是目标边长")]
    public float     planeSize   = 2f;      // Unity Plane 基于10

    [Header("Stick Scale (右摇杆左右=缩放)")]
    [Tooltip("缩放速度系数")]
    public float scaleSpeed = 0.8f;
    [Tooltip("缩放死区（避免手柄小抖动）")]
    public float stickDeadzone = 0.2f;
    [Tooltip("是否反向（默认：左小右大；若勾选则左大右小）")]
    public bool invertScale = false;
    [Tooltip("统一缩放下限/上限")]
    public float minUniformScale = 0.1f;
    public float maxUniformScale = 5f;

    [Header("Panels (Optional)")]
    public ToolbarManager toolbar;

    [Header("Plane Tweak (anti z-fighting)")]
    [Tooltip("Plane 放置到任意表面时的额外抬升高度")]
    public float planeLift = 0.01f;
    [Tooltip("计算堆叠高度时，Plane 视作至少这么厚的一半（避免与顶面 z-fight）")]
    public float planeMinHalfThickness = 0.005f;

    enum Shape { None, Cube, Sphere, Cylinder, Plane, Capsule }  // 已含 Capsule
    Shape currentShape = Shape.None;

    GameObject preview;
    Renderer  previewRenderer;
    bool      inPlacement = false;

    void Awake()
    {
        if (!ghostMaterial)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Sprites/Default")
                  ?? Shader.Find("Standard");
            ghostMaterial = new Material(sh);
            if (ghostMaterial.HasProperty("_Color")) ghostMaterial.color = ghostColor;
        }
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
    }

    // ===== UI 入口（把按钮 OnClick 绑定到这些方法） =====
    public void StartPlaceCube()     => StartPlacement(Shape.Cube);
    public void StartPlaceSphere()   => StartPlacement(Shape.Sphere);
    public void StartPlaceCylinder() => StartPlacement(Shape.Cylinder);
    public void StartPlacePlane()    => StartPlacement(Shape.Plane);
    public void StartPlaceCapsule()  => StartPlacement(Shape.Capsule);

    void StartPlacement(Shape shape)
    {
        CancelPlacement();
        if (toolbar){ toolbar.OnToolEnter(); toolbar.ShowDeleteTopPanel(false); }

        currentShape = shape;
        inPlacement  = true;

        preview = CreatePrimitive(shape, true);
        previewRenderer = preview.GetComponentInChildren<Renderer>();
        foreach (var c in preview.GetComponentsInChildren<Collider>()) c.enabled = false;

        // 预览材质（半透明）立即生效
        if (previewRenderer)
        {
            var mats = previewRenderer.sharedMaterials;
            for (int i=0;i<mats.Length;i++) mats[i] = ghostMaterial;
            previewRenderer.sharedMaterials = mats;
        }
        preview.SetActive(true);
    }

    void Update()
    {
        if (!inPlacement || currentShape==Shape.None || inputBridge==null) return;

        Vector3 pos;
        bool hasPos = false;

        // 一次射线取最近命中（placementMask 必须含 Ground+可堆叠对象）
        if (inputBridge.RayFromRight(out var ray) &&
            Physics.Raycast(ray, out RaycastHit hit, 1000f, placementMask, QueryTriggerInteraction.Ignore))
        {
            int layer = hit.collider.gameObject.layer;

            if (IsInLayerMask(layer, groundMask))
                pos = GetBottomAlignedPosition(hit, previewRenderer, preview.transform, currentShape, hoverOffset, planeLift, planeMinHalfThickness);
            else
                pos = GetStackTopPosition(hit, previewRenderer, preview.transform, currentShape, hoverOffset, planeLift, planeMinHalfThickness);

            if (snapToGrid) pos = SnapXZ(pos, gridSize);

            ApplyPreviewPose(pos);
            hasPos = true;
        }
        else
        {
            if (preview) preview.SetActive(false);
        }

        // Confirm 放置
        if (hasPos && inputBridge.ConfirmPressedThisFrame() && preview && preview.activeSelf)
        {
            PlaceAt(preview.transform.position, preview.transform.localScale);
            return;
        }

        // —— 右摇杆“左右”缩放：左<0=变小；右>0=变大 —— //
        Vector2 stick = inputBridge.RightStick();
        float v = stick.x;                 // 水平轴
        if (invertScale) v = -v;           // 可选反向
        if (Mathf.Abs(v) > stickDeadzone && preview)
        {
            float k = 1f + v * scaleSpeed * Time.deltaTime;
            float uni = Mathf.Clamp(preview.transform.localScale.x * k, minUniformScale, maxUniformScale);
            preview.transform.localScale = new Vector3(uni, uni, uni);
        }

        // Cancel 退出
        if (inputBridge.CancelPressedThisFrame()) CancelPlacement();
    }

    void ApplyPreviewPose(Vector3 pos)
    {
        if (!preview) return;
        preview.SetActive(true);
        preview.transform.position = pos;
        preview.transform.rotation = Quaternion.identity;
    }

    // —— 底面对齐（命中地面）——
    static Vector3 GetBottomAlignedPosition(RaycastHit hit, Renderer rend, Transform t,
        Shape shape, float hover, float planeLift, float planeHalfMin)
    {
        Vector3 groundPoint = hit.point;

        float extraLift = hover;
        if (shape == Shape.Plane)
            extraLift += planeLift; // Plane 额外抬升，避免与地面 z-fight

        if (!rend)
            return groundPoint + hit.normal * extraLift;

        Bounds lb = rend.localBounds;
        Vector3 s = t.lossyScale;
        float halfH = Mathf.Abs(lb.extents.y * s.y);

        // Plane 太薄时给它一个最小厚度
        if (shape == Shape.Plane)
            halfH = Mathf.Max(halfH, planeHalfMin);

        return groundPoint + hit.normal * (halfH + extraLift);
    }

    // —— 顶面对齐（堆叠在命中对象上方）——
    static Vector3 GetStackTopPosition(RaycastHit hit, Renderer newR, Transform newT,
        Shape shape, float hover, float planeLift, float planeHalfMin)
    {
        float topY = hit.collider.GetComponentInParent<Renderer>()?.bounds.max.y ?? hit.collider.bounds.max.y;

        float halfH = 0f;
        if (newR)
        {
            var lb = newR.localBounds;
            halfH = Mathf.Abs(lb.extents.y * newT.lossyScale.y);
        }
        if (shape == Shape.Plane)
            halfH = Mathf.Max(halfH, planeHalfMin); // 给 Plane 一个最小“厚度”

        // Plane 叠在物体上，也给一点额外抬升
        float extraLift = hover + (shape == Shape.Plane ? planeLift : 0f);

        Vector3 p = hit.point;
        p.y = Mathf.Max(topY + halfH + extraLift, p.y);
        return p;
    }

    void PlaceAt(Vector3 pos, Vector3 scale)
    {
        var placed = CreatePrimitive(currentShape, false);
        placed.transform.position = pos;
        placed.transform.rotation = Quaternion.identity;
        placed.transform.localScale = scale;
        if (parentContainer) placed.transform.SetParent(parentContainer, true);
        CancelPlacement();
    }

    void CancelPlacement()
    {
        inPlacement = false;
        currentShape = Shape.None;
        if (preview) Destroy(preview);
        preview = null; previewRenderer = null;
    }

    GameObject CreatePrimitive(Shape shape, bool isPreview)
    {
        GameObject go = shape switch {
            Shape.Cube     => GameObject.CreatePrimitive(PrimitiveType.Cube),
            Shape.Sphere   => GameObject.CreatePrimitive(PrimitiveType.Sphere),
            Shape.Cylinder => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            Shape.Plane    => GameObject.CreatePrimitive(PrimitiveType.Plane),
            Shape.Capsule  => GameObject.CreatePrimitive(PrimitiveType.Capsule),
            _ => new GameObject("Unknown")
        };

        // 初始缩放
        if (shape == Shape.Plane)
        {
            // Unity Plane 缩放以 10 为基
            float s = Mathf.Max(0.01f, planeSize / 10f);
            go.transform.localScale = new Vector3(s, 1f, s);
        }
        else
        {
            go.transform.localScale = defaultSize;
        }

        if (isPreview)
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;

        return go;
    }

    // —— 仅 XZ 吸附，保持正确的 Y —— //
    static Vector3 SnapXZ(Vector3 v, float step)
    {
        if (step<=0f) return v;
        return new Vector3(
            Mathf.Round(v.x/step)*step,
            v.y,
            Mathf.Round(v.z/step)*step
        );
    }

    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}
