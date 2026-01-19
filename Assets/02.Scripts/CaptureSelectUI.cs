using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GukjinChoice
{
    End,   // 띠 처리
    Pee     // 피 처리
}

public class CaptureSelectUI : MonoBehaviour
{
    public static CaptureSelectUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Image[] cardImages;
    [SerializeField] private Sprite endSprite;
    [SerializeField] private Sprite peeSprite;

    private Action<CardData> onSelected;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }


    //바닥에 같은 카드 2개 있을때 선택하는UI
    public void ShowCardSelect(List<CardData> candidates, Action<CardData> onSelected)
    {
        titleText.text = "Which card would you like to choose?";

        root.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].onClick.RemoveAllListeners();

            if (i < candidates.Count)
            {
                CardData card = candidates[i];

                cardImages[i].sprite = card.cardSprite;
                choiceButtons[i].gameObject.SetActive(true);

                choiceButtons[i].onClick.AddListener(() => { root.SetActive(false); onSelected.Invoke(card); });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }
   
    //국진 : 쌍피/열끗 선택 UI
    public void ShowGukjinSelect(Action<GukjinChoice> onSelected)
    {
        titleText.text = "Which one would you like to choose?";

        root.SetActive(true);

        //열끗
        choiceButtons[0].gameObject.SetActive(true);
        cardImages[0].sprite = endSprite;
        choiceButtons[0].onClick.RemoveAllListeners();
        choiceButtons[0].onClick.AddListener(() => 
        { 
            root.SetActive(false); 
            onSelected?.Invoke(GukjinChoice.End); 
        });

        //쌍피
        choiceButtons[1].gameObject.SetActive(true);
        cardImages[1].sprite = peeSprite;
        choiceButtons[1].onClick.RemoveAllListeners();
        choiceButtons[1].onClick.AddListener(() =>
        {
            root.SetActive(false);
            onSelected?.Invoke(GukjinChoice.Pee);
        });
    }
}
