using UnityEngine;
using TMPro;

public class CameraModeButton : MonoBehaviour
{
    public CameraSwitcher switcher;
    public TextMeshProUGUI label;

    void Start() => RefreshLabel();

    public void OnClickToggle()
    {
        if (!switcher) return;
        switcher.Toggle();
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (!label || !switcher) return;
        label.text = (switcher.current == CameraSwitcher.CamMode.FreeFly)
            ? "Mode: Free-Fly"
            : "Mode: XR Step Teleport";
    }
}
