using DG.Tweening.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;

public class SkillManager : MonoBehaviour // 플레이어 스킬 관리
{
    public static SkillManager Instance { get; private set; }

    public enum State { Normal ,Transform}; // 기본, 변신

    public State state = State.Normal;

    [SerializeField] private GameObject normalSkillBtnSpace;
    [SerializeField] private GameObject trfSkillBtnSpace;

    [Header("===LobbySkillButton===")]
    // 로비 하단 스킬 사용 버튼
    [SerializeField] SkillButton[] lobbySkillButtons; // 로비 스킬버튼들
    [SerializeField] SkillButton[] lobbyTrfSkillBtns; // 로비 변신 스킬버튼들

    [Header("===SkillPrefabs===")]

    public List<SkillContainerSO> skillContainers;


    [SerializeField] Button autoSkillBtn;
    [SerializeField] GameObject autoImage; // 자동 활성 이미지


    [Header("===User_Skill_Data===")]
    // [HideInInspector]
    public int[] curSkillDeckID; // 현재 스킬 덱ID;

   // [HideInInspector]
    public int[] curTrfSkillDeckID; // 변신 스킬 덱ID


    public bool isAuto = false; // 스킬 자동사용모드

    public int curNormalDeckGroup = 1; // 현재 일반 스킬 덱 페이지 =>1페이지 기본
    public int curTrfDeckGroup = 1; // 현재 변신 스킬 덱 페이지 =>1페이지 기본

    int openSkillCount;

    IEnumerator skillCoroutine = null;

    PlayerSkillHandler playerSkillHandler; // 유저 스킬핸들러

    private void Awake()
    {
        Instance = this;
        openSkillCount = CONSTANTS.MaxSkillCount;

        curSkillDeckID = new int[openSkillCount];
        curTrfSkillDeckID = new int[openSkillCount];

        autoSkillBtn.onClick.AddListener(() => OnClickAutoSkillButton());

    }

    // Start is called before the first frame update
    void Start()
    {
        playerSkillHandler = IdleGameManager.Instance.player.playerSkillHandler;

        playerSkillHandler.normalActiveSkills =  new ActiveSkill[CONSTANTS.MaxSkillCount];
   
        LoadSKillDeckData(curNormalDeckGroup);
        LoadTransformSKillDeckData(curTrfDeckGroup , BackendManager.Instance.GameData.UserData.TrfCharacterID);
        LoadPassiveSkillData();
   
        IdleGameManager.Instance.player.DieEvent += ResetUseSkillRoutine;

    }

    // 패시브 스킬 로드
    private void LoadPassiveSkillData()
    {
        if (playerSkillHandler.passiveSkillList.Count <= 0) return; 

        foreach(PassiveSkill skill in playerSkillHandler.passiveSkillList)
        {
            skill.UseSkill();
        }
    }


    // 스킬 덱 데이터 로드
    private void LoadSKillDeckData(int order)
    {
        string[] skillIDs = BackendManager.Instance.GameData.UserSkillData.userSkillDeck[order - 1].Split("/");

        for (int i = 0; i < openSkillCount; i++)
        {
            curSkillDeckID[i] = int.Parse(skillIDs[i]);
         
        }

     //   playerSkillHandler.InitSKillHandler(curTrfSkillDeckID.Length);

        for (int i=0; i< curSkillDeckID.Length; i++)
        {
            EquipItemData userSkillData = BackendManager.Instance.GameData.UserSkillData.GetSkillData(curSkillDeckID[i]);


            // 플레이어스킬세팅
            playerSkillHandler.SetEquipNormalSkill(i,curSkillDeckID[i], 0 , userSkillData.level);
        }

        

        for(int j=0; j< openSkillCount; j++)
        {
            // 로비 하단 스킬 버튼 변경
            SetSkillButton(curSkillDeckID[j], j, false);
        }
 
    }

    // 스킬 덱 데이터 로드
    private void LoadTransformSKillDeckData(int order , int trfCharacID)
    {   
        string[] trfSkillIDs = BackendManager.Instance.GameData.UserSkillData.userTrfSkillDeck[trfCharacID - 1][order - 1].Split("/");

        for (int i = 0; i < openSkillCount; i++)
        {
            curTrfSkillDeckID[i] = int.Parse(trfSkillIDs[i]);        
        }


        for (int i = 0; i < curTrfSkillDeckID.Length; i++)
        {
            EquipItemData userSkillData = BackendManager.Instance.GameData.UserSkillData.GetSkillData(curTrfSkillDeckID[i]);


            // 플레이어스킬세팅
            playerSkillHandler.SetEquipTransformSkill(i, curTrfSkillDeckID[i], trfCharacID, userSkillData.level);
        }


        for (int j = 0; j < openSkillCount; j++)
        {
            // 로비 하단 스킬 버튼 세팅
            SetSkillButton(curTrfSkillDeckID[j], j, true);
        }
    }

    // 변신 변경 시 새로운 스킬데이터 로드
    public void LoadNewTransformSkillDeckData(int trfCharacID)
    {
        LoadTransformSKillDeckData(curTrfDeckGroup,trfCharacID);
    }
   
    // 스킬 버튼 세팅  버튼 /id / 변신스킬여부
    void SetSkillButton(int id , int order  ,bool isTransform)
    {
        SkillButton skillButton = isTransform ? lobbyTrfSkillBtns[order] : lobbySkillButtons[order];
        // 비어있으면
        if (id == 0)
        {
            skillButton.ChangeState(SkillButton.State.Empty);
        }
        else
        {
            float coolTime = isTransform ? playerSkillHandler.trfActiveSkills[order].skillData.coolTime : playerSkillHandler.normalActiveSkills[order].skillData.coolTime;

            skillButton.SetSkillButton(GetSkillIcon(id), coolTime);
        }
    }

    // 스킬 프리팹 가져오기
    public GameObject GetSkillPrefab(int characType , int index)
    {
       // int characType = skillID / 100;
       //  int index = skillID - (100* characType);

        return skillContainers[characType].skillPrefab[index - 1];

    }

    // 스킬 프리팹 가져오기
    public Sprite GetSkillIcon(int skillID)
    {
        int characType = skillID / 100;
        int index = skillID - (100 * characType);

        return skillContainers[characType].skillIconList[index - 1];

    }


    // 덱 스킬 변경
    public void ChangeDeckSkill(int id , int order , bool isTransform)
    {
      //  Skill[] curSkills = isTransform ? curTrfSkillObject : curSkillObject;

        int[] curIDs = isTransform ? curTrfSkillDeckID : curSkillDeckID;

        // 덱 아이디 변경
        curIDs[order] = id;

       
        string deck = string.Join("/", curIDs);

        EquipItemData userSkillData = BackendManager.Instance.GameData.UserSkillData.GetSkillData(id);

        if (isTransform)
        {
            int trfCharacID = IdleGameManager.Instance.player.trfCharacID;

            playerSkillHandler.SetEquipTransformSkill(order, id, trfCharacID, userSkillData.level) ;

            BackendManager.Instance.GameData.UserSkillData.UpdateUserTransformSkillDeck(0, deck, trfCharacID);
        }
        else
        {
            playerSkillHandler.SetEquipNormalSkill(order, id , 0 , userSkillData.level);

            BackendManager.Instance.GameData.UserSkillData.UpdateUserSkillDeck(0, deck);
        }

        // 로비 스킬세팅
        SetSkillButton(id, order, isTransform);

    }



    // 덱 페이지 변경
    public void ChangeSkillDeckPage(int pageNum , bool isTransform)
    {
        // 일반
        if(isTransform == false)
        {
            curNormalDeckGroup = pageNum;
            LoadSKillDeckData(curNormalDeckGroup);
        }
        else // 변신
        {
            curTrfDeckGroup = pageNum;
            LoadTransformSKillDeckData(curTrfDeckGroup, BackendManager.Instance.GameData.UserData.TrfCharacterID);
        }
        
    }

    // 유저 스킬 레벨업
    public void PlayerSkillLevelUp(int id)
    {
        int index = -1;

        bool isTransform = id < 100 ? false : true;

        // 기본스킬
        if(!isTransform)
        {
            index = Array.IndexOf(curSkillDeckID, id);
        }
        else
        {
            index = Array.IndexOf(curTrfSkillDeckID, id);
        }

        // 덱에 장착중인 스킬일 경우 찾아서 레벨업
        if(index > -1)
        {
            if (!isTransform)
            {
                playerSkillHandler.normalActiveSkills[index].LevelUp();
            }
            else
            {
                playerSkillHandler.trfActiveSkills[index].LevelUp();
            }
        }
    }

    /// <summary>
    ///  스킬 사용 관련
    /// </summary>
    #region
    // 스킬 사용 시작
    public void StartUseSkillMethod()
    {
        if (isAuto == false) return;

        if (openSkillCount ==0) return;

        skillCoroutine = UseSkillCoro();

       // AbleUseSkillButton();

        StartCoroutine(skillCoroutine);
    }

    // 스킬 사용 루틴
    IEnumerator UseSkillCoro()
    {
        while (true)
        {
            if (IdleGameManager.Instance.player.IsLive == false) yield break;

            for (int i = 0; i < openSkillCount; i++)
            {
                SkillButton skillBtn = IdleGameManager.Instance.player.isTransform ? lobbyTrfSkillBtns[i] : lobbySkillButtons[i];

                if (skillBtn.curState != SkillButton.State.Ready || skillBtn.isWait == true) continue;

              //  Skill skill = IdleGameManager.Instance.player.isTransform ? trfSkill[i] : testSkill[i];

                if (UseSkill(skillBtn))
                {
                  // Debug.Log("사용");
                    yield return YieldCache.WaitForSeconds(1f);

                    break;
                }

            }

            yield return YieldCache.WaitForSeconds(0.5f);
        }
    }
    

    // 스킬 사용
    public bool UseSkill(SkillButton skillBtn)
    {
        if(IdleGameManager.Instance.player.playerAtkHandler.attackState == AttackHandler.AttackState.Stop) return false;

        int order = skillBtn.order;

        bool isTransform = IdleGameManager.Instance.player.isTransform;

        bool isUse = isTransform ? playerSkillHandler.UseTransformSkill(order) : playerSkillHandler.UseBaseSkill(order);

        ActiveSkill skill = isTransform ? playerSkillHandler.trfActiveSkills[order] : playerSkillHandler.normalActiveSkills[order];

        if (isUse)
        {
            skillBtn.ChangeState(SkillButton.State.CoolTime);
            WaitUseSkill(1f + skill.skillData.useTime);
       
        }

        return isUse;
    }

    // 스킬 사용 루틴 멈춤
    public void StopUseSkillMethod()
    {
        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
        }
        skillCoroutine = null;
    }

    // 스킬 루틴 재시작
    public void RestartUseSkillRoutine()
    {
        AbleUseSkillButton();
        StartUseSkillMethod();
    
    }


    // 스킬 사용 루틴 초기화
    public void ResetUseSkillRoutine()
    {
        StopUseSkillMethod();
        UnableUseSkillButton();

        playerSkillHandler.ResetSkillHandler();

    }

    // 스킬 버튼 사용 불가 => 캐릭터 죽음 OR 변신 모드 변경 시 이전 스킬 리셋
    public void UnableUseSkillButton()
    {
        SkillButton[] btnGroup = state == State.Normal ? lobbySkillButtons : lobbyTrfSkillBtns;

        for (int i = 0; i < openSkillCount; i++)
        {
            if (btnGroup[i].curState == SkillButton.State.Ready || btnGroup[i].curState == SkillButton.State.CoolTime)
                btnGroup[i].ChangeState(SkillButton.State.Unable);
        }
    }

    // 스킬 버튼 사용 가능
    void AbleUseSkillButton()
    {
        SkillButton[] btnGroup = state == State.Normal ? lobbySkillButtons : lobbyTrfSkillBtns;

        for (int i = 0; i < openSkillCount; i++)
        {
            if (btnGroup[i].curState == SkillButton.State.Unable)
            {
                btnGroup[i].ChangeState(SkillButton.State.Ready);
                btnGroup[i].isWait = false;
            }
        }
    }

    // 스킬 사용 시  버튼 대기상태로
    void WaitUseSkill(float waitTime)
    {
        SkillButton[] btnGroup = state == State.Normal ? lobbySkillButtons : lobbyTrfSkillBtns;

        for (int i = 0; i < openSkillCount; i++)
        {
            SkillButton.State state = btnGroup[i].curState;

            if (state == SkillButton.State.Lock || state == SkillButton.State.Empty) continue;
            btnGroup[i].EnterWaitState(waitTime);
        }
    }


    // 변신에 따른 스킬 UI 변경
    public void ChangeSkillUI(bool isTransform)
    {
        // 이전 상태 스킬 초기화
        UnableUseSkillButton();

        state = isTransform ? State.Transform : State.Normal;

        normalSkillBtnSpace.SetActive(!isTransform);
        trfSkillBtnSpace.SetActive(isTransform);

        // 현 상태 스킬 사용 준비완료
        AbleUseSkillButton();
    }
  
    // 스킬 자동사용 버튼 클릭
    public void OnClickAutoSkillButton()
    {
        isAuto = !isAuto;

        autoImage.SetActive(isAuto);

        if (isAuto)
        {
            StartUseSkillMethod();
        }
        else
        {
            StopUseSkillMethod();
        }
    }

    #endregion
}
