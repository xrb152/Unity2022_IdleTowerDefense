using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 上的「免费金币」按钮：点击弹出广告确认框，看完抖音激励视频发放金币。
/// 金币发放走本项目现有的 GameManager.instance.currentCoin。
/// </summary>
[RequireComponent(typeof(Button))]
public class FreeCoinButton : MonoBehaviour
{
    [SerializeField]
    private int rewardAmount = 200;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }

    public void OnClick()
    {
        Debug.Log("[广告] 点击免费金币按钮，弹出激励视频确认框");
        AdConfirm.Show(GrantReward);
    }

    private void GrantReward()
    {
        if (GameManager.instance == null)
        {
            Debug.LogError("[广告] GameManager 不存在，无法发放金币");
            return;
        }

        GameManager.instance.currentCoin += rewardAmount;
        Debug.Log($"[广告] 激励视频完成，发放金币 {rewardAmount}");
    }
}
