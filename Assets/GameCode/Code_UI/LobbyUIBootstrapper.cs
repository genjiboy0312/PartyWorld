using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyUIBootstrapper : MonoBehaviour
{
    private const string LOBBY_SCENE_NAME = "Scene_Lobby";
    private const string ROOT_NAME = "LobbyUI_Root";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 룸 로비 씬에서만 프로토타입 UI를 자동 구성
        if (scene.name != LOBBY_SCENE_NAME)
            return;

        if (Object.FindAnyObjectByType<LobbyUIController>() != null)
            return;

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();
        GameObject root = new GameObject(ROOT_NAME);
        root.transform.SetParent(canvas.transform, false);

        CreateLobbyUI(root.transform);
    }

    private static void EnsureEventSystem()
    {
        // UI 입력을 받을 EventSystem 보장
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static Canvas EnsureCanvas()
    {
        // 기본 Canvas 보장
        Canvas existing = Object.FindAnyObjectByType<Canvas>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("Canvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateLobbyUI(Transform parent)
    {
        // 간단한 프로토타입 UI 생성(Ready/상태/카운트다운)
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            return;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(20f, -20f);
        panelRt.sizeDelta = new Vector2(360f, 180f);

        Text statusText = CreateText(panel.transform, "StatusText", font, 14, TextAnchor.UpperLeft);
        RectTransform statusRt = statusText.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0f, 1f);
        statusRt.anchorMax = new Vector2(1f, 1f);
        statusRt.pivot = new Vector2(0f, 1f);
        statusRt.anchoredPosition = new Vector2(12f, -12f);
        statusRt.sizeDelta = new Vector2(-24f, 24f);

        Text countdownText = CreateText(panel.transform, "CountdownText", font, 18, TextAnchor.MiddleLeft);
        RectTransform countdownRt = countdownText.GetComponent<RectTransform>();
        countdownRt.anchorMin = new Vector2(0f, 1f);
        countdownRt.anchorMax = new Vector2(1f, 1f);
        countdownRt.pivot = new Vector2(0f, 1f);
        countdownRt.anchoredPosition = new Vector2(12f, -48f);
        countdownRt.sizeDelta = new Vector2(-24f, 36f);

        Button readyBtn = CreateButton(panel.transform, "ReadyButton", font, "Ready");
        RectTransform readyRt = readyBtn.GetComponent<RectTransform>();
        readyRt.anchorMin = new Vector2(0f, 0f);
        readyRt.anchorMax = new Vector2(0f, 0f);
        readyRt.pivot = new Vector2(0f, 0f);
        readyRt.anchoredPosition = new Vector2(12f, 12f);
        readyRt.sizeDelta = new Vector2(140f, 44f);

        Text readyBtnText = readyBtn.GetComponentInChildren<Text>();

        GameObject controllerGo = new GameObject("LobbyUIController");
        controllerGo.transform.SetParent(parent, false);
        LobbyUIController controller = controllerGo.AddComponent<LobbyUIController>();

        SetPrivateField(controller, "_readyBtn", readyBtn);
        SetPrivateField(controller, "_readyBtnText", readyBtnText);
        SetPrivateField(controller, "_statusText", statusText);
        SetPrivateField(controller, "_countdownText", countdownText);
    }

    private static Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.text = string.Empty;

        RectTransform rt = text.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Font font, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);

        Button btn = go.AddComponent<Button>();

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        Text txt = txtGo.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;
        txt.text = label;

        RectTransform txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        return btn;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        // SerializeField private에 런타임으로 바인딩(씬 수정 없이 프로토타입 구성)
        var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(target, value);
    }
}
