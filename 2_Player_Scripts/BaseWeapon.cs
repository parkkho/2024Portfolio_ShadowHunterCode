using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.VFX;

public class BaseWeapon : Weapon // 플레이어 기본 무기
{

    public Collider weaponCol;

    public ParticleSystem[] baseSlashEff;

    public Transform baseAtkEffTrf; // 기본 공격 이펙트 트랜스폼

    Vector3 rotAngle = Vector3.zero;

    Vector3 weaponBaseRot = Vector3.zero;//무기기본 회전값

    protected override void Update()
    {
        base.Update();
    }

    public override IEnumerator WeaponAttack(Transform target)
    {
        targetTrf = target;

        if (curCombo == 0)
        {
            character.PlayAnimation("attack1");

        }
        if (curCombo == 1)
        {
            character.PlayAnimation("attack2");
            //  spineAnimHandler.PlayOneShot("Attack", 0);
        }

        if (curCombo == 2)
        {
            character.PlayAnimation("attack3");
        }

        yield return YieldCache.WaitForSeconds(0.1f); // 공격 시간

        BaseAttack();

        yield return YieldCache.WaitForSeconds(0.2f); // 공격 시간
    }

    // 기본 공격
    public override void BaseAttack()
    {
        float angle = Utils.GetAngle3D(character.transform.position, targetTrf.position);

        weaponBaseRot.y = angle;

        transform.localEulerAngles = weaponBaseRot;

        // 이펙트 회전

        if (character.spineAnimHandler.GetDir() < 0)
            baseAtkEffTrf.localEulerAngles = new Vector3(0, 0, -angle);
        else
            baseAtkEffTrf.localEulerAngles = new Vector3(180, 0, angle - 180f);


        baseSlashEff[curCombo].Stop();
        baseSlashEff[curCombo].Play();

        checkTime = 0f;
        SlashAttack();

        curCombo++;

        if (curCombo == 3) curCombo = 0;

    }


    // 기본공격
    void SlashAttack()
    {
        float angle = 180;

        if (curCombo == 2) angle = 360;

        int combo = curCombo;

        weaponCol.enabled = true;

        rotAngle.y = -angle;

        weaponCol.transform.DOLocalRotate(rotAngle, 0.3f, RotateMode.FastBeyond360).SetEase(Ease.InQuart).SetRelative()
            .OnComplete(() => { weaponCol.enabled = false; });

    }


}
