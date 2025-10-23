using UnityEngine;

/// <summary>
/// XR 版变换工具：移动 / 旋转 / 缩放
/// - Move：右手射线拖动物体在地面上移动（XZ），同时 右摇杆 左=上 / 右=下 控制 Y 轴高度
/// - Rotate：右摇杆 左=逆时针，右=顺时针
/// - Scale：右摇杆 左=变大，右=变小
/// - Confirm 结束，Cancel 退出
/// </summary>
public class TransformTool : MonoBehaviour
{
    [Header("XR Input (Required)")]
    public XRInputBridge inputBridge;

    [Header("Dependencies")]
    public SelectionManager selectionManager;
    public ToolbarManager toolbar;

    [Header("Move - Ground Follow (XZ)")]
    public LayerMask groundMask;          // 只勾 Ground
    public bool snapToGrid = true;
    public float gridSize = 0.2f;

    [Header("Move - Vertical by Stick X")]
    [Tooltip("右摇杆 X 控制的垂直速度（米/秒）。左=上，右=下")]
    public float verticalSpeed = 1.5f;
    [Tooltip("是否对 Y 轴高度做吸附（例如台阶/层高对齐）")]
    public bool snapY = false;
    [Tooltip("Y 轴吸附步长（米）")]
    public float yStep = 0.1f;
    [Tooltip("可选的世界高度限制（关闭请把 minY>=maxY 或者把 useYClamp 设为 false）")]
    public bool useYClamp = true;
    public float minY = -5f;
    public float maxY = 5f;

    [Header("Speeds")]
    public float rotateSpeed = 90f;       // deg/sec
    public float scaleSpeed  = 0.8f;      // /sec

    [Header("Stick Settings")]
    [Tooltip("右摇杆死区")]
    public float stickDeadzone = 0.2f;

    private enum Mode { None, Move, Rotate, Scale }
    private Mode mode = Mode.None;

    // Move 状态
    bool        moving = false;
    GameObject  movingTarget;
    float       moveY;                    // 当前目标高度（可被摇杆修改）
    GameObject  lastSelected;

    void Awake()
    {
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>();
        if (!selectionManager) selectionManager = FindObjectOfType<SelectionManager>();
    }

    void Update()
    {
        if (!inputBridge || !selectionManager) return;

        // Cancel：退出当前模式
        if (mode != Mode.None && inputBridge.CancelPressedThisFrame())
        {
            StopAll();
            return;
        }

        var selected = selectionManager.Current;

        if (mode == Mode.Move)
        {
            if (!moving && selected && selected != lastSelected)
                BeginMove(selected);

            if (moving)
                HandleMoveFollow();

            lastSelected = selected;
            return;
        }

        if (!selected) return;

        switch (mode)
        {
            case Mode.Rotate:
                HandleRotate(selected);
                if (inputBridge.ConfirmPressedThisFrame()) StopAll();
                break;

            case Mode.Scale:
                HandleScale(selected);
                if (inputBridge.ConfirmPressedThisFrame()) StopAll();
                break;
        }
    }

    // ===== UI 按钮入口 =====
    public void StartMove()   { EnterMode(Mode.Move);  moving = false; movingTarget = null; lastSelected = null; }
    public void StartRotate() { EnterMode(Mode.Rotate); }
    public void StartScale()  { EnterMode(Mode.Scale);  }

    void EnterMode(Mode m)
    {
        mode = m;
        if (toolbar) { toolbar.OnToolEnter(); toolbar.ShowDeleteTopPanel(false); }
        selectionManager?.EnterSelectionMode();
    }

    public void StopAll()
    {
        mode = Mode.None;
        moving = false;
        movingTarget = null;
        selectionManager?.ExitSelectionMode();
    }

    // ===== Move 模式 =====
    void BeginMove(GameObject target)
    {
        moving = true;
        movingTarget = target;
        moveY = target.transform.position.y;   // 以当前高度为起点
    }

    void HandleMoveFollow()
    {
        if (!movingTarget) { moving = false; return; }

        // 1) 右摇杆 X → 调整 Y（左=上，右=下）
        float x = inputBridge.RightStick().x;
        if (Mathf.Abs(x) > stickDeadzone)
        {
            // x<0（向左）→ 上升；x>0（向右）→ 下降
            moveY += (-x) * verticalSpeed * Time.deltaTime;

            if (useYClamp && (maxY > minY))
                moveY = Mathf.Clamp(moveY, minY, maxY);

            if (snapY && yStep > 0f)
                moveY = Mathf.Round(moveY / yStep) * yStep;
        }

        // 2) 右手射线确定 XZ；Y 使用 moveY
        Vector3 pos = movingTarget.transform.position; // 兜底
        if (inputBridge.RaycastGroundFromRight(out var hit))
        {
            pos = hit.point;
        }
        pos.y = moveY;

        if (snapToGrid) pos = SnapXZ(pos, gridSize);
        movingTarget.transform.position = pos;

        // 3) Confirm：收尾
        if (inputBridge.ConfirmPressedThisFrame())
        {
            moving = false;
            movingTarget = null;
            StopAll();
        }
    }

    // ===== Rotate：右摇杆 X 左=逆时针，右=顺时针 =====
    void HandleRotate(GameObject t)
    {
        float x = inputBridge.RightStick().x;
        if (Mathf.Abs(x) > stickDeadzone)
        {
            // x<0(左) → 逆时针；x>0(右) → 顺时针
            float angle = -x * rotateSpeed * Time.deltaTime;
            t.transform.Rotate(Vector3.up, angle, Space.World);
        }
    }

    // ===== Scale：右摇杆 X 左=变大，右=变小 =====
    void HandleScale(GameObject t)
    {
        float x = inputBridge.RightStick().x;
        if (Mathf.Abs(x) > stickDeadzone)
        {
            var s = t.transform.localScale;
            // x<0(左) → 变大；x>0(右) → 变小
            float k = 1f - x * scaleSpeed * Time.deltaTime;
            s *= k;
            s.x = Mathf.Clamp(s.x, 0.1f, 10f);
            s.y = Mathf.Clamp(s.y, 0.1f, 10f);
            s.z = Mathf.Clamp(s.z, 0.1f, 10f);
            t.transform.localScale = s;
        }
    }

    // ===== Utils =====
    static Vector3 SnapXZ(Vector3 v, float step)
    {
        if (step <= 0f) return v;
        return new Vector3(
            Mathf.Round(v.x / step) * step,
            v.y,
            Mathf.Round(v.z / step) * step
        );
    }
}
