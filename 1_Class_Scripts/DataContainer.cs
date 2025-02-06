using JHSOFT_Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
///  수치 데이터 관련 클래스모음
/// </summary>

// 적 스폰 데이터
[Serializable]
public class EnemySpawnData
{
    public int stageSpawnCount; // 스테이지 스폰 몬스터 수 (클리어 조건)

    public int level;

    public int[] monType; // 스폰 몬스터타입

    // public int minEnemyCount; // 한번에 소폰되는 최소 수
    // public int maxEnemyCount; // 한번에 스폰되는 최대 수

    public float spawnTime; // 스폰 간격 



}

// 적 스폰 데이터
[Serializable]
public class StageData
{
    public int stageNum;

    public int stageAtt; // 스테이지 속성

    public int mapType; // 맵 타입

    public bool isBossStage;

    public int clearEnemyCount; // 스테이지 클리어 잡아야하는 적 수

    public string monsterType; // 몬스터 타입 EX) 1/2...

    public int level; // 레벨 

    public float spawnTime; //시간

    // public int minEnemyCount; // 한번에 소폰되는 최소 수
    // public int maxEnemyCount; // 한번에 스폰되는 최대 수

}


//public enum StatusType { Atk, Hp, Def, Cri, CriDamage, LifeSteal, AtkIncrese, HpIncrease };

public class CONSTANTS
{
    public enum StatusType { Atk, Hp, Def, Cri, CriDamage, LifeSteal , AtkIncrese , HpIncrease };

    public const float BasePlayerMoveSpeed = 4f;

    public const float AtkGrowthValue = 5f; // 성장 수치
    public const float DefGrowthValue = 5f; // 성장 수치

    public const float HpGrowthValue = 150f;

    public const float CriGrowthValue = 0.01f;
    public const float CriDamageGrowthValue = 0.1f;

    public const float LifeStealGrowthValue = 0.1f;

    public const float DefConst = 10000f; // 방어계수

    public const float BaseUserExp = 500f;
    public const float ExpIncrease = 0.5f; // 경험치 증가량 (이전 경험치 n%증가) => 50%

    public const float BaseEarnExp = 10f; // 몬스터 잡았을때 얻는 기본 경험치량
    public const float EarnExpValue = 1.2f; // 경험치계수

    public const float BaseStageTime = 60f; // 기본 스테이지 시간

    public const int MaxSkillCount = 4; //최대 장착 스킬 수

    public const int MaxTrfCharacCount = 5; // 현재 변신 가능 캐릭터 수

    public const int MaxPetCount = 4;

    public static readonly int[] SkillUpgradeLevel = new int[] { 10, 20, 30 }; // 스킬 업그레이드 기준 레벨

    public static readonly string[] GradeName = new string[] { "NORMAL", "RARE", "EPIC", "UNIQUE", "LEGENDARY", "MYTHIC" };
    public static readonly string[] GradeKorName = new string[] { "일반", "레어", "에픽", "유니크", "전설", "신화" };


    //public static int 
}

[Serializable]
public class BasePlayerStatus // 플레이어 기본 능력치
{
    [Header("# Player_Normal")]
    public float hp; //체력
    public float atk; // 공격력
    public float def; // 방어력

    public float atkIncrease; // 추가 공격력 증가량(%)
    public float hpIncrease; // 추가 체력 증가량(%)

    public float lifeStealValue; // 기본 생명력흡수(&) -> 적 최대체력 * 흡수량

    public float moveSpeed; //이동속도
    public float atkSpeed; // 공격속도

    public float atkRange; // 기본 공격사거리

    public float criProb; // 치명타 확률 , Prob - 확률
    public float criDamage; //치명타 데미지

    [Header("# Player_Transfrom")]
    public float trf_atkIncrease; // 변신 공격력 증가량(%)

    public float trf_lifeStealIncrese; // 변신 추가 생명력 흡수(%)

    public float trf_atkSpeed; // 변신 공격속도 증가량(%)
    public float trf_moveSpeed; // 변신 이동속도 증가량(%)
    public float trf_duration; // 변신 지속시간

    public void SetBaseStatus()
    {
        hp = 500f;
        atk = 50f;

        atkIncrease = 0f;
        hpIncrease = 0f;

        def = 100f;

        lifeStealValue = 5f;
        moveSpeed = 1f;
        atkSpeed = 1f;
        atkRange = 1.5f;

        criProb = 5f;//%
        criDamage = 120f; //%

        trf_atkIncrease = 100f;
        trf_lifeStealIncrese = 5f;

        trf_atkSpeed = 100f;
        trf_moveSpeed = 100f;
        trf_duration = 15f; // 기본 변신시간

    }

}


[Serializable]
public class AttackStatus // 전투 능력치
{
    public float maxHp;
    public float hp; // 현재 hp
    public float atk; // 공격력
    public float defAverage; // 방어율

    public float maxShield;
    public float shield; //현재 쉴드

    // public float lifeStealValue; // 기본 생명력흡수(&) -> 적 최대체력 * 흡수량

    public float criProb; // 치명 확률
    public float criDamage; // 치명 데미지

    public float moveSpeed; //이동속도
    public float atkSpeed; // 공격속도

    public float atkRange; // 공격사거리

    public AttackStatus(float _hp, float _atk, float _def, float _criProb, float _criDamage, float _moveSpeed, float _atkSpeed,float _atkRange)
    {
        maxHp = _hp;
        hp = maxHp;
        atk = _atk;

        criProb = _criProb;
        criDamage = _criDamage;

        moveSpeed = _moveSpeed;
        atkSpeed = _atkSpeed;

        atkRange = _atkRange;

        GetDefAverage(_def);
    }

    //받는 데미지 비율 (1 - 방어율)
    public void GetDefAverage(float def)
    {
        defAverage = 1f - (float)Math.Round(def / (def + CONSTANTS.DefConst), 2);

        //Debug.Log("defAverage" + defAverage);
    }
}


[Serializable]
public class EnemyStatus // 적 몬스터 능력치
{
    public int monID;

    public string name; //이름

    public int attNum; // 속성
    public int atkType; // 0: 근접 1: 원거리

    public float hp; //체력
    public float atk; // 공격력
    public float def; // 방어력

    public float criProb; // 치명타 확률 , Prob - 확률
    public float criDamage; //치명타 데미지


    public float moveSpeed; //이동속도
    public float atkSpeed; // 공격속도
    public float atkRange; //사거리

    public float projectileSpeed; // 투사체 속도
    public float projectileScale; // 투사체 크기

  
    // 레벨에 따른 능력치 데이터
    public EnemyStatus GetEnemyStatusData(int level)
    {
        EnemyStatus data = this.DeepCopy();

        data.hp = JHSOFT_BALANCE.MathMethods.GetMonsterStatus(data.hp, level, data.hp * 0.1f, data.hp * 0.1f);
        data.atk = JHSOFT_BALANCE.MathMethods.GetMonsterStatus(data.atk, level, data.atk * 0.1f, data.atk * 0.1f);

        return data;
    }

}

// 액티브 스킬데이터
[Serializable]
public class ActiveSkillData
{

    public int id; //ID 일반스킬(1~) 변신스킬(101~)
    public string name; //이름

    public int characType; // 스킬 사용 캐릭터 타입 - 0: 일반 1: 뱀파이어 2: 요정

    public int grade; // 스킬 등급

    // 스킬 생성 타입 -> 0: 외부 / 1: 플레이어 중심 ->타입에 따라 초기생성 장소다르게
    public int insType;

    public float coolTime; // 스킬 쿨타임
    public float skillDamage; // 스킬 데미지 계수 공격력 N%

    public float atkRange; // 스킬 공격범위 -> 원:반경 / 직선: 거리

    public float skillScale; // 스킬 충돌체 크기 -> 조정이 필요한 경우 사용

    public int atkCnt; // 공격 횟수

    public float duration; // 스킬 지속시간 -> 지속스킬 일 경우
    public float intervalTime; // 지속스킬일 경우 간격

    public int statusType;
    public float[] statusValue; //상태효과 수치

    public float levelUpValue; // 레벨업시 증가하는 능력치 값
    public float[] firstUpgradeValue; // 첫번째 스킬 업글 변수
    public float[] secondUpgradeValue; // 두번째 스킬 업글 변수
    public float[] thirdUpgradeValue; // 세번째 스킬 업글 변수

    public string animName; // 사용 애니메이션 이름
    public float animTimeScale; // 사용 애니메이션 재생속도

    // --- 스킬 타겟 관련 ---
    public int tDectectionType; // 스킬 타겟탐지 타입 - 0: 논타겟 1: 가장 가까운적
    public float tDetectionRange; // 스킬 타겟 탐지 거리 값 -> EX) 타입이 1번일 경우 사거리값
    public int targetCount; // 타게팅 스킬일 경우 찾아야하는 타겟 수

    public float useTime; //스킬 발동 시간

    public string explain;  // 스킬 설명

    // 스킬 강화 설명 1~3번
    public string firstUpgradeExplain;
    public string secondUpgradeExplain;
    public string thirdUpgradeExplain;
}


// 패시브 스킬데이터
[Serializable]
public class PassiveSkillData
{
    public int id; //ID 일반스킬(1~) 변신스킬(101~)
    public string name; //이름

    public int characType; // 스킬 사용 캐릭터 타입 - 0: 일반 1: 뱀파이어 2: 요정

    public float levelUpValue; // 레벨업시 증가하는 능력치 값

    public string explain;  // 스킬 설명

}


