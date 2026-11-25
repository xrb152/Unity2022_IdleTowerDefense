using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public abstract class BaseView : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    [HideInInspector]
    public bool isShow;

    // Start is called before the first frame update
    public abstract void Start();


    // Update is called once per frame
    public abstract void Update();

    public abstract void InitView();

    public abstract void SetLangText();

    public virtual void ShowView()
    {
        canvasGroup.alpha = 0.0f;
        

        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1.0f, 0.2f).SetEase(Ease.Linear)
            .OnComplete(() => {

                canvasGroup.alpha = 1.0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                isShow = true;
            });

        /*
            .OnUpdate(() => {


            });

        */
    }

    public virtual void ShowView(string content)
    {
        ShowView();
    }

    public virtual void HideView()
    {
        

        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.2f).SetEase(Ease.Linear)
           .OnComplete(() => {

               canvasGroup.alpha = 0.0f;
               canvasGroup.interactable = false;
               canvasGroup.blocksRaycasts = false;
               isShow = false;
           });
    }

    /// <summary>
    /// 弹一条纯文字提示（不触发插屏）。
    /// 激励视频失败时统一用它兜底：任何异常都不影响主流程，玩家至少知道「广告没看完」而不是毫无反应。
    /// </summary>
    protected void ShowAdTip(string message)
    {
        try
        {
            if (GameManager.instance != null && GameManager.instance.uiManager != null
                && GameManager.instance.uiManager.notiView != null)
            {
                GameManager.instance.uiManager.notiView.ShowTip(message);
            }
            else
            {
                Debug.LogWarning($"[广告] {message}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[广告] 提示文案显示失败：{e.Message}");
        }
    }



}
