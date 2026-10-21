#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TutorialPanelBuilder
{
    private const string PrefabPath = "Assets/Prefabs/TutorialPanel.prefab";
    private const string FontPath = "Assets/Quang/Font/Chewy-Regular SDF.asset";

    [MenuItem("Tools/OnlyOneUp/Create Tutorial Panel in Scene")]
    public static GameObject CreateTutorialPanelInScene()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // 1. Root Panel
        GameObject root = new GameObject("TutorialPanel", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 2. Dim Background (Overlay)
        GameObject dimOverlay = new GameObject("DimOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimOverlay.transform.SetParent(root.transform, false);
        RectTransform dimRect = dimOverlay.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        Image dimImage = dimOverlay.GetComponent<Image>();
        dimImage.color = new Color(0.04f, 0.05f, 0.08f, 0.78f);

        // 3. Main Window Box
        GameObject window = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        window.transform.SetParent(root.transform, false);
        RectTransform winRect = window.GetComponent<RectTransform>();
        winRect.anchorMin = new Vector2(0.5f, 0.5f);
        winRect.anchorMax = new Vector2(0.5f, 0.5f);
        winRect.pivot = new Vector2(0.5f, 0.5f);
        winRect.sizeDelta = new Vector2(920f, 600f);
        Image winImage = window.GetComponent<Image>();
        winImage.color = new Color(0.12f, 0.15f, 0.22f, 0.96f);

        // Header Background
        GameObject headerBar = new GameObject("HeaderBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerBar.transform.SetParent(window.transform, false);
        RectTransform headerRect = headerBar.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 75f);
        Image headerImage = headerBar.GetComponent<Image>();
        headerImage.color = new Color(0.18f, 0.22f, 0.32f, 1f);

        // Header Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerBar.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(30f, 0f);
        titleRect.offsetMax = new Vector2(-80f, 0f);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleText.font = fontAsset;
        titleText.text = "HƯỚNG DẪN CHƠI - ONLY ONE UP";
        titleText.fontSize = 32f;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(1f, 0.82f, 0.35f, 1f);
        titleText.fontStyle = FontStyles.Bold;

        // Close Button [X]
        GameObject closeBtnObj = CreateButton(headerBar.transform, "CloseButton", "✕", new Vector2(48f, 48f), new Color(0.85f, 0.28f, 0.25f, 1f), fontAsset, 24f);
        RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 0.5f);
        closeBtnRect.anchorMax = new Vector2(1f, 0.5f);
        closeBtnRect.pivot = new Vector2(1f, 0.5f);
        closeBtnRect.anchoredPosition = new Vector2(-15f, 0f);

        // Content Area
        GameObject contentArea = new GameObject("ContentArea", typeof(RectTransform));
        contentArea.transform.SetParent(window.transform, false);
        RectTransform contentRect = contentArea.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.offsetMin = new Vector2(30f, 85f);
        contentRect.offsetMax = new Vector2(-30f, -85f);

        // 4. Create Pages
        GameObject page1 = CreatePage(contentArea.transform, "Page_01_Movement",
            "1. CÁCH DI CHUYỂN",
            "<color=#FBD38D><b>[ W ][ A ][ S ][ D ]</b></color>  hoặc các phím mũi tên  <color=#FBD38D><b>[ ↑ ][ ↓ ][ ← ][ → ]</b></color>",
            "• Sử dụng các phím để điều khiển chú vịt của bạn di chuyển.\n\n" +
            "• <b>Cơ chế giữ tổ:</b> Bạn và đồng đội đang cùng nhau nâng một chiếc tổ chứa trứng.\n\n" +
            "• <b>Lưu ý quan trọng:</b> Hãy di chuyển nhịp nhàng cùng bạn chơi! Đi quá xa hoặc giật đột ngột sẽ kéo nghiêng tổ và khiến quả trứng dễ bị lăn ra ngoài.",
            fontAsset);

        GameObject page2 = CreatePage(contentArea.transform, "Page_02_Jump",
            "2. NHẢY VƯỢT CHƯỚNG NGẠI VẬT",
            "Phím nhảy:  <color=#FBD38D><b>[ SPACEBAR ] (Phím Cách)</b></color>",
            "• Nhấn phím <b>Space</b> để nhảy lên các khối đá và vượt qua chướng ngại vật.\n\n" +
            "• <b>Nhảy đồng bộ:</b> Khi gặp gờ cao, cả hai người chơi cần <b>cùng nhảy ăn ý</b> với nhau.\n\n" +
            "• <b>Cảnh báo:</b> Nếu một người nhảy mà người kia đứng yên, tổ sẽ bị lật nghiêng sang một bên khiến trứng rơi vực ngay lập tức!",
            fontAsset);

        GameObject page3 = CreatePage(contentArea.transform, "Page_03_Rules",
            "3. LUẬT CHƠI & BẢO VỆ TRỨNG",
            "Mục tiêu tối thượng:  <color=#68D391><b>ĐƯA TRỨNG VỀ ĐÍCH AN TOÀN</b></color>",
            "• <b>Độ bền của trứng:</b> Tránh để tổ va quệt mạnh vào vách núi hoặc chướng ngại vật. Va đập mạnh có thể làm nứt hoặc vỡ trứng!\n\n" +
            "• <b>Điểm hồi sinh (Checkpoint):</b> Khi trứng bị vỡ hoặc hai bạn rơi vực, đội sẽ được hồi sinh tại Checkpoint gần nhất.\n\n" +
            "• <b>Chiến thắng:</b> Hãy cùng nhau leo lên đỉnh và đưa chiếc tổ chạm vào <color=#FBD38D><b>Vùng Đích (Win Area)</b></color> để giành chiến thắng!",
            fontAsset);

        // 5. Footer Bar
        GameObject footerBar = new GameObject("FooterBar", typeof(RectTransform));
        footerBar.transform.SetParent(window.transform, false);
        RectTransform footerRect = footerBar.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.sizeDelta = new Vector2(0f, 75f);

        // Prev Button
        GameObject prevBtnObj = CreateButton(footerBar.transform, "PrevButton", "< Trang trước", new Vector2(170f, 50f), new Color(0.24f, 0.28f, 0.38f, 1f), fontAsset, 20f);
        RectTransform prevBtnRect = prevBtnObj.GetComponent<RectTransform>();
        prevBtnRect.anchorMin = new Vector2(0f, 0.5f);
        prevBtnRect.anchorMax = new Vector2(0f, 0.5f);
        prevBtnRect.pivot = new Vector2(0f, 0.5f);
        prevBtnRect.anchoredPosition = new Vector2(30f, 0f);

        // Page Indicator
        GameObject indicatorObj = new GameObject("PageIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        indicatorObj.transform.SetParent(footerBar.transform, false);
        RectTransform indRect = indicatorObj.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0.5f);
        indRect.anchorMax = new Vector2(0.5f, 0.5f);
        indRect.pivot = new Vector2(0.5f, 0.5f);
        indRect.sizeDelta = new Vector2(250f, 40f);
        TextMeshProUGUI indText = indicatorObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) indText.font = fontAsset;
        indText.text = "Trang 1 / 3";
        indText.fontSize = 20f;
        indText.alignment = TextAlignmentOptions.Center;
        indText.color = new Color(0.85f, 0.88f, 0.95f, 1f);

        // Next Button
        GameObject nextBtnObj = CreateButton(footerBar.transform, "NextButton", "Trang sau >", new Vector2(170f, 50f), new Color(0.95f, 0.55f, 0.15f, 1f), fontAsset, 20f);
        RectTransform nextBtnRect = nextBtnObj.GetComponent<RectTransform>();
        nextBtnRect.anchorMin = new Vector2(1f, 0.5f);
        nextBtnRect.anchorMax = new Vector2(1f, 0.5f);
        nextBtnRect.pivot = new Vector2(1f, 0.5f);
        nextBtnRect.anchoredPosition = new Vector2(-30f, 0f);

        // Hint Shortcut Text
        GameObject hintObj = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(window.transform, false);
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 8f);
        hintRect.sizeDelta = new Vector2(600f, 25f);
        TextMeshProUGUI hintText = hintObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) hintText.font = fontAsset;
        hintText.text = "💡 Mẹo: Bạn có thể nhấn phím [H] hoặc [F1] để xem lại hướng dẫn bất kỳ lúc nào.";
        hintText.fontSize = 14f;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(0.65f, 0.70f, 0.80f, 0.85f);

        // 6. Hook up TutorialPanelController & Trigger
        TutorialPanelController controller = root.AddComponent<TutorialPanelController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("_panelRoot").objectReferenceValue = root;
        serializedController.FindProperty("_closeButton").objectReferenceValue = closeBtnObj.GetComponent<Button>();
        serializedController.FindProperty("_nextButton").objectReferenceValue = nextBtnObj.GetComponent<Button>();
        serializedController.FindProperty("_prevButton").objectReferenceValue = prevBtnObj.GetComponent<Button>();
        serializedController.FindProperty("_pageIndicatorText").objectReferenceValue = indText;

        SerializedProperty pagesProp = serializedController.FindProperty("_pages");
        pagesProp.arraySize = 3;
        pagesProp.GetArrayElementAtIndex(0).objectReferenceValue = page1;
        pagesProp.GetArrayElementAtIndex(1).objectReferenceValue = page2;
        pagesProp.GetArrayElementAtIndex(2).objectReferenceValue = page3;
        serializedController.ApplyModifiedProperties();

        TutorialGameStartTrigger trigger = root.AddComponent<TutorialGameStartTrigger>();
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("_tutorialController").objectReferenceValue = controller;
        serializedTrigger.ApplyModifiedProperties();

        // 7. Save as Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);

        // Hide root in scene by default
        root.SetActive(false);

        Undo.RegisterCreatedObjectUndo(root, "Create Tutorial Panel");
        Selection.activeGameObject = root;

        Debug.Log("Đã tạo thành công TutorialPanel và lưu Prefab tại: " + PrefabPath);
        return root;
    }

    private static GameObject CreatePage(Transform parent, string name, string pageTitle, string badgeText, string bodyText, TMP_FontAsset font)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform pageRect = page.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;

        // Subtitle
        GameObject titleObj = new GameObject("PageTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(page.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(10f, -5f);
        titleRect.sizeDelta = new Vector2(-20f, 38f);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = pageTitle;
        titleTMP.fontSize = 24f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.95f, 0.95f, 0.98f, 1f);

        // Badge Box / Highlight Box
        GameObject badgeBox = new GameObject("BadgeBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeBox.transform.SetParent(page.transform, false);
        RectTransform badgeRect = badgeBox.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -48f);
        badgeRect.sizeDelta = new Vector2(0f, 52f);
        Image badgeImg = badgeBox.GetComponent<Image>();
        badgeImg.color = new Color(0.18f, 0.23f, 0.34f, 0.85f);

        GameObject badgeTextObj = new GameObject("BadgeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        badgeTextObj.transform.SetParent(badgeBox.transform, false);
        RectTransform badgeTextRect = badgeTextObj.GetComponent<RectTransform>();
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = new Vector2(15f, 0f);
        badgeTextRect.offsetMax = new Vector2(-15f, 0f);
        TextMeshProUGUI badgeTMP = badgeTextObj.GetComponent<TextMeshProUGUI>();
        if (font != null) badgeTMP.font = font;
        badgeTMP.text = badgeText;
        badgeTMP.fontSize = 20f;
        badgeTMP.alignment = TextAlignmentOptions.MidlineLeft;
        badgeTMP.color = Color.white;

        // Body Text
        GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(page.transform, false);
        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(10f, 15f);
        bodyRect.offsetMax = new Vector2(-10f, -115f);
        TextMeshProUGUI bodyTMP = bodyObj.GetComponent<TextMeshProUGUI>();
        if (font != null) bodyTMP.font = font;
        bodyTMP.text = bodyText;
        bodyTMP.fontSize = 19f;
        bodyTMP.lineSpacing = 18f;
        bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        bodyTMP.color = new Color(0.85f, 0.88f, 0.93f, 1f);

        return page;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector2 size, Color bgColor, TMP_FontAsset font, float fontSize)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.15f;
        colors.pressedColor = bgColor * 0.85f;
        btn.colors = colors;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }
}
#endif
