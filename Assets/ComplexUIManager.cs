using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>複雑なUIレイアウトを作成・管理するコンポーネント</summary>
public class ComplexUIManager : MonoBehaviour
{
    [Header("UI Prefab References")]
    public GameObject buttonPrefab;
    public GameObject panelPrefab;
    
    [Header("Dynamic Creation Settings")]
    public int numberOfButtons = 5;
    public int numberOfPanels = 3;
    public bool createNestedStructure = true;
    
    [Header("Layout Settings")]
    public bool useLayoutGroups = false;
    public bool addScrollRect = true;
    public bool includeInputFields = true;
    
    void Start()
    {
        CreateComplexUILayout();
    }
    
    /// <summary>複雑なUIレイアウトを動的に作成</summary>
    void CreateComplexUILayout()
    {
        Debug.Log("[ComplexUIManager] Creating complex UI layout...");
        
        // メインパネルを作成
        var mainPanel = CreateMainPanel();
        
        // ヘッダーセクション
        CreateHeaderSection(mainPanel);
        
        // コンテンツエリア（スクロール可能）
        var contentArea = CreateContentArea(mainPanel);
        
        // ナビゲーションパネル
        CreateNavigationPanel(mainPanel);
        
        // サイドバー
        CreateSidebar(mainPanel);
        
        // フッター
        CreateFooter(mainPanel);
        
        // 複数のオーバーレイパネル（非アクティブ）
        CreateOverlayPanels(mainPanel);
        
        Debug.Log("[ComplexUIManager] Complex UI layout created successfully!");
    }
    
    GameObject CreateMainPanel()
    {
        var mainPanel = new GameObject("MainPanel_Complex");
        mainPanel.transform.SetParent(transform, false);
        
        var rect = mainPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var image = mainPanel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        
        return mainPanel;
    }
    
    void CreateHeaderSection(GameObject parent)
    {
        var header = new GameObject("Header");
        header.transform.SetParent(parent.transform, false);
        
        var rect = header.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.9f);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var headerImage = header.AddComponent<Image>();
        headerImage.color = new Color(0.1f, 0.1f, 0.2f, 1f);
        
        // タイトルテキスト
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(header.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0);
        titleRect.anchorMax = new Vector2(0.6f, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Complex UI Dashboard";
        titleText.fontSize = 24;
        titleText.color = Color.white;
        
        // ヘッダーボタン群
        for (int i = 0; i < 3; i++)
        {
            var btnObj = new GameObject($"HeaderButton_{i}");
            btnObj.transform.SetParent(header.transform, false);
            
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.7f + i * 0.08f, 0.2f);
            btnRect.anchorMax = new Vector2(0.76f + i * 0.08f, 0.8f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            var btn = btnObj.AddComponent<Button>();
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.4f, 0.6f, 1f);
            
            // ボタンテキスト
            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = $"Btn{i + 1}";
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
        }
    }
    
    GameObject CreateContentArea(GameObject parent)
    {
        var contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(parent.transform, false);
        
        var rect = contentArea.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.1f);
        rect.anchorMax = new Vector2(0.8f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        if (addScrollRect)
        {
            // ScrollRectを追加
            var scrollRect = contentArea.AddComponent<ScrollRect>();
            var contentAreaImage = contentArea.AddComponent<Image>();
            contentAreaImage.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            
            // スクロール可能なコンテンツ
            var scrollContent = new GameObject("ScrollContent");
            scrollContent.transform.SetParent(contentArea.transform, false);
            
            var scrollContentRect = scrollContent.AddComponent<RectTransform>();
            scrollContentRect.anchorMin = Vector2.zero;
            scrollContentRect.anchorMax = Vector2.one;
            scrollContentRect.offsetMin = Vector2.zero;
            scrollContentRect.offsetMax = new Vector2(0, 500); // スクロール可能にするため高さを拡張
            
            scrollRect.content = scrollContentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // スクロールバー（簡易版）
            CreateScrollbar(contentArea, scrollRect);
            
            // コンテンツアイテムを作成
            CreateContentItems(scrollContent);
            
            return scrollContent;
        }
        else
        {
            CreateContentItems(contentArea);
            return contentArea;
        }
    }
    
    void CreateScrollbar(GameObject parent, ScrollRect scrollRect)
    {
        var scrollbar = new GameObject("Scrollbar");
        scrollbar.transform.SetParent(parent.transform, false);
        
        var scrollbarRect = scrollbar.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(0.95f, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.offsetMin = Vector2.zero;
        scrollbarRect.offsetMax = Vector2.zero;
        
        var scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        var scrollbarImage = scrollbar.AddComponent<Image>();
        scrollbarImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        // スクロールバーハンドル
        var handle = new GameObject("Handle");
        handle.transform.SetParent(scrollbar.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        
        scrollbarComponent.handleRect = handleRect;
        scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
        scrollRect.verticalScrollbar = scrollbarComponent;
    }
    
    void CreateContentItems(GameObject parent)
    {
        // 様々なタイプのUIアイテムを作成
        for (int i = 0; i < numberOfPanels; i++)
        {
            var itemPanel = new GameObject($"ContentPanel_{i}");
            itemPanel.transform.SetParent(parent.transform, false);
            
            var itemRect = itemPanel.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0.05f, 0.8f - i * 0.25f);
            itemRect.anchorMax = new Vector2(0.95f, 0.95f - i * 0.25f);
            itemRect.offsetMin = Vector2.zero;
            itemRect.offsetMax = Vector2.zero;
            
            var itemImage = itemPanel.AddComponent<Image>();
            itemImage.color = new Color(0.95f, 0.95f, 0.98f, 1f);
            
            // パネル内にコンテンツを追加
            CreatePanelContent(itemPanel, i);
        }
    }
    
    void CreatePanelContent(GameObject panel, int index)
    {
        // パネルタイトル
        var titleObj = new GameObject("PanelTitle");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.7f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = $"Panel {index + 1} - Data Section";
        titleText.fontSize = 16;
        titleText.color = new Color(0.2f, 0.2f, 0.4f, 1f);
        
        // ボタングループ
        if (useLayoutGroups)
        {
            var buttonGroup = new GameObject("ButtonGroup");
            buttonGroup.transform.SetParent(panel.transform, false);
            var buttonGroupRect = buttonGroup.AddComponent<RectTransform>();
            buttonGroupRect.anchorMin = new Vector2(0.05f, 0.4f);
            buttonGroupRect.anchorMax = new Vector2(0.95f, 0.65f);
            buttonGroupRect.offsetMin = Vector2.zero;
            buttonGroupRect.offsetMax = Vector2.zero;
            
            var layoutGroup = buttonGroup.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 5, 5);
            
            for (int i = 0; i < numberOfButtons; i++)
            {
                CreateLayoutButton(buttonGroup, i);
            }
        }
        else
        {
            // Layout Groupを使わない場合
            for (int i = 0; i < numberOfButtons; i++)
            {
                CreateManualButton(panel, i);
            }
        }
        
        // 入力フィールド
        if (includeInputFields)
        {
            CreateInputFields(panel);
        }
    }
    
    void CreateLayoutButton(GameObject parent, int index)
    {
        var btnObj = new GameObject($"LayoutButton_{index}");
        btnObj.transform.SetParent(parent.transform, false);
        
        var btn = btnObj.AddComponent<Button>();
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.4f, 0.6f, 0.8f, 1f);
        
        var btnText = new GameObject("Text");
        btnText.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnText.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        
        var text = btnText.AddComponent<TextMeshProUGUI>();
        text.text = $"Action {index + 1}";
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        // Layout Elementを追加
        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.minWidth = 80;
        layoutElement.minHeight = 30;
    }
    
    void CreateManualButton(GameObject parent, int index)
    {
        var btnObj = new GameObject($"ManualButton_{index}");
        btnObj.transform.SetParent(parent.transform, false);
        
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.05f + index * 0.18f, 0.4f);
        btnRect.anchorMax = new Vector2(0.2f + index * 0.18f, 0.65f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        var btn = btnObj.AddComponent<Button>();
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.6f, 0.4f, 0.4f, 1f); // 異なる色で区別
        
        var btnText = new GameObject("Text");
        btnText.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnText.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        
        var text = btnText.AddComponent<TextMeshProUGUI>();
        text.text = $"Manual {index + 1}";
        text.fontSize = 10;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }
    
    void CreateInputFields(GameObject parent)
    {
        for (int i = 0; i < 2; i++)
        {
            var inputObj = new GameObject($"InputField_{i}");
            inputObj.transform.SetParent(parent.transform, false);
            
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.05f + i * 0.48f, 0.1f);
            inputRect.anchorMax = new Vector2(0.45f + i * 0.48f, 0.35f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            
            var inputField = inputObj.AddComponent<TMP_InputField>();
            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = Color.white;
            
            // Placeholder
            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(inputObj.transform, false);
            var placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);
            
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = $"Enter data {i + 1}...";
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            // Text Area
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 0);
            textAreaRect.offsetMax = new Vector2(-10, 0);
            
            var textComponent = textArea.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 12;
            textComponent.color = Color.black;
            
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;
        }
    }
    
    void CreateNavigationPanel(GameObject parent)
    {
        var navPanel = new GameObject("NavigationPanel");
        navPanel.transform.SetParent(parent.transform, false);
        
        var rect = navPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.8f, 0.7f);
        rect.anchorMax = new Vector2(1, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var navImage = navPanel.AddComponent<Image>();
        navImage.color = new Color(0.3f, 0.5f, 0.3f, 1f);
        
        // Navigation buttons
        string[] navItems = { "Home", "Settings", "Help", "About" };
        for (int i = 0; i < navItems.Length; i++)
        {
            var navBtn = new GameObject($"Nav_{navItems[i]}");
            navBtn.transform.SetParent(navPanel.transform, false);
            
            var navBtnRect = navBtn.AddComponent<RectTransform>();
            navBtnRect.anchorMin = new Vector2(0.05f, 0.8f - i * 0.2f);
            navBtnRect.anchorMax = new Vector2(0.95f, 0.95f - i * 0.2f);
            navBtnRect.offsetMin = Vector2.zero;
            navBtnRect.offsetMax = Vector2.zero;
            
            var btn = navBtn.AddComponent<Button>();
            var btnImage = navBtn.AddComponent<Image>();
            btnImage.color = new Color(0.4f, 0.6f, 0.4f, 1f);
            
            var navText = new GameObject("Text");
            navText.transform.SetParent(navBtn.transform, false);
            var navTextRect = navText.AddComponent<RectTransform>();
            navTextRect.anchorMin = Vector2.zero;
            navTextRect.anchorMax = Vector2.one;
            navTextRect.offsetMin = Vector2.zero;
            navTextRect.offsetMax = Vector2.zero;
            
            var text = navText.AddComponent<TextMeshProUGUI>();
            text.text = navItems[i];
            text.fontSize = 10;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
        }
    }
    
    void CreateSidebar(GameObject parent)
    {
        var sidebar = new GameObject("Sidebar");
        sidebar.transform.SetParent(parent.transform, false);
        
        var rect = sidebar.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.8f, 0.1f);
        rect.anchorMax = new Vector2(1, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var sidebarImage = sidebar.AddComponent<Image>();
        sidebarImage.color = new Color(0.5f, 0.3f, 0.5f, 1f);
        
        // Sidebar content with vertical layout
        var layoutGroup = sidebar.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 5;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = false;
        
        // Add content size fitter
        var contentSizeFitter = sidebar.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        for (int i = 0; i < 6; i++)
        {
            var item = new GameObject($"SidebarItem_{i}");
            item.transform.SetParent(sidebar.transform, false);
            
            var itemImage = item.AddComponent<Image>();
            itemImage.color = new Color(0.6f, 0.4f, 0.6f, 1f);
            
            var layoutElement = item.AddComponent<LayoutElement>();
            layoutElement.minHeight = 40;
            
            var itemText = new GameObject("Text");
            itemText.transform.SetParent(item.transform, false);
            var itemTextRect = itemText.AddComponent<RectTransform>();
            itemTextRect.anchorMin = Vector2.zero;
            itemTextRect.anchorMax = Vector2.one;
            itemTextRect.offsetMin = new Vector2(5, 0);
            itemTextRect.offsetMax = new Vector2(-5, 0);
            
            var text = itemText.AddComponent<TextMeshProUGUI>();
            text.text = $"Sidebar Item {i + 1}";
            text.fontSize = 10;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
        }
    }
    
    void CreateFooter(GameObject parent)
    {
        var footer = new GameObject("Footer");
        footer.transform.SetParent(parent.transform, false);
        
        var rect = footer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var footerImage = footer.AddComponent<Image>();
        footerImage.color = new Color(0.1f, 0.1f, 0.2f, 1f);
        
        var footerText = new GameObject("FooterText");
        footerText.transform.SetParent(footer.transform, false);
        var footerTextRect = footerText.AddComponent<RectTransform>();
        footerTextRect.anchorMin = Vector2.zero;
        footerTextRect.anchorMax = Vector2.one;
        footerTextRect.offsetMin = Vector2.zero;
        footerTextRect.offsetMax = Vector2.zero;
        
        var text = footerText.AddComponent<TextMeshProUGUI>();
        text.text = "Complex UI Layout Demo - Footer Information";
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
    }
    
    void CreateOverlayPanels(GameObject parent)
    {
        // 複数の非アクティブなオーバーレイパネルを作成（問題検出テスト用）
        for (int i = 0; i < 3; i++)
        {
            var overlay = new GameObject($"OverlayPanel_{i}");
            overlay.transform.SetParent(parent.transform, false);
            overlay.SetActive(false); // 非アクティブに設定
            
            var rect = overlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.5f);
            
            // オーバーレイ内容（これも非アクティブ）
            var overlayContent = new GameObject("OverlayContent");
            overlayContent.transform.SetParent(overlay.transform, false);
            
            var contentRect = overlayContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.2f, 0.2f);
            contentRect.anchorMax = new Vector2(0.8f, 0.8f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            var contentImage = overlayContent.AddComponent<Image>();
            contentImage.color = Color.white;
            // わざとspriteを設定しない（問題検出テスト用）
            
            var overlayTitle = new GameObject("OverlayTitle");
            overlayTitle.transform.SetParent(overlayContent.transform, false);
            var titleRect = overlayTitle.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.8f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            var titleText = overlayTitle.AddComponent<TextMeshProUGUI>();
            titleText.text = $"Overlay {i + 1} - Hidden Panel";
            titleText.fontSize = 18;
            titleText.color = Color.black;
            titleText.alignment = TextAlignmentOptions.Center;
        }
    }
}