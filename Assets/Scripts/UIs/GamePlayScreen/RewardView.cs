using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RewardView : BaseView
{
    public int bonusCoinTotal;

    public int bonusGemTotal;

    public TextMeshProUGUI coinValueTxt;

    public TextMeshProUGUI gemValueTxt;

    public TextMeshProUGUI rw1Txt;

    public TextMeshProUGUI rw2Txt;

    public TextMeshProUGUI claimTxt;

    public TextMeshProUGUI claimX2Txt;

    public override void InitView()
    {
       
    }

    public void InitValue(int coins, int gems)
    {
        bonusCoinTotal = coins;
        bonusGemTotal = gems;

       
    }  

    public override void Start()
    {
        
    }

    public override void Update()
    {
        
    }

    public override void ShowView()
    {
        base.ShowView();
        AudioManager.instance.rwPopupSfx.Play();
        coinValueTxt.text = bonusCoinTotal.ToString();
        gemValueTxt.text = bonusGemTotal.ToString();
    }

    public void Claim()
    {
        
        AudioManager.instance.goldRWSfx.Play();
        GameManager.instance.MoreGold(bonusCoinTotal);
        GameManager.instance.MoreGems(bonusGemTotal);
        HideView();
        GameManager.instance.uiManager.homeTab.ChooseHome();
    }

    public void ClaimX2()
    {
        // 看激励视频领双倍奖励：直接拉起广告，不再弹 AdConfirm 确认框。
        // 玩家点「领取 ×2」本身就是「我要看广告」的明确表达，多一层确认只会增加流失。
        if (adRequesting)
            return;

        if (AdManager.Instance == null)
        {
            // 没有广告系统时不能让玩家白丢双倍，直接发双倍兜底
            Debug.LogError("[广告] 场景里没有 AdManager，双倍奖励广告无法播放：直接发双倍兜底");
            ClaimX2CB();
            return;
        }

        adRequesting = true;
        Debug.Log("[广告] 宝箱双倍奖励：请求激励视频");
        AdManager.Instance.ShowRewardedVideoAd(OnClaimX2AdSuccess, OnClaimX2AdFail);
    }

    // 广告请求中标记：防止玩家连点「领取 ×2」导致多次拉起广告
    private bool adRequesting;

    private void OnClaimX2AdSuccess()
    {
        adRequesting = false;
        ClaimX2CB();
    }

    private void OnClaimX2AdFail(string errorMessage)
    {
        adRequesting = false;
        Debug.LogError($"[广告] 双倍奖励广告失败，本次不发双倍：{errorMessage}");

        // 停留在奖励弹窗，玩家可以直接点「领取」拿基础奖励，不会卡死
        ShowAdTip("广告未看完，请重新观看或直接领取");
    }

    public void ClaimX2CB()
    {
        int coins = 2 * bonusCoinTotal;
        int gems = 2 * bonusGemTotal;
        Debug.Log($"[奖励] 双倍奖励到账：金币 +{coins}，宝石 +{gems}（基础值 {bonusCoinTotal}/{bonusGemTotal}）");

        // 先发奖再播音效：WebGL 下音频未加载完时 Play() 可能抛异常，
        // 一旦音效在前面抛掉，奖励就永远发不出去（表现为「看完广告没到账」）。
        GameManager.instance.MoreGold(coins);
        GameManager.instance.MoreGems(gems);
        PlayRewardSfx();

        HideView();
        GameManager.instance.uiManager.homeTab.ChooseHome();
    }

    /// <summary>奖励音效：完全隔离，任何异常都不影响已经到账的奖励</summary>
    private void PlayRewardSfx()
    {
        try
        {
            if (AudioManager.instance != null && AudioManager.instance.goldRWSfx != null)
            {
                AudioManager.instance.goldRWSfx.Play();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[奖励] 播放奖励音效失败（不影响奖励）：{e.Message}");
        }
    }

    public override void SetLangText()
    {
        rw1Txt.text = GleyLocalization.Manager.GetText("ACHIEVE_REWARD_TX");
        rw2Txt.text = GleyLocalization.Manager.GetText("DAILY_GIFT_REWARD_TX");
        claimTxt.text = GleyLocalization.Manager.GetText("ACHIEVE_CLAIM");
        claimX2Txt.text = GleyLocalization.Manager.GetText("DAILY_GIFT_CLAIM_VIDEO");
    }
}
