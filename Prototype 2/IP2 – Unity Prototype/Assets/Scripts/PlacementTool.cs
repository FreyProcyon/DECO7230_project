using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementTool : MonoBehaviour
{
    [Header("Ground & Snapping")]
    public LayerMask groundMask;               // 勾 Ground
    public LayerMask placementMask;
    public bool snapToGrid = true;
    public float gridSize = 0.2f;
    public float hoverOffset = 0.01f;          // 轻微抬起避免 z-fighting

    [Header("Preview Style")]
    public Material ghostMaterial;             // 半透明材质（可空，自动生成）
    public Color ghostColor = new Color(1, 0, 0, 0.5f);

    [Header("Defaults")]
    public Transform parentContainer;          // 放置后的父物体（可空）
    public Vector3 defaultSize = Vector3.one;  // 初始缩放（非 Plane）
    public float planeSize = 2f;               // Plane 的目标边长（Unity Plane 基于10）

    [Header("Scroll Scale")]
    public float scrollScaleStep = 0.1f;       // 每档缩放比例（滚轮）
    public float minUniformScale = 0.1f;
    public float maxUniformScale = 5f;

    [Header("Panels (Optional)")]
    public ToolbarManager toolbar;             // 进入放置模式时自动关闭面板（可空）

    enum Shape { None, Cube, Sphere, Cylinder, Plane }
    Shape currentShape = Shape.None;

    Camera cam;
    GameObject preview;        // 预览体（ghost 实例）
    Renderer previewRenderer;  // 用于计算底面对齐
    bool inPlacement = false;
    bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }


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
        }

        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
    }

    // ===== 按钮入口 =====
    public void StartPlaceCube() => StartPlacement(Shape.Cube);
    public void StartPlaceSphere() => StartPlacement(Shape.Sphere);
    public void StartPlaceCylinder() => StartPlacement(Shape.Cylinder);
    public void StartPlacePlane() => StartPlacement(Shape.Plane);

    void StartPlacement(Shape shape)
    {
        CancelPlacement();      // 清理旧的
        if (toolbar) toolbar.CloseAll(); // 自动关闭 UI 面板

        currentShape = shape;
        inPlacement = true;

        // 创建预览体（ghost）
        preview = CreatePrimitive(shape, isPreview: true);
        previewRenderer = preview.GetComponentInChildren<Renderer>();

        // 预览体禁用碰撞/设置半透明材质
        foreach (var col in preview.GetComponentsInChildren<Collider>()) col.enabled = false;
        if (previewRenderer)
        {
            var mats = previewRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
            previewRenderer.sharedMaterials = mats;
        }
    }

    void Update()
    {
        if (!inPlacement || currentShape == Shape.None || cam == null) return;

        // 鼠标滚轮缩放（忽略指向 UI 的滚轮）
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f && !IsPointerOverUI())
        {
            float factor = 1f + scroll * scrollScaleStep;          // e.g. 每格 ±10%
            Vector3 s = preview.transform.localScale * factor;

            // 统一缩放，限制范围
            float uni = Mathf.Clamp(s.x, minUniformScale, maxUniformScale);
            preview.transform.localScale = new Vector3(uni, uni, uni);
        }

        // 地面射线检测
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementMask))
        {
            Vector3 pos;

            // 命中地面？→ 底面对齐
            if (IsInLayerMask(hit.collider.gameObject.layer, groundMask))
            {
                pos = GetBottomAlignedPosition(hit, previewRenderer, preview.transform);
            }
            else
            {
                // 命中的是已有物体 → 顶面对齐（堆叠）
                pos = GetStackTopPosition(hit, previewRenderer, preview.transform);
            }

            if (snapToGrid) pos = Snap(pos, gridSize);

            preview.SetActive(true);
            preview.transform.position = pos;
            preview.transform.rotation = Quaternion.identity;

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                PlaceAt(pos, preview.transform.localScale);
            }
        }
        else
        {
            preview.SetActive(false);
        }


        // 右键 / Esc 取消
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    // 计算“底面对齐”的落点
    Vector3 GetBottomAlignedPosition(RaycastHit hit, Renderer rend, Transform t)
    {
        Vector3 groundPoint = hit.point;

        // 没有 Renderer（极少见）就用简单偏移
        if (!rend)
            return groundPoint + hit.normal * hoverOffset;

        // 使用共享网格的本地包围盒加上 lossyScale 计算半高
        // 注意：Renderer.bounds 是世界包围盒，受旋转影响；我们保持 rotation 水平，所以两者都可
        Bounds localBounds = rend.localBounds; // Unity 2021+ 支持；若版本不支持，用 sharedMesh.bounds
        Vector3 scale = t.lossyScale;
        float halfHeight = Mathf.Abs(localBounds.extents.y * scale.y);

        // 在法线方向抬起半高 + hoverOffset（让“底面贴地”）
        return groundPoint + hit.normal * (halfHeight + hoverOffset);
    }
    // 顶面对齐：把新物体“底面”放在被命中物体的“顶部”上方 hoverOffset
    Vector3 GetStackTopPosition(RaycastHit hit, Renderer newObjRenderer, Transform newObj)
    {
        // 被命中的旧物体的世界包围盒
        float topY = float.NaN;

        var hitRend = hit.collider.GetComponentInParent<Renderer>();
        if (hitRend != null)
        {
            topY = hitRend.bounds.max.y;  // 旧物体顶部（世界坐标）
        }
        else
        {
            // 没有 Renderer（少见，只有 Collider）→ 用 collider.bounds 兜底
            topY = hit.collider.bounds.max.y;
        }

        // 新物体的“半高”用于底面对齐
        float halfHeight = 0.0f;
        if (newObjRenderer != null)
        {
            // 用 localBounds * lossyScale 估算半高（rotation 保持水平时足够精确）
            var lb = newObjRenderer.localBounds;
            halfHeight = Mathf.Abs(lb.extents.y * newObj.lossyScale.y);
        }

        // 期望的水平位置：用命中点的 xz（对齐表面投影），y 放在“旧物体顶面 + 新物体半高 + hoverOffset”
        Vector3 pos = hit.point; // 水平位置更贴近命中处
        pos.y = Mathf.Max(topY + halfHeight + hoverOffset, pos.y); // 确保不低于顶面

        return pos;
    }


    void PlaceAt(Vector3 pos, Vector3 scale)
    {
        // 真正的实体
        var placed = CreatePrimitive(currentShape, isPreview: false);
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
        GameObject go;
        switch (shape)
        {
            case Shape.Cube: go = GameObject.CreatePrimitive(PrimitiveType.Cube); break;
            case Shape.Sphere: go = GameObject.CreatePrimitive(PrimitiveType.Sphere); break;
            case Shape.Cylinder: go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); break;
            case Shape.Plane: go = GameObject.CreatePrimitive(PrimitiveType.Plane); break;
            default: go = new GameObject("Unknown"); break;
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

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
