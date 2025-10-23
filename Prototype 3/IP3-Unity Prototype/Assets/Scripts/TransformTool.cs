using UnityEngine;

/// <summary>
/// XR 版变换工具：移动 / 旋转 / 缩放
/// - Move：右手射线拖动物体在地面上移动
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

    [Header("Move Settings")] 
    public LayerMask groundMask;
    public bool snapToGrid = true;
    public float gridSize = 0.2f;

    [Header("Speeds")]
    public float rotateSpeed = 90f;
    public float scaleSpeed = 0.8f;

    [Header("Stick Settings")]
    [Tooltip("控制灵敏度死区")]
    public float stickDeadzone = 0.2f;

    private enum Mode { None, Move, Rotate, Scale }
    private Mode mode = Mode.None;

    // Move 状态
    bool moving = false;
    GameObject movingTarget;
    float originalY;
    GameObject lastSelected;

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
    public void StartMove()   { EnterMode(Mode.Move); moving = false; movingTarget = null; lastSelected = null; }
    public void StartRotate() { EnterMode(Mode.Rotate); }
    public void StartScale()  { EnterMode(Mode.Scale); }

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
        originalY = target.transform.position.y;
    }

    void HandleMoveFollow()
    {
        if (!movingTarget) { moving = false; return; }

        if (inputBridge.RaycastGroundFromRight(out var hit))
        {
            var pos = hit.point;
            pos.y = originalY;
            if (snapToGrid) pos = SnapXZ(pos, gridSize);
            movingTarget.transform.position = pos;
        }

        if (inputBridge.ConfirmPressedThisFrame())
        {
            moving = false;
            movingTarget = null;
            StopAll();
        }
    }

    // ===== Rotate 模式：右摇杆 X 左负右正 → 左逆时针右顺时针 =====
    void HandleRotate(GameObject t)
    {
        float x = inputBridge.RightStick().x;
        if (Mathf.Abs(x) > stickDeadzone)
        {
            // 左负 → 逆时针（正角度）
            float angle = -x * rotateSpeed * Time.deltaTime;
            t.transform.Rotate(Vector3.up, angle, Space.World);
        }
    }

    // ===== Scale 模式：右摇杆 X 左负右正 → 左变大右变小 =====
    void HandleScale(GameObject t)
    {
        float x = inputBridge.RightStick().x;
        if (Mathf.Abs(x) > stickDeadzone)
        {
            var s = t.transform.localScale;
            // 左负 → 变大
            float k = 1f - x * scaleSpeed * Time.deltaTime;
            s *= k;
            s.x = Mathf.Clamp(s.x, 0.1f, 10f);
            s.y = Mathf.Clamp(s.y, 0.1f, 10f);
            s.z = Mathf.Clamp(s.z, 0.1f, 10f);
            t.transform.localScale = s;
        }
    }

    // ===== 辅助函数 =====
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
