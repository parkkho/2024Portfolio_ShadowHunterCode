using JHSOFT_Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 관련 클래스
/// </summary>

[Serializable]
public class Skill : MonoBehaviour
{
    public int level; // 스킬 레벨

    public SkillHandler skillUser; // 스킬 사용자

  
    public virtual void InitSkill(SkillHandler _skillUser, int skilLevel)
    {
        skillUser = _skillUser;

        level = skilLevel;

        if (level >= CONSTANTS.SkillUpgradeLevel[0]) AddFirstUpgrade();
        if (level >= CONSTANTS.SkillUpgradeLevel[1]) AddSecondUpgrade();
        if (level >= CONSTANTS.SkillUpgradeLevel[2]) AddThirdUpgrade();
    }

    public virtual void UseSkill() { } // 스킬 사용 효과

    // 레벨 업 시
    public virtual void LevelUp()
    {
        //스킬 레벨 증가
        level++;
        CheckSkillUpgrade();
    }

    public virtual void StopSkill()  // 스킬 종료시
    {
        // isActive = false;
        gameObject.SetActive(false);
    }

    // 스킬 레벨업 시 업그레이드 체크
    public virtual void CheckSkillUpgrade()
    {
        if (level == CONSTANTS.SkillUpgradeLevel[0]) AddFirstUpgrade();
        if (level == CONSTANTS.SkillUpgradeLevel[1]) AddSecondUpgrade();
        if (level == CONSTANTS.SkillUpgradeLevel[2]) AddThirdUpgrade();
    }

    // 1,2,3번 강화효과
    public virtual void AddFirstUpgrade() { }
    public virtual void AddSecondUpgrade() { }
    public virtual void AddThirdUpgrade() { }
}

/// <summary>
/// 액티브 스킬 ->타겟팅 스킬 구분
/// </summary>
[Serializable]
public class ActiveSkill : Skill
{

    public AudioClip skillSound;

    public ActiveSkillData skillData;

    public List<Collider> skillCollList = new List<Collider>(); // 스킬 콜라이더 리스트 -> 레이어변경이 필요한 콜라이더들

    public bool isActive = false;

    public float damage; // 데미지

    // 액티브 스킬 생성
    public virtual void InitActiveSkill(SkillHandler _skillUser, ActiveSkillData _skillData, int skillLevel)
    {
        skillData = _skillData;

        InitSkill(_skillUser ,skillLevel);

        SetSkillLayer(skillUser.gameObject.layer);

        skillData.skillDamage += (skillData.levelUpValue * (level - 1));
    }

    
    //public override void InitSkill(int skilLevel)
    //{
    //    base.InitSkill(skilLevel);
    //}

    // public abstract bool CheckSkillCondition();
    public virtual void OnTriggerEnter(Collider other)
    {
        other.GetComponent<IDamageable>().OnDamaged(damage, false);
    }

    // 액티브 스킬은 사용 시 데미지 계산
    public override void UseSkill()
    {
        isActive = true;

        if(skillSound != null)
        {
            SoundManager.instance.PlaySfx(skillSound, 1.0f);
        }
     
    }

    public override void LevelUp()
    {
        //스킬 데미지 증가
        skillData.skillDamage += skillData.levelUpValue;

        base.LevelUp();
    }

    // 스킬 종료시
    public override void StopSkill()
    {
        isActive = false;
        base.StopSkill();
    }

    //범위 공격 시 
    protected IEnumerator CastDoTSkillCoro(Collider collider, float delay)
    {
        //sDuration + delay = 파티클 재생 시간
        collider.enabled = false;
        collider.gameObject.SetActive(true);

        //콜라이더 켜질 때까지의 딜레이
        yield return YieldCache.WaitForSeconds(delay);

        StartCoroutine(Utils.AreaDoTAttack(collider, skillData.duration, skillData.intervalTime));

        yield return YieldCache.WaitForSeconds(skillData.duration);

        collider.gameObject.SetActive(false);
        StopSkill();
    }

    
    protected IEnumerator CastSkillCoro(Collider collider, float delay, float duration)
    {
        collider.enabled = false;
        collider.gameObject.SetActive(true);

        yield return YieldCache.WaitForSeconds(delay);

        collider.enabled = true;

        yield return YieldCache.WaitForFixedUpdate;

        collider.enabled = false;

        //오브젝트 꺼지는시간
        yield return YieldCache.WaitForSeconds(duration);

        collider.gameObject.SetActive(false);
    }

    // 상대 타겟 레이어 가져오기
    protected LayerMask GetTargetLayer(int myLayer)
    {
        if (myLayer == 6) return LayerMask.GetMask("Enemy");
        if (myLayer == 7) return LayerMask.GetMask("Player");

        return 0;
    }

    // 스킬 콜라이더들 찾아서 레이어 변경 적군 레이어로변경
    protected void SetSkillLayer(int myLayer)
    {
        LayerMask skillLayerMask = 0;

        if (myLayer == 6) skillLayerMask = LayerMask.NameToLayer("PlayerAttack");
        if (myLayer == 7) skillLayerMask = LayerMask.NameToLayer("EnemyAttack");

        gameObject.layer = skillLayerMask;

        // 레이어 변경
        //   Collider[] skillCols = GetComponentsInChildren<Collider>();

        if (skillCollList.Count == 0) return;

        foreach (Collider col in skillCollList)
        {
            col.gameObject.layer = skillLayerMask;
        }

    
    }

}

// 액티브 - 타게팅 스킬
[Serializable]
public class TargetingSkill : ActiveSkill
{
    // public LayerMask targetLayer; // 타겟레이어
    // 타겟리스트
    public List<Transform> targetList = new List<Transform>();

    // 타겟추가
    public void AddTarget(params Transform[] targets)
    {
        targetList.Clear();
        targetList.AddRange(targets);
    }
}

[Serializable]
public class PassiveSkill : Skill
{
    public PassiveSkillData passiveSkillData;

    // 패시브 스킬 생성
    public void InitPassiveSkill(SkillHandler _skillUser, PassiveSkillData _skillData, int skillLevel)
    {
        InitSkill(_skillUser, skillLevel);

        passiveSkillData = _skillData;
    }

}

// 스킬 처리기
public abstract class SkillHandler : MonoBehaviour
{
    public LayerMask targetLayer; // 적 레이어

    public AttackHandler skillUserAtkHandler; // 스킬 사용자의 공격기능

    public List<PassiveSkill> passiveSkillList = new List<PassiveSkill>(); // 활성 패시브 스킬 리스트

    public ActiveSkill[] normalActiveSkills; //착용 스킬 오브젝트 // = new ActiveSkill[CONSTANTS.maxSkillCount]

    public float SkillUserHp { get { return skillUserAtkHandler.atkStatus.maxHp; } }

    protected virtual void Awake()
    {
        skillUserAtkHandler = GetComponent<AttackHandler>();
    }

    public abstract void PlayAnimation(string name); //사용자 애니메이션
    public abstract void ControlAnimTimeScale(float scale); // 타임스케일 컨트롤

    // 초기화
    public virtual void ResetSkillHandler()
    {
        StopAllSkill(normalActiveSkills);
    }

    public virtual void InitSKillHandler(int skillCount)
    {
        normalActiveSkills = new ActiveSkill[skillCount];
    }

    //// 장착 스킬 데이터를 불러 오브젝트 생성
    //public void LoadSkillData(int order , int skillID , int characType , int level)
    //{
    ////    normalActiveSkills = new ActiveSkill[skillIDs.Length];

    //    //for (int i = 0; i < skillIDs.Length; i++)
    //   // {
    //        SetEquipNormalSkill(order , skillID , characType, level);
    //   // }
    //}

    // 스킬 장착 시 해당 스킬 오브젝트 세팅
    public void SetEquipNormalSkill(int order, int id , int characType , int level)
    {
        if (normalActiveSkills[order] != null) Destroy(normalActiveSkills[order].gameObject);

        normalActiveSkills[order] = GetActiveSkill(characType, id, level);
    }


    // 스킬 오브젝트 가져오기
    protected ActiveSkill GetActiveSkill(int characType, int id , int level)
    {
        if (id <= 0) return null;

        int skillOrder = id;

        if (id > 100) skillOrder = id % 100;

        GameObject skillObj = Instantiate(SkillManager.Instance.GetSkillPrefab(characType, skillOrder), Vector3.zero, Quaternion.identity, transform);

        ActiveSkill skill = skillObj.GetComponent<ActiveSkill>();

        InitActiveSkill(skill, id, level);

        return skill;


    }

    // 스킬 초기화 -> 데이터 및 초기위치 세팅
     void InitActiveSkill(ActiveSkill activeSkill, int id, int level)
    {
        activeSkill.InitActiveSkill(this, DataManager.Instance.GetSkillData(id).DeepCopy(), level);

        if (activeSkill.skillData.insType != 1)
        {
            activeSkill.transform.SetParent(SkillManager.Instance.transform);
        }

        activeSkill.transform.localPosition = Vector3.zero;
    }

    // 스킬 사용
    protected virtual bool UseActiveSkill(ActiveSkill skill)
    {
        // 조건 확인
        if (CheckActiveSkillCondition(skill))
        {
            // 타게팅
            if (skill.skillData.tDectectionType > 0)
            {
                SetTarget(skill.GetComponent<TargetingSkill>());
            }

            skill.gameObject.SetActive(true);

            if (skill.skillData.useTime > 0f && skillUserAtkHandler != null)
                skillUserAtkHandler.StopCharacter(skill.skillData.useTime);

            if (skillUserAtkHandler != null)
                skill.damage = Utils.GetSkillDamage(skill.skillData.skillDamage, skillUserAtkHandler.atkStatus.atk);


            // 애니메이션 같이 실행
            ControlAnimTimeScale(skill.skillData.animTimeScale);
            PlayAnimation(skill.skillData.animName);

            skill.UseSkill();

            return true;
        }

        return false;
    }

    // N번째 기본 스킬 사용
    public bool UseBaseSkill(int order)
    {
        return UseActiveSkill(normalActiveSkills[order]);
    }




    // 조건 체크 -> 주변에 적이없다면 스킬 사용X
    protected bool CheckActiveSkillCondition(ActiveSkill skill)
    {
        int type = skill.skillData.tDectectionType;

        // 주변에 적이 있는지만 확인 -> 공격범위 안에
        if (type == 0)
        {
            return (Utils.GetNearTargets(transform.position, skill.skillData.atkRange, targetLayer).Length > 0);
        }


        // 사용범위 안에 적이 있는지 확인
        if (type >= 1)
        {
            float value = skill.skillData.tDetectionRange;
            // 주변 타겟들
            return (Utils.GetNearTargets(transform.position, value, targetLayer).Length > 0);

        }

        return false;
        //  return true;
    }

    // 타겟팅 스킬 타겟지정
    public void SetTarget(TargetingSkill targetingSkill)
    {
        //  targetingSkill.targetList.Clear();

        Transform[] targets = null;

        int sUseConType = targetingSkill.skillData.tDectectionType;

        if (sUseConType == 1)
        {
            targets = new Transform[] { Utils.GetNearest(transform.position, targetingSkill.skillData.tDetectionRange, targetLayer) };
        }

        if (sUseConType == 2)
        {
            targets = new Transform[] { Utils.GetFarthest(transform.position, targetingSkill.skillData.tDetectionRange, targetLayer) };
        }

        if (sUseConType == 3)
        {
            targets = new Transform[] { Utils.GetRandom(transform.position, targetingSkill.skillData.tDetectionRange, targetLayer) };
        }

        if (sUseConType == 4)
        {
            targets = Utils.GetMultiFarthest(transform.position, targetingSkill.skillData.tDetectionRange, targetLayer, targetingSkill.skillData.targetCount);
        }

        targetingSkill.AddTarget(targets);
    }


    // 모든 스킬 멈춤
    protected void StopAllSkill(ActiveSkill[] skillObj)
    {
        for (int i = 0; i < skillObj.Length; i++)
        {
            if (skillObj[i] != null)
            {
                if (skillObj[i].isActive)
                {
                    skillObj[i].StopSkill();
                }

            }
        }
    }

}

