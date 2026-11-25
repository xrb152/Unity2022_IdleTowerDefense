using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StoreView : BaseView
{
    public RectTransform moreGemBtn;

    public TextMeshProUGUI retriveTimerTxt;

    public TextMeshProUGUI freeGemsTxt;

    public TextMeshProUGUI gemsTxt;

    public TextMeshProUGUI getTxt;

    public TextMeshProUGUI specialTxt;

    public TextMeshProUGUI luckyChestTxt;

    [HideInInspector]
    public float timer;

    private float remainTimer;

    private bool activeTimer;

    public override void InitView()
    {
        remainTimer = PlayerPrefs.GetFloat("RewardTimer");

        if(remainTimer <= 0.0f)
        {
            DisableTimer();
        }
        else
        {
            ActiveTimer();
        }

        timer = remainTimer;
    }

    public override void Start()
    {
        InitView();
    }

    public override void Update()
    {
        if (activeTimer)
        {
            if (timer - Time.deltaTime > 0.0f)
            {
                timer -= Time.deltaTime;
                retriveTimerTxt.text = "重置时间：" + Common.ConvertTimer(timer);
            }
            else
            {
                timer = 0.0f;
                if (activeTimer)
                   DisableTimer();
            }
        }

    }

    public void GetFreeGems()
    {
        // 看激励视频领免费宝石：直接拉起广告，不再弹 AdConfirm 确认框。
        // 按钮本身写的就是「看视频领取」，再确认一次属于重复打断。
        if (adRequesting)
            return;

        if (AdManager.Instance == null)
        {
            Debug.LogError("[广告] 场景里没有 AdManager，免费宝石广告无法播放：直接发奖兜底");
            GetFreeGemsCB();
            return;
        }

        adRequesting = true;
        Debug.Log("[广告] 商店免费宝石：请求激励视频");
        AdManager.Instance.ShowRewardedVideoAd(OnGetFreeGemsAdSuccess, OnGetFreeGemsAdFail);
    }

    // 广告请求中标记：防止玩家连点导致多次拉起广告
    private bool adRequesting;

    private void OnGetFreeGemsAdSuccess()
    {
        adRequesting = false;
        GetFreeGemsCB();
    }

    private void OnGetFreeGemsAdFail(string errorMessage)
    {
        adRequesting = false;
        Debug.LogError($"[广告] 免费宝石广告失败，本次不发奖：{errorMessage}");

        // 按钮保持可点，玩家可以重新看广告
        ShowAdTip("广告未看完，请稍后再试");
    }

    public void GetFreeGemsCB()
    {
        // 先发奖再播音效：WebGL 下音频未就绪时 Play() 抛异常会把奖励一起吞掉
        GameManager.instance.MoreGems(10);
        Debug.Log("[奖励] 看广告宝石到账：宝石 +10");

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

        moreGemBtn.gameObject.SetActive(false);
        ActiveTimer();
    }

    public void OpenChest()
    {
        if (GameManager.instance.currentGem < 20)
        {
            GameManager.instance.uiManager.notiView.ShowLackCurrency(GleyLocalization.Manager.GetText("ALERT_LACK_GEM"));
            return;
        }
            
        GameManager.instance.SubGem(20);
        GameManager.instance.uiManager.rewardView.InitValue(Random.Range(50,1000), Random.Range(1,9));
        GameManager.instance.uiManager.rewardView.ShowView();
        HideView();
    }

    public void SaveTimer()
    {
       // Debug.Log("SAVE TIMER");
        remainTimer = timer;
        PlayerPrefs.SetFloat("RewardTimer", remainTimer);
    }

    public void ActiveTimer()
    {
        activeTimer = true;
        timer = Common.REWARD_TIMER;
        retriveTimerTxt.gameObject.SetActive(true);
        moreGemBtn.gameObject.SetActive(false);
    }

    public void DisableTimer()
    {
        activeTimer = false;
        retriveTimerTxt.gameObject.SetActive(false);
        moreGemBtn.gameObject.SetActive(true);
    }

    public override void SetLangText()
    {
        freeGemsTxt.text = GleyLocalization.Manager.GetText("STORE_FREE_GEM");
        gemsTxt.text = GleyLocalization.Manager.GetText("GEM");
        getTxt.text = GleyLocalization.Manager.GetText("CLAIM_TX");
        specialTxt.text = GleyLocalization.Manager.GetText("STORE_SPECIAL_OFFER_TITLE");
        luckyChestTxt.text = GleyLocalization.Manager.GetText("LUCKY_CHEST");
    }
}
