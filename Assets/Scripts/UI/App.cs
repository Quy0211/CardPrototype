using UnityEngine;
using UnityEngine.UIElements;

namespace CardGame.UI
{
    /// <summary>
    /// Điều phối màn hình (menu ↔ lobby ↔ battle) trong cùng một UIDocument.
    /// Việc chuyển màn dùng USS transition (fade) thay vì bật/tắt tức thì.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        UIDocument doc;
        VisualElement menuHost;
        VisualElement lobbyHost;
        VisualElement battleHost;
        MenuScreen menu;
        LobbyScreen lobby;
        BattleScreen battle;
        Font regularFont;
        Font semiFont;

        void Start()
        {
            doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[CardGame] UIDocument.rootVisualElement rỗng");
                return;
            }

            regularFont = Resources.Load<Font>("Fonts/Inter-Regular");
            semiFont = Resources.Load<Font>("Fonts/Inter-SemiBold");
            if (regularFont != null)
            {
                // -unity-font-definition là thuộc tính kế thừa → cả cây dùng font này.
                root.style.unityFontDefinition = FontDefinition.FromFont(regularFont);
            }
            else
            {
                Debug.LogWarning("[CardGame] Không tìm thấy Inter-Regular.ttf trong Resources/Fonts");
            }

            menuHost = root.Q("screen-menu");
            lobbyHost = root.Q("screen-lobby");
            battleHost = root.Q("screen-battle");
            if (menuHost == null || lobbyHost == null || battleHost == null)
            {
                Debug.LogError("[CardGame] App.uxml thiếu screen-menu / screen-lobby / screen-battle");
                return;
            }

            var menuAsset = Resources.Load<VisualTreeAsset>("UI/Menu");
            var lobbyAsset = Resources.Load<VisualTreeAsset>("UI/Lobby");
            var battleAsset = Resources.Load<VisualTreeAsset>("UI/Battle");
            var cardAsset = Resources.Load<VisualTreeAsset>("UI/Card");
            var unitAsset = Resources.Load<VisualTreeAsset>("UI/Unit");
            if (menuAsset == null || lobbyAsset == null || battleAsset == null || cardAsset == null || unitAsset == null)
            {
                Debug.LogError("[CardGame] Thiếu Menu/Lobby/Battle/Card/Unit.uxml trong Resources/UI");
                return;
            }

            var menuTree = menuAsset.Instantiate();
            ApplySemiBold(menuTree);
            menu = new MenuScreen(this, menuTree);
            menuTree.style.flexGrow = 1f; // TemplateContainer phải giãn kín màn hình cha
            menuHost.Add(menuTree);

            var lobbyTree = lobbyAsset.Instantiate();
            ApplySemiBold(lobbyTree);
            lobby = new LobbyScreen(this, lobbyTree);
            lobbyTree.style.flexGrow = 1f;
            lobbyHost.Add(lobbyTree);

            var battleTree = battleAsset.Instantiate();
            ApplySemiBold(battleTree);
            battle = new BattleScreen(this, battleTree, cardAsset, unitAsset);
            battleTree.style.flexGrow = 1f;
            battleHost.Add(battleTree);

            lobbyHost.style.display = DisplayStyle.None;
            lobbyHost.pickingMode = PickingMode.Ignore;
            battleHost.style.display = DisplayStyle.None;
            battleHost.pickingMode = PickingMode.Ignore;

            ShowMenu();
        }

        /// <summary>Gán font SemiBold cho mọi phần tử có class "text-semibold" (cả cây con kế thừa).</summary>
        public void ApplySemiBold(VisualElement tree)
        {
            if (semiFont == null || tree == null) return;
            var def = FontDefinition.FromFont(semiFont);
            tree.Query(className: "text-semibold").ForEach(el => el.style.unityFontDefinition = def);
        }

        public void ShowLobby()
        {
            Switch(lobbyHost, menuHost);
        }

        public void ShowBattle()
        {
            battle.StartNewGame();
            Switch(battleHost, lobbyHost);
        }

        public void ShowMenu()
        {
            // từ cả menu lẫn lobby
            if (lobbyHost.style.display == DisplayStyle.Flex)
                Switch(menuHost, lobbyHost);
            else
                Switch(menuHost, battleHost);
        }

        void Switch(VisualElement show, VisualElement hide)
        {
            hide.AddToClassList("screen--hidden");
            hide.pickingMode = PickingMode.Ignore;
            hide.schedule.Execute(() => hide.style.display = DisplayStyle.None).StartingIn(420);

            show.style.display = DisplayStyle.Flex;
            show.pickingMode = PickingMode.Position;
            show.schedule.Execute(() => show.RemoveFromClassList("screen--hidden"));
        }
    }
}
