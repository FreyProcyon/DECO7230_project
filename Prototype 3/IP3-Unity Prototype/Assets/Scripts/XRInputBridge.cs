using UnityEngine;
using UnityEngine.InputSystem;

public class XRInputBridge : MonoBehaviour
{
    [Header("Right Hand Actions (Only)")]
    public InputActionReference selectRight;     // 右手 Select（Trigger）
    public InputActionReference confirmRight;    // 右手 Confirm（可与 Select 同键）
    public InputActionReference rightStick;      // 右手摇杆
    [Header("Global")]
    public InputActionReference cancel;          // Cancel（任意手二键）

    [Header("Ray Origins")]
    public Transform rightHand;                  // OVR: .../RightHandAnchor
    public Transform leftHand;                   // OVR: .../LeftHandAnchor（不用也可留）
    public LayerMask groundMask = ~0;

    [Header("Raycast")]
    public float maxRayDistance = 30f;

    void OnEnable()
    {
        EnableRef(selectRight); EnableRef(confirmRight);
        EnableRef(rightStick);  EnableRef(cancel);
    }
    void OnDisable()
    {
        DisableRef(selectRight); DisableRef(confirmRight);
        DisableRef(rightStick);  DisableRef(cancel);
    }
    void EnableRef(InputActionReference r){ if (r && r.action!=null) r.action.Enable(); }
    void DisableRef(InputActionReference r){ if (r && r.action!=null) r.action.Disable(); }

    // ---- Buttons ----
    public bool SelectPressedThisFrame()  => selectRight && selectRight.action.WasPressedThisFrame();
    public bool ConfirmPressedThisFrame() => confirmRight && confirmRight.action.WasPressedThisFrame();
    public bool CancelPressedThisFrame()  => cancel && cancel.action.WasPressedThisFrame();

    // ---- Stick (Right only) ----
    public Vector2 RightStick() => rightStick ? rightStick.action.ReadValue<Vector2>() : Vector2.zero;

    // ---- Rays (Right only for tools) ----
    public bool RayFromRight(out Ray ray)
    {
        ray = default;
        if (!rightHand) return false;
        ray = new Ray(rightHand.position, rightHand.forward);
        return true;
    }

    public bool RaycastGroundFromRight(out RaycastHit hit)
    {
        hit = default;
        if (!RayFromRight(out var ray)) return false;
        return Physics.Raycast(ray, out hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore);
    }
}
