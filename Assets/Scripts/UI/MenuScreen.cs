using UnityEngine;
using UnityEngine.UIElements;

namespace CardGame.UI
{
    /// <summary>Màn hình menu chính.</summary>
    public sealed class MenuScreen
    {
        public VisualElement Root { get; }

        public MenuScreen(App app, VisualElement root)
        {
            Root = root;

            var start = root.Q<Button>("btn-start");
            var quit = root.Q<Button>("btn-quit");

            if (start != null) start.clicked += app.ShowLobby;
            if (quit != null) quit.clicked += Application.Quit;
        }
    }
}