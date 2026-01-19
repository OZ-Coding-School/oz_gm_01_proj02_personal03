using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TableSlot
{
    public Transform root;
    public CardMonth month = CardMonth.None;
    public List<CardView> cards = new List<CardView>();

    public bool IsEmpty => month == CardMonth.None;

    public void Clear()
    {
        month = CardMonth.None;

        foreach(var card in cards)
        {
            if (card == null) continue;

            card.transform.SetParent(null);
            card.gameObject.SetActive(false);
        }
        cards.Clear();
    }

    public Vector3 GetPreviewWorldPosition()
    {
        if (root == null) return Vector3.zero;
        return root.position;
    }
}
