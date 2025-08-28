using System.Collections;
using UnityEngine;

public class StepTeleportCamera : MonoBehaviour
{
    [Header("Rig & Camera")]
    public Transform rig;   // 建议把 Main Camera 放到 CameraRig 下并把这个字段拖 CameraRig
    public Camera cam;      // 留空自动找 Camera.main 或本对象上的 Camera

    [Header("Ring Style")]
    public Color ringColor = new Color(1f, 0f, 0f, 0.8f); // 红色 + 透明度
    public float ringRadius = 0.35f;                      // 外半径（等同原 previewScale）
    public float ringThickness = 0.02f;                   // 线宽（环的粗细）
    [Range(16, 256)] public int ringSegments = 64;        // 圆的细分

    [Header("Teleport setting")]
    public float eyeHeight = 1.6f;
    public LayerMask groundMask = ~0;
    public float maxRayDistance = 100f;
    public float teleportLerpTime = 0.15f;

    [Header("previewPrefab")]
    public GameObject previewPrefab; // 可留空，代码会自动创建
    public float previewScale = 0.35f;

    [Header("Observe when stationary (right-click and hold down)）")]
    public bool allowMouseLook = true;
    public float lookSpeed = 2f;
    public float lookPitchClamp = 85f;

    private Transform previewInstance;
    private float yaw, pitch;

    void Awake()
    {
        if (!cam) cam = Camera.main ? Camera.main : GetComponent<Camera>();
        if (rig == null && cam != null) rig = cam.transform.parent;

        // 初始化朝向
        var rot = (rig ? rig.rotation : cam.transform.rotation).eulerAngles;
        yaw = rot.y; pitch = rot.x;
    }

    void Update()
    {
        if (!cam)
        {
            cam = Camera.main ? Camera.main : GetComponent<Camera>();
            if (!cam) return;
        }

        // —— 确保有预览圈（懒加载）——
        EnsurePreview();
        if (!previewInstance) return;

        // 指向检测
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask))
        {
            previewInstance.gameObject.SetActive(true);
            // 轻微抬起避免与地面 z-fighting
            previewInstance.position = hit.point + hit.normal * 0.01f;
            // 若不想随法线倾斜，可改成 Quaternion.identity
            previewInstance.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // 左键 → 瞬移/平滑瞬移
            if (Input.GetMouseButtonDown(0))
            {
                if (rig)
                {
                    StopAllCoroutines();
                    StartCoroutine(LerpTo(rig, hit.point, teleportLerpTime));

                    if (cam.transform.parent == rig)
                    {
                        var lp = cam.transform.localPosition;
                        cam.transform.localPosition = new Vector3(lp.x, eyeHeight, lp.z);
                    }
                }
                else
                {
                    StopAllCoroutines();
                    StartCoroutine(LerpTo(cam.transform, hit.point + Vector3.up * eyeHeight, teleportLerpTime));
                }
            }
        }
        else
        {
            previewInstance.gameObject.SetActive(false);
        }

        // 右键按住 → 第一人称看向
        if (allowMouseLook && Input.GetMouseButton(1))
        {
            yaw += lookSpeed * Input.GetAxis("Mouse X");
            pitch -= lookSpeed * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -lookPitchClamp, lookPitchClamp);

            var q = Quaternion.Euler(pitch, yaw, 0f);
            if (rig) rig.rotation = q;
            else cam.transform.rotation = q;
        }
    }

    // —— 懒加载/兜底创建预览圈 —— 
    void EnsurePreview()
    {
        if (previewInstance) return;

        // 如果有自带的 prefab，优先用它（比如你将来做了一个更漂亮的环）
        if (previewPrefab)
        {
            previewInstance = Instantiate(previewPrefab).transform;
            previewInstance.localScale = Vector3.one; // 让半径由 LineRenderer 控
            previewInstance.gameObject.SetActive(false);
            return;
        }

        // —— 无 prefab：自动创建一个 LineRenderer 圆环 ——
        var go = new GameObject("TeleportRing");
        var lr = go.AddComponent<LineRenderer>();

        // 材质：尽量选 Unlit，避免受光；找不到就用默认
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (!sh) sh = Shader.Find("Unlit/Color");
        if (!sh) sh = Shader.Find("Sprites/Default");
        if (!sh) sh = Shader.Find("Standard");
        var mat = new Material(sh);
        if (sh.name.Contains("Unlit") || sh.name.Contains("Sprites"))
            mat.color = ringColor;
        lr.material = mat;

        // 线渲染器参数
        lr.useWorldSpace = false;                // 用局部坐标，方便整体旋转到法线方向
        lr.loop = true;                          // 闭合成环
        lr.positionCount = ringSegments;
        lr.startWidth = lr.endWidth = ringThickness;
        lr.numCornerVertices = 4;                // 圆角更顺滑
        lr.alignment = LineAlignment.View;       // 线宽对相机友好（也可用 TransformZ）

        // 生成圆
        var pts = new Vector3[ringSegments];
        float step = Mathf.PI * 2f / (ringSegments - 1);
        for (int i = 0; i < ringSegments; i++)
        {
            float a = step * i;
            pts[i] = new Vector3(Mathf.Cos(a) * ringRadius, 0f, Mathf.Sin(a) * ringRadius);
        }
        lr.SetPositions(pts);

        // 作为 previewInstance 使用
        previewInstance = go.transform;
        previewInstance.gameObject.SetActive(false);
    }


    IEnumerator LerpTo(Transform t, Vector3 target, float time)
    {
        if (time <= 0f) { t.position = target; yield break; }
        Vector3 start = t.position;
        float timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, timer / time);
            t.position = Vector3.LerpUnclamped(start, target, k);
            yield return null;
        }
        t.position = target;
    }
    
    public void HidePreview()
{
    if (previewInstance)
        previewInstance.gameObject.SetActive(false);
}

}
