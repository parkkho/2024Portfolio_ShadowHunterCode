using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using JHSOFT_BALANCE;
using Cinemachine;
//using UnityEngine.Rendering.Universal;
//using UnityEngine.Rendering;
using System;

public class IdleGameManager : MonoBehaviour // 방치형 게임 전체적인 관리
{
    public static IdleGameManager Instance { get; private set; }

    public enum GameState { Ready, Play, Clear, Over }; // 게임 상태

    public enum GameMode { Idle, Dungeon, PVP , BossChallenge }; //방치, 던전 ,PVP , 보스도전

    public enum IdleGameType { Challenge , Repeat }; // 게임 방치모드 타입 -> 도전 / 반복

    public GameState gameState = GameState.Ready;

    public GameMode gameMode = GameMode.Idle;

    public IdleGameType idleGameType = IdleGameType.Challenge; // 게임 방치모드 타입 


    public Player player; // 플레이어

    public GameObject earnEffect; // 획득 이펙트

    public Transform earnEffParent; // 획득이펙트 생성 부모

   
    [SerializeField] CinemachineVirtualCamera mainVirtualCam; // 메인 카메라 - 시네머신 카메라 사용
    CinemachineBasicMultiChannelPerlin virtualCameraNoise;

    public bool isBossStage = false; // 보스 스테이지 인지

   

    /// <summary>
    /// 유저 데이터
    /// </summary>
    public float UserExp // 유저 경험치
    {
        get { return BackendManager.Instance.GameData.UserData.UserExp; }
        set
        {
            BackendManager.Instance.GameData.UserData.ExpUpdate(value);
        }
    }

    public float UserGem // 유저 보석
    {
        get { return BackendManager.Instance.GameData.UserData.Gem; }
        set
        {
            BackendManager.Instance.GameData.UserData.GemUpdate(value);
            UIManager.Instance.UpdateGemUI();
        }
    }

    public double UserGold // 유저 골드
    {
        get { return BackendManager.Instance.GameData.UserData.Gold; }
        set
        {
            BackendManager.Instance.GameData.UserData.GoldUpdate(value);
            UIManager.Instance.UpdateGoldUI();
        }
    }
    
    public int UserLevel // 유저 레벨
    {
        get { return BackendManager.Instance.GameData.UserData.Level; }
        set
        {
            BackendManager.Instance.GameData.UserData.LevelUpdate(value);
            UIManager.Instance.userLevelTMP.text = string.Format("LV.{0}", value);
        }
    }

    // 메인 스테이지 UI
    [Header("### StageUI ###")]

    public GameObject stageUI;
    public TMP_Text stageTMP; // 스테이지
    public TMP_Text dieEnemyCountTMP; // 죽은 몬스터 정보
    public Slider stageSlider; // 스테이지 진행슬라이더

    // 보스 스테이지 UI
    [Header("### BossUI ###")]

    public GameObject bossUI;
    public TMP_Text bossHpTMP; // 보스 총 체력 정보
    public Slider bossHpSlider; // 보스 체력 슬라이더
    public Timer timer; // 타이머

    // 메인 게임 관련 UI
    [Header("### UI ###")]
    public TMP_Text clearRewardTMP; // 스테이지 클리어 보석 보상 텍스트

    public Slider userExpSlider; // 유저 경험치

    public GameObject gameClearPanel;
    public GameObject gameOverPanel;
    public GameObject bossSummonPanel;

    public Button nextStageBtn; //상단에 있는 다음 스테이지 버튼
    public Button autoChallengeBtn; //자동도전
    public Button repeatStageBtn; // 반복전투

    public List<HuntRewardSlot> huntRewardSlots; // 사냥 보상 획득 UI

    public ItemConfig itemConfig; // 아이템DB

    [Header("#MapData")]

    public List<GameObject> mapPrefabs; // 사용하는 맵 프리팹 -> 테스트를 위해 리스트에 저장 후 사용

    public GameObject curMap; //현재 맵 오브젝트
    public GameObject curSky; // 현재 하늘 오브젝트

    public int curMapID = -1;
    public int curMapAtt = -1; // 현재 맵 속성
    public int curMapType = -1; // 현재 맵 타임

    Dictionary<int, GameObject> mapDictionary = new Dictionary<int, GameObject>(); // 미리 생성한 맵 불필요 시 비활성 후 저장

    [Header("#Information")]

    public int curStage; // 현재 스테이지

    public int lastStage = 0; // 마지막 플레이 스테이지

  
    public StageData StageData { get; private set; } // 현재 스테이지 데이터

    bool isRepeat = false; //반복전투 버튼 활성 여부 false: 자동도전 / true: 반복전투

    public int level;

    public float maxExp;

    public Action GameStartEvent; // 게임 시작 이벤트
    public Action GameClearEvent; // 게임 클리어 이벤트
    public Action GameOverEvent; // 게임 오버 이벤트
    

    private void Awake()
    {
        Instance = this;

        DOTween.SetTweensCapacity(3000, 200);
    }

    // Start is called before the first frame update
    void Start()
    {
        virtualCameraNoise = mainVirtualCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        nextStageBtn.onClick.AddListener(() => OnClickNextStageButton());
        autoChallengeBtn.onClick.AddListener(() => OnClickAutoChallengeButton());
        repeatStageBtn.onClick.AddListener(() => OnClickRepeatStageButton());

        LoadPlayerData();

      //  gameMode = GameMode.PVP;
     //   gameState = GameState.Play;

        // 게임시작
        if (gameMode == GameMode.Idle)
        { StartGame(); }

       

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    void LoadPlayerData()
    {
        // 유저 데이터 불러오기
        level = BackendManager.Instance.GameData.UserData.Level;
        maxExp = MathMethods.GetUserMaxExp(level);

        userExpSlider.maxValue = maxExp;
        userExpSlider.value = UserExp;

        UIManager.Instance.userLevelTMP.text = string.Format("LV.{0}", level);

        curStage = BackendManager.Instance.GameData.UserData.ClearStage;

        // 반복전투 활성화 시 반복 전투 모드로 실행
        if (isRepeat)
        {
            repeatStageBtn.gameObject.SetActive(true);
            idleGameType = IdleGameType.Repeat;
        }
        else
        {
            autoChallengeBtn.gameObject.SetActive(true);
            idleGameType = IdleGameType.Challenge;
            curStage++;
        
        }

        // 최초 0스테이지 일경우
        if (curStage == 0) curStage++;

    }

    // 게임 시작
    public void StartGame()
    {
        // 현재 플레이할 스테이지와 최근 플레이 스테이지가 다를 경우 스테이지 데이터를 다시 로드
        if (curStage != lastStage)
        {
            lastStage = curStage;
            StageData = DataManager.Instance.GetStageData(curStage);

            // 맵 로드
            LoadStageMap();
        }
        else
        {
            if(curMapID != StageData.stageAtt)
            LoadStageMap();
        }

        

        // 게임 모드에 따른 UI 정리
        if (idleGameType == IdleGameType.Repeat)
        {
            stageSlider.gameObject.SetActive(false);
            nextStageBtn.gameObject.SetActive(true);

        }
        else
        {
            stageSlider.gameObject.SetActive(true);
            nextStageBtn.gameObject.SetActive(false);
        }

        stageTMP.text = string.Format("STAGE {0}", curStage);

        // 보스 스테이지 여부 체크
        isBossStage = StageData.isBossStage;

        gameState = GameState.Play;

        EnemyManager.Instance.LoadStageEnemySpawnData();
        EnemyManager.Instance.StartSpawnEnemy();
        //  StartCoroutine(StartGameCoro());

    }

    // 맵 불러오기
    void LoadStageMap()
    {
        int mapAtt = StageData.stageAtt;
        int mapType = StageData.mapType;

        curMapAtt = mapAtt;
        curMapType = mapType;

        LoadGameMap(mapAtt);

    }

    public void LoadGameMap(int mapID)
    {
        if (curMapID != mapID)
        {
            // 기존 맵 있을 시 비활성
            if (curMapID >= 0)
            {
                curMap.SetActive(false);
                curSky.SetActive(false);
                curSky.transform.SetParent(curMap.transform);
                curSky.transform.localPosition = Vector3.zero;
            }

            curMapID = mapID;
            //curMapType = mapType;

            if (mapDictionary.ContainsKey(curMapID) == false)
            {
                curMap = Instantiate(mapPrefabs[curMapID], transform);
                curMap.transform.localPosition = Vector3.zero;

                mapDictionary.Add(curMapID, curMap);

            }
            else
            {
                curMap = mapDictionary[curMapID];
            }

            // 하늘은 메인 카메라 자식으로 변경
            curSky = curMap.transform.GetChild(2).gameObject;
            curSky.transform.SetParent(mainVirtualCam.transform);

            curMap.SetActive(true);
            curSky.SetActive(true);
            //   curSky.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);



        }
    }
    

 
    // 게임 초기화 -> 재시작 or 컨텐츠이동
    public void ResetGame()
    {
        player.ResetPlayer();
        player.gameObject.SetActive(false);

        // 적 스폰 초기화
        EnemyManager.Instance.ResetSpawnEnemy();
    }

    // 죽고 다시 시작  or 반복 전투 => 자동도전으로 모드 변경 시
    public void RestartGame()
    {
        // 화면 페이드
        // UIManager.Instance.OpenFadePanel(1.5f);

        // 유저 초기화
        ResetGame();

        StartCoroutine(RestartGameCoro());
    }

    // 재시작 코루틴
    IEnumerator RestartGameCoro()
    {
        yield return new WaitForSeconds(0.5f);

        player.ReadyPlayer();
        player.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        StartGame();
    }


    // 스테이지 클리어
    public void StageClear()
    {
        if (gameState != GameState.Play) return;
        gameState = GameState.Clear;

        SoundManager.instance.PlaySfx(SoundManager.Sfx.GameClear, 0.5f);

        // 보스스테이지 일 경우 타이머 멈춤
        if (isBossStage)
        {
            timer.StopTimer();
        }

        if (gameMode == GameMode.Idle)
        {
            float rewardGem = MathMethods.GetStageGemReward(curStage);
            clearRewardTMP.text = rewardGem.ToString();

            UserGem = MathMethods.GetStageGemReward(curStage);

            // 스테이지 데이터 저장
            BackendManager.Instance.GameData.UserData.StageClear();

            // 다음스테이지로 데이터로드
            if (isRepeat == false)
            {
                curStage++;
 
            }
            else // 반복켜기 활성화 시 반복 모드로 변경
            {
                idleGameType = IdleGameType.Repeat;
            }

            // 플레이어 체력회복
            player.playerAtkHandler.RecoverHp(player.basePlayerStat.hp);

            QuestManager.Instance.CheckQuest(MissionType.Stage , 1);

            StartCoroutine(ClearCoro());

            
        }
        else
        {
            GameClearEvent?.Invoke();
            GameClearEvent = null;

            StartCoroutine(FinishContentCoro());
        }
       
        // gameState = GameState.Play;

      

    }

    // 클리어 코루틴
    IEnumerator ClearCoro()
    {
        gameClearPanel.SetActive(true);

        PlayEarnEffect(1002, gameClearPanel.transform.position, UIManager.Instance.userGemTMP.transform.position, earnEffParent);

        yield return YieldCache.WaitForSeconds(1.0f);

        if (isBossStage)
        {       
            EndBossStage();
        }

        gameClearPanel.SetActive(false);
        StartGame();

    }

    // 일반 스테이지 외 클리어 코루틴
    IEnumerator FinishContentCoro()
    {
        UIManager.Instance.OpenFadePanel(1f, 0.5f);
        yield return YieldCache.WaitForSeconds(1.0f);

        if (isBossStage)
        {
            if (gameState == GameState.Over)
            {
                EnemyManager.Instance.RemoveBossEnemy();

            }

            Debug.Log("Test");
            EndBossStage();
        }

        if(gameMode == GameMode.Dungeon)
        {
             DungeonManager.Instance.dungeonButton.SetActive(true);
        }
       

        gameMode = GameMode.Idle;
        RestartGame();
    }


    // 게임오버 UI 활성
    IEnumerator GameOverCoro()
    {
        gameOverPanel.SetActive(true);
        StartCoroutine(FinishContentCoro());
        yield return YieldCache.WaitForSeconds(1.0f);

        
        //  RestartGame();

        if (gameOverPanel.activeSelf == true)
        {
            gameOverPanel.SetActive(false);
        }

    }

    // 게임오버 -> 이전 스테이지로
    public void GameOver()
    {
        if (gameState != GameState.Play) return;

        gameState = GameState.Over;

        SoundManager.instance.PlaySfx(SoundManager.Sfx.GameOver, 0.5f);

        if (gameMode == GameMode.Idle)
        {
            if (idleGameType == IdleGameType.Challenge)
            {
                if (curStage > 1)
                {
                    // 이전 스테이지, 반복모드로
                    curStage--;

                    // 이전 스테이지가 보스 스테이지면 한단계 더 이전으로
                    if (CheckBossStage(curStage)) curStage--;

                    idleGameType = IdleGameType.Repeat;

                    // UI정리
                    if (isRepeat == false) OnClickAutoChallengeButton();
                    // isRepeat = true;
                }
            }

          //  StartCoroutine(GameOverCoro());
        }
        else
        {
            GameOverEvent?.Invoke();
            GameOverEvent = null;
            
               
        }

        StartCoroutine(GameOverCoro());
    }



    public IEnumerator MoveEarnItem(GameObject itemObj)
    {
        yield return YieldCache.WaitForSeconds(0.5f);

        float moveTime = 0.5f;

        float time = 0f;

        Vector3 sPos = itemObj.transform.position;

        while (time < moveTime)
        {
            time += Time.deltaTime;

            // Debug.Log(time);

            itemObj.transform.position = Vector3.Lerp(sPos, player.transform.position, time / moveTime);

            yield return null;
        }

       // player.MonsterLifeSteal(hp);
       // player.EarnTransformEnergy(0.01f);

        itemObj.SetActive(false);
      //  bloodPool.Add(itemObj);

    }


    // 죽은 몬스터 수 업데이트
    public void UpdateDieEnemy(int dieCnt)
    {
        if (isBossStage) return;

        stageSlider.value = dieCnt;

        dieEnemyCountTMP.text = string.Format("{0}/{1}", dieCnt, (int)stageSlider.maxValue);

    }

    // 아이템 획득 이펙트
    public void PlayEarnEffect(int itemID, Vector3 sPos, Vector3 fPos, Transform parent)
    {
        GameObject eff = Instantiate(earnEffect, parent);

        RectTransform rect = eff.GetComponent<RectTransform>();

        rect.position = new Vector3(sPos.x, sPos.y, 0);

        rect.anchoredPosition3D = new Vector3(rect.anchoredPosition.x, rect.anchoredPosition.y, 0);

        eff.GetComponent<EarnEffect>().SetEffect(itemID, fPos);
    }

    // 경험치 획득
    public void EarnUserExp(float exp)
    {
        SetHuntRewardSlot(1000, exp.ToString());

        float userExp = UserExp;

        userExp += exp;

        // 레벨업
        if (userExp >= maxExp)
        {
            level++;

            UserLevel = level;

            userExp = 0f;

            maxExp = MathMethods.GetUserMaxExp(level);

            userExpSlider.maxValue = maxExp;

            //BackendManager.Instance.GameData.UserData.LevelUpdate(level);
            //UIManager.Instance.userLevelTMP.text = string.Format("LV.{0}", level);
        }

        userExpSlider.value = userExp;

        UserExp = userExp;

    }

    //반복전투 버튼 클릭 -> 자동도전으로 변경
    public void OnClickRepeatStageButton()
    {
        isRepeat = false;

        repeatStageBtn.gameObject.SetActive(false);
        autoChallengeBtn.gameObject.SetActive(true);

        int userClearStage = BackendManager.Instance.GameData.UserData.ClearStage;

        // 클리어 해야할 최근 스테이지로 재시작
        if (curStage <= userClearStage)
        {
            Debug.Log("체크");
            curStage = userClearStage + 1;

            idleGameType = IdleGameType.Challenge;

            // 화면 페이드
            UIManager.Instance.OpenFadePanel(1.5f, 0.5f);
            RestartGame();
        }
    }

    //자동전투 버튼 클릭 -> 반복도전으로 변경
    public void OnClickAutoChallengeButton()
    {
        isRepeat = true;

        repeatStageBtn.gameObject.SetActive(true);
        autoChallengeBtn.gameObject.SetActive(false);
    }

    // 다음 스테이지 버튼
    public void OnClickNextStageButton()
    {
        OnClickRepeatStageButton();

        // nextStageBtn.gameObject.SetActive(false);
    }

    // 사냥보상 정보창 활성
    public void SetHuntRewardSlot(int itemID, string value)
    {
        Item reward = itemConfig.GetItemData(itemID);

        HuntRewardSlot slot = huntRewardSlots[0];

        huntRewardSlots.RemoveAt(0);
        huntRewardSlots.Add(slot);

        slot.transform.SetAsFirstSibling();

        slot.InitSlot(reward.itemIcon, reward.name, value);
        slot.gameObject.SetActive(true);
        ScaleSequence(slot.transform, 0.3f);

    }

    void ScaleSequence(Transform trf, float time)
    {
        Sequence ScaleSequence = DOTween.Sequence();

        ScaleSequence.OnStart(() => trf.localScale = new Vector3(0, 1, 1))
            .Append(trf.DOScaleX(1f, time).SetEase(Ease.InQuart));

    }

    // 보스 스테이지 여부 체크
    bool CheckBossStage(int checkStage)
    {
        if (checkStage == 5) return true;

        if (checkStage % 10 == 0) return true;

        return false;
    }


    //---------------------------------------
    // 보스 출현 관련
    //---------------------------------------
    #region

    // 보스몬스터 출현 시 카메라 보스몬스터 추적 or 다시 되돌리기
    public void FollowCameraBossMonster(Transform target, float dampingValue)
    {
        mainVirtualCam.GetCinemachineComponent<CinemachineTransposer>().m_XDamping = dampingValue;
        mainVirtualCam.GetCinemachineComponent<CinemachineTransposer>().m_YDamping = dampingValue;

        mainVirtualCam.Follow = target;

    }

    // 보스 소환 루틴
    public IEnumerator BossSummonCoro(Transform bossTrf)
    {
        gameState = GameState.Ready;

        SetContentUI(true);

        // dieEnemyCountTMP.gameObject.SetActive(false);

        FollowCameraBossMonster(bossTrf, 2f);

        yield return YieldCache.WaitForSeconds(1.5f);

        SoundManager.instance.PlaySfx(SoundManager.Sfx.BossStage, 0.5f);
        bossSummonPanel.SetActive(true);

        yield return YieldCache.WaitForSeconds(1.0f);

        bossSummonPanel.SetActive(false);

        FollowCameraBossMonster(player.transform, 0.5f);

        gameState = GameState.Play;

        // 타이머 세팅
        timer.gameObject.SetActive(true);
        timer.StartTimer(CONSTANTS.BaseStageTime);
        timer.EndTimerEvent = GameOver;

        EnemyManager.Instance.MoveBossEnemy();
    }

    // 보스 체력 정보 UI 세팅
    public void SetBossHpUI(float hp)
    {
        bossHpSlider.maxValue = hp;
        bossHpSlider.value = hp;

        bossHpTMP.text = Utils.ToCurrencyString(hp);
    }

    // 보스 스테이지 종료
    public void EndBossStage()
    {
       // bossHpSlider.gameObject.SetActive(false);
        bossHpSlider.maxValue = 0;
        bossHpSlider.value = 0;

       
        timer.gameObject.SetActive(false);
        SetContentUI(false);
    }

    #endregion

    // 스테이지 모드 -> 스테이지 UI활성 , 보스 일 경우 보스UI 활성
    public void SetContentUI(bool isBossMode)
    {
        stageUI.SetActive(!isBossMode);
        bossUI.SetActive(isBossMode);
    }

    // 카메라 쉐이크
    public void ShakeMainCamera(float duration, float shakePower)
    {
        StartCoroutine(ShakeCameraCoro(duration, shakePower));
    }

    IEnumerator ShakeCameraCoro(float duration, float shakePower)
    {
        virtualCameraNoise.m_AmplitudeGain = shakePower;
        virtualCameraNoise.m_FrequencyGain = shakePower;

        yield return YieldCache.WaitForSeconds(duration);

        virtualCameraNoise.m_AmplitudeGain = 0f;
        virtualCameraNoise.m_FrequencyGain = 0f;
    }

    // 오프라인 보상 클릭
    public void OnClickOfflineReward()
    {
        Vector3 sPos = earnEffParent.transform.position;

        PlayEarnEffect(1001, sPos, UIManager.Instance.userGoldTMP.transform.position, earnEffParent);
        PlayEarnEffect(1002, sPos, UIManager.Instance.userGemTMP.transform.position, earnEffParent);
        PlayEarnEffect(2001, sPos, UIManager.Instance.userLevelTMP.transform.position, earnEffParent);
        PlayEarnEffect(2002, sPos, UIManager.Instance.userLevelTMP.transform.position, earnEffParent);
    }

  

    // 다른게임모드로 바꾸기
    public void ChangeGameMode(GameMode _gameMode)
    {
        gameMode = _gameMode;
        gameState = GameState.Ready;

        UIManager.Instance.OpenFadePanel(1f, 1f);

        StartCoroutine(ChangeGameModeCoro());
    }

    IEnumerator ChangeGameModeCoro()
    {
        yield return YieldCache.WaitForSeconds(0.5f);

        ResetGame();

        yield return YieldCache.WaitForSeconds(1.0f);

        player.ReadyPlayer();
        player.gameObject.SetActive(true);

        GameStartEvent?.Invoke();

        gameState = GameState.Play;

    }

  
}
