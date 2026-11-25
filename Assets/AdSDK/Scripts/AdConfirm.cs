using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 激励广告弹窗
/// </summary>
public class AdConfirm : MonoBehaviour
{
    private static AdConfirm instance;

    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnConfirm;

    /// <summary>奖励标题文本（预制体里的「添加金币」）</summary>
    private Text rewardTitleTxt;
    /// <summary>奖励数值文本（预制体里的 100）</summary>
    private Text rewardValueTxt;
    private string defaultRewardTitle;
    private string defaultRewardValue;

    private Action onCompleted;

    /// <summary>
    /// 全局待发奖回调。
    /// 原来是存在实例字段 onCompleted 上，一旦弹窗被重新实例化（切场景、预制体重复加载、
    /// 旧对象被 Destroy 后新建），点「确认」的那个对象和 Show() 写入回调的对象就可能不是同一个，
    /// 于是出现日志里的「没有发奖回调，本次看完广告不会获得奖励」——玩家明明看完了广告却没有奖励。
    /// 这里改成静态字段：回调跟着「这一次广告请求」走，不再依赖具体是哪个实例。
    /// </summary>
    private static Action pendingReward;

    /// <summary>广告请求中，防连点重复拉起</summary>
    private bool requesting;

    /// <summary>弹窗上的数值文本不是数字时的兜底宝石数（与预制体默认文案「20」一致）</summary>
    private const int FallbackRewardGems = 20;

    /// <summary>
    /// 显示激励视频确认弹窗
    /// </summary>
    /// <param name="callBack">看完广告后的回调</param>
    /// <param name="rewardTitle">奖励标题，传 null 用预制体默认文案（添加金币）</param>
    /// <param name="rewardValue">奖励数值，传 null 用预制体默认数值（100）</param>
    public static void Show(Action callBack, string rewardTitle = null, string rewardValue = null)
    {
        // 先落地回调：即使下面实例化/取组件出问题，回调也不会丢
        pendingReward = callBack;

        // 打开弹窗时把「谁打开的、有没有回调」打进日志：
        // 万一以后再出现「看完广告没发奖」，一眼就能看出是哪个入口没带回调。
        string titleHint = string.IsNullOrEmpty(rewardTitle) ? "默认" : rewardTitle;
        string cbHint = callBack != null ? "有" : "无";
        Debug.Log($"[广告] AdConfirm.Show 打开弹窗（{titleHint}），回调={cbHint}，调用方={GetCallerHint()}");

        if (instance == null)
        {
            // 加载 AdConfirm 预制体
            GameObject prefab = Resources.Load<GameObject>("AdConfirm");
            if (prefab == null)
            {
                Debug.LogError("AdConfirm 预制体未找到，请确保其位于 Resources 文件夹中。");
                return;
            }

            // 实例化 AdConfirm
            GameObject adConfirmInstance = Instantiate(prefab);
            instance = adConfirmInstance.GetComponent<AdConfirm>();

            if (instance == null)
            {
                Debug.LogError("AdConfirm 脚本未附加到预制体上，请检查预制体设置。");
                Destroy(adConfirmInstance);
                return;
            }
        }

        // 设置回调并显示弹窗
        instance.requesting = false;
        instance.onCompleted = callBack;
        instance.SetRewardText(rewardTitle, rewardValue);
        instance.gameObject.SetActive(true);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 按钮引用兜底：预制体里序列化的字段一旦丢失（换预制体、混淆器改字段名等），
        // btnConfirm/btnClose 会是 null，点「确认」毫无反应，表现就是「看完广告没发奖」。
        // 这里按对象名再找一次，找不到就打错误日志。
        if (btnConfirm == null)
            btnConfirm = FindButtonByName("btn_confirm");
        if (btnClose == null)
            btnClose = FindButtonByName("btn_close");

        if (btnConfirm == null)
            Debug.LogError("[广告] AdConfirm 没找到「确认」按钮（btn_confirm），点确认不会有任何反应");
        if (btnClose == null)
            Debug.LogWarning("[广告] AdConfirm 没找到「关闭」按钮（btn_close）");

        // 奖励文案没有序列化到脚本上，按对象名从预制体里找，找不到就跳过（不影响弹窗本身）
        rewardTitleTxt = FindTextByName("tips (1)");
        rewardValueTxt = FindTextByName("value");
        defaultRewardTitle = rewardTitleTxt != null ? rewardTitleTxt.text : null;
        defaultRewardValue = rewardValueTxt != null ? rewardValueTxt.text : null;

        gameObject.SetActive(false);
        btnClose?.onClick.AddListener(this.OnClose);
        btnConfirm?.onClick.AddListener(this.OnConfirm);
    }

    /// <summary>
    /// 弹窗被打开时（不管是 Show() 打开的还是场景按钮直接 SetActive(true) 打开的）补登记回调。
    ///
    /// Game.unity 里 Canvas 下的「领取奖励」按钮（jiliBtn）的 OnClick 是直接对 AdConfirm
    /// 这个 GameObject 调 SetActive(true) 的 —— 这条路径完全不经过 Show()，onCompleted 和
    /// pendingReward 都是空的。老代码在这种情况下点「确认」只会打一条「没拿到发奖回调」，
    /// 玩家看完广告什么都没拿到。这里兜底：没有回调就按弹窗上显示的数值发钻石。
    /// </summary>
    private void OnEnable()
    {
        // 上一次的请求状态不能带到这一次（广告回调晚到 / 看完直接关弹窗都可能留下脏标记）
        requesting = false;

        if (onCompleted == null && pendingReward == null)
        {
            int amount = ResolveDisplayedReward();
            pendingReward = () => GrantDefaultReward(amount);
            Debug.LogWarning($"[广告] AdConfirm 被直接激活（未走 Show，实例 {GetInstanceID()}），" +
                             $"已按弹窗显示的 {amount} 宝石登记默认发奖");
        }
    }

    /// <summary>取弹窗「数值」文本里的奖励数，取不到用默认值</summary>
    private int ResolveDisplayedReward()
    {
        if (rewardValueTxt != null)
        {
            string raw = rewardValueTxt.text;
            if (!string.IsNullOrEmpty(raw) && int.TryParse(raw.Trim(), out int value) && value > 0)
                return value;
        }
        return FallbackRewardGems;
    }

    /// <summary>默认奖励：发钻石（场景按钮直接打开弹窗时用）</summary>
    private void GrantDefaultReward(int amount)
    {
        if (GameManager.instance == null)
        {
            Debug.LogError($"[广告] GameManager 不存在，默认奖励 {amount} 宝石无法发放");
            return;
        }

        GameManager.instance.MoreGems(amount);
        Debug.Log($"[广告] 默认奖励已发放：宝石 +{amount}");
    }

    /// <summary>取调用方名字（跳过 AdConfirm 自己的栈帧），用于日志定位是哪个入口弹的窗</summary>
    private static string GetCallerHint()
    {
        try
        {
            var st = new System.Diagnostics.StackTrace();
            for (int i = 2; i < st.FrameCount && i < 8; i++)
            {
                var method = st.GetFrame(i)?.GetMethod();
                if (method == null || method.DeclaringType == typeof(AdConfirm))
                    continue;
                return $"{method.DeclaringType?.Name}.{method.Name}";
            }
        }
        catch
        {
            // 取不到调用栈不能影响弹窗本身
        }
        return "未知";
    }

    private Button FindButtonByName(string name)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == name)
            {
                return buttons[i];
            }
        }
        return null;
    }

    private Text FindTextByName(string name)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == name)
            {
                return texts[i];
            }
        }
        return null;
    }

    /// <summary>切换弹窗展示的奖励内容，空值回落到预制体默认文案</summary>
    private void SetRewardText(string rewardTitle, string rewardValue)
    {
        if (rewardTitleTxt != null)
        {
            rewardTitleTxt.text = string.IsNullOrEmpty(rewardTitle) ? defaultRewardTitle : rewardTitle;
        }

        if (rewardValueTxt != null)
        {
            rewardValueTxt.text = string.IsNullOrEmpty(rewardValue) ? defaultRewardValue : rewardValue;
        }
    }


    private void OnClose()
    {
        // 关窗即作废本次回调：否则上一次入口留下的回调会「粘」到下一次打开的弹窗上，
        // 玩家从 A 入口关掉弹窗、再从 B 入口打开看完广告，奖会发到 A 上。
        // （广告已经在播的情况不用担心：回调是通过闭包传进 AdManager 的，这里清空不影响已发出的请求）
        onCompleted = null;
        pendingReward = null;
        requesting = false;

        this.gameObject.SetActive(false);
    }

    void OnConfirm()
    {
        if (requesting)
        {
            Debug.Log("[广告] 上一次广告还在请求中，忽略本次点击");
            return;
        }

        // 这一次请求用哪个回调：实例字段优先，回落到全局待发奖回调。
        // 两条兜底都拿不到只能说明弹窗是被别的对象打开的，至少要把日志打清楚。
        Action cb = onCompleted != null ? onCompleted : pendingReward;
        if (cb == null)
        {
            Debug.LogError($"[广告] AdConfirm(实例 {GetInstanceID()}) 没拿到发奖回调，本次不会发奖" +
                           $"（弹窗被直接 SetActive 打开、且 OnEnable 兜底也没登记上）");
        }

#if UNITY_EDITOR
        Debug.Log("[广告] 编辑器环境，跳过广告直接发奖励");
        InvokeRewardSafely(cb);
        this.OnClose();
#else
        if (AdManager.Instance == null)
        {
            // 广告系统整个不可用（AdManager 没跟着场景走）是系统性故障，
            // 玩家点什么都没反应比「白给一次奖励」更糟，这里直接发奖并打错误日志。
            Debug.LogError("[广告] 场景里没有 AdManager，广告无法播放：直接发放奖励兜底");
            InvokeRewardSafely(cb);
            this.OnClose();
            return;
        }

        requesting = true;
        Debug.Log("[广告] 请求激励视频（AdConfirm）");

        // 关键：把回调通过闭包带进这次请求，不再依赖「回调执行时 this 还是原来那个实例」。
        // 真机上广告是异步的，期间切场景/重建弹窗都不会再把回调弄丢。
        AdManager.Instance.ShowRewardedVideoAd(() => OnAdWatched(cb), OnAdError);
#endif
    }

    private void OnAdWatched(Action cb)
    {
        Debug.Log("[广告] 广告观看完成，发放奖励");

        // 先发奖，再收尾：真机上广告是异步的，期间切场景可能已经把弹窗销毁，
        // 关弹窗失败绝不能连累奖励。
        InvokeRewardSafely(cb);
        requesting = false;

        try
        {
            OnClose();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[广告] 关闭确认弹窗失败（不影响奖励）：{e.Message}");
        }
    }

    /// <summary>
    /// 发奖回调统一包一层 try/catch：回调里任何异常（音效、UI 引用丢失等）
    /// 都不能把「奖励已发放」这件事吞掉，同时把异常打到日志里方便定位。
    /// </summary>
    private void InvokeRewardSafely(Action cb)
    {
        if (cb == null)
        {
            cb = onCompleted != null ? onCompleted : pendingReward;
        }

        if (cb == null)
        {
            Debug.LogWarning("[广告] 没有发奖回调，本次看完广告不会获得奖励");
            return;
        }

        // 先清空再执行：回调里如果又弹了新的广告，不会被这次的清理逻辑误删
        onCompleted = null;
        pendingReward = null;

        try
        {
            cb.Invoke();
            Debug.Log("[广告] 发奖回调已执行完毕");
        }
        catch (Exception e)
        {
            Debug.LogError($"[广告] 发奖回调执行异常：{e}");
        }
    }

    private void OnAdError(string errorMessage)
    {
        requesting = false;
        Debug.LogError($"广告播放失败: {errorMessage}");

        try
        {
            // 加载 AdError 预制体
            GameObject adErrorPrefab = Resources.Load<GameObject>("AdError");
            if (adErrorPrefab != null)
            {
                // 实例化 AdError
                GameObject adErrorInstance = Instantiate(adErrorPrefab, transform.position, Quaternion.identity);
                // 1 秒后销毁实例
                Destroy(adErrorInstance, 1.5f);
            }
            else
            {
                Debug.LogError("AdError 预制体未找到，请确保其位于 Resources 文件夹中。");
            }

            // 广告失败时把确认弹窗关掉，避免玩家以为「点了没反应」或者卡在弹窗上
            OnClose();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[广告] 广告失败后的收尾处理异常（不影响主流程）：{e.Message}");
        }
    }
}
