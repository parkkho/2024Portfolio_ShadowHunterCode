

public class PlayerSkillHandler : SkillHandler
{
    private SkeletonAnimationHandler animHandler; // 애니메이션
    public ActiveSkill[] trfActiveSkills = new ActiveSkill[CONSTANTS.MaxSkillCount];

    protected override void Awake()
    {
        base.Awake();
        
        //player = GetComponent<Player>();
    }

    public override void ControlAnimTimeScale(float scale)
    {
        animHandler.ControlTimeScale(scale);
    }

    public override void PlayAnimation(string name)
    {
        animHandler.PlayOneShot(animHandler.GetAnimationForState(name),0);
    }

    

    public void InitPlayerSkillHandler(SkeletonAnimationHandler _playerAnimator)
    {
        animHandler = _playerAnimator;
    }

    //public void InitTransformSkillHandler(int skillCount)
    //{

    //}

    //// 변신 스킬 로드
    //public void LoadTransformSkillData(int[] skillIDs , int trfCharacID , int level)
    //{
     
    //    for (int i = 0; i < skillIDs.Length; i++)
    //    {
    //        SetEquipTransformSkill(i, skillIDs[i] , trfCharacID, level);
    //    }
    //}

    // 장착 변신스킬 세팅
    public void SetEquipTransformSkill(int order ,int id , int trfCharacID , int level)
    {
        if (trfActiveSkills[order] != null) Destroy(trfActiveSkills[order].gameObject);
        trfActiveSkills[order] = GetActiveSkill(trfCharacID, id,level);
    }

    // 변신 스킬 사용
    public bool UseTransformSkill(int order)
    {
        return UseActiveSkill(trfActiveSkills[order]);
    }

    public override void ResetSkillHandler()
    {
        base.ResetSkillHandler();
        StopAllSkill(trfActiveSkills);
    }

}
