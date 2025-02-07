using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable] // 타겟 찾기
public class TargetScanner : MonoBehaviour // 해당 클래스를 상속받아 플레이어 및 적 탐지 기능 구현
{
    public float scanRange;

    public LayerMask targetLayer; //타겟 레이어
    public Collider[] targetCol;
    public AttackHandler nearestTarget; 

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

public class Scanner : TargetScanner // 플레이어 적 탐지
{

    PlayerAttackHandler playerAtkHandler;

     void Awake()
    {
        playerAtkHandler = GetComponent<PlayerAttackHandler>();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

    }

    // 추적 조건
    protected override bool CheckCondition()
    {
        if(playerAtkHandler.attackState == AttackHandler.AttackState.Stop) return false;

        return base.CheckCondition();
    }

}
