using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GleyLocalization;

public class RetriveView : BaseView
{
    public Button gemBtn, rewardBtn;


    public TextMeshProUGUI deadTxt;

    public TextMeshProUGUI notifyTxt;

    public TextMeshProUGUI continueTxt,continueBtnTxt;

    public override void InitView()
    {
        deadTxt.text = GleyLocalization.Manager.GetText("DEAD_RESULT");
        notifyTxt.text = GleyLocalization.Manager.GetText("REVIVAL_GUIDE");
        continueTxt.text = GleyLocalization.Manager.GetText("REVIVAL_REFRESH_CONTINUE");
        continueBtnTxt.text = GleyLocalization.Manager.GetText("REVIVAL_REFRESH_CONTINUE");
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
    }

    // 广告请求中标记：防止玩家连点「看广告复活」导致多次拉起广告
    private bool adRequesting;

    /// <summary>
    /// 看广告复活：直接拉起激励视频，不再弹 AdConfirm 确认框。
    /// 死亡复活是玩家已经明确做出的选择，多一层确认只会增加流失。
    /// </summary>
    public void RetriveByRewardVideo()
    {
        if (adRequesting)
            return;

        if (AdManager.Instance == null)
        {
            // 广告系统不可用时不能让玩家卡死在死亡界面，直接复活兜底
            Debug.LogError("[广告] 场景里没有 AdManager，复活广告无法播放：直接复活兜底");
            RetriveByRewardVideoCB();
            return;
        }

        adRequesting = true;
        Debug.Log("[广告] 复活：请求激励视频");
        AdManager.Instance.ShowRewardedVideoAd(OnRetriveAdSuccess, OnRetriveAdFail);
    }

    private void OnRetriveAdSuccess()
    {
        adRequesting = false;
        RetriveByRewardVideoCB();
    }

    private void OnRetriveAdFail(string errorMessage)
    {
        adRequesting = false;
        Debug.LogError($"[广告] 复活广告失败，本次不复活：{errorMessage}");
        ShowTip("广告未看完，请稍后再试");
    }

    public void RetriveByRewardVideoCB()
    {
        Debug.Log("[奖励] 看广告复活奖励已发放：本局继续");
        HideView();
        GameManager.instance.uiManager.gameView.GetRetrive();
        GameManager.instance.Retrive();
    }

    /// <summary>弹一条文字提示，任何异常都不影响主流程</summary>
    private void ShowTip(string message)
    {
        try
        {
            if (GameManager.instance != null && GameManager.instance.uiManager != null
                && GameManager.instance.uiManager.notiView != null)
            {
                GameManager.instance.uiManager.notiView.ShowTip(message);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[广告] 提示文案显示失败：{e.Message}");
        }
    }

    public void RetriveByGems()
    {
        if (GameManager.instance.currentGem >= 5)
        {
            HideView();
            GameManager.instance.uiManager.gameView.GetRetrive();
            GameManager.instance.Retrive();
            GameManager.instance.SubGem(5);
        }
        else
        {
            GameManager.instance.uiManager.notiView.ShowLackCurrency(GleyLocalization.Manager.GetText("ALERT_LACK_GEM"));
        }
    }

    public void Exit()
    {
        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.5f).SetEase(Ease.Linear)
          .OnComplete(() => {

              canvasGroup.alpha = 0.0f;
              canvasGroup.interactable = false;
              canvasGroup.blocksRaycasts = false;
              isShow = false;
              GameManager.instance.uiManager.resultView.ShowView();
          });

       
    }

    public override void SetLangText()
    {
       
    }
}
