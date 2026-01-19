using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CaptureResult
{
    public bool captured;
    public bool needSelect;
    public CardData playedCard;
    public List<CardData> capturedCards;
    public List<CardData> selectableCards;

    public static CaptureResult None => new CaptureResult
    { 
        captured = false, 
        needSelect = false
    };
}


public sealed class CaptureResolver
{
    public CaptureResult Resolve(CardData playedCard, // 낸 카드
        List<CardData> tableCards) // 바닥 카드
    {
        if(playedCard == null || tableCards == null)
        {
            return CaptureResult.None;
        }

        //같은 월 카드 찾기
        List<CardData> sameMonthCards = tableCards.FindAll(c => c != null && c.month == playedCard.month);

        //못먹는 경우
        if(sameMonthCards.Count == 0)
        {
            return CaptureResult.None;
        }

        //먹는 경우 (1장)
        if(sameMonthCards.Count == 1)
        {
            return new CaptureResult
            {
                captured = true,
                needSelect = false,
                capturedCards = new List<CardData> { playedCard, sameMonthCards[0] }
            };

        }
        //먹는 경우 (2장)
        return new CaptureResult
        {
            captured = false,
            needSelect = true,
            playedCard = playedCard,
            selectableCards = sameMonthCards
        };
    }
}
