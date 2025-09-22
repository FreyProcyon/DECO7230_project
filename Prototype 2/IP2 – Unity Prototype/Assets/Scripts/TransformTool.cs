using UnityEngine;
using UnityEngine.EventSystems;

public class TransformTool : MonoBehaviour
{
    [Header("Dependencies")]
    public SelectionManager selectionManager;   // 拖 SelectionManager
    public ToolbarManager toolbar;              // 拖 ToolbarManager（用于关闭面板）

    [Header("Ground & Snapping (Move)")]
    public LayerMask groundMask;                // 只勾 Ground
    public bool snapToGrid = true;
    public float gridSize = 0.2f;

    private enum Mode { None, Move, Rotate, Scale }
    private Mode currentMode = Mode.None;

    private Camera cam;

    // —— Move 模式状态 —— 
    private bool moving = false;                // 是否正在“跟随鼠标”
    private bool armedForConfirm = false;       // 是否允许下一次左键作为“确认放置”
    private GameObject movingTarget;            // 正在移动的对象
    private float originalY;                    // 记录初始高度（保持不变）
    private GameObject lastSelected;            // 记录上一次选中的对象（用于检测“刚刚选中”）

    void Awake()
    {
        cam = Camera.main ?? FindObjectOfType<Camera>();
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>(); // 兜底
    }

    void Update()
    {
        if (!selectionManager) return;

        // 每帧拿当前选中
        var selected = selectionManager.Current;

        // —— Move：若刚刚选中一个新对象，立刻进入“跟随鼠标移动” —— 
        if (currentMode == Mode.Move)
        {
            // 1) 监测“刚刚发生了选中变化”
            if (!moving && selected != null && selected != lastSelected)
            {
                BeginMove(selected); // 立刻进入跟随，不必再点第二次
            }

            // 2) 正在跟随 → 更新位置 & 检查确认
            if (moving) HandleMoveFollow();

            // 记录本帧的选中对象
            lastSelected = selected;

            return; // 这里直接返回，避免下面 Rotate/Scale 执行
        }

        // —— Rotate/Scale：需要已选中才生效 —— 
        if (!selected) return;

        switch (currentMode)
        {
            case Mode.Rotate:  HandleRotate(selected); break;
            case Mode.Scale:   HandleScale(selected);  break;
        }
    }

    // ==== 三个按钮入口 ====
    public void StartMove()
    {
        currentMode = Mode.Move;
        selectionManager.selectionEnabled = true;    // 开启点击选中
        moving = false; movingTarget = null;
        armedForConfirm = false;
        lastSelected = null;                         // 重置，以便“下一次选中就立刻跟随”
        if (toolbar) toolbar.CloseAll();             // 关闭UI面板
    }

    public void StartRotate()
    {
        currentMode = Mode.Rotate;
        selectionManager.selectionEnabled = true;
        if (toolbar) toolbar.CloseAll();
    }

    public void StartScale()
    {
        currentMode = Mode.Scale;
        selectionManager.selectionEnabled = true;
        if (toolbar) toolbar.CloseAll();
    }

    // ==== Move：开始/跟随/结束 ====
    void BeginMove(GameObject target)
    {
        moving = true;
        movingTarget = target;
        originalY = target.transform.position.y;
        armedForConfirm = false; // 需要等到这次点击释放后，下一次点击才算确认
    }

    void HandleMoveFollow()
    {
        if (!movingTarget) { moving = false; return; }

        // 鼠标射线落到地面
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            Vector3 pos = hit.point;
            pos.y = originalY; // 仅在 XZ 平面移动
            if (snapToGrid) pos = SnapXZ(pos, gridSize);

            movingTarget.transform.position = pos;
        }

        // 松开所有鼠标键后，武装“确认点击”
        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            armedForConfirm = true;

        // 点击确认放置（忽略 UI 区域）
        if (armedForConfirm && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            moving = false;
            movingTarget = null;
            armedForConfirm = false;

            // 退出 Move 模式 & 关闭选择（你也可以保留选择）
            currentMode = Mode.None;
            selectionManager.selectionEnabled = false;

            selectionManager.ClearSelection();
        }
    }

    // ==== Rotate ====
    void HandleRotate(GameObject target)
    {
        if (Input.GetMouseButton(0) && !IsPointerOverUI())
        {
            float delta = Input.GetAxis("Mouse X"); // 左右拖动
            target.transform.Rotate(Vector3.up, delta * 5f, Space.World);
        }

        // 滚轮快速旋转
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
            target.transform.Rotate(Vector3.up, scroll * 15f, Space.World);
    }

    // ==== Scale ====
    void HandleScale(GameObject target)
    {
        if (Input.GetMouseButton(0) && !IsPointerOverUI())
        {
            float delta = Input.GetAxis("Mouse Y"); // 上下拖动
            Vector3 s = target.transform.localScale * (1f + delta * 0.01f);
            target.transform.localScale = ClampScale(s);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 s = target.transform.localScale * (1f + scroll * 0.1f);
            target.transform.localScale = ClampScale(s);
        }
    }

    // ==== Utils ====
    Vector3 ClampScale(Vector3 s)
    {
        float min = 0.1f, max = 10f;
        s.x = Mathf.Clamp(s.x, min, max);
        s.y = Mathf.Clamp(s.y, min, max);
        s.z = Mathf.Clamp(s.z, min, max);
        return s;
    }

    static Vector3 SnapXZ(Vector3 v, float step)
    {
        if (step <= 0f) return v;
        return new Vector3(
            Mathf.Round(v.x / step) * step,
            v.y,
            Mathf.Round(v.z / step) * step
        );
    }

    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
