using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


// 게임에 필요한 클래스들 정리

/// <summary>
/// 전투 관련 클래스
/// </summary>

#region
[Serializable] // 타겟 찾기
public class TargetScanner : MonoBehaviour
{
    public float scanRange;
    // public float maxScanRange;

    public LayerMask targetLayer;
    //  public RaycastHit2D[] targets;
    public Collider[] targetCol;
    public AttackHandler nearestTarget; // 가

    protected virtual void FixedUpdate()
    {
        // 게임 시작 시
        if (IdleGameManager.Instance.gameState != IdleGameManager.GameState.Play) return;

        if (nearestTarget == null && CheckCondition())
        {
            targetCol = Physics.OverlapSphere(transform.position, scanRange, targetLayer);
            nearestTarget = GetNearest();
        }

        // 타겟 확인 -> 죽었을 경우 타겟에서 제거
        if (nearestTarget != null)
        {
            if (!nearestTarget.isLive)
            {
                nearestTarget = null;
            }
        }

    }

    //타겟 탐지 조건 체크
    protected virtual bool CheckCondition() { return true; }

    AttackHandler GetNearest()
    {
        Transform result = null;
        float diff = 100;

        // Debug.Log("target" + targetCol.Length);

        foreach (Collider target in targetCol)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.transform.position;
            float curDiff = Vector3.Distance(myPos, targetPos);

            if (curDiff < diff)
            {
                diff = curDiff;
                result = target.transform;
            }
        }

        if (result != null)
        {
            return result.GetComponent<AttackHandler>();
        }

        return null;
    }

}

[Serializable] // 전투를 담당하는 클래스
public abstract class AttackHandler : MonoBehaviour, IDamageable
{
    public enum AttackerType // 공격자 타입
    {
        Player, // 플레이어
        Enemy, // 적
        Ally // 아군
    }

    public enum AttackState
    {
        Find, //탐지
        Move, // 추적
        Attack, // 공격
        Stop //딜레이
    }

    public AttackerType attakerType;

    public UIHandler uiHandler;

    public StatusEffectManager statusEffMgr;

    public Rigidbody rigid; // 리지드

    public Collider detectionCol; // 탐지 콜라이더

    public AttackStatus atkStatus; // 전투 능력치

    public bool isLive; // 생존여부

    public float attackDelay; // 공격 딜레이

    public float sqrAtkRange; // 공격사거리 제곱 => 거리체크를위한


    public AttackState preAttackState; // 이전 상태
    public AttackState attackState; // 공격 상태

    public bool isHit = false; // 피격 중

    public AttackHandler Target { get { return GetTarget(); } set { SetTarget(value); } }

    protected IEnumerator startAttackCoro; // 현재 진행 중인 공격 루틴

   // protected IEnumerator baseAttackCoro; // 기본 공격 코루틴

    public List<ShieldData> shieldDatas = new List<ShieldData>(); // 쉴드 데이터

    protected virtual void Awake()
    {
        // Debug.Log("Awake");
        uiHandler = GetComponent<UIHandler>();
        statusEffMgr = GetComponent<StatusEffectManager>();
        rigid = GetComponent<Rigidbody>();
    }

    protected virtual void FixedUpdate()
    {
        if (!isLive)
            return;

        // 타겟 상태 체크
        if (Target != null) CheckTarget();

        if (Target != null) 
        {
            // 타겟 탐지 시 추적
            if (attackState == AttackState.Find)
                ChangeState(AttackState.Move);
        }
        else
        {
            // 타겟이 없는데 Find상태가 아니라면 Find상태로변경
            if (attackState == AttackState.Move || attackState == AttackState.Attack)
                ChangeState(AttackState.Find);
        }

        // 추적 상태일 시
        if (attackState == AttackState.Move)
        {
            if (Target == null) return;

            Vector3 dirVec = Target.transform.position - rigid.position;

            // 공격사거리보다 타겟이 멀면 추적
            if (Vector3.SqrMagnitude(dirVec) > sqrAtkRange)
            {
                Vector3 nextVec = dirVec.normalized * atkStatus.moveSpeed * Time.fixedDeltaTime;
                rigid.MovePosition(rigid.position + nextVec);
            }
            else // 사거리 내 일시 공격 시작
            {
                rigid.velocity = Vector3.zero;
                ChangeState(AttackState.Attack);
            }


        }
        else if (attackState == AttackState.Attack) // 공격 중 타겟이 사거리보다 멀어지면 리타겟팅
        {
            if (Target != null)
            {
                Vector3 dirVec = Target.transform.position - rigid.position;

                if (Vector3.SqrMagnitude(dirVec) > sqrAtkRange)
                {
                    Target = null;

                    ChangeState(AttackState.Find);
                }
            }

        }
    }

    // 공격 핸들러 초기생성
    public virtual void InitAttackHandler(AttackStatus _atkStatus)
    {
        atkStatus = _atkStatus;

        sqrAtkRange = atkStatus.atkRange * atkStatus.atkRange;

    }

    public abstract void SetFlip(Transform targetTrf); // 좌우에따른 이미지 뒤집기

    protected abstract AttackHandler GetTarget(); // 타겟 가져오기

    protected abstract void SetTarget(AttackHandler target); // 타겟 세팅

    protected abstract void CheckTarget(); //타겟 체크

    public abstract void ChangeState(AttackState state); // 상태 변경

    public abstract void OnDamaged(float damage, bool isCri);

    public abstract void StartAttack(); // 공격시작

    public abstract IEnumerator BaseAttackCoro(); // 기본 공격 행동 루틴

    public virtual IEnumerator NormalAttackCoro()   // 전투 공격 코루틴
    {
        while (isLive)
        {
            yield return StartCoroutine(BaseAttackCoro());

            yield return YieldCache.WaitForSeconds(attackDelay);
        }
    }
     

    //초기화
    public virtual void ResetAttackHandler()
    {
        statusEffMgr.RemoveAllBuff();

        Target = null;
        ChangeState(AttackState.Find);
    }


    public virtual void StopAttack() // 공격멈춤
    {
        if (startAttackCoro != null)
        {
            StopCoroutine(startAttackCoro);
        }
    }
    // 체력 회복
    public virtual void RecoverHp(float recoverHp)
    {
        atkStatus.hp += recoverHp;

        if (atkStatus.hp > atkStatus.maxHp)
        {
            atkStatus.hp = atkStatus.maxHp;
        }
    }

    //쉴드 활성
    public void SetShield(ShieldData _shieldData)
    {
        //각자 쉴드 데이터 추가, 쉴드 총량 증가
        shieldDatas.Add(_shieldData);

        atkStatus.maxShield += _shieldData.shield;
        atkStatus.shield += _shieldData.shield;

        uiHandler.SetShieldMaxValue(atkStatus.maxShield);
        uiHandler.SetSheildValue(atkStatus.shield);

        //쉴드 UI 꺼져있으면 활성화
        if (!uiHandler.shieldSlider.gameObject.activeSelf)
        {
            uiHandler.SetActiveShieldSlider(true);
        }
    }

    //활성된 쉴드 제거
    public void RemoveShield(ShieldData _shieldData)
    {
        //데이터 쉴드량만큼 제거
        atkStatus.maxShield -= _shieldData.maxShield;
        atkStatus.shield -= _shieldData.shield;
        shieldDatas.Remove(_shieldData);

        uiHandler.SetShieldMaxValue(atkStatus.maxShield);
        uiHandler.SetSheildValue(atkStatus.shield);

        //남아있는 쉴드 없을 시 비활성화
        if (shieldDatas.Count == 0)
        {
            EndShield();
        }
    }

    //쉴드 피해
    public void OnDamagedShield(float damage)
    {
        float remainDamage = 0f; // 남는 데미지


        //첫 번째로 들어온 쉴드부터 쉴드량 감소
        shieldDatas[0].shield -= damage;
        atkStatus.shield -= damage;

        //첫 번째 쉴드 전부 감소 시 제거
        if (shieldDatas[0].shield <= 0)
        {
            remainDamage = -shieldDatas[0].shield;

            shieldDatas[0].shield = 0;
            RemoveShield(shieldDatas[0]);
        }

        // 첫번째 쉴드 피해 후 남은 데미지가 있고, 다음 보호막이 남아있다면 다음 보호막에서 남은 데미지 감소
        if (remainDamage > 0f && shieldDatas.Count > 0)
        {
            OnDamagedShield(remainDamage);
        }

        uiHandler.SetSheildValue(atkStatus.shield);
    }

    public virtual void EndShield()
    {
        atkStatus.shield = 0;
        atkStatus.maxShield = 0;

        //ui 비활성화
        uiHandler.SetActiveShieldSlider(false);
    }

    // 최종 데미지 계산
    public (float, bool) GetDamage()
    {
        bool isCri = false;

        float baseDamage = atkStatus.atk;
        // int rndAccuracy = 1000;

        float rnd = Random.Range(0, 10000f);

        if (atkStatus.criProb * 100f >= rnd)
        {
            isCri = true;
        }

        //  Debug.LogFormat("{0} % , {1} , {2}", criProb, isCri , rnd);

        if (isCri) baseDamage *= 1f + atkStatus.criDamage / 100f;

        //float dps = Mathf.Round(baseDamage); // 반올림

        return (Mathf.Round(baseDamage), isCri);
    }


    // 플레이어 잠시 멈춤 -> EX) 스킬 사용 시 선 딜레이 등...
    public void StopCharacter(float stopTime)
    {
       // Debug.Log("StopCharac");
        ChangeState(AttackState.Stop);
       // Debug.Log("PreState " + preAttackState);

        StartCoroutine(StopCharacterCoro(stopTime));

    }

    // 스킬 사용 딜레이
    IEnumerator StopCharacterCoro(float delayTime)
    {
        yield return YieldCache.WaitForSeconds(delayTime);

        rigid.velocity = Vector3.zero;  

        //  if(isLive)
        if (Target != null)
        {
            // AttackHandler target = Target;

            if (Target.isLive == false)
            {
                ChangeState(AttackState.Find);
            }
            else
            {
                AttackState state = preAttackState;
                // Debug.Log("preState" + preState);
                ChangeState(state);
            }
        }
        else
        {
            ChangeState(AttackState.Find);
        }
    }


}

// 전투 FSM
public class StateMachine<T>
{
    private T sender;

    // 현재 상태를 담는 프로퍼티
    public IState<T> CurState { get; set; }

    //기본 상태를 생성 시에 설정하게 생성자 선언
    public StateMachine(T sender, IState<T> state)
    {
        this.sender = sender;

        ChangeState(state);
    }

    // 상태 변경
    public void ChangeState(IState<T> state)
    {
        // null에러출력
        if (sender == null)
        {
            Debug.LogError("m_sender ERROR");
            return;
        }

        if (CurState == state)
        {
            Debug.Log("SetState : " + state);
            Debug.LogWarningFormat("Same state : ", state.ToString());
            return;
        }

        if (CurState != null)
            CurState.OnStateEnd(sender);

        //상태 교체.
        CurState = state;

        //새 상태의 Enter를 호출한다.
        if (CurState != null)
            CurState.OnStateEnter(sender);
    }
}

//public abstract class BattleBaseState
//{
//    // 상태에 들어왔을 때 한번 실행
//    public abstract void OnStateStart();
//    // 상태에 있을 때 계속 실행
//    public abstract void OnStateUpdate();
//    // 상태를 빠져나갈 때 한번 실행
//    public abstract void OnStateEnd();
//}

#endregion

public interface IDamageable
{
    public void OnDamaged(float damage, bool isCri);
}

public interface IState<T>
{
    // 상태에 들어왔을 때 한번 실행
    void OnStateEnter(T sender);
    // 상태에 있을 때 계속 실행
    void OnStateUpdate(T sender);
    // 상태를 빠져나갈 때 한번 실행
    void OnStateEnd(T sender);
}

public class ShieldData
{
    public float maxShield;
    public float shield;

    public Action EndShieldEvent;

    public ShieldData(float maxShield, float shield, Action endShieldEvent)
    {
        this.maxShield = maxShield;
        this.shield = shield;
        EndShieldEvent = endShieldEvent;
    }
}

[Serializable]
public class UIHandler : MonoBehaviour
{
    //전투 관련 UI
    [Header("Attack_UI")]

    public GameObject uiCanvas;

    public Slider hpSlider;
    public Slider shieldSlider;

    protected RectTransform canvasRect;

    protected virtual void Start()
    {
        canvasRect = uiCanvas.GetComponent<RectTransform>();
    }

    // HP바 활성비활성
    public virtual void SetActiveHpSlider(bool isActive)
    {
        hpSlider.gameObject.SetActive(isActive);
    }

    // 쉴드 활성비활성
    public virtual void SetActiveShieldSlider(bool isActive)
    {
        shieldSlider.gameObject.SetActive(isActive);
    }

    // HP바 최대값 조절
    public virtual void SetHpMaxValue(float maxValue)
    {
        hpSlider.maxValue = maxValue;
    }

    // HP바 현재값 조절
    public virtual void SetHpValue(float value)
    {
        hpSlider.value = value;
    }

    //쉴드 슬라이더 초기화
    public void SetShieldMaxValue(float maxValue)
    {
        shieldSlider.maxValue = maxValue;
    }

    //쉴드 슬라이더 조절
    public void SetSheildValue(float value)
    {
        shieldSlider.value = value;
    }


}

// 플레이어 무기 클래스
[Serializable]
public abstract class Weapon : MonoBehaviour
{
    // 무기를 사용하는 캐릭터 -> 애니메이션 활용
    public CharacterHandler character; 

    protected Transform targetTrf;

    public int curCombo = 0; // 현재 콤보

    protected float comboTime = 2f; // 콤보 판정 타임

    protected float checkTime = 0f;

    public AttackHandler weaponAttacker; // 무기 공격자

    // 콤보체크
    protected virtual void Update()
    {
        if (curCombo >= 1)
        {
            checkTime += Time.deltaTime;

            if (checkTime > comboTime)
            {
                curCombo = 0;
            }
        }
    }
    // 초기화
    public virtual void Init(AttackHandler _AttackHandler)
    {
        weaponAttacker = _AttackHandler;
    }

    // 무기에 맞았을 시 무기 데미지 반환
    public virtual (float, bool) GetDamage()
    {
        return weaponAttacker.GetDamage();
    }

    // 무기 공격 -> 애니메이션 처리 및 콤보 처리도 함께
    public abstract IEnumerator WeaponAttack(Transform target);

    public abstract void BaseAttack(); //기본공격

}

// 플레이어 캐릭터 관리자 -> 애니메이션,무기 정보 등
[Serializable]
public class CharacterHandler : MonoBehaviour
{
    public enum CharacterType { Base , Vampire , Fairy , Werewolf , SandSeth}

    public CharacterType characType;

    public GameObject characObj;

    public Weapon characWeapon;

    public SkeletonAnimationHandler spineAnimHandler;

    public float atkRange; // 캐릭터 공격사거리

    // public float dirX; // 바라보는 방향
    // 외부에서 플레이어 애니메이션 재생
    public void PlayAnimation(string name)
    {
        spineAnimHandler.PlayOneShot(spineAnimHandler.GetAnimationForState(name), 0);
    }

   // public abstract IEnumerator StartTransformEffect(); 

}

// 변신하는 캐릭터 인터페이스 -> 이펙트 연출
public interface ITransformAble
{
    IEnumerator StartTransformEffect(); // 변신 이펙트
}

// 애니메이션 및 적 캐릭터 연출(피격 or 플립 등) 시스템
[SerializeField]
public abstract class AnimHandler : MonoBehaviour
{
    public Enemy enemy;

    public abstract void FlipEnemy(bool dir); // 이미지 좌우뒤집기

    public abstract void IdleAnim();

    public abstract void AttackAnim(); // 공격 애니메이션 실행
    public abstract void MoveAnim(); // 움직임 애니메이션 실행
    public abstract void DeadAnim(); // 죽는 애니메이션 실행

    public abstract void HitAnim(); // 맞는 연출 ->  흰색깜박임
    public abstract void ReturnHitAnim(); // 맞는 연출 후 제자리로

    public abstract void StopAnim(); //애니메이션 멈춤
    public abstract void ReplayAnim(); //재시작

    public abstract void PlayAnim(string name , bool isLoop);

    public abstract void SetShadowMonster(); // 몬스터 그림자화 -> 색 변경 등

    public virtual void Init()
    {
        enemy = GetComponent<Enemy>();
    }

}


// 옵저버 추상클래스

public abstract class Observer
{
    public abstract void OnNotify(int type);
}

// 옵저버 관리
public interface IMissionObserver
{
     void Notify(MissionType type , int value);
}

public enum ResetType { Main, Repeat, Daily, Weekly }; // 초기화 타입

public enum MissionType
{
    Hunt, Stage, AtkGrowth, HpGrowth, DefGrowth, WeaponSummon, ArmorSummon
}

public class MissionObserver : IMissionObserver
{
    private ResetType resetType;
    private MissionType missionType;

    private int curValue;

    private int completeValue;

    private bool isComplete = false;

    public ResetType ResetType { get { return resetType; } }
    public MissionType MissionType { get { return missionType; } }
    public int CurValue { get { return curValue; } }
    public int CompleteValue { get { return completeValue; } }

    public Action/*<int , int>*/ OnProgress;

    public Action OnCompleted;

    public MissionObserver(ResetType resetType,MissionType missionType, int curValue, int completeValue)
    {
        this.resetType = resetType;
        this.missionType = missionType;
        this.curValue = curValue;
        this.completeValue = completeValue;

      //  isComplete = _isComplete;
    }

    public void Notify(MissionType type , int value)
    {
        if (isComplete) return;

        if (missionType == type)
        {
            curValue+= value;
            OnProgress?.Invoke(/*curValue, completeValue*/); // 이벤트 호출

            if (curValue >= completeValue)
            {
                curValue = completeValue;

                isComplete = true;

                OnCompleted?.Invoke();
            }
        }

      
    }
}

// 옵저버 관리
public interface ISubject
{
    void AddObserver(Observer o);
    void RemoveObserver(Observer o);
    void Notify();
}

// 보상 데이터
public class RewardData
{
    public int itemID;
    public int amount;

    public RewardData(int itemID, int amount)
    {
        this.itemID = itemID;
        this.amount = amount;
    }
}
