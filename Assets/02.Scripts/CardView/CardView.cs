using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardView : MonoBehaviour, IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
{
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image frontImage;
    [SerializeField] private Image backImage;
    [SerializeField] private Image highlight;

    [SerializeField] private RectTransform rect;
    [SerializeField] private float hoverOffsetY = 40f;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float tweenDuration = 0.15f;

    private Vector2 originVisualPos;
    private Vector3 originVisualScale;

    private CardData data;
    private CardAreaType areaType;

    //손패 인덱스 주입
    public int HandIndex { get; private set; } = -1;
    //클릭 가능한 카드인지 체크
    public bool IsClickable { get; private set; } = false;

    private void Awake()
    {
        originVisualPos = visualRoot.anchoredPosition;
        originVisualScale = visualRoot.localScale;
    }

    public void Init(CardData card, bool front, CardAreaType area)
    {
        data = card;
        areaType = area;

        frontImage.sprite = card.cardSprite;
        SetFront(front);
        SetHighlight(false);

        visualRoot.anchoredPosition = originVisualPos;
        visualRoot.localScale = originVisualScale;
        frontImage.color = Color.white;
    }

    //카드 앞면 뒷면
    public void SetFront(bool front)
    {
        frontImage.gameObject.SetActive(front);
        backImage.gameObject.SetActive(!front);
    }

    //하이라이트
    public void SetHighlight(bool on)
    {
        if(highlight != null)
        {
            highlight.gameObject.SetActive(on);
        }
    }
    
    // 손패에 인덱스 + 클릭 가능 여부
    public void BindHandIndex(int index, bool clickable)
    {
        HandIndex = index;
        IsClickable = clickable;
    }

    public void SetVisualScale(float scale)
    {
        if(visualRoot == null)
        {
            return;
        }

        visualRoot.localScale = Vector3.one * scale;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        //클릭 불가(내 손패만 해야됨 다른영역 XXX)
        if(!CanInteract())
        {
            return;
        }

        if(areaType != CardAreaType.HumanHandCard)
        {
            return;
        }

        RoundManager.Instance.SetHumanSelectIndex(HandIndex);

        //임시 : 선택했을때 시각적 하이라이트(미구현)
        SetHighlight(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!CanInteract())
        {
            return;
        }

        visualRoot.DOKill();
        transform.SetAsLastSibling();

        visualRoot.DOAnchorPos(
             originVisualPos + Vector2.up * hoverOffsetY,
             tweenDuration
         ).SetEase(Ease.OutBack);

        visualRoot.DOScale(
            originVisualScale * hoverScale,
            tweenDuration
        ).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        visualRoot.DOKill();

        visualRoot.DOAnchorPos(originVisualPos, tweenDuration)
            .SetEase(Ease.OutQuad);

        visualRoot.DOScale(originVisualScale, tweenDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
        { 
            return; 
        }

        frontImage.DOKill();

        frontImage.DOColor(new Color(0.8f, 0.8f, 0.8f, 1f),0.05f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (areaType != CardAreaType.HumanHandCard)
        {
            return;
        }

        frontImage.DOKill();

        frontImage.DOColor(Color.white, 0.05f);
    }

    public void OnDisable()
    {
        visualRoot.DOKill();
        frontImage.DOKill();

        visualRoot.anchoredPosition = originVisualPos;
        visualRoot.localScale = originVisualScale;
        frontImage.color = Color.white;
        SetHighlight(false);

        HandIndex = -1;
        IsClickable = false;
    }

    public void PlayMoveTo(Vector3 worldTarget, float duration, System.Action onComplete = null)
    {
        transform.DOKill();

        transform.DOMove(worldTarget, duration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
    }

    private bool CanInteract()
    {
        if (RoundManager.Instance.CurrentState != RoundState.PlayerTurn &&
           RoundManager.Instance.CurrentState != RoundState.Resolve)
            return false;

        return IsClickable;
    }

    public CardData Data => data;

}
