using Unity.Properties;
using UnityEngine.UIElements;

namespace CardGame.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    /// <summary>Vai trò của một đơn vị — quyết định màu hình và cách ra đòn.</summary>
    public enum Role
    {
        Swordsman, // Kiếm sĩ: lao tới chém
        Archer,    // Cung thủ: bắn từ xa
        Tank,      // Tank: thu hút đòn đánh
        Boss,      // Thạch Quỷ: có skill đánh lan
        Guard,     // Đá Vệ: đệ tank của phe quái
        Sniper     // Đá Tiễn: đệ bắn xa của phe quái
    }

    /// <summary>
    /// Một đơn vị trong trận (tướng của người chơi hoặc quái).
    /// Bind thẳng vào HUD của Unit.uxml qua data binding.
    /// </summary>
    public sealed class UnitState : BindableBase
    {
        int hp;
        int block;
        bool taunting;
        string intentText = "";

        public string Id { get; }
        public string Name { get; }
        public string Glyph { get; }
        public Role Role { get; }
        public Team Team { get; }
        public int MaxHp { get; }

        public bool IsAlive => hp > 0;

        public UnitState(string id, string name, string glyph, Role role, Team team, int maxHp)
        {
            Id = id;
            Name = name;
            Glyph = glyph;
            Role = role;
            Team = team;
            MaxHp = maxHp;
            hp = maxHp;
        }

        [CreateProperty]
        public int Hp
        {
            get => hp;
            set
            {
                if (hp == value) return;
                hp = value;
                Raise(nameof(Hp));
                Raise(nameof(HpText));
                Raise(nameof(HpWidth));
            }
        }

        [CreateProperty]
        public int Block
        {
            get => block;
            set
            {
                if (block == value) return;
                block = value;
                Raise(nameof(Block));
                Raise(nameof(BlockText));
            }
        }

        /// <summary>Đang khiêu khích (taunt) — chỉ Tank mới bật.</summary>
        [CreateProperty]
        public bool Taunting
        {
            get => taunting;
            set
            {
                if (taunting == value) return;
                taunting = value;
                Raise(nameof(Taunting));
                Raise(nameof(TauntText));
            }
        }

        [CreateProperty]
        public string IntentText
        {
            get => intentText;
            set
            {
                if (intentText == value) return;
                intentText = value;
                Raise(nameof(IntentText));
            }
        }

        [CreateProperty]
        public string HpText => hp <= 0 ? "HẠ GỤC" : hp + "/" + MaxHp;

        /// <summary>Thanh máu — bind vào style.width nên width chuyển mượt qua USS transition.</summary>
        [CreateProperty]
        public StyleLength HpWidth =>
            new StyleLength(new Length(MaxHp <= 0 ? 0f : 100f * hp / MaxHp, LengthUnit.Percent));

        [CreateProperty]
        public string BlockText => block > 0 ? "Khiên: " + block : "";

        [CreateProperty]
        public string TauntText => taunting ? "ĐANG THÚ HÚT ĐÒN" : "";
    }
}
