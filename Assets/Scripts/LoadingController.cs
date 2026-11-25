using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loading 场景控制器：用 SceneManager.LoadSceneAsync 真实异步加载下一个场景，
/// 显示真实进度百分比，加载完成后自动跳转。
///
/// 用法：把本脚本挂到 Loading 场景中任意 GameObject（建议 Canvas）即可。
/// 兜底：若未手动挂载，脚本会在 Loading 场景运行时自动创建一个控制器（见 AutoBootstrap）。
/// 无需手动拖任何引用，运行时按名字自动查找 UI 控件。
/// </summary>
public class LoadingController : MonoBehaviour
{
    [Header("下一个场景（Build Settings 中的索引 / 名称）")]
    [Tooltip("优先使用场景名；留空则使用索引")]
    public string nextSceneName = "Game";
    public int nextSceneIndex = 1;

    [Header("显示设置")]
    [Range(0f, 0.99f)]
    public float minDisplayProgress = 0.05f;
    [Tooltip("加载完成后的最短展示时间（秒）")]
    public float minDisplayTime = 0.5f;

    private Text uiText;
    private TextMeshProUGUI tmpText;
    private Slider slider;
    private float startTime;

    // 运行时自动引导：只要进入 Loading 场景且场景里没有控制器，就自动建一个
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (SceneManager.GetActiveScene().name != "Loading")
            return;
        if (Object.FindObjectOfType<LoadingController>() != null)
            return;

        var go = new GameObject("[LoadingController]");
        go.AddComponent<LoadingController>();
        Debug.Log("[LoadingController] 自动引导已创建（未手动挂载时使用）。");
    }

    void Awake()
    {
        // 按名字精确查找，找不到再退化为取场景中第一个
        uiText = FindNamed<Text>("Text_Load_Value");
        tmpText = FindNamed<TextMeshProUGUI>("Text_Load_Value");
        slider = FindNamed<Slider>("Slider_LoadingBar");
    }

    static T FindNamed<T>(string name) where T : Component
    {
        foreach (var c in Object.FindObjectsOfType<T>())
            if (c.gameObject.name == name)
                return c;
        return null;
    }

    void Start()
    {
        startTime = Time.unscaledTime;
        StartCoroutine(LoadAsync());
    }

    IEnumerator LoadAsync()
    {
        AsyncOperation op;
        if (!string.IsNullOrEmpty(nextSceneName))
            op = SceneManager.LoadSceneAsync(nextSceneName);
        else
            op = SceneManager.LoadSceneAsync(nextSceneIndex);

        if (op == null)
        {
            Debug.LogError("[LoadingController] 无法开始异步加载场景，请检查 Build Settings 场景配置。");
            yield break;
        }

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            UpdateUI(Mathf.Max(minDisplayProgress, realProgress));

            if (op.progress >= 0.9f)
            {
                UpdateUI(1f);
                float elapsed = Time.unscaledTime - startTime;
                if (elapsed < minDisplayTime)
                    yield return new WaitForSecondsRealtime(minDisplayTime - elapsed);
                op.allowSceneActivation = true;
            }
            yield return null;
        }

        UpdateUI(1f);
    }

    void UpdateUI(float progress)
    {
        string label = "加载中..." + Mathf.RoundToInt(progress * 100f) + "%";
        if (tmpText != null) tmpText.text = label;
        if (uiText != null) uiText.text = label;
        if (slider != null) slider.value = progress;
    }
}
