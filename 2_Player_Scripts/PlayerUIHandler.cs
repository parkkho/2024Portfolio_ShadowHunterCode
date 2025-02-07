using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerUIHandler : UIHandler
{
    // 플레이어 관련 UI
    [Header("Player_UI")]

    public Sprite[] trfFillSprite; // 변신게이지 스프라이트 
    public Image transformEnergyImg; // 변신게이지 이미지
    public Image trfFadePanel; // 변신 시 페이드 이미지
    public Button trfButton; // 변신 버튼
    public SkeletonGraphic trfEnergyAnim;

    public RectTransform fingerImage;
 

    Vector3 baseCanvasPos = new Vector3(0, 1.5f, 0);
    Vector3 trfCanvasPos = new Vector3(0, 2.5f, 0);

    // 캔버스 위치조절
    public void ChangeCanvasPosition(bool isTransform)
    {
        canvasRect.anchoredPosition = isTransform ? trfCanvasPos : baseCanvasPos;

    } 

    // 페이드 창 열기
    public void OpenPlayerFadePanel(float time)
    {
        trfFadePanel.color = new Color(0, 0, 0, 0.3f);
        trfFadePanel.gameObject.SetActive(true);

        trfFadePanel.DOFade(0.8f, time).OnComplete(() => { trfFadePanel.gameObject.SetActive(false); });
    }

    // 변신 게이지 스킨 변경
    public void SetSkinTransformEnergyUI(string skinName)
    {
        trfEnergyAnim.Skeleton.SetSkin(skinName);
        trfEnergyAnim.Skeleton.SetSlotsToSetupPose();
    }

    public void SetFillImage(int trfCharacID)
    {
        transformEnergyImg.sprite = trfFillSprite[trfCharacID - 1];
    }

    public void OnFingerImage()
    {
       // DOTween.Kill(fingerImage);

        fingerImage.anchoredPosition = new Vector2(0, 520f);
        fingerImage.gameObject.SetActive(true);

        fingerImage.DOAnchorPosY(100f, 0.5f).SetRelative().SetLoops(-1,LoopType.Yoyo);
        
    }

    public void OffFingerImage()
    {
        DOTween.Kill(fingerImage);
        fingerImage.gameObject.SetActive(false);
    }
}
