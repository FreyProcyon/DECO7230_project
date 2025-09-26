using UnityEngine;
using UnityEngine.InputSystem;

public class XRInputBridge : MonoBehaviour
{
    [Header("Action References - Right Hand")]
    public InputActionReference selectAction;      // 右手 Select
    public InputActionReference confirmAction;     // 右手 Confirm
    public InputActionReference cancelAction;      // 通用 Cancel（可只右手）
    public InputActionReference primary2DAxis;     // 右手摇杆（可选）

    [Header("Action References - Left Hand (optional)")]
    public InputActionReference selectLeftAction;  // 左手 Select
    public InputActionReference confirmLeftAction; // 左手 Confirm
    public InputActionReference primary2DAxisLeft; // 左手摇杆（可选）

    [Header("Ray Origins")]
    public Transform rightHand;                    // 右手/控制器 Transform
    public Transform leftHand;                     // 左手/控制器 Transform
    public LayerMask groundMask = ~0;

    enum ActiveHand { None, Right, Left }
    ActiveHand lastActiveHand = ActiveHand.Right;  // 默认右手

    void OnEnable()
    {
        EnableRef(selectAction); EnableRef(confirmAction);
        EnableRef(cancelAction); EnableRef(primary2DAxis);

        EnableRef(selectLeftAction); EnableRef(confirmLeftAction);
        EnableRef(primary2DAxisLeft);
    }
    void OnDisable()
    {
        DisableRef(selectAction); DisableRef(confirmAction);
        DisableRef(cancelAction); DisableRef(primary2DAxis);

        DisableRef(selectLeftAction); DisableRef(confirmLeftAction);
        DisableRef(primary2DAxisLeft);
    }
    void EnableRef(InputActionReference r)  { if (r) r.action.Enable(); }
    void DisableRef(InputActionReference r) { if (r) r.action.Disable(); }

    // ===== 按钮查询（任意手）=====
    public bool SelectPressedThisFrame()
    {
        bool right = selectAction && selectAction.action.WasPressedThisFrame();
        bool left  = selectLeftAction && selectLeftAction.action.WasPressedThisFrame();
        if (right) lastActiveHand = ActiveHand.Right;
        if (left)  lastActiveHand = ActiveHand.Left;
        return right || left;
    }
    public bool ConfirmPressedThisFrame()
    {
        bool right = confirmAction && confirmAction.action.WasPressedThisFrame();
        bool left  = confirmLeftAction && confirmLeftAction.action.WasPressedThisFrame();
        if (right) lastActiveHand = ActiveHand.Right;
        if (left)  lastActiveHand = ActiveHand.Left;
        return right || left;
    }
    public bool CancelPressedThisFrame()
    {
        return cancelAction && cancelAction.action.WasPressedThisFrame();
    }

    // ===== 摇杆（优先用“最后一次激活的那只手”的摇杆；退化到右手）=====
    public Vector2 Stick()
    {
        if (lastActiveHand == ActiveHand.Left && primary2DAxisLeft)
            return primary2DAxisLeft.action.ReadValue<Vector2>();
        if (primary2DAxis) return primary2DAxis.action.ReadValue<Vector2>();
        return Vector2.zero;
    }

    // ===== 从“最后激活的手”发射射线 =====
    public bool RayFromHand(out Ray ray)
    {
        ray = default;
        Transform t = (lastActiveHand == ActiveHand.Left) ? leftHand : rightHand;
        if (!t) t = rightHand ? rightHand : leftHand; // 兜底
        if (!t) return false;
        ray = new Ray(t.position, t.forward);
        return true;
    }

    public bool RaycastGround(out RaycastHit hit, float maxDistance = 30f)
    {
        hit = default;
        if (!RayFromHand(out var ray)) return false;
        return Physics.Raycast(ray, out hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore);
    }
}
