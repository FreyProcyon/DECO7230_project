using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPanel : MonoBehaviour
{
    [Header("Optional Fade")]
    public bool useFade = false;
    public float fadeDuration = 0.25f;

    CanvasGroup cg;

    void Awake()
    {
        if (useFade)
        {
            cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    // 绑定到 OK 按钮 OnClick
    public void Dismiss()
    {
        if (!useFade)
        {
            gameObject.SetActive(false);
            return;
        }
        StartCoroutine(FadeOutAndDisable());
    }

    IEnumerator FadeOutAndDisable()
    {
        float t = 0f;
        float start = cg.alpha;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}
