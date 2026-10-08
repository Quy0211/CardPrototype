using System;
using System.Collections.Generic;

namespace CardGame.Core
{
    public enum Phase
    {
        PlayerTurn,
        EnemyTurn,
        Won,
        Lost
    }

    /// <summary>Một hành động của phe quái trong lượt địch (UI sẽ diễn hoạt rồi gọi Apply).</summary>
    public sealed class EnemyAction
    {
        public UnitState Actor;
        public UnitState Target; // null nếu đánh lan (AoE)
        public int Damage;
        public int Block;
        public bool IsAoe;
        public string Label = "";
    }

    /// <summary>Kết quả chơi một lá bài — UI diễn hoạt animation rồi mới Apply(1).</summary>
    public sealed class CardPlay
    {
        public CardDef Card;
        public UnitState Actor;
        public UnitState Target;
        public int Value;
        public bool IsAoe;
        public bool IsTaunt;
        public Action Apply;
    }

    /// <summary>
    /// Trận đấu theo đội: 3 tướng cố định (Kiếm sĩ, Cung thủ, Tank)
    /// vs Thạch Quỷ + 2 thuộc hạ (1 tank, 1 bắn xa).
    /// Thuần C#, không phụ thuộc UI. UI gọi TryBeginPlay → diễn hoạt → ApplyCardPlay.
    /// </summary>
    public sealed class TeamBattleEngine
    {
        const int HandSize = 5;

        readonly List<CardDef> fullDeck;
        readonly List<CardDef> drawPile = new List<CardDef>();
        readonly List<CardDef> hand = new List<CardDef>();
        readonly List<CardDef> discardPile = new List<CardDef>();
        readonly List<EnemyAction> plans = new List<EnemyAction>();
        readonly List<UnitState> allUnits = new List<UnitState>();
        readonly Random rng = new Random();
        readonly int[] enemyCycle;

        int turn;

        public UnitState[] Champions { get; }
        public UnitState[] Enemies { get; }
        public HudState Hud { get; }
        public Phase Phase { get; private set; }
        public IReadOnlyList<CardDef> Hand => hand;
        public IReadOnlyList<EnemyAction> Plans => plans;
        public int DrawCount => drawPile.Count;
        public int DiscardCount => discardPile.Count;

        public event Action HandChanged;
        public event Action<string> LogAdded;
        public event Action<bool> BattleEnded;

        public TeamBattleEngine()
        {
            Hud = new HudState { MaxEnergy = 3 };

            Champions = new[]
            {
                NewUnit("swordsman", "Kiếm Sĩ", "K", Role.Swordsman, Team.Player, 52),
                NewUnit("archer", "Cung Thủ", "C", Role.Archer, Team.Player, 40),
                NewUnit("tank", "Thiết Vệ", "T", Role.Tank, Team.Player, 74),
            };

            Enemies = new[]
            {
                NewUnit("minion-tank", "Đá Vệ", "V", Role.Guard, Team.Enemy, 32),
                NewUnit("minion-ranged", "Đá Tiễn", "X", Role.Sniper, Team.Enemy, 24),
                NewUnit("boss", "Thạch Quỷ", "Q", Role.Boss, Team.Enemy, 64),
            };

            enemyCycle = new int[Enemies.Length];
            allUnits.AddRange(Champions);
            allUnits.AddRange(Enemies);
            fullDeck = CardLibrary.CreateStarterDeck();
        }

        static UnitState NewUnit(string id, string name, string glyph, Role role, Team team, int maxHp)
        {
            return new UnitState(id, name, glyph, role, team, maxHp);
        }

        public UnitState ChampionFor(Role owner)
        {
            for (int i = 0; i < Champions.Length; i++)
                if (Champions[i].Role == owner)
                    return Champions[i];
            return null;
        }

        /// <summary>Bắt đầu (hoặc chơi lại) một trận mới.</summary>
        public void StartRun()
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            plans.Clear();

            foreach (var u in allUnits)
            {
                u.Hp = u.MaxHp;
                u.Block = 0;
                u.Taunting = false;
                u.IntentText = "";
            }

            for (int i = 0; i < enemyCycle.Length; i++) enemyCycle[i] = 0;

            drawPile.AddRange(fullDeck);
            Shuffle(drawPile);

            turn = 0;
            Hud.TurnText = "Lượt 1";
            Hud.ResultTitle = "";
            Hud.ResultSub = "";
            Hud.Energy = Hud.MaxEnergy;

            LogAdded?.Invoke("Trận đấu bắt đầu! 3 tướng vs Thạch Quỷ và 2 thuộc hạ.");
            BeginPlayerTurn();
        }

        void BeginPlayerTurn()
        {
            turn++;
            Hud.TurnText = "Lượt " + turn;
            Hud.Energy = Hud.MaxEnergy;

            // Khiên của tướng hết hạn đầu lượt mình; taunt cũng tan sau lượt địch.
            foreach (var c in Champions)
            {
                if (!c.IsAlive) continue;
                c.Block = 0;
                c.Taunting = false;
            }

            Draw(HandSize);
            BuildPlans();
            Phase = Phase.PlayerTurn;

            LogAdded?.Invoke("— Lượt " + turn + " của bạn —");
            HandChanged?.Invoke();
        }

        void Draw(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0) break;
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    Shuffle(drawPile);
                    LogAdded?.Invoke("Xáo lại bài bỏ.");
                }
                hand.Add(drawPile[drawPile.Count - 1]);
                drawPile.RemoveAt(drawPile.Count - 1);
            }
            UpdateHudCounters();
        }

        void UpdateHudCounters()
        {
            Hud.DrawText = "Bài rút: " + drawPile.Count;
            Hud.DiscardText = "Bỏ: " + discardPile.Count;
        }

        // ---------------------------------------------------------------- playing

        public bool CanPlay(CardDef card)
        {
            return Phase == Phase.PlayerTurn
                   && card != null
                   && hand.Contains(card)
                   && card.Cost <= Hud.Energy
                   && ChampionFor(card.Owner) is { IsAlive: true };
        }

        public bool IsValidTarget(CardDef card, UnitState target)
        {
            if (target == null || !target.IsAlive) return false;
            switch (card.Target)
            {
                case TargetKind.EnemySingle:
                case TargetKind.AllEnemies:
                    return target.Team == Team.Enemy;
                case TargetKind.AllySingle:
                    return target.Team == Team.Player;
                case TargetKind.Self:
                    return target == ChampionFor(card.Owner);
                default:
                    return false;
            }
        }

        /// <summary>Trừ năng lượng, rút thẻ khỏi tay. UI diễn hoạt rồi gọi play.Apply().</summary>
        public bool TryBeginPlay(CardDef card, UnitState target, out CardPlay play)
        {
            play = null;
            if (!CanPlay(card) || !IsValidTarget(card, target)) return false;

            var actor = ChampionFor(card.Owner);
            Hud.Energy -= card.Cost;
            hand.Remove(card);
            discardPile.Add(card);
            UpdateHudCounters();
            HandChanged?.Invoke();

            var created = new CardPlay
            {
                Card = card,
                Actor = actor,
                Target = target,
                Value = card.Value,
                IsAoe = card.Target == TargetKind.AllEnemies,
                IsTaunt = card.Target == TargetKind.Self && card.Id == "taunt",
            };
            created.Apply = () => ApplyCardPlay(created);
            play = created;
            return true;
        }

        void ApplyCardPlay(CardPlay p)
        {
            if (p.Card.Kind == CardKind.Attack)
            {
                if (p.IsAoe)
                {
                    foreach (var e in Enemies)
                    {
                        if (!e.IsAlive) continue;
                        DealDamage(e, p.Value);
                    }
                    LogAdded?.Invoke(p.Actor.Name + " bắn Mưa Tên: " + p.Value + " sát thương lên toàn bộ phe quái!");
                }
                else if (p.Target != null && p.Target.IsAlive)
                {
                    int rest = DealDamage(p.Target, p.Value);
                    LogAdded?.Invoke(p.Actor.Name + " dùng " + p.Card.Name + ": " + p.Value +
                                     " sát thương lên " + p.Target.Name +
                                     (rest < p.Value ? " (trừ khiên " + (p.Value - rest) + ")" : "") +
                                     ". " + p.Target.Name + " còn " + p.Target.Hp + "/" + p.Target.MaxHp + ".");
                }
            }
            else if (p.IsTaunt)
            {
                p.Actor.Taunting = true;
                p.Actor.Block += p.Value;
                LogAdded?.Invoke(p.Actor.Name + " THÚ HÚT ĐÒN (+ " + p.Value + " khiên): quái buộc phải đánh " + p.Actor.Name + "!");
            }
            else if (p.Target != null && p.Target.IsAlive)
            {
                p.Target.Block += p.Value;
                LogAdded?.Invoke(p.Card.Name + ": " + p.Target.Name + " nhận " + p.Value + " khiên.");
            }

            CheckEnd();
        }

        int DealDamage(UnitState target, int amount)
        {
            int absorbed = Math.Min(target.Block, amount);
            target.Block -= absorbed;
            int rest = amount - absorbed;
            target.Hp = Math.Max(0, target.Hp - rest);
            return rest;
        }

        // ---------------------------------------------------------------- enemy turn

        /// <summary>Dự đoán hành động phe quái cho lượt sắp tới (hiện ý định trên HUD).</summary>
        void BuildPlans()
        {
            plans.Clear();

            for (int i = 0; i < Enemies.Length; i++)
            {
                var e = Enemies[i];
                if (!e.IsAlive)
                {
                    e.IntentText = "";
                    continue;
                }

                var plan = NextPlanFor(e, i);
                plans.Add(plan);
                e.IntentText = plan.Label;
            }
        }

        EnemyAction NextPlanFor(UnitState e, int index)
        {
            var plan = new EnemyAction { Actor = e };

            switch (e.Role)
            {
                case Role.Boss:
                    switch (enemyCycle[index] % 4)
                    {
                        case 0:
                            plan.Damage = 10;
                            plan.Label = "Ý định: Đánh 10";
                            break;
                        case 1:
                            plan.Damage = 8;
                            plan.IsAoe = true;
                            plan.Label = "Ý định: ĐÁNH LAN 8 (cả đội)";
                            break;
                        case 2:
                            plan.Block = 10;
                            plan.Label = "Ý định: Phòng thủ 10";
                            break;
                        default:
                            plan.Damage = 16;
                            plan.Label = "Ý định: Quật 16";
                            break;
                    }
                    break;

                case Role.Guard:
                    if (enemyCycle[index] % 3 == 2)
                    {
                        plan.Block = 8;
                        plan.Label = "Ý định: Phòng thủ 8";
                    }
                    else
                    {
                        plan.Damage = 7;
                        plan.Label = "Ý định: Đánh 7";
                    }
                    break;

                default: // Sniper
                    plan.Damage = enemyCycle[index] % 2 == 0 ? 6 : 9;
                    plan.Label = "Ý định: Bắn " + plan.Damage;
                    break;
            }

            enemyCycle[index]++;
            return plan;
        }

        public void EndPlayerTurn()
        {
            if (Phase != Phase.PlayerTurn) return;

            foreach (var c in hand) discardPile.Add(c);
            hand.Clear();
            UpdateHudCounters();
            HandChanged?.Invoke();

            // Khiên quái được hồi ở đầu lượt địch của chúng.
            foreach (var e in Enemies)
                if (e.IsAlive)
                    e.Block = 0;

            Phase = Phase.EnemyTurn;
            LogAdded?.Invoke("— Lượt của phe quái —");
        }

        /// <summary>Mục tiêu thật khi thi hành: taunt ưu tiên, nếu mục tiêu đã chết thì chọn tướng khác.</summary>
        public UnitState ResolveTarget(EnemyAction plan)
        {
            if (plan.IsAoe) return null;

            foreach (var c in Champions)
                if (c.IsAlive && c.Taunting)
                    return c;

            if (plan.Target != null && plan.Target.IsAlive) return plan.Target;

            var alive = new List<UnitState>();
            foreach (var c in Champions)
                if (c.IsAlive)
                    alive.Add(c);
            if (alive.Count == 0) return null;
            return alive[rng.Next(alive.Count)];
        }

        public void ApplyEnemyAction(EnemyAction plan, UnitState target)
        {
            if (Phase != Phase.EnemyTurn || plan.Actor == null || !plan.Actor.IsAlive) return;

            if (plan.Block > 0)
            {
                plan.Actor.Block += plan.Block;
                LogAdded?.Invoke(plan.Actor.Name + " Phòng thủ: nhận " + plan.Block + " khiên.");
            }

            if (plan.Damage > 0)
            {
                if (plan.IsAoe)
                {
                    foreach (var c in Champions)
                    {
                        if (!c.IsAlive) continue;
                        int rest = DealDamage(c, plan.Damage);
                        LogAdded?.Invoke(plan.Actor.Name + " ĐÁNH LAN " + plan.Damage + " lên " + c.Name +
                                         (rest < plan.Damage ? " (trừ khiên " + (plan.Damage - rest) + ")" : "") +
                                         ". " + c.Name + " còn " + c.Hp + "/" + c.MaxHp + ".");
                    }
                }
                else if (target != null && target.IsAlive)
                {
                    int rest = DealDamage(target, plan.Damage);
                    string tauntNote = target.Taunting ? " [THÚ HÚT]" : "";
                    LogAdded?.Invoke(plan.Actor.Name + " đánh " + target.Name + " " + plan.Damage + " sát thương" +
                                     tauntNote + (rest < plan.Damage ? " (trừ khiên " + (plan.Damage - rest) + ")" : "") +
                                     ". " + target.Name + " còn " + target.Hp + "/" + target.MaxHp + ".");
                }
            }

            CheckEnd();
        }

        public void FinishEnemyTurn()
        {
            if (Phase != Phase.EnemyTurn) return;
            if (CheckEnd()) return;
            BeginPlayerTurn();
        }

        bool CheckEnd()
        {
            if (Phase == Phase.Won || Phase == Phase.Lost) return true;

            bool enemiesDead = true;
            foreach (var e in Enemies)
                if (e.IsAlive)
                {
                    enemiesDead = false;
                    break;
                }

            if (enemiesDead)
            {
                Phase = Phase.Won;
                Hud.ResultTitle = "CHIẾN THẮNG!";
                Hud.ResultSub = "Thạch Quỷ và 2 thuộc hạ đã bị tiêu diệt.";
                LogAdded?.Invoke("Toàn bộ phe quái đã bị hạ gục!");
                BattleEnded?.Invoke(true);
                return true;
            }

            bool champsDead = true;
            foreach (var c in Champions)
                if (c.IsAlive)
                {
                    champsDead = false;
                    break;
                }

            if (champsDead)
            {
                Phase = Phase.Lost;
                Hud.ResultTitle = "THẤT BẠI";
                Hud.ResultSub = "Cả đội đã gục ngã trước Thạch Quỷ.";
                LogAdded?.Invoke("Cả đội đã gục ngã...");
                BattleEnded?.Invoke(false);
                return true;
            }

            return false;
        }

        void Shuffle(List<CardDef> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
