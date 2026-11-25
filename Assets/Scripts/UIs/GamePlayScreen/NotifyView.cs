using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NotifyView : BaseView
{
    public TextMeshProUGUI contentTxt;

    public override void InitView()
    {
        
    }

    public override void SetLangText()
    {
        
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
        StartCoroutine(HideViewIE());
    }

    IEnumerator HideViewIE()
    {
        yield return new WaitForSeconds(1.0f);
        HideView();
    }

    public override void HideView()
    {
        base.HideView();
    }

    public void InitContent(string _value)
    {
        contentTxt.text = _value;
    }

    /// <summary>
    /// 只弹文字提示，不带插屏广告（用于广告失败、操作失败等场景）
    /// </summary>
    /// <param name="_value">提示文案</param>
    public void ShowTip(string _value)
    {
        InitContent(_value);
        ShowView();
    }

    /// <summary>
    /// 货币不足提示：弹出文字提示，同时顺带展示一次插屏广告。
    /// 所有「金币/宝石不足」的判断都走这里，保证提示与插屏曝光口径一致。
    /// </summary>
    /// <param name="_value">提示文案</param>
    public void ShowLackCurrency(string _value)
    {
        InitContent(_value);
        ShowView();

        if (AdManager.Instance == null)
        {
            Debug.LogWarning("[广告] 场景里没有 AdManager，跳过货币不足插屏");
            return;
        }

        // 带节流：30 秒内只曝光一次，避免玩家连点导致抖音插屏报错刷屏
        AdManager.Instance.TryShowInterstitialAd();
    }
}
