using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [Header("Raycast")]
    public bool selectionEnabled = false;
    public Camera cam;                      // 为空则自动 Camera.main
    public LayerMask selectableMask;        // 可选物体层（如: Default | Placeable）
    public LayerMask groundMask;            // 地面层（用于点空白时取消）
    public float maxRayDistance = 1000f;

    [Header("Highlight")]
    public Color highlightColor = new Color(1f, 0.85f, 0.1f, 1f); // 高亮色(暖黄)
    [Range(0f, 1f)] public float highlightEmission = 0.25f;      // 轻微“自发光”效果
    public bool useSharedMaterial = false;  // 勿改共享材质：默认 false，更安全

    [Header("Input")]
    public KeyCode addToSelectionKey = KeyCode.LeftShift; // 预留多选: 按住 Shift 追加（现在先单选）

    // —— 当前选中（单选）——
    private GameObject current;
    public GameObject Current => current;

    // 还原材质所需缓存
    private struct OriginalMat
    {
        public Renderer renderer;
        public Material[] mats;
    }
    private readonly List<OriginalMat> originalMats = new();

    void Awake()
    {
        if (!cam) cam = Camera.main ?? FindObjectOfType<Camera>();
    }

    void Update()
    {
        if (!selectionEnabled) return; // 禁用时不处理选中

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, selectableMask))
            {
                Select(hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.gameObject
                                                       : hit.collider.gameObject);
            }
            else if (Physics.Raycast(ray, out hit, maxRayDistance, groundMask))
            {
                // 点到地面：取消选中
                ClearSelection();
            }
            else
            {
                // 点到其它不可选层：也取消
                ClearSelection();
            }
        }
    }

    public void Select(GameObject go)
    {
        if (go == current) return;

        ClearSelection();

        current = go;
        ApplyHighlight(current);
    }

    public void ClearSelection()
    {
        if (!current) return;

        // 还原材质
        foreach (var entry in originalMats)
        {
            if (!entry.renderer) continue;
            if (useSharedMaterial)
                entry.renderer.sharedMaterials = entry.mats;
            else
                entry.renderer.materials = Clone(entry.mats); // 防止引用同一实例
        }
        originalMats.Clear();

        current = null;
    }

    // —— 高亮实现：逐个 Renderer 改材质颜色（兼容标准/URP），轻微 Emission 提升可见性 ——
    void ApplyHighlight(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
        foreach (var r in renderers)
        {
            if (!r) continue;

            // 记录原材质阵列
            var record = new OriginalMat
            {
                renderer = r,
                mats = useSharedMaterial ? r.sharedMaterials : Clone(r.materials)
            };
            originalMats.Add(record);

            // 修改可见材质颜色和发光（尽量不破坏原贴图）
            var mats = useSharedMaterial ? r.sharedMaterials : r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                // 主色
                if (mats[i].HasProperty("_BaseColor"))       mats[i].SetColor("_BaseColor", highlightColor);
                else if (mats[i].HasProperty("_Color"))      mats[i].SetColor("_Color", highlightColor);

                // 轻微自发光（不同管线属性名不同）
                if (highlightEmission > 0f)
                {
                    if (mats[i].HasProperty("_EmissionColor"))
                    {
                        mats[i].EnableKeyword("_EMISSION");
                        mats[i].SetColor("_EmissionColor", highlightColor * highlightEmission);
                    }
                }
            }

            if (!useSharedMaterial) r.materials = mats; // 写回实例材质
            else r.sharedMaterials = mats;
        }
    }

    // 工具：克隆材质数组（避免多对象共用同一实例被永久污染）
    Material[] Clone(Material[] src)
    {
        var arr = new Material[src.Length];
        for (int i = 0; i < src.Length; i++)
            arr[i] = src[i] ? new Material(src[i]) : null;
        return arr;
    }

    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
