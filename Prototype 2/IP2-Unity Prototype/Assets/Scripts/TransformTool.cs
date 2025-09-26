using UnityEngine;

/// <summary>
/// XR 版三合一变换工具：Move / Rotate / Scale
/// - 进入某模式后由 SelectionManager 开启点选（Trigger）
/// - Move：选中后立即跟随右手射线在地面上移动；Confirm 放置；Cancel 取消
/// - Rotate：摇杆 X 控制 Y 轴旋转（可按需扩展多轴）
/// - Scale：摇杆 Y 统一缩放
/// - 所有输入均来自 XRInputBridge（右手 trigger / thumbstick / cancel）
/// </summary>
public class TransformTool : MonoBehaviour
{
    [Header("XR Input (Required)")]
    public XRInputBridge inputBridge;             // 拖 Managers 上的 XRInputBridge

    [Header("Dependencies")]
    public SelectionManager selectionManager;     // 拖 SelectionManager
    public ToolbarManager toolbar;                // 拖 ToolbarManager（用于关闭面板）

    [Header("Ground & Snapping (Move)")]
    public LayerMask groundMask;                  // 只勾 Ground
    public bool snapToGrid = true;
    public float gridSize = 0.2f;

    [Header("Speeds")]
    public float rotateSpeed = 90f;               // deg/sec（摇杆旋转速度）
    public float scaleSpeed  = 0.8f;              // 摇杆缩放速度

    private enum Mode { None, Move, Rotate, Scale }
    private Mode currentMode = Mode.None;

    // —— Move 模式状态 —— 
    private bool moving = false;                  // 是否正在“跟随射线”
    private GameObject movingTarget;              // 正在移动的对象
    private float originalY;                      // 记录初始高度（保持不变）
    private GameObject lastSelected;              // 记录上一次选中的对象（用于检测“刚刚选中”）

    void Awake()
    {
        if (!toolbar) toolbar = FindObjectOfType<ToolbarManager>(); // 兜底
    }

    void Update()
    {
        if (!selectionManager || !inputBridge) return;

        // 每帧拿当前选中
        var selected = selectionManager.Current;

        // ===== Move 模式 =====
        if (currentMode == Mode.Move)
        {
            // 1) 监测“刚刚发生了选中变化”→ 立刻开始跟随
            if (!moving && selected != null && selected != lastSelected)
            {
                BeginMove(selected);
            }

            // 2) 正在跟随 → 更新位置 & 检查确认/取消
            if (moving) HandleMoveFollow();

            // 记录本帧的选中对象
            lastSelected = selected;
            return; // 避免下面 Rotate/Scale 执行
        }

        // ===== Rotate / Scale 模式：需要已选中才生效 =====
        if (!selected) return;

        switch (currentMode)
        {
            case Mode.Rotate:  HandleRotate(selected); break;
            case Mode.Scale:   HandleScale(selected);  break;
        }

        // 通用 Cancel：退出当前模式
        if (inputBridge.CancelPressedThisFrame())
        {
            StopAll();
        }
    }

    // ==== 三个按钮入口 ====
    public void StartMove()
    {
        currentMode = Mode.Move;
        if (selectionManager) selectionManager.selectionEnabled = true; // 开启点击选中
        moving = false; movingTarget = null;
        lastSelected = null;                                           // 下次选中就立刻跟随
        if (toolbar) toolbar.CloseAll();                               // 关闭UI面板
    }

    public void StartRotate()
    {
        currentMode = Mode.Rotate;
        if (selectionManager) selectionManager.selectionEnabled = true;
        if (toolbar) toolbar.CloseAll();
    }

    public void StartScale()
    {
        currentMode = Mode.Scale;
        if (selectionManager) selectionManager.selectionEnabled = true;
        if (toolbar) toolbar.CloseAll();
    }

    public void StopAll()
    {
        currentMode = Mode.None;
        moving = false;
        movingTarget = null;
        if (selectionManager) selectionManager.selectionEnabled = false;
    }

    // ==== Move：开始/跟随/结束 ====
    void BeginMove(GameObject target)
    {
        moving = true;
        movingTarget = target;
        originalY = target.transform.position.y;
    }

    void HandleMoveFollow()
    {
        if (!movingTarget) { moving = false; return; }

        // 从右手 forward 发出射线，命中地面
        if (inputBridge.RaycastGround(out RaycastHit hit, 1000f))
        {
            Vector3 pos = hit.point;
            pos.y = originalY; // 仅在 XZ 平面移动
            if (snapToGrid) pos = SnapXZ(pos, gridSize);

            movingTarget.transform.position = pos;
        }

        // Confirm：放置并退出 Move 模式
        if (inputBridge.ConfirmPressedThisFrame())
        {
            moving = false;
            movingTarget = null;
            currentMode = Mode.None;

            // 退出后可选择是否清除高亮；保留/清空按你的需求
            if (selectionManager)
            {
                selectionManager.selectionEnabled = false;
                selectionManager.ClearSelection();
            }
        }

        // Cancel：放弃移动并退出
        if (inputBridge.CancelPressedThisFrame())
        {
            moving = false;
            movingTarget = null;
            currentMode = Mode.None;
            if (selectionManager) selectionManager.selectionEnabled = false;
        }
    }

    // ==== Rotate（摇杆 X 控制 Y 轴旋转） ====
    void HandleRotate(GameObject target)
    {
        var stick = inputBridge.Stick();
        if (Mathf.Abs(stick.x) > 0.2f)
        {
            target.transform.Rotate(Vector3.up, stick.x * rotateSpeed * Time.deltaTime, Space.World);
        }

        if (inputBridge.ConfirmPressedThisFrame())
        {
            StopAll();
        }
    }

    // ==== Scale（摇杆 Y 统一缩放） ====
    void HandleScale(GameObject target)
    {
        float y = inputBridge.Stick().y;
        if (Mathf.Abs(y) > 0.2f)
        {
            var s = target.transform.localScale;
            float k = 1f + y * scaleSpeed * Time.deltaTime;
            s *= k;
            s.x = Mathf.Clamp(s.x, 0.1f, 10f);
            s.y = Mathf.Clamp(s.y, 0.1f, 10f);
            s.z = Mathf.Clamp(s.z, 0.1f, 10f);
            target.transform.localScale = s;
        }

        if (inputBridge.ConfirmPressedThisFrame())
        {
            StopAll();
        }
    }

    // ==== Utils ====
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
