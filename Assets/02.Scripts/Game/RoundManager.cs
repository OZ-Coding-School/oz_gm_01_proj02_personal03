using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoundState
{ 
    Distribution,   //카드분배
    PlayerTurn,     //플레이어 턴 
    OpponentTurn,   //상대 턴
    Resolve,        //판정
    GoStop,         //고스톱 선택
    Settlement,     //정산
    End             //라운드 끝
}
public enum ResolveSource
{ 
    Hand,
    Draw
}

public class RoundManager : Singleton<RoundManager>
{
    [SerializeField] private Transform distributionTempRoot;

    private Dictionary<CardMonth, TableSlot> previewSlotMap = new Dictionary<CardMonth, TableSlot>();

    protected override bool IsDontDestroyOnLoad => false;

    public static event Action<RoundState> OnRoundStateChanged;

    private List<CardData> tableCards = new List<CardData>();

    private ScoreManager humanScore;
    private ScoreManager aiScore;

    private GoStopManager humanGoStop;
    private GoStopManager aiGoStop;

    private HumanPlayer humanPlayer;
    private AIPlayer aiPlayer;

    private TurnResolver turnResolver;

    private Coroutine roundRoutine;
    private Coroutine arrangeRoutine;

    private Player pendingPlayer;
    private CardData pendingPlayedCard;
    private List<CardData> pendingSelectCards;

    private ResolveSource pendingResolveSource;

    //Stop선언 종료 플래그
    private bool isStoped = false;

    public RoundState CurrentState;

    

    protected override void Awake()
    {
        base.Awake();

        humanPlayer = new HumanPlayer("Human");
        aiPlayer = new AIPlayer("AI");

        turnResolver = new TurnResolver();

        humanScore = new ScoreManager();
        aiScore = new ScoreManager();

        humanGoStop = new GoStopManager();
        aiGoStop = new GoStopManager();

    }

    public void StartRound()
    {
        isStoped = false;

        if(roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
        }
        roundRoutine = StartCoroutine(RoundRoutine());
    }

    private IEnumerator RoundRoutine()
    {
        //====================Distribution=====================
        yield return StartCoroutine(PlayDistributionAnmaition());
        //======================================================
        //======================Turn Loop=======================
        while (true)
        {
            //============PlayerTurn============
            ChangeState(RoundState.PlayerTurn);
            humanScore.BeginTurn();
            
            while(true)
            {
                if (humanPlayer.SelectIndex >= 0)
                {
                    var result = turnResolver.ExecuteTurn(humanPlayer, tableCards);

                    if (result == TurnExecuteResult.WaitingSelection)
                    {
                        yield return new WaitUntil(() => CurrentState != RoundState.Resolve);
                        continue; // 선택 끝난 뒤 턴 재개
                    }

                    break; // 턴 정상 종료
                }

                yield return null;
            }

            RefreshHandAndTableView();

            //점수 업뎃 로그 -> 추후 삭제
            int before = humanScore.CurrentScore;
            humanScore.EndTurnAndRecalculate(humanPlayer);
            Debug.Log($"[Score] Human : {before} → {humanScore.CurrentScore}");
            
            humanGoStop.JudgeAfterTurn(humanScore);
            
            yield return StartCoroutine(HandleGoStop(humanPlayer, humanScore, humanGoStop));
            if(CheckRoundEnd())
            {
                break;
            }
            //==============AITurn==============
            ChangeState(RoundState.OpponentTurn);
            aiScore.BeginTurn();

            turnResolver.ExecuteTurn(aiPlayer, tableCards);

            RefreshHandAndTableView();

            //점수 업뎃 로그 -> 추후 삭제
            int aiBefore = aiScore.CurrentScore;
            aiScore.EndTurnAndRecalculate(aiPlayer);
            Debug.Log($"[Score] AI : {aiBefore} → {aiScore.CurrentScore}");
            
            aiGoStop.JudgeAfterTurn(aiScore);

            yield return StartCoroutine(HandleGoStop(aiPlayer, aiScore, aiGoStop));
            
            if(CheckRoundEnd())
            {
                break;
            } 
        }
        //======================================================
        //=========================End==========================
        ChangeState(RoundState.End);

        humanGoStop.Reset();
        aiGoStop.Reset();

        Debug.Log("========== Round End ==========");
    }

    private IEnumerator HandleGoStop(Player player, ScoreManager score, GoStopManager goStop)
    {
        ChangeState(RoundState.GoStop);

        if(!goStop.CanGo(score) && !goStop.CanStop(score))
        {
            yield break;
        }

        //GoStop선택 로그 -> 추후 삭제
        Debug.Log($"[GoStop] {player.Name} 현재 점수 = {score.CurrentScore}");
        Debug.Log("[GoStop] 선택 대기중... (G = Go / S = Stop)");
        
        bool decided = false;

        while(!decided)
        {
            if(player is HumanPlayer)
            {
                if(Input.GetKeyDown(KeyCode.G) && goStop.CanGo(score))
                {
                    Debug.Log("[Input] Human pressed G");
                    goStop.LetsGo(score);
                    Debug.Log("[Game] Go 선택 → 게임 계속");
                    decided = true;
                }
                else if(Input.GetKeyDown(KeyCode.S) && goStop.LetsStop(score))
                {
                    Debug.Log("[Input] Human pressed S");
                    Debug.Log("[Game] Stop 선택 → 라운드 종료");
                    isStoped = true;
                    decided = true;
                }
            }
            else
            {
                if(goStop.CanStop(score))
                {
                    Debug.Log("[AI] Stop 선택");
                    isStoped = true;
                }
                else
                {
                    Debug.Log("[AI] Go 선택");
                    goStop.LetsGo(score);
                }
                decided = true;
            }

            yield return null;
        }
    }

    private bool CheckRoundEnd()
    {
        if(DeckManager.Instance.IsEmpty())
        {
            Debug.Log("덱 소진. 라운드 종료");
            return true;
        }
        if(isStoped)
        {
            Debug.Log("Stop 선언. 라운드 종료");
            return true;
        }

        return false;
    }

    private void ChangeState(RoundState next)
    {
        CurrentState = next;
        Debug.Log($"[RoundManager] State -> {next}");
    }

    // 숫자 누르면 Human만 선택 인덱스
    public void SetHumanSelectIndex(int index)
    {
        humanPlayer.SetSelectIndex(index);
        Debug.Log($"[Human Select] index={index}");
    }

    //카드 시각화 메서드
    private void ShowDistributionViews()
    {
        CardViewManager.Instance.ClearArea(CardAreaType.HumanHandCard);
        CardViewManager.Instance.ClearArea(CardAreaType.AIHandCard);

        RebuildTableView();

        for(int i = 0; i<humanPlayer.Hand.Count; i++)
        {
            var view = CardViewManager.Instance.GetCard(humanPlayer.Hand[i], CardAreaType.HumanHandCard, true);
            view.BindHandIndex(i, clickable: true);
        }

        for (int i = 0; i < aiPlayer.Hand.Count; i++)
        {
            var view = CardViewManager.Instance.GetCard(aiPlayer.Hand[i], CardAreaType.AIHandCard, false);
            view.BindHandIndex(-1, clickable: false);
        }
    }

    //손패/테이블 갱신 메서드
    private void RefreshHandAndTableView()
    {
        //HumanHand
        CardViewManager.Instance.ClearArea(CardAreaType.HumanHandCard);

        for (int i = 0; i < humanPlayer.Hand.Count; i++)
        {
            var view = CardViewManager.Instance.GetCard(
                humanPlayer.Hand[i],
                CardAreaType.HumanHandCard,
                true
            );
            view.BindHandIndex(i, true);
        }

        //AIHand
        CardViewManager.Instance.ClearArea(CardAreaType.AIHandCard);
        List<Transform> aiHandCards = new List<Transform>();

        for (int i = 0; i < aiPlayer.Hand.Count; i++)
        {
            var view = CardViewManager.Instance.GetCard(
                aiPlayer.Hand[i],
                CardAreaType.AIHandCard,
                false
            );

            view.BindHandIndex(-1, clickable: false);
        }

        RebuildTableView();

        RequestArrangeHands();
    }

    private void ArrangeHands()
    {
        ArrangeHandArea(CardAreaType.HumanHandCard);
        ArrangeHandArea(CardAreaType.AIHandCard);
    }

    private void ArrangeHandArea(CardAreaType areaType)
    {
        var area = CardViewManager.Instance.GetAreaTransform(areaType);
        if (area == null) return;

        var layout = area.GetComponent<HandFanLayOut>();
        if (layout == null)
        {
            Debug.LogWarning($"[{nameof(RoundManager)}] {areaType}에 HandFanLayout이 없습니다.");
            return;
        }

        List<Transform> cards = new List<Transform>(area.childCount);
        for (int i = 0; i < area.childCount; i++)
        {
            var child = area.GetChild(i);
            if(!child.gameObject.activeSelf)
            {
                continue;
            }
            cards.Add(child);
        }
            

        layout.Arrange(cards);
    }

    private void RequestArrangeHands()
    {
        if (arrangeRoutine != null)
            StopCoroutine(arrangeRoutine);

        arrangeRoutine = StartCoroutine(CoArrangeHandsNextFrame());
    }

    private IEnumerator CoArrangeHandsNextFrame()
    {
        // 1프레임 대기: UI 레이아웃 / Destroy 정리 / Instantiate 반영
        yield return null;

        // (옵션) 강제 레이아웃 갱신
        Canvas.ForceUpdateCanvases();

        ArrangeHands();
        arrangeRoutine = null;
    }

    //선택 요청 메서드
    public void RequestCaptureSelection(Player player, CardData playedCard, List<CardData> candidates, ResolveSource source)
    {
        ChangeState(RoundState.Resolve);

        pendingPlayer = player;
        pendingPlayedCard = playedCard;
        pendingResolveSource = source;

        if (player is AIPlayer)
        {
            ResolveSelectedCapture(candidates[0]);
            return;
        }

        //UI 띄우기
        CaptureSelectUI.Instance.ShowCardSelect(candidates, selected => { ResolveSelectedCapture(selected); });
    }

    //선택 확정 메서드
    public void ResolveSelectedCapture(CardData selected)
    {
        // Human 선택 확정
        ResolveSelectedCaptureInternal(pendingPlayer, pendingPlayedCard, selected, pendingResolveSource);

        // pending 정리
        pendingPlayer = null;
        pendingPlayedCard = null;
        pendingSelectCards = null;
    }

    private void ResolveSelectedCaptureInternal(Player player, CardData playedCard, CardData selected, ResolveSource source)
    {
        if(player == null || playedCard == null || selected == null)
        {
            return;
        }

        if(source == ResolveSource.Hand)
        {
            player.PlayCard(playedCard);
        }

        player.AddCapturedCard(playedCard);
        player.AddCapturedCard(selected);
        tableCards.Remove(selected);

        CapturedCardManager.Instance.RefreshCaptured(player);

        player.ClearPlayedCard();

        RefreshHandAndTableView();

        ResumeAfterResolve(source);
    }

    public void ResolveSelectedCaptureInternalDirect(
    Player player,
    CardData playedCard,
    CardData selected,
    ResolveSource source)
    {
        if (player == null || playedCard == null)
            return;

        // 손에서 낸 카드 제거
        if (source == ResolveSource.Hand)
        {
            player.PlayCard(playedCard);
        }

        // 캡처 성공
        if (selected != null)
        {
            player.AddCapturedCard(playedCard);
            player.AddCapturedCard(selected);
            tableCards.Remove(selected);
        }
        else
        {
            // 못 먹으면 테이블로
            tableCards.Add(playedCard);
        }

        player.ClearPlayedCard();
        CapturedCardManager.Instance.RefreshCaptured(player);
        RefreshHandAndTableView();

        // 턴 복귀
        ResumeAfterResolve(source);
    }

    private void ResumeAfterResolve(ResolveSource source)
    {
        if (source == ResolveSource.Hand)
        {
            ChangeState(RoundState.PlayerTurn);
            return;
        }
        ChangeState(RoundState.OpponentTurn);
    }

    private void RebuildTableView()
    {
        CardViewManager.Instance.ClearArea(CardAreaType.TableCard);

        TableSlotManager.Instance.ClearAll();

        foreach (var card in tableCards)
        {
            if (card == null) continue;

            var view = CardViewManager.Instance.GetCard(card, CardAreaType.TableCard, front: true);
            view.BindHandIndex(-1, clickable: false);

            TableSlotManager.Instance.PlaceCard(view, card);
        }
    }

    private IEnumerator PlayDistributionAnmaition()
    {
        ChangeState(RoundState.Distribution);

        tableCards.Clear();
        humanPlayer.Hand.Clear();
        aiPlayer.Hand.Clear();

        DeckManager.Instance.InitializeDeck(true);

        Transform deck = CardViewManager.Instance.GetAreaTransform(CardAreaType.Deck);

        // 1. 바닥 4장
        previewSlotMap.Clear();
        yield return StartCoroutine(DealToTable(4, deck));

        // 2. AI 5장
        yield return StartCoroutine(DealToPlayer(aiPlayer, CardAreaType.AIHandCard, 5, deck));

        // 3. Human 5장
        yield return StartCoroutine(DealToPlayer(humanPlayer, CardAreaType.HumanHandCard, 5, deck));

        // 4. 바닥 4장
        previewSlotMap.Clear();
        yield return StartCoroutine(DealToTable(4, deck));

        // 5. AI 5장
        yield return StartCoroutine(DealToPlayer(aiPlayer, CardAreaType.AIHandCard, 5, deck));

        // 6. Human 5장
        yield return StartCoroutine(DealToPlayer(humanPlayer, CardAreaType.HumanHandCard, 5, deck));
    }

    private IEnumerator DealToTable(int count, Transform deck)
    {
        for (int i = 0; i < count; i++)
        {
            CardData card = DeckManager.Instance.Draw();
            tableCards.Add(card);

            var view = CardViewManager.Instance.GetCard(card, CardAreaType.Deck, true);
            
            view.transform.SetParent(distributionTempRoot, worldPositionStays: true);
            view.transform.position = deck.position;

            Vector3 center = GetReservedPreviewPosition(card.month);

            // 같은 슬롯 안에서도 살짝 퍼지게 (선택)
            float spreadX = UnityEngine.Random.Range(-20f, 20f);
            float spreadY = UnityEngine.Random.Range(-10f, 10f);

            Vector3 target = center + new Vector3(spreadX, spreadY, 0f);

            bool done = false;
            view.PlayMoveTo(target, 0.25f, () => done = true);

            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.05f);
        }

        RebuildTableView(); // 정렬은 마지막에 한 번
    }

    private IEnumerator DealToPlayer(
     Player player,
     CardAreaType area,
     int count,
     Transform deck)
    {
        Transform handArea =
            CardViewManager.Instance.GetAreaTransform(area);

        float fanAngle = 40f;     // 전체 부채꼴 각도
        float radius = 120f;      // 부채 반경
        float startAngle = -fanAngle * 0.5f;
        float step = (count == 1) ? 0f : fanAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            CardData card = DeckManager.Instance.Draw();
            player.Hand.Add(card);

            var view = CardViewManager.Instance.GetCard(
                card,
                CardAreaType.Deck, 
                area == CardAreaType.HumanHandCard
            );

            // 임시 루트 유지
            view.transform.SetParent(distributionTempRoot, true);
            view.transform.position = deck.position;

            float angle = startAngle + step * i;
            Vector3 offset =
                Quaternion.Euler(0, 0, angle) * Vector3.up * radius;

            Vector3 target = handArea.position + offset;

            bool done = false;
            view.PlayMoveTo(target, 0.25f, () => done = true);

            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.05f);
        }


        RefreshHandAndTableView();

        CardViewManager.Instance.ClearArea(CardAreaType.Deck);
        distributionTempRoot.DetachChildren();
    }

    private Vector3 GetReservedPreviewPosition(CardMonth month)
    {
        // 1. 이미 이 월이 예약돼 있으면 같은 슬롯 사용
        if (previewSlotMap.TryGetValue(month, out var reservedSlot))
        {
            return reservedSlot.root.position;
        }

        // 2. 아직 예약 안 된 월 → 빈 슬롯 중 하나 예약
        var slots = TableSlotManager.Instance.GetAllSlots();

        foreach (var slot in slots)
        {
            // 아직 실제로도 비어 있고, 프리뷰 예약도 안 된 슬롯
            if (slot.IsEmpty && !previewSlotMap.ContainsValue(slot))
            {
                previewSlotMap[month] = slot;
                return slot.root.position;
            }
        }

        // 3. 예외 상황 (슬롯 부족)
        return TableSlotManager.Instance.transform.position;
    }
}
