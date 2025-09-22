using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private FreeFlyCamera     freeFly;
    [SerializeField] private StepTeleportCamera stepTeleport;

    public enum CamMode { FreeFly, StepTeleport }
    public CamMode current = CamMode.StepTeleport;

    void Awake()
    {
        if (!freeFly)     freeFly     = GetComponent<FreeFlyCamera>();
        if (!stepTeleport) stepTeleport = GetComponent<StepTeleportCamera>();
    }

    void Start() => SetMode(current);

    public void Toggle()
    {
        current = (current == CamMode.FreeFly) ? CamMode.StepTeleport : CamMode.FreeFly;
        SetMode(current);
    }

    public void SetMode(CamMode mode)
{
    bool useFree = (mode == CamMode.FreeFly);

    if (freeFly)      freeFly.enabled = useFree;
    if (stepTeleport) {
        stepTeleport.enabled = !useFree;

        // 关键：关闭 StepTeleport 时，把预览圈关掉
        if (useFree) stepTeleport.HidePreview();
    }
}

}
