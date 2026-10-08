using System.Collections.Generic;

namespace CardGame.Core
{
    public enum CardKind
    {
        Attack,
        Skill
    }

    /// <summary>Thẻ cần thả vào đâu để hợp lệ.</summary>
    public enum TargetKind
    {
        EnemySingle, // thả lên một quái còn sống
        AllEnemies,  // thả lên một quái (hiệu ứng lan toàn đội quái)
        AllySingle,  // thả lên một tướng còn sống (kể cả bản thân)
        Self         // thả lên đúng tướng sở hữu thẻ
    }

    /// <summary>
    /// Định nghĩa một lá bài (dữ liệu thuần, không phụ thuộc Unity).
    /// Mỗi thẻ gắn với một tướng (Owner) — tướng đó sẽ ra đòn khi thẻ được chơi.
    /// </summary>
    public sealed class CardDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Desc { get; }
        public string Glyph { get; }
        public int Cost { get; }
        public CardKind Kind { get; }
        public int Value { get; }

        /// <summary>Tướng thực hiện thẻ này.</summary>
        public Role Owner { get; }

        public TargetKind Target { get; }

        public CardDef(string id, string name, string desc, string glyph, int cost,
                       CardKind kind, int value, Role owner, TargetKind target)
        {
            Id = id;
            Name = name;
            Desc = desc;
            Glyph = glyph;
            Cost = cost;
            Kind = kind;
            Value = value;
            Owner = owner;
            Target = target;
        }
    }

    public static class CardLibrary
    {
        /// <summary>
        /// Bộ bài khởi đầu 13 lá, chia theo tướng:
        /// 4x Chém + 1x Nện (Kiếm sĩ) · 3x Bắn + 1x Mưa Tên (Cung thủ) ·
        /// 3x Thủ + 1x Thu Hút (Tank).
        /// </summary>
        public static List<CardDef> CreateStarterDeck()
        {
            var deck = new List<CardDef>();

            // ---- Kiếm sĩ: lao tới chém ----
            var slash = new CardDef("slash", "Chém",
                "Kiếm sĩ lao tới chém mục tiêu: 6 sát thương.",
                "K", 1, CardKind.Attack, 6, Role.Swordsman, TargetKind.EnemySingle);
            for (int i = 0; i < 4; i++) deck.Add(slash);

            deck.Add(new CardDef("smash", "Nện",
                "Kiếm sĩ vung chùy: 12 sát thương lên một mục tiêu.",
                "N", 2, CardKind.Attack, 12, Role.Swordsman, TargetKind.EnemySingle));

            // ---- Cung thủ: bắn từ xa ----
            var shoot = new CardDef("shoot", "Bắn",
                "Cung thủ bắn tên: 5 sát thương từ xa.",
                "C", 1, CardKind.Attack, 5, Role.Archer, TargetKind.EnemySingle);
            for (int i = 0; i < 3; i++) deck.Add(shoot);

            deck.Add(new CardDef("volley", "Mưa Tên",
                "Cung thủ bắn rải: 4 sát thương lên TOÀN BỘ phe quái.",
                "M", 2, CardKind.Attack, 4, Role.Archer, TargetKind.AllEnemies));

            // ---- Tank: khiên và khiêu khích ----
            var guard = new CardDef("guard", "Thủ",
                "Tank che chắn cho một đồng đội: +6 khiên.",
                "T", 1, CardKind.Skill, 6, Role.Tank, TargetKind.AllySingle);
            for (int i = 0; i < 3; i++) deck.Add(guard);

            deck.Add(new CardDef("taunt", "Thu Hút",
                "Tank khiêu khích (+3 khiên): quái buộc phải đánh Tank ở lượt sau.",
                "H", 1, CardKind.Skill, 3, Role.Tank, TargetKind.Self));

            return deck;
        }
    }
}
