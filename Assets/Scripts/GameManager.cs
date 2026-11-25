using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
//using static UnityEditor.Progress;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public ConfigManager configManager;

    public DataManager dataManager;

    public TurretController turretController;

    public EnemyGenerator enemyGenerator;

    public GameObject[] mapList;

    public FireballManager fireBallManager;

    public float enemyAttackRange;

    public GiftItem giftItem;

    public FloatingText giftFLoatingTxt;

    public FloatingText effectFLoatingTxt;

    [HideInInspector]
    public List<GameObject> enemyList;

    [HideInInspector]
    public int currentCoin;

    [HideInInspector]
    public int currentGold;

    [HideInInspector]
    public int goldInGame;

    [HideInInspector]
    public int gemsInGame;

    [HideInInspector]
    public int currentGem;

    [HideInInspector]
    public int currentKilledEnemy;

    [HideInInspector]
    public int currentKilledBoss;

    [HideInInspector]
    public int currentCrystal;

    [HideInInspector]
    public int currentWorld;

    [HideInInspector]
    public int bestWave;

    [HideInInspector]
    public int remainRetrive;

    [HideInInspector]
    public int turretUnlockAtkLevel;

    [HideInInspector]
    public int turretUnlockHpLevel;

    [HideInInspector]
    public int turretUnlockResourceLevel;

    [HideInInspector]
    public int turretUnlockAlliesLevel;

    [HideInInspector]
    public ParticleSystem upgradeTurretFx;

    [HideInInspector]
    public ParticleSystem clickFx;

    [HideInInspector]
    public ParticleSystem gemsFx;

    [HideInInspector]
    public GetCoinVfx getCoinVfx;

    public UIManager uiManager;

    public enum GAME_STATE
    {
        HOME_MENU,
        PLAYING,
        PAUSING,
        GAME_OVER,
        WIN
    }

    public GAME_STATE currentState;

    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;

        
    }

    // Start is called before the first frame update
    void Start()
    {
        currentState = GAME_STATE.HOME_MENU;
        remainRetrive = 5;
        GetFirstData();
        LoadData();
        //GleyLocalization.Manager.SetCurrentLanguage(SupportedLanguages.Vietnamese);
        CheckSystemLang();
        dataManager.InitData();
        turretController.InitTurret();
        uiManager.InitView();
        uiManager.homeView.InitView();
        uiManager.homeView.ShowView();
        uiManager.gameView.RefreshCurrencyText();
        enemyList = new List<GameObject>();

        if (uiManager.tutView.finishTut1 == 0)
        {
            uiManager.tutView.ShowView();
            uiManager.tutView.ShowTut1();
            uiManager.homeTab.HideView();
            uiManager.gameView.settingBtn.SetActive(false);
        }
        else
        {
            uiManager.gameView.settingBtn.SetActive(true);
            uiManager.homeTab.ShowView();
        }
            
    }

    void CheckSystemLang()
    {
        // 项目只提供简体中文，忽略系统语言与历史存档中的语言设置，始终强制简体中文
        GleyLocalization.Manager.SetCurrentLanguage(SupportedLanguages.ChineseTraditional);
    }

    public void InitGame()
    {
        currentState = GAME_STATE.PLAYING;
        goldInGame = 0;
        gemsInGame = 0;
        currentKilledEnemy = 0;
        currentKilledBoss = 0;
        RemoveAllEnemies();
        turretController.InitTurret();
        fireBallManager.ShowFireBall(Mathf.RoundToInt((float)GameManager.instance.dataManager.turretSatelliteUpgradeData[0].currentValue));
        enemyGenerator.InitLevel();
        uiManager.gameView.InitView();
        uiManager.gameView.mainBoard.IniteView();
        LoadWorld();
    }

    public void LoadWorld()
    {
        for (int i = 0; i < mapList.Length; i++)
            mapList[i].SetActive(false);

        mapList[currentWorld].SetActive(true);
    }

    void RemoveAllEnemies()
    {
        if (enemyList.Count == 0)
            return;

        for(int i = 0; i < enemyList.Count; i++)
        {
            Destroy(enemyList[i]);
            //SimplePool.Despawn(enemyList[i]);
        }

        enemyList.Clear();
    }

    public void BackToHome()
    {
        DefaultSpeed();
        RemoveAllEnemies();
        enemyGenerator.StopAllCoroutines();
        uiManager.settingView.HideView();
        uiManager.homeView.ShowView();
        uiManager.homeTab.ShowView();

        uiManager.gameView.HideView();
        
        uiManager.resultView.HideView();
        currentState = GAME_STATE.HOME_MENU;

        currentCoin = 0;
        goldInGame = 0;
        gemsInGame = 0;
        currentKilledEnemy = 0;
        currentKilledBoss = 0;
        dataManager.Refresh();
        uiManager.gameView.RefreshCurrencyText();

        if (uiManager.tutView.finishTut1 == 1)
        {
            uiManager.gameView.settingBtn.SetActive(true);
            
        }

        if (uiManager.tutView.finishTut3 == 0)
        {
            uiManager.tutView.ShowTut3();

        }

    }

    public void Retrive()
    {
        turretController.RemoveAllEnemiesInRange();
        currentState = GAME_STATE.PLAYING;
        turretController.InitTurret();
       
      
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
            InitGame();
        if (Input.GetKeyDown(KeyCode.R))
            Retrive();

        if (Input.GetKeyDown(KeyCode.G))
            MoreGold(10000);
        if (Input.GetKeyDown(KeyCode.Q))
            MoreGems(40);

        if (Input.GetKeyDown(KeyCode.S))
            SaveAllData();

    }

    public void PlayClickFx()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousPos2D = new Vector2(mousePos.x, mousePos.y);
        clickFx.transform.position = mousPos2D;
        clickFx.Play();
    }

    //to get if the palyer click on an ui element or on an object
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }


  

    public void ChangeGameSpeed()
    {
        if(Time.timeScale == 1)
        {
            Time.timeScale = 1.5f;
        }
        else if (Time.timeScale == 1.5f)
        {
            Time.timeScale = 2.0f;
        }
        else
        {
            Time.timeScale = 1.0f;
        }
    }

    public void DefaultSpeed()
    {
        Time.timeScale = 1.0f;
        uiManager.gameView.RefreshSpeedText();
    }

    IEnumerator GenGiftIE(int coinNum, int goldNum, Vector3 genPos)
    {
        yield return new WaitForSeconds(0.4f);

        //GiftItem coinItem = Instantiate(giftItem, genPos, Quaternion.identity);
        GameObject coinItemObj = SimplePool.Spawn(giftItem.gameObject, genPos, Quaternion.identity);
        GiftItem coinItem = coinItemObj.GetComponent<GiftItem>();
        coinItem.type = GiftItem.TYPE.COIN;
        coinItem.InitItem();

        //FloatingText mTextCoin = Instantiate(giftFLoatingTxt, genPos, Quaternion.identity);
        GameObject mTextCoinObj = SimplePool.Spawn(giftFLoatingTxt.gameObject, genPos, Quaternion.identity);
        FloatingText mTextCoin = mTextCoinObj.GetComponent<FloatingText>();
        mTextCoin.InitText("+" + coinNum, FloatingText.TYPE.COIN);
        currentCoin += coinNum;

        if (goldNum > 0)
        {
            yield return new WaitForSeconds(0.5f);

            Vector3 randomPos = genPos + new Vector3(Random.Range(0.0f, 0.1f), Random.Range(0.0f, 0.1f));

            //GiftItem goldItem = Instantiate(giftItem, randomPos, Quaternion.identity);
            GameObject goldItemObj = SimplePool.Spawn(giftItem.gameObject, randomPos, Quaternion.identity);
            GiftItem goldItem = goldItemObj.GetComponent<GiftItem>();
            goldItem.type = GiftItem.TYPE.GOLD;
            goldItem.InitItem();
            goldInGame += goldNum;

            // FloatingText mTextGold = Instantiate(giftFLoatingTxt, randomPos, Quaternion.identity);
            GameObject mTextGoldObj = SimplePool.Spawn(giftFLoatingTxt.gameObject, randomPos, Quaternion.identity);
            FloatingText mTextGold = mTextGoldObj.GetComponent<FloatingText>();
            mTextGold.InitText("+" + goldNum, FloatingText.TYPE.GOLD);
        }

       
        
        uiManager.gameView.RefreshCurrencyText();

    }

    public void GenGift(int coinNum, int goldNum, Vector3 genPos)
    {
        StartCoroutine(GenGiftIE(coinNum,goldNum,genPos));
    }

    public void SpawnFLoatingTxt(string mContent, FloatingText.TYPE mType, Vector3 genPos)
    {
        //FloatingText mTextGold = Instantiate(giftFLoatingTxt, genPos, Quaternion.identity);
        GameObject mTextGoldObj = SimplePool.Spawn(giftFLoatingTxt.gameObject, genPos, Quaternion.identity);
        FloatingText mTextGold = mTextGoldObj.GetComponent<FloatingText>();
        mTextGold.InitText(mContent, mType);
    }

    public void SpawnEffectTxt(string mContent, FloatingText.TYPE mType, Vector3 genPos)
    {
        //FloatingText mTextGold = Instantiate(giftFLoatingTxt, genPos, Quaternion.identity);
        GameObject mTextGoldObj = SimplePool.Spawn(effectFLoatingTxt.gameObject, genPos, Quaternion.identity);
        FloatingText mTextGold = mTextGoldObj.GetComponent<FloatingText>();
        mTextGold.InitText(mContent, mType);
    }

    public void CalBonusReward()
    {
        int goldBonus1 = Mathf.RoundToInt((float)GameManager.instance.dataManager.turretCoinUpgradeData[0].currentValue) * (enemyGenerator.currentWave + 1);

        //Debug.Log("GOLD 1 " + goldBonus1);

        int gemBonus = Mathf.RoundToInt((float)GameManager.instance.dataManager.turretCoinUpgradeData[1].currentValue) * (currentKilledEnemy/50);

        //Debug.Log("GOLD 2 " + gemBonus);

        int goldBonus2 = Mathf.RoundToInt((float)GameManager.instance.dataManager.turretCoinUpgradeData[2].currentValue) * (currentKilledEnemy / 100);

        //Debug.Log("GOLD 1 " + goldBonus2);

        int goldBonus3 = Mathf.RoundToInt((float)GameManager.instance.dataManager.turretCoinUpgradeData[3].currentValue) * currentKilledBoss;

        //Debug.Log("GOLD 1 " + goldBonus3);

        goldInGame = goldInGame + goldBonus1 + goldBonus2 + goldBonus3;
        gemsInGame = gemsInGame + gemBonus;
    }

    public void GetRWGameOver()
    {
        MoreGold(goldInGame);

        MoreGems(gemsInGame);

        
    }

    public void GetRWGameOverX2()
    {
        MoreGold(2*goldInGame);

        MoreGems(2*gemsInGame);


    }


    public void GetFirstData()
    {
        if(PlayerPrefs.GetInt("FirstData") == 0)
        {
            PlayerPrefs.SetInt("RemainRetrive", 5);
            PlayerPrefs.SetInt("FirstData", 1);
        }


    }

    public void LoadData()
    {
        currentGold = PlayerPrefs.GetInt("Gold");
        currentGem = PlayerPrefs.GetInt("Gem");
        currentCrystal = PlayerPrefs.GetInt("Crystal");
        turretUnlockAtkLevel = PlayerPrefs.GetInt("TurretUnlockAtkLevel");
        turretUnlockHpLevel = PlayerPrefs.GetInt("TurretUnlockHpLevel");
        turretUnlockResourceLevel = PlayerPrefs.GetInt("TurretUnlockResourceLevel");
        turretUnlockAlliesLevel = PlayerPrefs.GetInt("TurretUnlockAlliesLevel");
        remainRetrive = PlayerPrefs.GetInt("RemainRetrive");
        uiManager.gameView.timer = Common.COUNT_DOWN_TIMER;
        currentWorld = PlayerPrefs.GetInt("World");
        bestWave = PlayerPrefs.GetInt("BestWave");

        uiManager.tutView.finishTut1 = PlayerPrefs.GetInt("Tut1");
        uiManager.tutView.finishTut2 = PlayerPrefs.GetInt("Tut2");
        uiManager.tutView.finishTut3 = PlayerPrefs.GetInt("Tut3");
        uiManager.tutView.finishTut4 = PlayerPrefs.GetInt("Tut4");
    }

    public void SaveAllData()
    {
        SaveCurrency();
        PlayerPrefs.SetInt("RemainRetrive", remainRetrive);
        PlayerPrefs.SetInt("World", currentWorld);
        uiManager.storeView.SaveTimer();
        uiManager.homeView.SaveTimer();
    }

    public void MoreGold(int moreGold)
    {
        // 收益为 0 时不播特效、不加金币，避免连续调用时反复播放飞金币动画
        if (moreGold <= 0)
        {
            Debug.LogWarning($"[奖励] MoreGold 收到非正数 {moreGold}，已忽略（若出现在看广告后，说明奖励数值算出来是 0）");
            return;
        }

        // 先结算：金币到账 + 立刻落盘。后面的音效/特效/UI 刷新各自兜底，
        // 任何一处失败都不会让「奖励已经到账」这件事变成泡影。
        currentGold += moreGold;
        PlayerPrefs.SetInt("Gold", currentGold);
        Debug.Log($"[奖励] 金币到账 +{moreGold}，当前金币 {currentGold}");
        SafeRefreshCurrency();

        //uiManager.gameView.coinIconInBoard.localScale = new Vector3(1.25f, 1.25f, 1.25f);
        //uiManager.shopView.coinIconInBoard.localScale = new Vector3(1.25f, 1.25f, 1.25f);
        try
        {
            if (AudioManager.instance != null && AudioManager.instance.flyingCoinSfx != null)
            {
                AudioManager.instance.flyingCoinSfx.Play();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[奖励] 飞金币音效播放失败（不影响奖励）：{e.Message}");
        }

        try
        {
            if (getCoinVfx != null)
            {
                getCoinVfx.SpawnCoinVfx();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[奖励] 金币粒子特效播放失败（不影响奖励）：{e.Message}");
        }
        //uiManager.gameView.coinIconInBoard.DOScale(1f, 0.25f).SetDelay(0.75f).SetEase(Ease.Linear);

        int currentMoreCoin = 0;

        DOTween.To(() => currentMoreCoin, x => currentMoreCoin = x, moreGold, 1.0f).SetEase(Ease.Linear)
            .SetUpdate(true)   // 用非缩放时间，暂停状态下动画也能跑完
            .OnUpdate(() =>
            {

                //uiManager.gameView.coinTxt.text = (GameManager.instance.currentCoin + currentMoreCoin).ToString();
                //uiManager.shopView.coinTxt.text = (GameManager.instance.currentCoin + currentMoreCoin).ToString();

            });
    }

    /// <summary>
    /// 刷新货币文本：UI 管理器或界面对象缺失时不能抛异常，
    /// 否则调用方（发奖回调）会被中断，玩家看到的就是「奖励没到账」。
    /// </summary>
    private void SafeRefreshCurrency()
    {
        try
        {
            if (uiManager != null && uiManager.gameView != null)
            {
                uiManager.gameView.RefreshCurrencyText();
            }
            else
            {
                Debug.LogWarning("[奖励] uiManager/gameView 为空，货币数字暂不刷新（数据已入账并落盘）");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[奖励] 刷新货币文本失败（数据已入账并落盘）：{e.Message}");
        }
    }

    public void MoreGems(int moreGems)
    {
        // 收益为 0 时不播特效，避免连续调用时反复播放宝石特效
        if (moreGems <= 0)
        {
            Debug.LogWarning($"[奖励] MoreGems 收到非正数 {moreGems}，已忽略（若出现在看广告后，说明奖励数值算出来是 0）");
            return;
        }

        currentGem += moreGems;
        PlayerPrefs.SetInt("Gem", currentGem);
        Debug.Log($"[奖励] 宝石到账 +{moreGems}，当前宝石 {currentGem}");
        SafeRefreshCurrency();

        // 特效放在最后：即使特效缺失/报错，也不影响奖励已经到账
        try
        {
            if (gemsFx != null)
            {
                gemsFx.Play();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[奖励] 宝石特效播放失败（不影响奖励）：{e.Message}");
        }
    }

    public void SubGold(int subGold)
    {
        currentGold -= subGold;
        PlayerPrefs.SetInt("Gold", currentGold);
        uiManager.gameView.RefreshCurrencyText();
    }

    public void MoreX4Coin()
    {
        currentCoin *= 4;
        uiManager.gameView.RefreshCurrencyText();
    }

    public void SubGem(int subGem)
    {
        currentGem -= subGem;
        PlayerPrefs.SetInt("Gem", currentGem);
        uiManager.gameView.RefreshCurrencyText();
    }

    public void SaveCurrency()
    {
        PlayerPrefs.SetInt("Gold", currentGold);
        PlayerPrefs.SetInt("Gem", currentGem);
    }

    void OnApplicationQuit()
    {
        SaveAllData();
    }
    bool isPaused = false;
    void OnApplicationFocus(bool hasFocus)
    {
        isPaused = !hasFocus;
        if (isPaused)
            SaveAllData();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        isPaused = pauseStatus;
        if (isPaused)
            SaveAllData();
    }

  
}
