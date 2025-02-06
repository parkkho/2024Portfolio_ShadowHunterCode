using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using static Cinemachine.DocumentationSortingAttribute;
using Unity.VisualScripting;

class GrowthValue
{
    // 레벨업시 증가 수치
    public const int AtkGrowthValue = 5; 
    public const int DefGrowthValue = 5; 

    public const int HpGrowthValue = 120;

    public const float CriGrowthValue = 0.01f;
    public const float CriDamageGrowthValue = 0.1f;

    public const float LifeStealGrowthValue = 0.1f;

    // 비용 이전 비용 n% 씩 증가 초기비용 구분

    //public const float BaseAtkPrice = 100;
    //public const float BaseHpPrice = 100;
    //public const float BaseDefPrice = 100;

    public const float BaseGrowthValue_1 = 100;
    public const float BaseGrowthValue_2 = 500;

   // public const float BaseCriPrice = 200;

   // public const float BaseCriDamagePrice = 500;

   // public const float BaseLifeStealGrowthPrice = 500;

    public const float PriceIncrease = 1.01f; // 비용 증가량 1%;

    public const int StartValue = 3; // 시작 값(첫항)

    // 가격 올라가는 추가 비용 (공차)
    public const int PriceIncrease_1 = 5; // 공,방,체
    public const int PriceIncrease_2 = 7; // 크리확률
    public const int PriceIncrease_3 = 10; // 크뎀, 생명력흡수
   
}


public class PlayerGrowth : MonoBehaviour // 플레이어 능력치 성장 관리
{

    Player player;

    // Start is called before the first frame update
    void Start()
    {
        player = IdleGameManager.Instance.player;
    }

    //성장 탭 레벨 상승 -> 플레이어 스텟 업그레이드
    public void UpgradeGrowthLevel(CONSTANTS.StatusType statType , int level)
    {
        // 데이터 업데이트
        BackendManager.Instance.GameData.PlayerGrowthData.GrowthLevelUp(statType , level);

        float statValue = 0f; // 변화량


        switch (statType)
        {
            case CONSTANTS.StatusType.Atk:

                statValue = GrowthValue.AtkGrowthValue * level;
               

                QuestManager.Instance.CheckQuest(MissionType.AtkGrowth,level);

                break;

            case CONSTANTS.StatusType.Def:
                statValue = GrowthValue.DefGrowthValue * level;
              

                QuestManager.Instance.CheckQuest(MissionType.DefGrowth, level);

                break;

            case CONSTANTS.StatusType.Hp:

                statValue = GrowthValue.HpGrowthValue* level;
             
                QuestManager.Instance.CheckQuest(MissionType.HpGrowth, level);

                break;

            case CONSTANTS.StatusType.Cri:
                statValue = GrowthValue.CriGrowthValue * level;

           
                break;

            case CONSTANTS.StatusType.CriDamage:
                statValue = GrowthValue.CriDamageGrowthValue * level;


                break;

            case CONSTANTS.StatusType.LifeSteal:
                statValue = GrowthValue.LifeStealGrowthValue* level;
              
                break;

        }

        ChangePlayerStatus(statType, statValue,false , true);
        
    }

    // 플레이어 능력치 변화 -> 능력치타입 / 변화량 / isApply true이면 전투능력치에 바로 적용
    public void ChangePlayerStatus(CONSTANTS.StatusType statType , float value, bool isRemove  ,bool isApply)
    {
        // 제거 시 마이너스
        if (isRemove) value *= -1f;

        switch (statType)
        {
            case CONSTANTS.StatusType.Atk:
                player.basePlayerStat.atk += value;

                if(isApply)
                player.playerAtkHandler.atkStatus.atk = player.basePlayerStat.atk;

                break;

            case CONSTANTS.StatusType.Def:
                player.basePlayerStat.def += value;

                if(isApply)
                player.playerAtkHandler.atkStatus.GetDefAverage(player.basePlayerStat.def);

                break;
            case CONSTANTS.StatusType.Hp:

                float hpPoint = value;

                player.basePlayerStat.hp += hpPoint;

                if (isApply)
                {
                    player.playerAtkHandler.atkStatus.maxHp = player.basePlayerStat.hp;

                    player.playerUIHandler.SetHpMaxValue(player.basePlayerStat.hp);
                    player.playerAtkHandler.RecoverHp(hpPoint);
                }

                   
                break;

            case CONSTANTS.StatusType.Cri:

                float criValue = player.basePlayerStat.criProb + value;

                // 부동소수 불필요한 소수점 제거
                player.basePlayerStat.criProb = Mathf.Round(criValue * 100f) * 0.01f;

                if (isApply) player.playerAtkHandler.atkStatus.criProb = player.basePlayerStat.criProb;

                break;

            case CONSTANTS.StatusType.CriDamage:
                float criDmgValue = player.basePlayerStat.criDamage + value;

                player.basePlayerStat.criDamage = Mathf.Round(criDmgValue * 10f) * 0.1f;

                if (isApply) player.playerAtkHandler.atkStatus.criDamage = player.basePlayerStat.criDamage;

                break;

            case CONSTANTS.StatusType.LifeSteal:
                float lifeStealValue = player.basePlayerStat.lifeStealValue + value;
                player.basePlayerStat.lifeStealValue = Mathf.Round(lifeStealValue * 10f) * 0.1f;

                if(isApply)
                player.playerAtkHandler.lifeStealValue = player.basePlayerStat.lifeStealValue;
                break;

            case CONSTANTS.StatusType.AtkIncrese:

                player.basePlayerStat.atkIncrease += value;

                if (isApply)
                {                  
                    player.playerAtkHandler.atkStatus.atk = player.basePlayerStat.atk * (1f + (player.basePlayerStat.atkIncrease * 0.01f));
                }

                break;


            case CONSTANTS.StatusType.HpIncrease:

                player.basePlayerStat.hpIncrease += value;

                if (isApply)
                {
                   
                    player.playerAtkHandler.atkStatus.maxHp = player.basePlayerStat.hp * ( 1f + (player.basePlayerStat.hpIncrease * 0.01f));

                    player.playerUIHandler.SetHpMaxValue(player.playerAtkHandler.atkStatus.maxHp);
                }
             
               // player.playerAtkHandler.RecoverHp(hpPoint);

                break;

        }
    }
}
