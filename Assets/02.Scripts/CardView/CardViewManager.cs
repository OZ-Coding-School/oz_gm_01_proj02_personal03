using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum CardAreaType
{
    AIHandCard,
    AICapturedCard,
    Deck,
    HumanHandCard,
    HumanCapturedCard,
    TableCard
}


public class CardViewManager : MonoBehaviour
{
    public static CardViewManager Instance { get; private set; }

    [SerializeField] private CardView cardViewPrefab;

    private Dictionary<CardAreaType, ObjectPooling<CardView>> pools
    = new Dictionary<CardAreaType, ObjectPooling<CardView>>();

    [SerializeField] private Transform AIHandCardArea;      //»ó´ë ¼Õ
    [SerializeField] private Transform AICapturedCardArea;  //»ó´ë ¸ÔÀº
    [SerializeField] private Transform DeckArea;            //µ¦
    [SerializeField] private Transform HumanHandCardArea;   //»ç¶÷ ¼Õ
    [SerializeField] private Transform HumanCapturedCardArea;   //»ç¶÷ ¸ÔÀº
    [SerializeField] private Transform TableCardArea;       //¹Ù´ÚÄ«µå

    private void Awake()
    {
        Instance = this;

        pools[CardAreaType.HumanHandCard] =
       new ObjectPooling<CardView>(cardViewPrefab, HumanHandCardArea, preload: 10);

        pools[CardAreaType.AIHandCard] =
            new ObjectPooling<CardView>(cardViewPrefab, AIHandCardArea, preload: 10);

        pools[CardAreaType.TableCard] =
            new ObjectPooling<CardView>(cardViewPrefab, TableCardArea, preload: 12);

        pools[CardAreaType.HumanCapturedCard] =
            new ObjectPooling<CardView>(cardViewPrefab, HumanCapturedCardArea, preload: 20);

        pools[CardAreaType.AICapturedCard] =
            new ObjectPooling<CardView>(cardViewPrefab, AICapturedCardArea, preload: 20);
        
        pools[CardAreaType.Deck] =
            new ObjectPooling<CardView>(cardViewPrefab, DeckArea, preload: 48);
    }

    public CardView GetCard(CardData data, CardAreaType area, bool front)
    {
        var view = pools[area].Get();
        var parent = GetAreaTransform(area);

        view.transform.SetParent(parent, worldPositionStays: false);
        view.transform.SetAsLastSibling(); 

        view.Init(data, front,area);
        return view; ;
    }

    public void ClearArea(CardAreaType area)
    {
        pools[area].ReleaseAll();
    }

    private Transform GetParent(CardAreaType area)
    {
        switch (area)
        {
            case CardAreaType.AIHandCard:
                return AIHandCardArea;
            case CardAreaType.AICapturedCard:
                return AICapturedCardArea;
            case CardAreaType.Deck:
                return DeckArea;
            case CardAreaType.HumanHandCard:
                return HumanHandCardArea;
            case CardAreaType.HumanCapturedCard:
                return HumanCapturedCardArea;
            case CardAreaType.TableCard:
                return TableCardArea;
            default:
                return null;
        }
    }

    public Transform GetAreaTransform(CardAreaType area)
    {
        return GetParent(area);
    }
}
