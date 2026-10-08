using Unity.Properties;

namespace CardGame.Core
{
    /// <summary>Các chỉ số chung trên HUD (lượt, tủ bài, năng lượng, kết quả).</summary>
    public sealed class HudState : BindableBase
    {
        string turnText = "Lượt 1";
        string drawText = "Bài rút: 5";
        string discardText = "Bỏ: 0";
        string resultTitle = "";
        string resultSub = "";
        int energy;
        int maxEnergy = 3;

        [CreateProperty]
        public string TurnText
        {
            get => turnText;
            set { if (turnText == value) return; turnText = value; Raise(nameof(TurnText)); }
        }

        [CreateProperty]
        public string DrawText
        {
            get => drawText;
            set { if (drawText == value) return; drawText = value; Raise(nameof(DrawText)); }
        }

        [CreateProperty]
        public string DiscardText
        {
            get => discardText;
            set { if (discardText == value) return; discardText = value; Raise(nameof(DiscardText)); }
        }

        [CreateProperty]
        public string ResultTitle
        {
            get => resultTitle;
            set { if (resultTitle == value) return; resultTitle = value; Raise(nameof(ResultTitle)); }
        }

        [CreateProperty]
        public string ResultSub
        {
            get => resultSub;
            set { if (resultSub == value) return; resultSub = value; Raise(nameof(ResultSub)); }
        }

        [CreateProperty]
        public int Energy
        {
            get => energy;
            set
            {
                if (energy == value) return;
                energy = value;
                Raise(nameof(Energy));
                Raise(nameof(EnergyText));
            }
        }

        [CreateProperty]
        public int MaxEnergy
        {
            get => maxEnergy;
            set
            {
                if (maxEnergy == value) return;
                maxEnergy = value;
                Raise(nameof(MaxEnergy));
                Raise(nameof(EnergyText));
            }
        }

        [CreateProperty]
        public string EnergyText => "Năng lượng: " + energy + "/" + maxEnergy;
    }
}
