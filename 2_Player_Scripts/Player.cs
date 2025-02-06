using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using UnityEngine.VFX;
using DG.Tweening;
public class Player : MonoBehaviour
{
   
    public enum PlayerBuff
    {
        None,
        Immortal //무적 상태 EX) 변신 진행 중
    }


    public PlayerBuff playerBuff = PlayerBuff.None;

    public bool IsLive { get { return playerAtkHandler.isLive; } set { playerAtkHandler.isLive = value; } }
    public bool isTransform = false; // 변신 여부

    public PlayerGrowth playerGrowth; // 플레이어 스탯 성장 관리
    public PlayerAttackHandler playerAtkHandler; // 플레이어 공격 기능
    public PlayerUIHandler playerUIHandler; //플레이어 UI 관리
    public PlayerSkillHandler playerSkillHandler; // 플레이어 스킬 관리

  
    public DamageTextController damageTextController; // 데미지 텍스트 관리

    public Rigidbody Rigid { get { return playerAtkHandler.rigid; } } // 리지드바디

    [Header("---Player_Status---")]
    public BasePlayerStatus basePlayerStat; // 기본 플레이어 능력치

    [SerializeField]
    float transformEnergy; // 현재 변신 에너지 양

    [Header("---BaseCharacter ---")]


    public CharacterHandler baseCharacterHandler;  

    [Header("---TrfCharacter ---")]

    public List<GameObject> trfCharacList; // 변신 후 캐릭터 리스트

    public GameObject trfVolumeObj; // 변신 시 포스트프로세싱
  
    public CharacterHandler TrfCharacterHandler { get; set; }

    public SkeletonAnimationHandler PlayerAnimHandler { get; set; }

    public int trfCharacID = 1; // 변신 시 캐릭터 ID

 
    public Action DieEvent; // 죽을 때 이벤트

    

    [Header("### Player Pet ###")]

    [SerializeField] Pet[] playerPets = new Pet[CONSTANTS.MaxPetCount];

   // public float dirX = -1; // 캐릭터 보는 방향

    float baseAtkDelay = 0f;
    float trfAtkDelay = 0f; // 변신 공격 딜레이

    const float baseAtkSpeed = 3.0f; // 기본 공격속도



    protected virtual void Awake()
    {
        playerGrowth = GetComponent<PlayerGrowth>();    
        playerAtkHandler = GetComponent<PlayerAttackHandler>();
        playerUIHandler = GetComponent<PlayerUIHandler>();
        playerSkillHandler = GetComponent<PlayerSkillHandler>();
        //  scanner = GetComponent<Scanner>();

        PlayerAnimHandler = baseCharacterHandler.spineAnimHandler; // = characTrf.GetComponentInChildren<SkeletonAnimationHandler>();

        damageTextController.InitDamageTextController(0);

        basePlayerStat.SetBaseStatus();

        playerAtkHandler.InitAttackHandler(new AttackStatus(basePlayerStat.hp, basePlayerStat.atk, basePlayerStat.def,
            basePlayerStat.criProb, basePlayerStat.criDamage, basePlayerStat.moveSpeed, basePlayerStat.atkSpeed, basePlayerStat.atkSpeed) /*, playerUIHandler*/);


        // 1초를 공속속도 나눠 공격간 간격 시간을 정한다
        baseAtkDelay = Mathf.Floor(1f / (basePlayerStat.atkSpeed * baseAtkSpeed) * 100f) / 100f;

        float trfSpeed = basePlayerStat.atkSpeed * (1f + basePlayerStat.trf_atkSpeed / 100f);

        trfAtkDelay = Mathf.Floor(1f / (trfSpeed * baseAtkSpeed) * 100f) / 100f;

        playerAtkHandler.attackDelay = baseAtkDelay;

    }

    // Start is called before the first frame update
    void Start()
    {
        InitPlayer();
        ReadyPlayer();

    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("PlayerWeapon"))
        {
           Weapon weapon = collision.GetComponent<Weapon>();

            (float, bool) damage =  weapon.GetDamage();

            playerAtkHandler.OnDamaged(damage.Item1, damage.Item2);

        }

    }

    // 시작 시 플레이어 데이터 로드
    void InitPlayer()
    {
        trfCharacID = BackendManager.Instance.GameData.UserData.TrfCharacterID;

        SetNormalCharacterSkin(trfCharacID);
        LoadTransformCharacter();

        LoadUserPetData();

        baseCharacterHandler.characWeapon.Init(playerAtkHandler);
        playerAtkHandler.SetMainWeapon(baseCharacterHandler.characWeapon);
        playerSkillHandler.InitPlayerSkillHandler(PlayerAnimHandler);

    }

    // 플레이어 전투 세팅
    public void ReadyPlayer()
    {

        playerAtkHandler.lifeStealValue = basePlayerStat.lifeStealValue;
        playerAtkHandler.atkStatus.hp = playerAtkHandler.atkStatus.maxHp;

        playerUIHandler.SetHpMaxValue(playerAtkHandler.atkStatus.maxHp);
        playerUIHandler.SetHpValue(playerAtkHandler.atkStatus.maxHp);

        IsLive = true;


        if (SkillManager.Instance != null)
            SkillManager.Instance.RestartUseSkillRoutine();

    }

    // 변신 캐릭터 불러오기
    private void LoadTransformCharacter()
    {
        GameObject trfCharac = Instantiate(trfCharacList[trfCharacID - 1].gameObject, transform);

        trfCharac.transform.localPosition = Vector3.zero;
        trfCharac.transform.localRotation = Quaternion.identity;    

        TrfCharacterHandler = trfCharac.GetComponent<CharacterHandler>();

        trfCharac.SetActive(false);

        TrfCharacterHandler.characWeapon.Init(playerAtkHandler);
       
    }
    
    // 기본 캐릭터 스킨 세팅
    private void SetNormalCharacterSkin(int trfCharacID)
    {
        string skinName = "";

        if(trfCharacID == 1)
        {
            skinName = "V";
       
        }

        if(trfCharacID == 2)
        {
            skinName = "O";
        }

        if(trfCharacID == 3)
        {
            skinName = "F";
        }

        if(trfCharacID == 4)
        {
            skinName = "S";
        }

        baseCharacterHandler.spineAnimHandler.SetSkin(skinName);
        playerUIHandler.SetSkinTransformEnergyUI(skinName);

        playerUIHandler.SetFillImage(trfCharacID);
    }

    // 플레이어 변신 캐릭터 변경
    public void ChangeTransformCharacter(int changeCharacID)
    {
        trfCharacID = changeCharacID;
        // 데이터 저장
        BackendManager.Instance.GameData.UserData.SetTransformCharacterID(changeCharacID);

        // 기본 캐릭터 변경
        SetNormalCharacterSkin(trfCharacID);

        Destroy(TrfCharacterHandler.gameObject);

        LoadTransformCharacter();
    }

    // 플레이어 펫 데이터 불러오기
    void LoadUserPetData()
    {
        string[] petIdData = BackendManager.Instance.GameData.UserPetData.userPetDeck[0].Split("/");

        int[] petIDs = new int[CONSTANTS.MaxPetCount];
        

        for (int i = 0; i < CONSTANTS.MaxPetCount; i++)
        {
            petIDs[i] = int.Parse(petIdData[i]);
        }

        for(int j =0; j < petIDs.Length; j++)
        {
            if (petIDs[j] == 0) continue;

            ActivePlayerPet(j, petIDs[j]);
        }
    }

    // 장착 펫 활성
    public void ActivePlayerPet(int order , int petID)
    {
        PetConfig petDB = DataManager.Instance.GetPetData(petID);

        //따라다니는 펫 생성
        GameObject petObj = Instantiate(petDB.petObj, null);

        Pet pet = petObj.GetComponent<Pet>();
        pet.SetPetImagePosition(order);

        playerPets[order] = pet;

        UpdatePetEquipStat(false, petDB.addAtkValue, petDB.addHpValue);
    }

    // 펫 장착해제 시 펫 제거
    public void RemovePlayerPet(int order , int petID)
    {
        PetConfig petDB = DataManager.Instance.GetPetData(petID);
        UpdatePetEquipStat(true, petDB.addAtkValue, petDB.addHpValue);
        Destroy(playerPets[order].gameObject);
    }

    //펫 장착 스탯 변화
     void UpdatePetEquipStat(bool isRemove , float atkValue , float hpValue)
    {    
        playerGrowth.ChangePlayerStatus(CONSTANTS.StatusType.AtkIncrese,  atkValue, isRemove, true);
        playerGrowth.ChangePlayerStatus(CONSTANTS.StatusType.HpIncrease, hpValue, isRemove, true);
    }


    // 외부에서 플레이어 애니메이션 재생
    public void PlayAnimation(string name)
    {     
        PlayerAnimHandler.PlayOneShot(PlayerAnimHandler.GetAnimationForState(name), 0);
    }

    // 캐릭터 방향전환
    public void FlipCharacter(float _dirX)
    {
       // dirX = _dirX;
        PlayerAnimHandler.SetFlip(_dirX);
    }



    // 플레이어 초기화 -> 게임 재시작시
    public void ResetPlayer()
    {
        StopAllCoroutines();

        IsLive = false;
        
        transform.position = Vector3.zero;
     
        // 변신 초기화

        if (isTransform)
        {
            StopTransfromMode();
        }
       
        playerUIHandler.OffFingerImage();
        playerUIHandler.transformEnergyImg.fillAmount = 0f;
        transformEnergy = 0f;

        playerAtkHandler.ResetAttackHandler();

    }

  

    // 플레이어 죽음
    public void Die()
    {
        Debug.Log("플레이어 죽음");

        DieEvent?.Invoke();

        IsLive = false;
        StopAllCoroutines();
        PlayerAnimHandler.ControlTimeScale(1f);
       // spineAnimHandler.PlayOneShot(spineAnimHandler.GetAnimationForState("Dead"), 0);

        if (transformEnergy >= 1f)
        {
            playerUIHandler.OffFingerImage();
            playerUIHandler.trfButton.enabled = false;
          
        }

        StartCoroutine(DieCoro());

    }

    IEnumerator DieCoro()
    {
        yield return YieldCache.WaitForSeconds(1.0f);

        IdleGameManager.Instance.GameOver();

    }

   
    // 변신 에너지 획득
    public void EarnTransformEnergy(float energy)
    {
        if (isTransform || !IsLive) return;

        if (transformEnergy >= 1f) return;

        transformEnergy += energy * 10f;

#if TEST_MODE
        transformEnergy += energy * 100f;
#endif
        
        playerUIHandler.transformEnergyImg.fillAmount = transformEnergy;
        
        if (transformEnergy >= 1f)
        {
            if (isTransform == false)
            {
                //  isTransform = true;

                playerUIHandler.OnFingerImage();

                playerUIHandler.trfButton.enabled = true;
                playerUIHandler.trfEnergyAnim.AnimationState.SetAnimation(0, "Alarm", true);
                
            }

        }
    }

   
    // 변신 게이지 스파인 애님 컨트롤
    IEnumerator ControlTransformEnergyAnimCoro()
    {
        float checkTime = 0f;

        float time = 0f;

        while (transformEnergy < 1f)
        {
            time = transformEnergy - checkTime;
            checkTime = transformEnergy;

            //  Debug.Log("time" + time);
            if (time > 0f)
            {
                playerUIHandler.trfEnergyAnim.freeze = false;
                yield return YieldCache.WaitForSeconds(time * 2f);
                playerUIHandler.trfEnergyAnim.freeze = true;
            }

            yield return null;
        }

    }

    // 변신 버튼 클릭
    public void OnClickTransformButton()
    {
        if (IsLive == false || isTransform) return;

        playerUIHandler.OffFingerImage();
        playerUIHandler.trfButton.enabled = false;
        StartTransformMode();
    }

    // 플레이어 변신 모드
    void StartTransformMode()
    {
        isTransform = true;

        playerUIHandler.ChangeCanvasPosition(isTransform);

        playerBuff = PlayerBuff.Immortal; // 무적 상태

        // 능력치 변경 - 공격력, 공격속도 , 이동속도
        playerAtkHandler.atkStatus.atk = playerAtkHandler.atkStatus.atk * (1f + basePlayerStat.trf_atkIncrease / 100f);
   
        playerAtkHandler.attackDelay = trfAtkDelay; //1f / basePlayerStat.trf_atkSpeed;

        playerAtkHandler.lifeStealValue = playerAtkHandler.lifeStealValue * (1f + basePlayerStat.trf_lifeStealIncrese / 100f);

        playerAtkHandler.sqrAtkRange = TrfCharacterHandler.atkRange * TrfCharacterHandler.atkRange;

        playerAtkHandler.SetMainWeapon(TrfCharacterHandler.characWeapon);
        StartCoroutine(StartTransformRoutine());

    }

    // 변신 진행 루틴
    IEnumerator StartTransformRoutine()
    {

        // 스킬 사용 못하게
        SkillManager.Instance.UnableUseSkillButton();
        SkillManager.Instance.StopUseSkillMethod();

        playerAtkHandler.StopCharacter(2.0f);

        playerAtkHandler.player.PlayerAnimHandler.PlayAnimationForState("Idle", 0);

        // 카메라흔들림
        IdleGameManager.Instance.ShakeMainCamera(1f, 2f);

        // 페이드 창       
        trfVolumeObj.SetActive(true);

        SoundManager.instance.PlaySfx(SoundManager.Sfx.VT_Transform, 1f);
        // 변신 이펙트 연출

        TrfCharacterHandler.gameObject.SetActive(true);

        UIManager.Instance.ShowTransformCharacterUI();

        yield return StartCoroutine(TrfCharacterHandler.GetComponent<ITransformAble>().StartTransformEffect());

        SoundManager.instance.PlaySfx(SoundManager.Sfx.VT_Explode, 1f);

        // 캐릭터 외형 변경

        baseCharacterHandler.gameObject.SetActive(false);

        PlayerAnimHandler = TrfCharacterHandler.spineAnimHandler;


        // 스킬 애니메이션 변경
        playerSkillHandler.InitPlayerSkillHandler(PlayerAnimHandler);
     
        // 스킬UI변경
        SkillManager.Instance.ChangeSkillUI(true);
        SkillManager.Instance.StartUseSkillMethod();

        // 에너지 소비
        StartCoroutine(ConsumeEnergy());

        playerBuff = PlayerBuff.None;

        trfVolumeObj.SetActive(false);

        SoundManager.instance.PlayBgm(1);
    }

   
    // 변신 모드 해제
    void StopTransfromMode()
    {
     
        isTransform = false;

        TrfCharacterHandler.gameObject.SetActive(false);

        PlayerAnimHandler = baseCharacterHandler.spineAnimHandler;
        baseCharacterHandler.gameObject.SetActive(true);

        // 스킬 애니메이션 변경
        playerSkillHandler.InitPlayerSkillHandler(PlayerAnimHandler);

        transformEnergy = 0f;
        playerUIHandler.transformEnergyImg.fillAmount = 0f;
        playerUIHandler.ChangeCanvasPosition(isTransform);

        playerAtkHandler.SetMainWeapon(baseCharacterHandler.characWeapon);

        // 공격 관련 능력치 변신 전 능력치로 변경
        playerAtkHandler.attackDelay = baseAtkDelay;
        playerAtkHandler.atkStatus.atk = basePlayerStat.atk;

        playerAtkHandler.lifeStealValue = basePlayerStat.lifeStealValue;

        playerAtkHandler.sqrAtkRange = basePlayerStat.atkRange * basePlayerStat.atkRange;


        // 스킬UI변경
        SkillManager.Instance.ChangeSkillUI(false);

        SoundManager.instance.PlayBgm(0);
    }

    // 에너지 소비
    IEnumerator ConsumeEnergy()
    {
        playerUIHandler.trfEnergyAnim.AnimationState.SetAnimation(0, "Idle", true);

        float duration = basePlayerStat.trf_duration;

        transformEnergy = 0f;
        while (duration > 0f)
        {
            // 게임 준비 중 일 경우 게이지소모X
            while (IdleGameManager.Instance.gameState == IdleGameManager.GameState.Ready)
            {
                yield return null;
            }

            duration -= Time.deltaTime;

            playerUIHandler.transformEnergyImg.fillAmount = duration / basePlayerStat.trf_duration;

            yield return null;
        }

        StopTransfromMode();
    }

    public void CheckHitEffect(Transform trf)
    {
        if (!isTransform) return;

        ParticleSystem particle = EffectManager.Instance.GetParticle(100 + trfCharacID);

        particle.transform.position = trf.position;

        StartCoroutine(PlayEffect(particle));

        Debug.Log("HitEffectCheck");
    }

    IEnumerator PlayEffect(ParticleSystem particle)
    {
        particle.gameObject.SetActive(true);
        particle.Play();

        yield return YieldCache.WaitForSeconds(0.3f);

        particle.Stop();
        particle.gameObject.SetActive(false);
       
    }
  
}
