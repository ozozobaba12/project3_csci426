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

    [Header("Boss Death Timing")]
    [Tooltip("How long the freeze lasts for bosses without a custom death sequence (Boss 2, 3).")]
    public float defaultDeathDuration = 1.5f;

    public int bossesDefeated = 0;
    float graceTimer = 0.75f;
    bool isPaused;
    bool gameOver;
    public bool IsGameOver => gameOver;

    // Boss death sequence state
    bool bossDying;
    float bossDeathTimer;
    public bool IsBossDying => bossDying;

    // Player death sequence state
    bool playerDying;
    float playerDeathTimer;
    public bool IsPlayerDying => playerDying;

    Canvas canvas;
    GameObject pauseMenuRoot;
    GameObject deathScreenRoot;
    Text scoreText;
    Text highScoreText;
    bool deathScreenShown;

    const string HighScoreKey = "BossRushHighScore";
    const string MasterVolKey = "MasterVolume";
    Slider masterVolumeSlider;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolKey, 1f);
        ApplyScaling();
        FindCanvas();
        CreatePauseMenu();
        pauseMenuRoot.SetActive(false);
        CreateDeathScreen();
        deathScreenRoot.SetActive(false);
    }

    void Update()
    {
        // R to restart — works even when paused or dead
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
            return;
        }

        // Space to restart from death screen
        if (deathScreenShown && Input.GetKeyDown(KeyCode.Space))
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

        // 1 to instantly skip/kill the current boss (debug)
        if (Input.GetKeyDown(KeyCode.Alpha1) && !bossDying && !playerDying)
        {
            controller.progress = 1f;
        }

        // --- Player death sequence: wait for UI to finish, then show death screen ---
        if (playerDying)
        {
            playerDeathTimer -= Time.deltaTime;
            if (playerDeathTimer <= 0f)
                FinishPlayerDeath();
            return;
        }

        // --- Boss death sequence: wait, then transition ---
        if (bossDying)
        {
            bossDeathTimer -= Time.deltaTime;
            if (bossDeathTimer <= 0f)
                FinishBossTransition();
            return;
        }

        if (graceTimer > 0f)
        {
            graceTimer -= Time.deltaTime;
            return;
        }

        if (controller.PlayerLost())
        {
            // Freeze gameplay — bar, boss icon, and progress all stop
            controller.isFrozen = true;
            boss.isActive = false;

            playerDying = true;
            playerDeathTimer = 999f; // BossfightUI will override via SetPlayerDeathTimer
            return;
        }

        if (controller.PlayerWon())
        {
            // Freeze gameplay — bar, boss icon, and progress all stop
            controller.isFrozen = true;
            boss.isActive = false;

            bossDying = true;
            bossDeathTimer = defaultDeathDuration;
        }
    }

    /// <summary>
    /// Called by BossfightUI (or the timer) when the death sequence is done.
    /// Advances to the next boss.
    /// </summary>
    public void FinishBossTransition()
    {
        bossDying = false;

        bossesDefeated++;
        boss.bossesDefeated = bossesDefeated;

        ApplyScaling();

        boss.ResetPosition();
        controller.ResetForNextBoss();

        graceTimer = 0.75f;
    }

    /// <summary>
    /// Overrides the default death timer so the UI can drive timing instead.
    /// Pass a large value so the timer never expires on its own.
    /// </summary>
    public void SetDeathTimer(float duration)
    {
        bossDeathTimer = duration;
    }

    /// <summary>
    /// Overrides the player death timer so the UI can drive the sequence.
    /// </summary>
    public void SetPlayerDeathTimer(float duration)
    {
        playerDeathTimer = duration;
    }

    /// <summary>
    /// Called by BossfightUI when the player death sequence is done.
    /// Freezes the game and shows the death screen.
    /// </summary>
    public void FinishPlayerDeath()
    {
        playerDying = false;
        gameOver = true;
        Time.timeScale = 0f;
        ShowDeathScreen();
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
        panelRect.sizeDelta = new Vector2(400, 340);

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

        // Volume label
        CreateLabel(panel.transform, "VOLUME", 20, new Color(0.8f, 0.8f, 0.8f, 1f), 25);

        // Master volume slider
        CreateVolumeSlider(panel.transform);

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

    void CreateVolumeSlider(Transform parent)
    {
        float savedVol = PlayerPrefs.GetFloat(MasterVolKey, 1f);

        // Root object for the slider
        GameObject sliderObj = new GameObject("VolumeSlider");
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(280, 30);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = savedVol;
        slider.wholeNumbers = false;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 0.9f, 1f);

        // Handle slide area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 30);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Wire slider references
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        // Color transitions for the handle
        ColorBlock cb = slider.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        slider.colors = cb;

        slider.onValueChanged.AddListener((float val) =>
        {
            AudioListener.volume = val;
            PlayerPrefs.SetFloat(MasterVolKey, val);
        });

        masterVolumeSlider = slider;
    }

    // ========================================================
    //  DEATH SCREEN
    // ========================================================

    void CreateDeathScreen()
    {
        // Fullscreen darkened overlay
        deathScreenRoot = new GameObject("DeathScreen");
        deathScreenRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = deathScreenRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        Image rootBg = deathScreenRoot.AddComponent<Image>();
        rootBg.color = new Color(0.15f, 0f, 0f, 0.85f);

        // Center panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(deathScreenRoot.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(450, 320);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.padding = new RectOffset(40, 40, 30, 30);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // "YOU DIED" title
        CreateLabel(panel.transform, "YOU DIED", 36,
            new Color(0.9f, 0.2f, 0.2f, 1f), 45);

        // Score (placeholder, updated when shown)
        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(panel.transform, false);
        RectTransform scoreRect = scoreObj.AddComponent<RectTransform>();
        scoreRect.sizeDelta = new Vector2(300, 35);
        scoreText = scoreObj.AddComponent<Text>();
        scoreText.text = "Score: 0";
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 26;
        scoreText.color = Color.white;
        scoreText.alignment = TextAnchor.MiddleCenter;

        // High score
        GameObject highObj = new GameObject("HighScore");
        highObj.transform.SetParent(panel.transform, false);
        RectTransform highRect = highObj.AddComponent<RectTransform>();
        highRect.sizeDelta = new Vector2(300, 35);
        highScoreText = highObj.AddComponent<Text>();
        highScoreText.text = "High Score: 0";
        highScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        highScoreText.fontSize = 26;
        highScoreText.color = new Color(1f, 0.85f, 0.3f, 1f);
        highScoreText.alignment = TextAnchor.MiddleCenter;

        // Spacer
        CreateLabel(panel.transform, "", 10, Color.clear, 10);

        // "Press Space to Continue"
        CreateLabel(panel.transform, "Press Space to Continue", 20,
            new Color(0.7f, 0.7f, 0.7f, 1f), 30);
    }

    public void ShowDeathScreen()
    {
        if (deathScreenShown)
            return;

        deathScreenShown = true;

        int score = bossesDefeated;
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        scoreText.text = $"Score: {score}";
        highScoreText.text = $"High Score: {highScore}";

        // Ensure death screen is a direct child of the canvas root
        // (not trapped inside ScreenShakeWrapper) so it renders on top of everything
        deathScreenRoot.transform.SetParent(canvas.transform, false);
        deathScreenRoot.transform.SetAsLastSibling();
        deathScreenRoot.SetActive(true);
    }

    // ========================================================
    //  GAME ACTIONS
    // ========================================================

    void TogglePause()
    {
        isPaused = !isPaused;

        // Ensure pause menu renders on top of everything
        if (isPaused)
        {
            pauseMenuRoot.transform.SetParent(canvas.transform, false);
            pauseMenuRoot.transform.SetAsLastSibling();
        }

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
