using System;
using System.Collections.Generic;
using CardGame.Core;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace CardGame.UI
{
    /// <summary>
    /// Màn hình chiến đấu theo đội: 3 tướng vs boss + 2 thuộc hạ.
    /// Bind model (UnitState/HudState) vào UXML bằng data binding,
    /// kéo-thả thẻ theo TargetKind, và diễn hoạt animation cho từng kiểu ra đòn.
    /// </summary>
    public sealed class BattleScreen
    {
        public VisualElement Root { get; }

        readonly App app;
        readonly VisualTreeAsset cardAsset;
        readonly VisualTreeAsset unitAsset;

        readonly VisualElement teamPlayerRow;
        readonly VisualElement teamEnemyRow;
        readonly VisualElement zonePlayer;
        readonly VisualElement zoneEnemy;
        readonly VisualElement battlefield;
        readonly VisualElement hand;
        readonly VisualElement battleLog;
        readonly VisualElement overlay;

        readonly Label lblTurn;
        readonly Label lblDraw;
        readonly Label lblDiscard;
        readonly Label lblEnergy;
        readonly Label resultTitle;
        readonly Label resultSub;
        readonly Button btnEndTurn;

        TeamBattleEngine engine;
        bool animateNextHand;

        sealed class UnitView
        {
            public UnitState Model;
            public VisualElement Container; // TemplateContainer trong hàng
            public VisualElement Root;      // phần tử name="unit"
            public VisualElement Art;       // vùng hình (name="unit-art")
            public UnitVisual Visual;       // sprite animation (nếu có)
        }

        readonly List<UnitView> playerViews = new List<UnitView>();
        readonly List<UnitView> enemyViews = new List<UnitView>();

        // ---- trạng thái kéo thả ----
        bool dragging;
        VisualElement dragged;
        CardDef draggedCard;
        Vector2 dragStartPointer;
        int dragPointerId = -1;
        bool dragMoved; // đã di chuyển quá ngưỡng → tính là kéo, không phải bấm chọn
        UnitView hoverView;
        VisualElement hoverZone;

        // ---- cơ chế 2: bấm chọn thẻ rồi bấm mục tiêu ----
        VisualElement selectedEl;
        CardDef selectedCard;

        public BattleScreen(App app, VisualElement root, VisualTreeAsset cardAsset, VisualTreeAsset unitAsset)
        {
            Root = root;
            this.app = app;
            this.cardAsset = cardAsset;
            this.unitAsset = unitAsset;

            teamPlayerRow = Req<VisualElement>(root, "team-player-row");
            teamEnemyRow = Req<VisualElement>(root, "team-enemy-row");
            zonePlayer = Req<VisualElement>(root, "team-player");
            zoneEnemy = Req<VisualElement>(root, "team-enemy");
            hand = Req<VisualElement>(root, "hand");
            battleLog = Req<VisualElement>(root, "battle-log");
            overlay = Req<VisualElement>(root, "result-overlay");
            battlefield = root.Q(className: "battlefield");
            if (battlefield == null)
                Debug.LogError("[CardGame] Battle.uxml thiếu phần tử class='battlefield'");

            lblTurn = Req<Label>(root, "lbl-turn");
            lblDraw = Req<Label>(root, "lbl-draw");
            lblDiscard = Req<Label>(root, "lbl-discard");
            lblEnergy = Req<Label>(root, "lbl-energy");
            resultTitle = Req<Label>(root, "result-title");
            resultSub = Req<Label>(root, "result-sub");
            btnEndTurn = Req<Button>(root, "btn-end-turn");

            btnEndTurn.clicked += OnEndTurn;
            Req<Button>(root, "btn-retry").clicked += OnRetry;
            Req<Button>(root, "btn-menu").clicked += OnBackToMenu;

            overlay.style.display = DisplayStyle.None;
            overlay.pickingMode = PickingMode.Ignore;

            // bấm nền sân đấu (không trúng thẻ/unit) → bỏ chọn thẻ đang chọn
            battlefield?.RegisterCallback<ClickEvent>(evt =>
            {
                if (selectedCard != null) ClearSelection();
            });
        }

        // ------------------------------------------------------------------
        //  Khởi động ván
        // ------------------------------------------------------------------

        public void StartNewGame()
        {
            if (engine == null)
            {
                engine = new TeamBattleEngine();
                engine.LogAdded += AddLog;
                engine.HandChanged += RenderHand;
                engine.BattleEnded += OnBattleEnded;
                CreateUnitViews();
                BindAll();
            }

            battleLog.Clear();
            animateNextHand = true;
            engine.StartRun(); // bắn LogAdded + HandChanged (→ RenderHand)
            animateNextHand = false;
            RefreshUnits();
            RefreshInteractivity();
            StartIdleAll();
        }

        void StartIdleAll()
        {
            for (int i = 0; i < playerViews.Count; i++)
            {
                var v = playerViews[i];
                v?.Visual?.PlayIdle(app);
            }
            for (int i = 0; i < enemyViews.Count; i++)
            {
                var v = enemyViews[i];
                v?.Visual?.PlayIdle(app);
            }
        }

        void OnRetry()
        {
            if (engine == null) return;
            HideOverlay();
            battleLog.Clear();
            animateNextHand = true;
            engine.StartRun();
            animateNextHand = false;
            RefreshUnits();
            RefreshInteractivity();
            StartIdleAll();
        }

        void OnBackToMenu()
        {
            HideOverlay();
            app.ShowMenu();
        }

        // ------------------------------------------------------------------
        //  Data binding
        // ------------------------------------------------------------------

        void BindAll()
        {
            Bind(lblTurn, engine.Hud, "TurnText");
            Bind(lblDraw, engine.Hud, "DrawText");
            Bind(lblDiscard, engine.Hud, "DiscardText");
            Bind(lblEnergy, engine.Hud, "EnergyText");
            Bind(resultTitle, engine.Hud, "ResultTitle");
            Bind(resultSub, engine.Hud, "ResultSub");
        }

        static void Bind(VisualElement element, object source, string sourcePath)
        {
            if (element == null) return;
            element.dataSource = source;
            element.SetBinding("text", new DataBinding
            {
                dataSourcePath = new PropertyPath(sourcePath),
                bindingMode = BindingMode.ToTarget
            });
        }

        static void BindWidth(VisualElement element, object source, string sourcePath)
        {
            if (element == null) return;
            element.dataSource = source;
            element.SetBinding("style.width", new DataBinding
            {
                dataSourcePath = new PropertyPath(sourcePath),
                bindingMode = BindingMode.ToTarget
            });
        }

        // ------------------------------------------------------------------
        //  6 đơn vị trên sân
        // ------------------------------------------------------------------

        void CreateUnitViews()
        {
            foreach (var u in engine.Champions)
            {
                var v = BuildUnitView(u);
                playerViews.Add(v);
                teamPlayerRow.Add(v.Container);
            }
            foreach (var u in engine.Enemies)
            {
                var v = BuildUnitView(u);
                enemyViews.Add(v);
                teamEnemyRow.Add(v.Container);
            }

            // bấm trực tiếp lên đơn vị = chơi lá đang chọn (cơ chế chọn-thẻ-rồi-chọn-mục-tiêu)
            foreach (var v in playerViews)
                if (v?.Root != null) RegisterUnitClick(v);
            foreach (var v in enemyViews)
                if (v?.Root != null) RegisterUnitClick(v);
        }

        void RegisterUnitClick(UnitView view)
        {
            view.Root.RegisterCallback<ClickEvent>(evt => OnUnitClick(view, evt));
        }

        UnitView BuildUnitView(UnitState u)
        {
            var container = unitAsset.Instantiate();
            app.ApplySemiBold(container);

            var el = container.Q("unit");
            if (el == null)
            {
                Debug.LogError("[CardGame] Unit.uxml thiếu phần tử name='unit'");
                return null;
            }

            el.AddToClassList("role--" + u.Role.ToString().ToLowerInvariant());
            el.AddToClassList(u.Team == Team.Player ? "unit--player" : "unit--enemy");

            var glyph = el.Q<Label>("unit-glyph");
            var name = el.Q<Label>("unit-name");
            if (glyph != null) glyph.text = u.Glyph;
            if (name != null) name.text = u.Name;

            Bind(el.Q<Label>("unit-hp"), u, "HpText");
            Bind(el.Q<Label>("unit-block"), u, "BlockText");
            Bind(el.Q<Label>("unit-taunt"), u, "TauntText");
            Bind(el.Q<Label>("unit-intent"), u, "IntentText");
            BindWidth(el.Q("unit-hp-fill"), u, "HpWidth");

            var el2 = container.Q("unit");
            if (el2 == null)
            {
                Debug.LogError("[CardGame] Unit.uxml thiếu phần tử name='unit'");
                return null;
            }
            var e2 = el2;

            e2.AddToClassList("role--" + u.Role.ToString().ToLowerInvariant());
            e2.AddToClassList(u.Team == Team.Player ? "unit--player" : "unit--enemy");

            var g = e2.Q<Label>("unit-glyph");
            var nm = e2.Q<Label>("unit-name");
            if (g != null) g.text = u.Glyph;
            if (nm != null) nm.text = u.Name;

            Bind(e2.Q<Label>("unit-hp"), u, "HpText");
            Bind(e2.Q<Label>("unit-block"), u, "BlockText");
            Bind(e2.Q<Label>("unit-taunt"), u, "TauntText");
            Bind(e2.Q<Label>("unit-intent"), u, "IntentText");
            BindWidth(e2.Q("unit-hp-fill"), u, "HpWidth");

            var spriteEl = e2.Q("unit-sprite");
            UnitVisual visual = null;
            if (u.Role == Role.Swordsman && spriteEl != null)
            {
                var subs = Resources.LoadAll<Sprite>("Sprites/swordsman_anim_sheet");
                if (subs != null && subs.Length > 0)
                {
                    Sprite id1=null,id2=null,r1=null,r2=null,s1=null,s2=null,s3=null;
                    for (int i = 0; i < subs.Length; i++)
                    {
                        var sn = subs[i].name;
                        if (sn.EndsWith("idle1")) id1=subs[i];
                        if (sn.EndsWith("idle2")) id2=subs[i];
                        if (sn.EndsWith("run1")) r1=subs[i];
                        if (sn.EndsWith("run2")) r2=subs[i];
                        if (sn.EndsWith("slash1")) s1=subs[i];
                        if (sn.EndsWith("slash2")) s2=subs[i];
                        if (sn.EndsWith("slash3")) s3=subs[i];
                    }
                    visual = new UnitVisual(spriteEl, new[]{id1,id2}, new[]{r1,r2}, new[]{s1,s2,s3});
                    e2.AddToClassList("unit--sprite");
                }
            }

            return new UnitView
            {
                Model = u,
                Container = container,
                Root = e2,
                Art = e2.Q("unit-art"),
                Visual = visual
            };
        }

        void RefreshUnits()
        {
            foreach (var v in playerViews) RefreshUnit(v);
            foreach (var v in enemyViews) RefreshUnit(v);
        }

        static void RefreshUnit(UnitView v)
        {
            if (v == null || v.Root == null) return;
            v.Root.EnableInClassList("unit--dead", !v.Model.IsAlive);
            if (v.Visual != null)
            {
                var host = v.Container?.panel?.visualTree?.userData as MonoBehaviour;
                // fallback: try to find App MonoBehaviour? easier: just call from coroutine host? alternatively use root.schedule? but Coroutine needs MB
                // BattleScreen có thể lưu App reference? App là MonoBehaviour
            }
        }

        UnitView ViewOf(UnitState unit)
        {
            if (unit == null) return null;
            foreach (var v in playerViews)
                if (v != null && v.Model == unit) return v;
            foreach (var v in enemyViews)
                if (v != null && v.Model == unit) return v;
            return null;
        }

        // ------------------------------------------------------------------
        //  Bàn tay (render + kéo thả)
        // ------------------------------------------------------------------

        void RenderHand()
        {
            if (hand == null || engine == null) return;

            ClearSelection(); // tay bài đổi → bỏ chọn (thẻ cũ không còn trên tay)
            hand.Clear();
            bool animate = animateNextHand;

            foreach (var card in engine.Hand)
            {
                var tree = cardAsset.Instantiate();
                var cardEl = tree.Q("card");
                if (cardEl == null) continue;

                cardEl.Q<Label>("card-cost-label").text = card.Cost.ToString();
                cardEl.Q<Label>("card-title").text = card.Name;
                cardEl.Q<Label>("card-desc").text = card.Desc;
                cardEl.Q<Label>("card-type").text = RoleName(card.Owner) + " · " +
                                                     (card.Kind == CardKind.Attack ? "Tấn công" : "Kỹ năng");
                cardEl.Q<Label>("card-art-glyph").text = card.Glyph;

                var art = cardEl.Q("card-art");
                if (art != null)
                    art.AddToClassList(card.Kind == CardKind.Attack ? "card-art--attack" : "card-art--skill");

                if (card.Cost > engine.Hud.Energy) cardEl.AddToClassList("card--expensive");

                RegisterDrag(cardEl, card);

                if (animate) cardEl.AddToClassList("card--enter");
                hand.Add(tree);
                if (animate)
                    cardEl.schedule.Execute(() => cardEl.RemoveFromClassList("card--enter"));
            }
        }

        static string RoleName(Role role)
        {
            switch (role)
            {
                case Role.Swordsman: return "Kiếm sĩ";
                case Role.Archer: return "Cung thủ";
                case Role.Tank: return "Tank";
                default: return role.ToString();
            }
        }

        void RegisterDrag(VisualElement cardEl, CardDef card)
        {
            cardEl.RegisterCallback<PointerDownEvent>(evt => OnDragStart(cardEl, card, evt));
            cardEl.RegisterCallback<PointerMoveEvent>(OnDragMove);
            cardEl.RegisterCallback<PointerUpEvent>(OnDragEnd);
            cardEl.RegisterCallback<PointerCaptureOutEvent>(evt => { if (dragged == cardEl) CancelDrag(); });
        }

        void OnDragStart(VisualElement cardEl, CardDef card, PointerDownEvent evt)
        {
            if (engine == null || engine.Phase != Phase.PlayerTurn || dragging) return;

            dragging = true;
            dragged = cardEl;
            draggedCard = card;
            dragPointerId = evt.pointerId;
            dragStartPointer = evt.position;
            dragMoved = false;

            cardEl.AddToClassList("card--dragging");
            PointerCaptureHelper.CapturePointer(cardEl, evt.pointerId);
            evt.StopPropagation();
        }

        void OnDragMove(PointerMoveEvent evt)
        {
            if (!dragging || dragged == null || evt.pointerId != dragPointerId) return;

            var delta = (Vector2)evt.position - dragStartPointer;

            // còn trong vùng nhỏ quanh thẻ → vẫn là "bấm chọn", đừng dịch chuyển
            if (!dragMoved)
            {
                if (delta.magnitude < 6f) return;
                dragMoved = true;
                ClearSelection(); // bắt đầu kéo thật → bỏ chế độ chọn
            }

            dragged.style.translate = new Translate(
                new Length(delta.x, LengthUnit.Pixel),
                new Length(delta.y, LengthUnit.Pixel));

            UpdateHover((Vector2)evt.position);
            evt.StopPropagation();
        }

        void OnDragEnd(PointerUpEvent evt)
        {
            if (!dragging || dragged == null || evt.pointerId != dragPointerId) return;

            var cardEl = dragged;
            var card = draggedCard;
            bool wasClick = !dragMoved &&
                            ((Vector2)evt.position - dragStartPointer).magnitude < 6f;
            var dropView = wasClick ? null : UpdateHover((Vector2)evt.position);

            ClearDragState(cardEl);
            PointerCaptureHelper.ReleasePointer(cardEl, evt.pointerId);
            evt.StopPropagation();

            if (wasClick)
            {
                ToggleSelection(card, cardEl); // cơ chế 2: bấm để chọn / bỏ chọn
                return;
            }

            if (dropView == null) return; // thả chỗ trống → thẻ tự trượt về (transition)

            PlayCard(card, dropView);
        }

        // ------------------------------------------------------------------
        //  Cơ chế 2: chọn thẻ → chọn mục tiêu
        // ------------------------------------------------------------------

        void ToggleSelection(CardDef card, VisualElement cardEl)
        {
            if (selectedCard == card)
            {
                ClearSelection();
                return;
            }

            ClearSelection();
            selectedCard = card;
            selectedEl = cardEl;
            cardEl?.AddToClassList("card--selected");
            HighlightValidTargets();
        }

        void ClearSelection()
        {
            if (selectedEl != null) selectedEl.RemoveFromClassList("card--selected");
            selectedEl = null;
            selectedCard = null;

            foreach (var v in playerViews)
                v?.Root?.RemoveFromClassList("unit--target");
            foreach (var v in enemyViews)
                v?.Root?.RemoveFromClassList("unit--target");
        }

        /// <summary>Tô sáng các đơn vị hợp lệ làm mục tiêu của thẻ đang chọn.</summary>
        void HighlightValidTargets()
        {
            if (engine == null || selectedCard == null) return;
            foreach (var v in playerViews)
                if (v?.Root != null)
                    v.Root.EnableInClassList("unit--target", engine.IsValidTarget(selectedCard, v.Model));
            foreach (var v in enemyViews)
                if (v?.Root != null)
                    v.Root.EnableInClassList("unit--target", engine.IsValidTarget(selectedCard, v.Model));
        }

        /// <summary>Bấm lên đơn vị khi đang có thẻ được chọn → chơi thẻ đó.</summary>
        void OnUnitClick(UnitView view, ClickEvent evt)
        {
            if (selectedCard == null || engine == null) return;

            evt.StopPropagation(); // giữ selection khi bấm nhầm mục tiêu không hợp lệ

            if (engine.Phase != Phase.PlayerTurn)
            {
                ClearSelection();
                return;
            }

            if (PlayCard(selectedCard, view)) ClearSelection();
        }

        /// <summary>Thử chơi một lá lên một đơn vị. Trả về false nếu không hợp lệ.</summary>
        bool PlayCard(CardDef card, UnitView view)
        {
            if (engine == null || card == null || view == null) return false;
            if (engine.Phase != Phase.PlayerTurn) return false;
            if (!engine.IsValidTarget(card, view.Model)) return false;

            if (!engine.CanPlay(card))
            {
                FlashEnergy(); // không đủ năng lượng
                return false;
            }

            if (!engine.TryBeginPlay(card, view.Model, out var play)) return false;

            RefreshInteractivity();
            AnimateCardPlay(play);
            return true;
        }

        /// <summary>Tìm đơn vị đang trỏ chuột theo TargetKind của thẻ; bật/tắt vùng thả.</summary>
        UnitView UpdateHover(Vector2 point)
        {
            var found = HitTest(point, draggedCard);

            if (hoverView != null && hoverView.Root != null)
                hoverView.Root.RemoveFromClassList("unit--target");
            hoverZone?.RemoveFromClassList("zone--target");

            hoverView = found;
            hoverZone = null;

            if (found != null)
            {
                found.Root.AddToClassList("unit--target");
                hoverZone = found.Model.Team == Team.Player ? zonePlayer : zoneEnemy;
                hoverZone.AddToClassList("zone--target");
            }

            return found;
        }

        UnitView HitTest(Vector2 point, CardDef card)
        {
            if (card == null) return null;

            List<UnitView> candidates;
            switch (card.Target)
            {
                case TargetKind.EnemySingle:
                case TargetKind.AllEnemies:
                    candidates = enemyViews;
                    break;
                case TargetKind.AllySingle:
                case TargetKind.Self:
                    candidates = playerViews;
                    break;
                default:
                    return null;
            }

            foreach (var v in candidates)
            {
                if (v == null || !v.Model.IsAlive || v.Root == null) continue;
                if (card.Target == TargetKind.Self && v.Model.Role != card.Owner) continue;
                if (v.Root.worldBound.Contains(point)) return v;
            }
            return null;
        }

        void CancelDrag()
        {
            if (dragged == null) return;
            ClearDragState(dragged);
        }

        void ClearDragState(VisualElement cardEl)
        {
            dragging = false;
            dragged = null;
            draggedCard = null;
            dragPointerId = -1;

            if (cardEl != null)
            {
                cardEl.RemoveFromClassList("card--dragging");
                cardEl.style.translate = new StyleTranslate(StyleKeyword.Null);
            }

            if (hoverView != null && hoverView.Root != null)
                hoverView.Root.RemoveFromClassList("unit--target");
            hoverZone?.RemoveFromClassList("zone--target");
            hoverView = null;
            hoverZone = null;
        }

        // ------------------------------------------------------------------
        //  Diễn hoạt lá bài của người chơi
        // ------------------------------------------------------------------

        void AnimateCardPlay(CardPlay play)
        {
            var actorView = ViewOf(play.Actor);
            if (actorView == null)
            {
                play.Apply();
                RefreshUnits();
                return;
            }

            if (play.Card.Kind == CardKind.Attack)
            {
                if (play.IsAoe)
                {
                    // Mưa Tên: một loạt tên bay sang phía quái rồi lan toàn đội
                    ArrowShot(actorView, zoneEnemy.worldBound.center, false,
                        () => ApplyPlayerPlay(play, null));
                    return;
                }

                var targetView = ViewOf(play.Target);
                if (targetView == null)
                {
                    play.Apply();
                    RefreshUnits();
                    return;
                }

                if (play.Card.Owner == Role.Swordsman)
                {
                    // Kiếm sĩ lao tới chém
                    DashToSwordsman(actorView, targetView.Root.worldBound.center, false, 380,
                        () =>
                        {
                            actorView.Visual?.PlaySlash(app, () =>
                            {
                                ApplyPlayerPlay(play, targetView);
                                SlashAt(targetView);
                                DashBack(actorView, savedSwordsmanFrom, 420);
                            });
                        }, 700);
                }
                else
                {
                    // Cung thủ bắn từ xa
                    ArrowShot(actorView, targetView.Root.worldBound.center, false,
                        () => ApplyPlayerPlay(play, targetView));
                }
            }
            else if (play.IsTaunt)
            {
                // Tank thú hút đòn
                play.Apply();
                RefreshUnits();
                actorView.Root.AddToClassList("unit--buff");
                SpawnFloat(actorView, "+" + play.Value + " khiên · THÚ HÚT", true);
                actorView.Root.schedule.Execute(() => actorView.Root.RemoveFromClassList("unit--buff")).StartingIn(520);
            }
            else
            {
                // Thủ: che chắn cho một đồng đội
                var targetView = ViewOf(play.Target);
                Root.schedule.Execute(() =>
                {
                    ApplyPlayerPlay(play, targetView);
                    if (targetView != null && targetView.Root != null)
                    {
                        targetView.Root.AddToClassList("unit--buff");
                        targetView.Root.schedule.Execute(() => targetView.Root.RemoveFromClassList("unit--buff")).StartingIn(460);
                    }
                }).StartingIn(180);
            }
        }

        void ApplyPlayerPlay(CardPlay play, UnitView primary)
        {
            if (play.IsAoe)
            {
                var before = new int[enemyViews.Count];
                for (int i = 0; i < enemyViews.Count; i++)
                    before[i] = enemyViews[i].Model.Hp;

                play.Apply();
                RefreshUnits();

                for (int i = 0; i < enemyViews.Count; i++)
                {
                    int dealt = before[i] - enemyViews[i].Model.Hp;
                    if (dealt > 0)
                    {
                        FlashHit(enemyViews[i]);
                        SpawnFloat(enemyViews[i], "-" + dealt);
                    }
                }
                return;
            }

            if (primary == null)
            {
                play.Apply();
                RefreshUnits();
                return;
            }

            int hpBefore = primary.Model.Hp;
            play.Apply();
            RefreshUnits();

            if (play.Card.Kind == CardKind.Attack)
            {
                int dealt = hpBefore - primary.Model.Hp;
                if (dealt > 0)
                {
                    FlashHit(primary);
                    SpawnFloat(primary, "-" + dealt);
                }
                else
                {
                    SpawnFloat(primary, "Khiên!", true);
                }
            }
            else
            {
                SpawnFloat(primary, "+" + play.Value + " khiên", true);
            }
        }

        // ------------------------------------------------------------------
        //  Lượt đi & lượt quái
        // ------------------------------------------------------------------

        void OnEndTurn()
        {
            if (engine == null || engine.Phase != Phase.PlayerTurn) return;

            btnEndTurn.SetEnabled(false);
            engine.EndPlayerTurn(); // bỏ bài trên tay → RenderHand xoá sạch
            Root.schedule.Execute(RunEnemyTurn).StartingIn(650);
        }

        void RunEnemyTurn()
        {
            if (engine == null || engine.Phase != Phase.EnemyTurn) return;
            RunEnemyAction(0);
        }

        void RunEnemyAction(int index)
        {
            // trận kết thúc giữa chừng (đánh bại boss / cả đội gục)
            if (engine == null || engine.Phase != Phase.EnemyTurn)
            {
                RefreshInteractivity();
                return;
            }

            var plans = engine.Plans;
            if (index >= plans.Count)
            {
                animateNextHand = true;
                engine.FinishEnemyTurn(); // → BeginPlayerTurn → Draw → RenderHand
                animateNextHand = false;
                RefreshInteractivity();
                return;
            }

            var plan = plans[index];
            if (plan.Actor == null || !plan.Actor.IsAlive)
            {
                Root.schedule.Execute(() => RunEnemyAction(index + 1)).StartingIn(150);
                return;
            }

            var actorView = ViewOf(plan.Actor);
            Action next = () => Root.schedule.Execute(() => RunEnemyAction(index + 1)).StartingIn(200);

            // ---- quái dựng khiên ----
            if (plan.Block > 0)
            {
                engine.ApplyEnemyAction(plan, null);
                RefreshUnits();
                actorView.Root.AddToClassList("unit--guard");
                SpawnFloat(actorView, "+" + plan.Block + " khiên", true);
                actorView.Root.schedule.Execute(() => actorView.Root.RemoveFromClassList("unit--guard")).StartingIn(540);
                next();
                return;
            }

            // ---- boss đánh lan cả đội ----
            if (plan.IsAoe)
            {
                actorView.Root.AddToClassList("unit--roar");
                actorView.Root.schedule.Execute(() =>
                {
                    ApplyEnemyAction(plan, null);
                    actorView.Root.RemoveFromClassList("unit--roar");
                }).StartingIn(500);
                actorView.Root.schedule.Execute(next).StartingIn(1300);
                return;
            }

            var target = engine.ResolveTarget(plan);
            var targetView = ViewOf(target);
            if (targetView == null)
            {
                next();
                return;
            }

            if (plan.Actor.Role == Role.Guard || plan.Actor.Role == Role.Boss)
            {
                // quái cận chiến lao tới tướng
                DashTo(actorView, targetView.Root.worldBound.center, true, 400,
                    () => ApplyEnemyAction(plan, targetView), 780);
                Root.schedule.Execute(next).StartingIn(1350);
            }
            else
            {
                // quái bắn xa
                ArrowShot(actorView, targetView.Root.worldBound.center, true,
                    () => ApplyEnemyAction(plan, targetView));
                Root.schedule.Execute(next).StartingIn(1100);
            }
        }

        void ApplyEnemyAction(EnemyAction plan, UnitView targetView)
        {
            if (plan.IsAoe)
            {
                var before = new int[engine.Champions.Length];
                for (int i = 0; i < engine.Champions.Length; i++)
                    before[i] = engine.Champions[i].Hp;

                engine.ApplyEnemyAction(plan, null);
                RefreshUnits();

                for (int i = 0; i < engine.Champions.Length; i++)
                {
                    var view = ViewOf(engine.Champions[i]);
                    int dealt = before[i] - engine.Champions[i].Hp;
                    if (dealt > 0)
                    {
                        FlashHit(view);
                        SpawnFloat(view, "-" + dealt);
                    }
                    else if (engine.Champions[i].IsAlive)
                    {
                        SpawnFloat(view, "Khiên!", true);
                    }
                }
                return;
            }

            if (targetView == null) return;

            int hpBefore = targetView.Model.Hp;
            engine.ApplyEnemyAction(plan, targetView.Model);
            RefreshUnits();

            int dmg = hpBefore - targetView.Model.Hp;
            if (dmg > 0)
            {
                FlashHit(targetView);
                SpawnFloat(targetView, "-" + dmg);
            }
            else if (targetView.Model.IsAlive)
            {
                SpawnFloat(targetView, "Khiên!", true);
            }
        }

        void RefreshInteractivity()
        {
            if (engine == null) return;

            btnEndTurn.SetEnabled(engine.Phase == Phase.PlayerTurn);

            if (engine.Phase == Phase.Won || engine.Phase == Phase.Lost) ShowOverlay();
            else HideOverlay();
        }

        // ------------------------------------------------------------------
        //  Hiệu ứng (ghost lao tới, mũi tên, chém, số bay)
        // ------------------------------------------------------------------

        Vector2 LocalPoint(Vector2 world)
        {
            var fb = battlefield.worldBound;
            return new Vector2(world.x - fb.x, world.y - fb.y);
        }

        Vector2 savedSwordsmanFrom;

        void DashToSwordsman(UnitView actor, Vector2 toWorld, bool foe, int hitMs, Action onHit, int removeMs)
        {
            if (battlefield == null || actor == null)
            {
                onHit?.Invoke();
                return;
            }

            savedSwordsmanFrom = actor.Root.worldBound.center;
            actor.Visual?.PlayRun(app);

            var from = savedSwordsmanFrom;
            var delta = toWorld - from;

            // dịch chuyển chính sprite element
            actor.Root.style.translate = new Translate(
                new Length(delta.x, LengthUnit.Pixel),
                new Length(delta.y, LengthUnit.Pixel));
            actor.Root.style.transitionDuration = new StyleList<TimeValue>(new System.Collections.Generic.List<TimeValue>{new TimeValue(0.3f)});

            actor.Root.schedule.Execute(() => onHit?.Invoke()).StartingIn(hitMs);
        }

        void DashTo(UnitView actor, Vector2 toWorld, bool foe, int hitMs, Action onHit, int removeMs)
        {
            if (battlefield == null || actor == null)
            {
                onHit?.Invoke();
                return;
            }

            var ghost = new VisualElement();
            ghost.AddToClassList("dash-ghost");
            if (foe) ghost.AddToClassList("dash-ghost--foe");
            var label = new Label(actor.Model.Glyph);
            label.AddToClassList("dash-ghost-label");
            label.AddToClassList("text-semibold");
            ghost.Add(label);

            var from = actor.Root.worldBound.center;
            var start = LocalPoint(from);
            ghost.style.left = new StyleLength(new Length(start.x - 48, LengthUnit.Pixel));
            ghost.style.top = new StyleLength(new Length(start.y - 48, LengthUnit.Pixel));
            battlefield.Add(ghost);
            app.ApplySemiBold(ghost);

            var delta = toWorld - from;
            ghost.schedule.Execute(() => ghost.style.translate = new Translate(
                new Length(delta.x, LengthUnit.Pixel),
                new Length(delta.y, LengthUnit.Pixel))).StartingIn(30);

            ghost.schedule.Execute(() => onHit?.Invoke()).StartingIn(hitMs);
            ghost.schedule.Execute(() => ghost.RemoveFromHierarchy()).StartingIn(removeMs);
        }

        /// <summary>Mũi tên bay đường thẳng từ đơn vị tới điểm đích.</summary>
        void ArrowShot(UnitView actor, Vector2 toWorld, bool foe, Action onHit)
        {
            if (battlefield == null || actor == null)
            {
                onHit?.Invoke();
                return;
            }

            var arrow = new VisualElement();
            arrow.AddToClassList("projectile");
            arrow.AddToClassList(foe ? "projectile--foe" : "projectile--ally");

            var from = actor.Art != null ? actor.Art.worldBound.center : actor.Root.worldBound.center;
            var start = LocalPoint(from);
            arrow.style.left = new StyleLength(new Length(start.x - 8, LengthUnit.Pixel));
            arrow.style.top = new StyleLength(new Length(start.y - 8, LengthUnit.Pixel));
            battlefield.Add(arrow);

            var delta = toWorld - from;
            arrow.schedule.Execute(() => arrow.style.translate = new Translate(
                new Length(delta.x, LengthUnit.Pixel),
                new Length(delta.y, LengthUnit.Pixel))).StartingIn(30);

            arrow.schedule.Execute(() => onHit?.Invoke()).StartingIn(370);
            arrow.schedule.Execute(() => arrow.RemoveFromHierarchy()).StartingIn(640);
        }

        /// <summary>Vệt trắng chém ngang lên đơn vị.</summary>
        void SlashAt(UnitView view)
        {
            if (battlefield == null || view == null) return;

            var slash = new VisualElement();
            slash.AddToClassList("fx-slash");
            var c = LocalPoint(view.Root.worldBound.center);
            slash.style.left = new StyleLength(new Length(c.x - 42, LengthUnit.Pixel));
            slash.style.top = new StyleLength(new Length(c.y - 5, LengthUnit.Pixel));
            battlefield.Add(slash);

            slash.schedule.Execute(() => slash.AddToClassList("fx-slash--show")).StartingIn(20);
            slash.schedule.Execute(() => slash.RemoveFromHierarchy()).StartingIn(420);
        }

        /// <summary>Số sát thương / khiên bay lên trên đầu đơn vị.</summary>
        void SpawnFloat(UnitView view, string text, bool heal = false)
        {
            if (battlefield == null || view == null) return;

            var lbl = new Label(text);
            lbl.AddToClassList("fx-float");
            if (heal) lbl.AddToClassList("fx-float--heal");

            var c = LocalPoint(view.Root.worldBound.center);
            lbl.style.left = new StyleLength(new Length(c.x - 44, LengthUnit.Pixel));
            lbl.style.top = new StyleLength(new Length(c.y - 24, LengthUnit.Pixel));
            battlefield.Add(lbl);

            lbl.schedule.Execute(() => lbl.AddToClassList("fx-float--up")).StartingIn(40);
            lbl.schedule.Execute(() => lbl.RemoveFromHierarchy()).StartingIn(1150);
        }

        void FlashHit(UnitView view)
        {
            if (view == null || view.Root == null) return;
            view.Root.AddToClassList("unit--hit");
            view.Root.schedule.Execute(() => view.Root.RemoveFromClassList("unit--hit")).StartingIn(300);
        }

        // ------------------------------------------------------------------
        //  Overlay & nhật ký
        // ------------------------------------------------------------------

        void OnBattleEnded(bool won)
        {
            ShowOverlay();
        }

        void ShowOverlay()
        {
            overlay.pickingMode = PickingMode.Position;
            overlay.style.display = DisplayStyle.Flex;
            overlay.schedule.Execute(() => overlay.RemoveFromClassList("result--hidden"));
        }

        void HideOverlay()
        {
            overlay.AddToClassList("result--hidden");
            overlay.pickingMode = PickingMode.Ignore;
            overlay.schedule.Execute(() => overlay.style.display = DisplayStyle.None).StartingIn(380);
        }

        void FlashEnergy()
        {
            if (lblEnergy == null) return;
            lblEnergy.AddToClassList("energy--flash");
            lblEnergy.schedule.Execute(() => lblEnergy.RemoveFromClassList("energy--flash")).StartingIn(320);
        }

        void AddLog(string message)
        {
            if (battleLog == null) return;
            var line = new Label(message);
            line.AddToClassList("log-line");
            battleLog.Add(line);
            while (battleLog.childCount > 9) battleLog.RemoveAt(0);
        }

        void DashBack(UnitView actor, Vector2 toWorld, int durationMs)
        {
            if (battlefield == null || actor == null) return;
            actor.Visual?.PlayRun(app);

            var from = actor.Root.worldBound.center;
            var delta = toWorld - from;

            actor.Root.style.translate = new Translate(
                new Length(delta.x, LengthUnit.Pixel),
                new Length(delta.y, LengthUnit.Pixel));
            actor.Root.style.transitionDuration = new StyleList<TimeValue>(new System.Collections.Generic.List<TimeValue>{new TimeValue(durationMs/1000f)});

            actor.Root.schedule.Execute(() =>
            {
                actor.Visual?.PlayIdle(app);
                actor.Root.style.translate = new StyleTranslate(StyleKeyword.Null);
                actor.Root.style.transitionDuration = StyleKeyword.Null;
            }).StartingIn(durationMs);
        }

        // ------------------------------------------------------------------

        static T Req<T>(VisualElement root, string name) where T : VisualElement
        {
            var el = root.Q<T>(name);
            if (el == null) Debug.LogError($"[CardGame] Battle.uxml thiếu phần tử '{name}'");
            return el;
        }
    }
}
