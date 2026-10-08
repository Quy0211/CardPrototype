using UnityEngine.UIElements;

namespace CardGame.UI
{
    /// <summary>Màn hình Lobby: xem 3 tướng cố định + phe địch, rồi bấm "VÀO TRẬN".</summary>
    public sealed class LobbyScreen
    {
        public VisualElement Root { get; }

        public LobbyScreen(App app, VisualElement root)
        {
            Root = root;

            var intoBattle = root.Q<Button>("btn-into-battle");
            var back = root.Q<Button>("btn-lobby-back");

            if (intoBattle != null) intoBattle.clicked += app.ShowBattle;
            if (back != null) back.clicked += app.ShowMenu;
        }
    }
}
