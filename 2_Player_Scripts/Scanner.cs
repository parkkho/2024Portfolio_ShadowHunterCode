using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
