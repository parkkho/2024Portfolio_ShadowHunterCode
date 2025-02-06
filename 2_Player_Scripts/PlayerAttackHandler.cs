using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PlayerAttackHandler : AttackHandler
{

    [Header("---Class---")]
    public Player player;

    public Weapon mainWeapon;

     private Scanner scanner;

    [Header("---STATUS---")]
 //   public float atkRange = 5f; // 공격사거리

    public float lifeStealValue; // 생명력 흡수

    public float baseMoveSpeed;

    //각 상태 클래스 딕셔너리
    private Dictionary<AttackState, IState<PlayerAttackHandler>> dicState = new Dictionary<AttackState, IState<PlayerAttackHandler>>();
    // 공격상태머신
    private StateMachine<PlayerAttackHandler> attackFSM;


    private void Start()
    {
        player = GetComponent<Player>();
       
        scanner = GetComponent<Scanner>();

        IState<PlayerAttackHandler> find = new PlayerFind();
        IState<PlayerAttackHandler> move = new PlayerMove();
        IState<PlayerAttackHandler> attack = new PlayerAttack();
        IState<PlayerAttackHandler> delay = new PlayerStop();

        dicState.Add(AttackState.Find, find);
        dicState.Add(AttackState.Move, move);
        dicState.Add(AttackState.Attack, attack);
        dicState.Add(AttackState.Stop, delay);

        attackFSM = new StateMachine<PlayerAttackHandler>(this, dicState[AttackState.Find]);
        baseMoveSpeed = CONSTANTS.BasePlayerMoveSpeed;

        atkStatus.moveSpeed *= baseMoveSpeed;

        attakerType = AttackerType.Player;

       // baseAttackCoro = BaseAttackCoro();
        //  ChangeState(AttackState.Find);
    }

    protected override void FixedUpdate()
    {
      base.FixedUpdate();

    }

    private void LateUpdate()
    {
        if (Target != null)
        {
            SetFlip(Target.transform);
            // spineAnim.

        }
    }

    public override void SetFlip(Transform targetTrf)
    {
        float dirX = Mathf.Sign(targetTrf.position.x - rigid.position.x);

        player.PlayerAnimHandler.SetFlip(dirX);

      //  player.dirX = dirX;
    }

    //public override void InitAttackHandler(AttackStatus _atkStatus , UIHandler _uiHandler)
    //{
      
    //    base.InitAttackHandler(_atkStatus , _uiHandler);
    //}

    // 초기화
    public override void ResetAttackHandler()
    {
        base.ResetAttackHandler();

        atkStatus.hp = atkStatus.maxHp;
    }

    public override void ChangeState(AttackState state)
    {
        preAttackState = attackState;
        attackState = state;
        switch (attackState)
        {
            case AttackState.Find:
                attackFSM.ChangeState(dicState[AttackState.Find]);
                break;
            case AttackState.Move:
                attackFSM.ChangeState(dicState[AttackState.Move]);
                break;
            case AttackState.Attack:
                attackFSM.ChangeState(dicState[AttackState.Attack]);
                break;

            case AttackState.Stop:
                attackFSM.ChangeState(dicState[AttackState.Stop]);
                break;

            default:
                break;
        }
    }

    protected override void CheckTarget()
    {
        if (Target.isLive == false || Target.attakerType != AttackerType.Enemy)
        {
            Target = null;
            ChangeState(AttackState.Find);
        }
    }


    protected override AttackHandler GetTarget()
    {
        return scanner.nearestTarget;
    }

    protected override void SetTarget(AttackHandler target)
    {
        scanner.nearestTarget = target;
    }


    public override void StartAttack()
    {
        //if (player.isHide && player.isTransform)
        //{
        //    // StartCoroutine(ShowCharacter());
        //}

        if (IdleGameManager.Instance.gameMode == IdleGameManager.GameMode.PVP)
        {
            startAttackCoro = PvPNormalAttackCoro();
        }
        else
        {
            startAttackCoro = NormalAttackCoro();
        }
        // anim.SetBool("isRun", false);
        StartCoroutine(startAttackCoro);
    }

    // 기본 공격 루틴
    public override IEnumerator BaseAttackCoro()
    {

        SoundManager.instance.PlaySfx(SoundManager.Sfx.H_Sword, 0.5f);


        player.PlayerAnimHandler.ControlTimeScale(2f);
        // yield return YieldCache.WaitForSeconds(atkAnimDelay);

        // 변신상태 일 경우 카메라 쉐이크
        if (player.isTransform) IdleGameManager.Instance.ShakeMainCamera(0.5f, 1f);


        yield return StartCoroutine(mainWeapon.WeaponAttack(Target.transform));

        player.PlayerAnimHandler.ControlTimeScale(1f);


    }

    // 기본 공격 루틴
    IEnumerator PvPNormalAttackCoro()
    {
        Player target = Target.transform.parent.GetComponent<Player>();

     //   float angle = Utils.GetAngle3D(transform.position, target.transform.position);

        // Debug.Log("angle" + angle);
        while (true)
        {
            
            yield return StartCoroutine(BaseAttackCoro());

         
            if (!target.IsLive)
            {
                ChangeState(AttackState.Find);
                startAttackCoro = null;
                yield break;
            }

            yield return YieldCache.WaitForSeconds(attackDelay);

        }

    }


    // 몬스터 죽음 시 생명력 흡수
    public void MonsterLifeSteal(float hp)
    {
        if (!isLive) return;

        float life = Mathf.Round(hp * lifeStealValue * 0.01f);

        //Debug.Log("lifeSteal" + life);

        RecoverHp(life);
    }

    // 체력회복
    public override void RecoverHp(float recoverHp)
    {
        base.RecoverHp(recoverHp);

        uiHandler.SetHpValue(atkStatus.hp);
    }

    // 데미지 받음
    public override void OnDamaged(float damage, bool isCri)
    {
        if (player.playerBuff == Player.PlayerBuff.Immortal) return;

        if (!isLive) return;

        float playerDamage = Mathf.Round(damage * atkStatus.defAverage);

        //쉴드를 가지고 있을 때는 쉴드 먼저 없어짐
        if (atkStatus.shield > 0)
        {
            OnDamagedShield(playerDamage);

            if (atkStatus.shield < 0)
            {
                playerDamage = -atkStatus.shield;
            }
            else return;
      
        }

        if (atkStatus.hp > 0)
        {
            //Debug.Log("플레이어 데미지받음");

            atkStatus.hp -= playerDamage;

            if (atkStatus.hp <= 0)
            {
                atkStatus.hp = 0;

                player.Die();
            }

            player.damageTextController.ShowDamageText(-playerDamage);

            // HP바 업데이트
            uiHandler.SetHpValue(atkStatus.hp);
        }

    }

    // 무기 세팅
    public void SetMainWeapon(Weapon _mainWeapon)
    {
        mainWeapon =_mainWeapon;
    }

   
}

/// <summary>
/// 플레이어 공격 상태에 따른 클래스
/// </summary>

#region
public class PlayerFind : IState<PlayerAttackHandler>
{
    private PlayerAttackHandler playerAtkHandler;

    public void OnStateEnter(PlayerAttackHandler sender)
    {
        if (playerAtkHandler == null) playerAtkHandler = sender;

        playerAtkHandler.player.PlayerAnimHandler.PlayAnimationForState("Idle", 0);

    }

    public void OnStateUpdate(PlayerAttackHandler sender)
    {

    }

    public void OnStateEnd(PlayerAttackHandler sender)
    {

    }

}
// 플레이어 적 추적
public class PlayerMove : IState<PlayerAttackHandler>
{
    private PlayerAttackHandler playerAtkHandler;

    public void OnStateEnter(PlayerAttackHandler sender)
    {
        if (playerAtkHandler == null) playerAtkHandler = sender;

        Vector3 dirVec = playerAtkHandler.Target.transform.position - playerAtkHandler.rigid.position /* rigid.position*/;

        // 공격범위 밖에 있을 시 이동 애니메이션
        if (Vector3.SqrMagnitude(dirVec) > playerAtkHandler.sqrAtkRange)
        {
            playerAtkHandler.player.PlayerAnimHandler.PlayAnimationForState("running", 0);
        }

        // 변신상태
        if (playerAtkHandler.player.isTransform)
        {
            if (Vector3.SqrMagnitude(dirVec) > 100f)
            {
                //  playerAtkHandler.player.StartCoroutine(HideCharacter());
            }
        }

    }

    public void OnStateUpdate(PlayerAttackHandler sender)
    {

    }

    public void OnStateEnd(PlayerAttackHandler sender)
    {

    }

}

public class PlayerAttack : IState<PlayerAttackHandler>
{
    private PlayerAttackHandler playerAtkHandler;

    public void OnStateEnter(PlayerAttackHandler sender)
    {
        if (playerAtkHandler == null) playerAtkHandler = sender;

        playerAtkHandler.StartAttack();

    }

    public void OnStateUpdate(PlayerAttackHandler sender)
    {

    }

    public void OnStateEnd(PlayerAttackHandler sender)
    {
        playerAtkHandler.StopAttack();
    }

}

public class PlayerStop : IState<PlayerAttackHandler>
{
    private PlayerAttackHandler playerAtkHandler;

    public void OnStateEnter(PlayerAttackHandler sender)
    {
        if (playerAtkHandler == null) playerAtkHandler = sender;
      //  playerAtkHandler.StopCh

    }

    public void OnStateUpdate(PlayerAttackHandler sender)
    {

    }

    public void OnStateEnd(PlayerAttackHandler sender)
    {

    }

}
#endregion