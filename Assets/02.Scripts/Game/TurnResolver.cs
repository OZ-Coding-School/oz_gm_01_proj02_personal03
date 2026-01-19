using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnExecuteResult
{
    Completed,
    WaitingSelection
}


public sealed class TurnResolver
{
    private readonly CaptureResolver captureResolver = new CaptureResolver();

    public TurnExecuteResult ExecuteTurn(Player player, List<CardData> tableCards)
    {
        //손에서 카드 선택
        CardData played = SelectCard(player);
        if (player == null)
        {
            return TurnExecuteResult.Completed;
        }

        var handResult = captureResolver.Resolve(played, tableCards);
        //바닥과 손에서 낸 카드 판정
        if (handResult.needSelect)
        {
            RoundManager.Instance.RequestCaptureSelection(
                player,
                played,
                handResult.selectableCards,
                ResolveSource.Hand
            );
            return TurnExecuteResult.WaitingSelection;
        }

        // 선택 필요 없을 때만 손에서 제거
        RoundManager.Instance.ResolveSelectedCaptureInternalDirect(
        player,
        played,
        handResult.capturedCards?[1],
        ResolveSource.Hand
    );
        //덱에서 카드 1장 드로우
        CardData draw = DeckManager.Instance.Draw();
        if (draw == null)
        {
            return TurnExecuteResult.Completed;
        }

        //바닥과 드로우 카드 판정
        var drawResult = captureResolver.Resolve(draw, tableCards);

        if (drawResult.needSelect)
        {
            RoundManager.Instance.RequestCaptureSelection(
                player,
                draw,
                drawResult.selectableCards,
                ResolveSource.Draw
            );
            return TurnExecuteResult.WaitingSelection;
        }

        RoundManager.Instance.ResolveSelectedCaptureInternalDirect(
           player,
           draw,
           drawResult.capturedCards?[1],
           ResolveSource.Draw
        );

        return TurnExecuteResult.Completed;
    }

    private CardData SelectCard(Player player)
    {
        if(player is HumanPlayer humanplayer)
        {
            return humanplayer.SelectedCardSubmit();
        }

        if(player is AIPlayer aiplayer)
        {
            return aiplayer.SelectCard();
        }

        return null;
    }
}
