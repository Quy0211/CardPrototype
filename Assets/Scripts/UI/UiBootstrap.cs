using UnityEngine;
using UnityEngine.UIElements;

namespace CardGame.UI
{
    /// <summary>
    /// Tạo UI hoàn toàn bằng code: không cần sửa scene. Khi vào Play,
    /// một GameObject ẩn chứa UIDocument + App được sinh ra tự động.
    /// UXML nạp từ Resources/UI, font từ Resources/Fonts.
    /// </summary>
    public static class UiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<UIDocument>() != null) return;

            var appAsset = Resources.Load<VisualTreeAsset>("UI/App");
            if (appAsset == null)
            {
                Debug.LogError("[CardGame] Không tìm thấy Resources/UI/App.uxml");
                return;
            }

            var go = new GameObject("~CardGameUI");
            Object.DontDestroyOnLoad(go);
            go.SetActive(false); // cấu hình trước khi OnEnable chạy

            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = CreatePanelSettings();
            doc.visualTreeAsset = appAsset;
            go.AddComponent<App>();

            go.SetActive(true);
        }

        static PanelSettings CreatePanelSettings()
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;

            var theme = Resources.Load<ThemeStyleSheet>("UI/RuntimeTheme");
            if (theme != null) ps.themeStyleSheet = theme;
            else Debug.LogWarning("[CardGame] Không tải được Resources/UI/RuntimeTheme.tss");

            return ps;
        }
    }
}