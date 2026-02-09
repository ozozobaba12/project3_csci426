using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndlessDirector : MonoBehaviour
{
    public BossfightController controller;
    public BossAI boss;

    [Header("Difficulty Scaling")]
    [Tooltip("Starting catch rate (fill speed when on the boss).")]
    public float baseCatchRate = 0.50f;
    [Tooltip("Catch rate change per boss defeated (positive = faster fill).")]
    public float catchRatePerBoss = 0f;

    [Tooltip("Starting escape rate (drain speed when off the boss).")]
    public float baseEscapeRate = 0.50f;
    [Tooltip("Escape rate change per boss defeated (positive = faster drain).")]
    public float escapeRatePerBoss = 0.05f;

    [Tooltip("Starting player bar size (fraction of column, 0-1).")]
    public float baseBarHeight = 0.325f;
    [Tooltip("How much the bar shrinks per boss defeated.")]
    public float barHeightDecreasePerBoss = 0.02f;
    [Tooltip("The bar will never shrink below this value.")]
    public float minBarHeight = 0.15f;

    [Header("Pause Menu Styling")]
    [SerializeField] Color backgroundColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] Color panelColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
    [SerializeField] Color resumeButtonColor = new Color(0.2f, 0.6f, 0.9f, 1f);
    [SerializeField] Color exitButtonColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    public int bossesDefeated = 0;
    float graceTimer = 0.75f;
    bool isPaused;
    bool gameOver;

    Canvas canvas;
    GameObject pauseMenuRoot;

    void Start()
    {
        Time.timeScale = 1f;
        ApplyScaling();
        FindCanvas();
        CreatePauseMenu();
        pauseMenuRoot.SetActive(false);
    }

    void Update()
    {
        // R to restart — works even when paused or dead
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
            return;
        }

        // Escape to toggle pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
            return;
        }

        if (isPaused || gameOver)
            return;

        if (graceTimer > 0f)
        {
            graceTimer -= Time.deltaTime;
            return;
        }

        if (controller.PlayerLost())
        {
            gameOver = true;
            Time.timeScale = 0f;
            return;
        }

        if (controller.PlayerWon())
        {
            bossesDefeated++;
            boss.bossesDefeated = bossesDefeated;

            ApplyScaling();

            boss.ResetPosition();
            controller.ResetForNextBoss();

            graceTimer = 0.75f;
        }
    }

    // ========================================================
    //  DIFFICULTY SCALING
    // ========================================================

    void ApplyScaling()
    {
        controller.catchRate = baseCatchRate + catchRatePerBoss * bossesDefeated;
        controller.escapeRate = baseEscapeRate + escapeRatePerBoss * bossesDefeated;
        controller.barHeight = Mathf.Max(
            minBarHeight,
            baseBarHeight - barHeightDecreasePerBoss * bossesDefeated
        );
    }

    // ========================================================
    //  PAUSE MENU — built in code, no Editor setup needed
    // ========================================================

    void FindCanvas()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                return;
            }
        }

        // Create one if none exists
        GameObject canvasObj = new GameObject("UICanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void CreatePauseMenu()
    {
        // Fullscreen darkened overlay
        pauseMenuRoot = new GameObject("PauseMenu");
        pauseMenuRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = pauseMenuRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        Image rootBg = pauseMenuRoot.AddComponent<Image>();
        rootBg.color = backgroundColor;

        // Center panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(pauseMenuRoot.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 260);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(40, 40, 30, 30);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // "PAUSED" title
        CreateLabel(panel.transform, "PAUSED", 32, Color.white, 40);

        // Resume button
        CreateButton(panel.transform, "RESUME", resumeButtonColor, ResumeGame);

        // Exit button
        CreateButton(panel.transform, "EXIT", exitButtonColor, ExitGame);
    }

    void CreateLabel(Transform parent, string content, int fontSize, Color color, float height)
    {
        GameObject obj = new GameObject(content);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, height);

        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
    }

    void CreateButton(Transform parent, string label, Color color,
                      UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label + "Button");
        buttonObj.transform.SetParent(parent, false);

        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(200, 50);

        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = color;

        Button btn = buttonObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(onClick);

        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(
            color.r + 0.15f, color.g + 0.15f, color.b + 0.15f, 1f);
        colors.pressedColor = new Color(
            color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, 1f);
        btn.colors = colors;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text btnText = textObj.AddComponent<Text>();
        btnText.text = label;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 24;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontStyle = FontStyle.Bold;
    }

    // ========================================================
    //  GAME ACTIONS
    // ========================================================

    void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenuRoot.SetActive(isPaused);

        if (!gameOver)
            Time.timeScale = isPaused ? 0f : 1f;
    }

    void ResumeGame()
    {
        isPaused = false;
        pauseMenuRoot.SetActive(false);

        if (!gameOver)
            Time.timeScale = 1f;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
