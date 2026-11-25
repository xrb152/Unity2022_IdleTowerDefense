using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GleyLocalization;
using System.Data;
using UnityEngine.SceneManagement;

public class SettingView : BaseView
{
    public GameObject virOn, virOff, alarmOn, alarmOff;

    public TextMeshProUGUI settingTxt;

    public TextMeshProUGUI alarmTxt;

    public TextMeshProUGUI soundTxt;

    public TextMeshProUGUI musicTxt;

    public TextMeshProUGUI quitGameTxt;

    public TextMeshProUGUI vibrationTxt;

    public TextMeshProUGUI rateTxt;

    public Slider musicSlider;

    public Slider soundSlider;

    public override void InitView()
    {
        // 只保留简体中文，不再提供语言切换
    }

    public override void SetLangText()
    {
        settingTxt.text = GleyLocalization.Manager.GetText("SETTING_TITLE");
        alarmTxt.text = GleyLocalization.Manager.GetText("SETTING_ALARM");
        soundTxt.text = GleyLocalization.Manager.GetText("SETTING_SOUND");
        musicTxt.text = GleyLocalization.Manager.GetText("SETTING_MUSIC");
        vibrationTxt.text = GleyLocalization.Manager.GetText("SETTING_VIBRATE");
        rateTxt.text = GleyLocalization.Manager.GetText("SETTING_REVIEW");
        quitGameTxt.text = GleyLocalization.Manager.GetText("QUIT_GAME");
    }

    public override void Start()
    {
        InitView();
    }

    public override void Update()
    {
        
    }

    public override void HideView()
    {
        base.HideView();
    }

    public void QuitGame()
    {
        GameManager.instance.SaveAllData();
        if (GameManager.instance.currentState == GameManager.GAME_STATE.PLAYING)
            GameManager.instance.BackToHome();
        else
            Application.Quit();
    }

    public void SetMusic()
    {
        AudioManager.instance.SetMusic(musicSlider.value);
    }

    public void SetSound()
    {
        AudioManager.instance.SetSound(soundSlider.value);
    }

    public void ToggleAlarm()
    {
        AudioManager.instance.btnClickSfx.Play();
        if (AudioManager.instance.isAlarm)
        {
            AudioManager.instance.isAlarm = false;
            alarmOn.SetActive(false);
            alarmOff.SetActive(true);
        }
        else
        {
            AudioManager.instance.isAlarm = true;
            alarmOn.SetActive(true);
            alarmOff.SetActive(false);
        }
    }

    public void ToggleVir()
    {
        AudioManager.instance.btnClickSfx.Play();
        if (AudioManager.instance.isVibration)
        {
            AudioManager.instance.isVibration = false;
            virOn.SetActive(false);
            virOff.SetActive(true);
        }
        else
        {
            AudioManager.instance.isVibration = true;
            virOn.SetActive(true);
            virOff.SetActive(false);
        }
    }

    public void Rate()
    {
#if UNITY_IOS
        Application.OpenURL("");
#endif

#if UNITY_ANDROID
 Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
#endif
    }
}
