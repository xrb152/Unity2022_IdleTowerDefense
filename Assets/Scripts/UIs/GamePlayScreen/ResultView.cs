using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultView : BaseView
{
    public TextMeshProUGUI finishTxt, bestWaveTxt, receiveRewardTxt, receivex2RewardTxt;

    public TextMeshProUGUI goldValueTxt,gemValueTxt;

    public TextMeshProUGUI worldIDxt;

    public TextMeshProUGUI waveProgressTxt;

    public Slider waveProgressSlider;

    public override void InitView()
    {
        waveProgressTxt.text = GameManager.instance.enemyGenerator.currentWave + "/" + GameManager.instance.enemyGenerator.levelData[GameManager.instance.currentWorld].listEnemyWave.Count;
        waveProgressSlider.value = (float)GameManager.instance.enemyGenerator.currentWave/(float)GameManager.instance.enemyGenerator.levelData[GameManager.instance.currentWorld].listEnemyWave.Count;
        worldIDxt.text = GameManager.instance.enemyGenerator.levelData[GameManager.instance.currentWorld].worldID.ToString();

        int bestWavevalue = PlayerPrefs.GetInt("BestWave");

        if (GameManager.instance.enemyGenerator.currentWave >= bestWavevalue)
        {
           
            bestWavevalue = GameManager.instance.enemyGenerator.currentWave;
            PlayerPrefs.SetInt("BestWave", bestWavevalue);
        }

        GameManager.instance.bestWave = bestWavevalue;


        finishTxt.text = GleyLocalization.Manager.GetText("LAB_FINISH");
        bestWaveTxt.text = string.Format(GleyLocalization.Manager.GetText("GAME_OVER_BEST_WAVE"), bestWavevalue);
        receiveRewardTxt.text = GleyLocalization.Manager.GetText("GO_CLAIM_REWARD");
        receivex2RewardTxt.text = "X2 " + GleyLocalization.Manager.GetText("GO_CLAIM_REWARD");
        GameManager.instance.CalBonusReward();
        goldValueTxt.text = GameManager.instance.goldInGame.ToString();
        gemValueTxt.text = GameManager.instance.gemsInGame.ToString();

        //FirebaseManager.instance.LogScreen("WORLD " + GameManager.instance.currentWorld);
        //FirebaseManager.instance.LogScreen("BEST WAVE " + GameManager.instance.bestWave);

        if (GameManager.instance.currentState == GameManager.GAME_STATE.WIN)
        {
            GameManager.instance.currentWorld++;
            PlayerPrefs.SetInt("World", GameManager.instance.currentWorld);
        }

        StartCoroutine(ShowAds());
    }

    IEnumerator ShowAds()
    {
        yield return new WaitForSeconds(1.0f);

        // 玩家已经在看激励视频（点了 X2）时不要再弹插屏，避免两个广告打架
        if (adRequesting)
        {
            yield break;
        }

        // 结算插屏统一走 AdManager（带 30 秒节流），编辑器直连 Game 场景时没有 AdManager 就跳过
        if (AdManager.Instance == null)
        {
            yield break;
        }

        AdManager.Instance.TryShowInterstitialAd();
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
        InitView();
    }

    public override void HideView()
    {
        base.HideView();
    }

    public void Claim()
    {
        GameManager.instance.GetRWGameOver();
        GameManager.instance.BackToHome();
    }

    public void ClaimX2CB()
    {
        Debug.Log($"[奖励] 结算双倍到账：金币 +{2 * GameManager.instance.goldInGame}，宝石 +{2 * GameManager.instance.gemsInGame}");
        GameManager.instance.GetRWGameOverX2();
        GameManager.instance.BackToHome();
    }

    // 广告请求中标记：防止玩家连点「X2 领取奖励」导致多次拉起广告
    private bool adRequesting;

    /// <summary>
    /// X2 领取奖励：直接拉起激励视频，不再弹 AdConfirm 确认框。
    /// 结算页玩家点 X2 就已经是明确要看广告的意图，多一层确认纯属多余。
    /// </summary>
    public void ClaimX2()
    {
        if (adRequesting)
            return;

        if (AdManager.Instance == null)
        {
            Debug.LogError("[广告] 场景里没有 AdManager，双倍奖励广告无法播放：直接发双倍兜底");
            ClaimX2CB();
            return;
        }

        adRequesting = true;
        Debug.Log("[广告] 结算双倍：请求激励视频");
        AdManager.Instance.ShowRewardedVideoAd(OnClaimX2AdSuccess, OnClaimX2AdFail);
    }

    private void OnClaimX2AdSuccess()
    {
        adRequesting = false;
        ClaimX2CB();
    }

    private void OnClaimX2AdFail(string errorMessage)
    {
        adRequesting = false;
        Debug.LogError($"[广告] 双倍奖励广告失败，本次不发双倍：{errorMessage}");

        // 停留在结算页，玩家可以直接点「领取奖励」拿普通奖励，不会卡死
        try
        {
            if (GameManager.instance != null && GameManager.instance.uiManager != null
                && GameManager.instance.uiManager.notiView != null)
            {
                GameManager.instance.uiManager.notiView.ShowTip("广告未看完，请重新观看或直接领取");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[广告] 提示文案显示失败：{e.Message}");
        }
    }

    public override void SetLangText()
    {
        
    }
}
