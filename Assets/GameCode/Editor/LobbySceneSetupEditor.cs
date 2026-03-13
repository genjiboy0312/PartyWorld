#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LobbySceneSetupEditor
{
    private const string LOBBY_SCENE_PATH = "Assets/GameData/Scenes/Scene_Lobby.unity";

    [MenuItem("Tools/PartyWorld/Setup Scene_Lobby UI")]
    private static void SetupLobbyUI()
    {
        // Scene_Lobby를 열고, Ready/상태/카운트다운 UI를 생성 후 저장
        Scene scene = EditorSceneManager.OpenScene(LOBBY_SCENE_PATH, OpenSceneMode.Single);

        if (Object.FindAnyObjectByType<LobbyUIController>() != null)
        {
            Debug.Log("[LobbySceneSetup] LobbyUIController가 이미 존재합니다.");
            return;
        }

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();

        GameObject root = new GameObject("LobbyUI_Root");
        root.transform.SetParent(canvas.transform, false);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            Debug.LogError("[LobbySceneSetup] Built-in font(LegacyRuntime.ttf)을 찾을 수 없습니다.");
            return;
        }

        GameObject panel = CreatePanel(root.transform);
        Text statusText = CreateText(panel.transform, "StatusText", font, 14, TextAnchor.UpperLeft);
        SetRect(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(-24f, 24f));

        Text countdownText = CreateText(panel.transform, "CountdownText", font, 18, TextAnchor.MiddleLeft);
        SetRect(countdownText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(12f, -48f), new Vector2(-24f, 36f));

        Button readyBtn = CreateButton(panel.transform, "ReadyButton", font, "Ready");
        RectTransform readyRt = readyBtn.GetComponent<RectTransform>();
        readyRt.anchorMin = new Vector2(0f, 0f);
        readyRt.anchorMax = new Vector2(0f, 0f);
        readyRt.pivot = new Vector2(0f, 0f);
        readyRt.anchoredPosition = new Vector2(12f, 12f);
        readyRt.sizeDelta = new Vector2(140f, 44f);

        Text readyBtnText = readyBtn.GetComponentInChildren<Text>();

        GameObject controllerGo = new GameObject("LobbyUIController");
        controllerGo.transform.SetParent(root.transform, false);
        LobbyUIController controller = controllerGo.AddComponent<LobbyUIController>();
        Bind(controller, readyBtn, readyBtnText, statusText, countdownText);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LobbySceneSetup] Scene_Lobby UI 구성 및 저장 완료.");
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

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(360f, 180f);
        return panel;
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

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static void Bind(LobbyUIController controller, Button readyBtn, Text readyBtnText, Text statusText, Text countdownText)
    {
        // SerializeField private 필드를 에디터에서 바인딩
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_readyBtn").objectReferenceValue = readyBtn;
        so.FindProperty("_readyBtnText").objectReferenceValue = readyBtnText;
        so.FindProperty("_statusText").objectReferenceValue = statusText;
        so.FindProperty("_countdownText").objectReferenceValue = countdownText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
