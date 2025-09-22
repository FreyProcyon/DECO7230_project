using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeleteTool : MonoBehaviour
{
    [Header("UI Buttons")]
    public GameObject topPanel;       // 顶部的 UI 面板（包含 Delete 和 Cancel 按钮）
    public Button deleteButton;

    public Button cancelButton;

    [Header("Highlight Colors")]
    public Color deleteColor = Color.red;
    public float emissionStrength = 0.5f;
    [Header("Managers")]
    public ToolbarManager toolbar;


    [Header("Raycast")]
    public Camera cam;
    public LayerMask selectableMask;  // 可以删除的层

    private List<GameObject> markedForDelete = new List<GameObject>();
    private Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();

    private bool deleteMode = false;

    void Awake()
    {
        if (!cam) cam = Camera.main ?? FindObjectOfType<Camera>();

        if (deleteButton) deleteButton.onClick.AddListener(DeleteSelected);
        if (cancelButton) cancelButton.onClick.AddListener(CancelDelete);
        if (topPanel) topPanel.SetActive(false);
    }

    void Update()
    {
        if (!deleteMode) return;

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, selectableMask))
            {
                var go = hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.gameObject : hit.collider.gameObject;

                if (markedForDelete.Contains(go))
                {
                    // 已经选中 → 取消选中
                    Restore(go);
                }
                else
                {
                    // 没选中 → 标红
                    Mark(go);
                }
            }
            // 点到空白地面 → 什么都不做
        }
    }

    // ====== 外部入口 ======
    public void StartDeleteMode()
    {
        if (toolbar) toolbar.CloseAll();   // 关闭 SelectOptionsPanel / CreateOptionsPanel 等
        deleteMode = true;
        if (topPanel) topPanel.SetActive(true);
        markedForDelete.Clear();
        originalMats.Clear();
    }


    void DeleteSelected()
    {
        foreach (var go in markedForDelete)
        {
            if (go) Destroy(go);
        }
        markedForDelete.Clear();
        originalMats.Clear();

        ExitDeleteMode();
    }

    void CancelDelete()
    {
        // 还原所有标红物体
        foreach (var go in markedForDelete)
        {
            Restore(go);
        }
        markedForDelete.Clear();
        originalMats.Clear();

        ExitDeleteMode();
    }

    void ExitDeleteMode()
    {
        deleteMode = false;
        if (topPanel) topPanel.SetActive(false);
    }

    // ====== 高亮控制 ======
    void Mark(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;

        if (!originalMats.ContainsKey(r))
        {
            originalMats[r] = r.materials; // 保存原材质引用
        }

        // 克隆一个红色材质阵列
        Material[] mats = new Material[r.materials.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = new Material(r.materials[i]);
            if (mats[i].HasProperty("_Color"))
                mats[i].color = deleteColor;
            if (mats[i].HasProperty("_EmissionColor"))
            {
                mats[i].EnableKeyword("_EMISSION");
                mats[i].SetColor("_EmissionColor", deleteColor * emissionStrength);
            }
        }
        r.materials = mats;

        markedForDelete.Add(go);
    }

    void Restore(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;

        if (originalMats.ContainsKey(r))
        {
            r.materials = originalMats[r];
            originalMats.Remove(r);
        }

        markedForDelete.Remove(go);
    }

    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
