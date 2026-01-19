using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableSlotManager : MonoBehaviour
{
    public static TableSlotManager Instance;

    [SerializeField] private List<TableSlot> slots = new List<TableSlot>();

    [SerializeField] private Vector3 stackOffset = new Vector3(0.15f, -0.1f, -0.01f);

    private void Awake()
    {
        Instance = this;
    }

    public void ClearAll()
    {
        foreach(var slot in slots)
        {
            slot.Clear();
        }
    }

    public void PlaceCard(CardView view, CardData data)
    {
        TableSlot slot = FindSlot(data.month);

        if(slot == null)
        {
            return;
        }

        if(slot.IsEmpty)
        {
            slot.month = data.month;
        }

        slot.cards.Add(view);
        view.transform.SetParent(slot.root, false);

        ArrangeSlot(slot);
    }

    private TableSlot FindSlot(CardMonth month)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.month == month)
            {
                return slot;
            }
        }

        foreach(var slot in slots)
        {
            if(slot.IsEmpty)
            {
                return slot;
            }
        }
        return null;
    }

    private void ArrangeSlot(TableSlot slot)
    {
        for(int i = 0; i < slot.cards.Count; i++)
        {
            slot.cards[i].transform.localPosition = stackOffset * i;
        }
    }

    public Vector3 GetNextSlotPreviewPosition(CardMonth month)
    {
        // 이미 배정된 월 슬롯이 있으면 그 슬롯
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.month == month)
                return slot.GetPreviewWorldPosition();
        }

        // 없으면 비어있는 슬롯(처음 들어갈 자리)
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
                return slot.GetPreviewWorldPosition();
        }

        // 슬롯이 모자라면 fallback
        return transform.position;
    }
    public IReadOnlyList<TableSlot> GetAllSlots()
    {
        return slots;
    }
}
