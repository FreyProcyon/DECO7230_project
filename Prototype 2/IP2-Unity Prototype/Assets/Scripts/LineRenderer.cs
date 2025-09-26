using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class XRHandRayLine : MonoBehaviour
{
    public XRInputBridge bridge;
    public float length = 2f;
    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.widthMultiplier = 0.005f;   // 细线
        lr.useWorldSpace = true;
    }

    void Update()
    {
        if (bridge && bridge.rightHand)
        {
            var p = bridge.rightHand.position;
            var d = bridge.rightHand.forward;
            lr.SetPosition(0, p);
            lr.SetPosition(1, p + d * length);
            lr.enabled = true;
        }
        else lr.enabled = false;
    }
}
