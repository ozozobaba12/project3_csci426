using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MagicPigGames;

public class BossfightUI : MonoBehaviour
{
    public BossfightController controller;
    public BossAI boss;
    public EndlessDirector director;

    [Header("UI References")]
    public RectTransform column;
    public RectTransform player;
    public RectTransform bossIcon;
    public ProgressBar progressBar;
    public TextMeshProUGUI enemyCounterText;

    [Header("Rotation Mechanic")]
    [Tooltip("Seconds between each 90-degree rotation.")]
    public float rotationInterval = 4f;
    [Tooltip("How fast the rotation animates (degrees per second).")]
    public float rotationSpeed = 270f;

    [Header("Rotation Tick Shake")]
    [Tooltip("Seconds between each warning shake during rotation bosses.")]
    public float tickShakeInterval = 1f;
    [Tooltip("Pixel displacement of each tick shake.")]
    public float tickShakeIntensity = 10f;
    [Tooltip("How long each tick shake lasts (seconds).")]
    public float tickShakeDuration = 0.1f;

    [Header("Gravity Mechanic")]
    [Tooltip("Progress value (0-1) that triggers phase 2 gravity. One-shot, never reverts.")]
    public float gravityPhaseThreshold = 0.6f;
    [Tooltip("How long gravity stays ON (seconds).")]
    public float gravityOnDuration = 2f;
    [Tooltip("How long gravity stays OFF / floating (seconds).")]
    public float gravityOffDuration = 2f;
    [Tooltip("Downward acceleration when gravity is on (pixels/s^2).")]
    public float gravityAccel = 800f;
    [Tooltip("Upward acceleration when gravity is off (pixels/s^2).")]
    public float floatAccel = 300f;
    [Tooltip("Fraction of velocity retained after bouncing off a screen edge (0-1).")]
    [Range(0f, 1f)]
    public float bounceFactor = 0.4f;
    [Tooltip("Random angular impulse range when gravity toggles (degrees/s).")]
    public float tumbleImpulse = 120f;
    [Tooltip("How quickly spin decays when floating (0 = never, higher = faster).")]
    public float tumbleDamping = 2f;

    [Header("Boss 1 — Wolf Visuals")]
    [Tooltip("The wolf Image (UI) on the right side of the screen. Disabled for other bosses.")]
    public Image wolfImage;
    [Tooltip("Background Image that only shows during Boss 1. Disabled for other bosses.")]
    public Image boss1Background;

    [Header("Wolf Death Sequence")]
    [Tooltip("Number of red flashes before death animation plays.")]
    public int wolfFlashCount = 4;
    [Tooltip("Duration of each flash on/off cycle (seconds).")]
    public float wolfFlashInterval = 0.1f;
    [Tooltip("How long the death animation plays before the wolf disappears (seconds). Match your wolf_dead clip length.")]
    public float wolfDeathAnimDuration = 1.0f;
    [Tooltip("Particle effect prefab spawned where the wolf was after it disappears.")]
    public GameObject wolfDeathExplosionPrefab;
    [Tooltip("How long to wait for the explosion to play before transitioning (seconds).")]
    public float explosionWaitDuration = 3f;

    [Header("Boss 2 — Clock Visuals")]
    [Tooltip("The clock Image (UI) on the right side of the screen. Disabled for other bosses.")]
    public Image clockImage;
    [Tooltip("Background Image that only shows during Boss 2. Disabled for other bosses.")]
    public Image boss2Background;

    [Header("Clock Rotate Animation Timing")]
    [Tooltip("Seconds before the column rotation where the clock starts its rotate animation.")]
    public float clockRotateLeadIn = 0.5f;
    [Tooltip("Seconds after the column rotation finishes where the clock continues its rotate animation.")]
    public float clockRotateTailOut = 0.3f;

    [Header("Clock Hurt")]
    [Tooltip("Cooldown between hurt triggers so the animation can play through (seconds).")]
    public float clockHurtCooldown = 0.4f;

    [Header("Clock Death Sequence")]
    [Tooltip("Number of red flashes before death animation plays.")]
    public int clockFlashCount = 4;
    [Tooltip("Duration of each flash on/off cycle (seconds).")]
    public float clockFlashInterval = 0.1f;
    [Tooltip("How long the looping death animation plays before the clock disappears (seconds).")]
    public float clockDeathAnimDuration = 1.0f;
    [Tooltip("Particle effect prefab spawned where the clock was after it disappears.")]
    public GameObject clockDeathExplosionPrefab;
    [Tooltip("How long to wait for the explosion to play before transitioning (seconds).")]
    public float clockExplosionWaitDuration = 3f;

    [Header("Boss Defeat Effects")]
    [Tooltip("Pixel displacement of the impact shake when a boss is defeated.")]
    public float victoryShakeIntensity = 20f;
    [Tooltip("How long the victory shake lasts (seconds). Keep short for a punchy hit feel.")]
    public float victoryShakeDuration = 0.2f;

    [Header("Death Effects")]
    [Tooltip("Max pixel displacement of the health bar during grace period.")]
    public float graceShakeMax = 8f;
    [Tooltip("Pixel displacement of the screen shake on death.")]
    public float deathShakeIntensity = 15f;
    [Tooltip("How long the death screen shake lasts (seconds, uses real time).")]
    public float deathShakeDuration = 0.25f;

    // Auto-discovered fill textures inside the progress bar
    RawImage[] progressFills;

    // Wolf animator (auto-fetched from wolfImage)
    Animator wolfAnimator;
    bool wolfActive;
    float lastProgress;

    // Wolf death sequence state: 0=not dying, 1=flashing, 2=death anim, 3=explosion, 4=done
    int wolfDeathPhase;
    float wolfDeathTimer;
    int wolfFlashCounter;
    bool wolfFlashOn;
    GameObject bossExplosionObj;
    RenderTexture explosionRT;
    int mainCamOriginalMask;

    // Clock animator (auto-fetched from clockImage)
    Animator clockAnimator;
    bool clockActive;
    float clockLastProgress;

    // Clock rotate/hurt animation timing
    float clockRotateTailTimer;
    float clockHurtTimer;

    // Clock death sequence state: 0=not dying, 1=flashing, 2=death anim, 3=explosion, 4=done
    int clockDeathPhase;
    float clockDeathTimer;
    int clockFlashCounter;
    bool clockFlashOn;

    // Universal boss defeat effects
    bool defeatEffectsStarted;
    float victoryShakeTimer;

    // Rotation state
    RectTransform rotationWrapper;
    float rotationTimer;
    float targetAngle;
    float currentAngle;
    bool rotationActive;
    int lastBossCount = -1;

    // Tick shake state (rotation warning)
    float tickTimer;
    float tickShakeTimer;
    bool tickShaking;

    // Gravity mechanic state
    bool gravityMechanicActive;
    bool gravityPhase2;
    bool gravityIsOn;
    float gravityToggleTimer;
    Vector2 columnVelocity;
    Vector2 columnOffset;
    float columnAngularVel;
    float columnAngle;
    Vector2 progressBarVelocity;
    Vector2 progressBarOffset;
    float progressBarAngularVel;
    float progressBarAngle;
    RectTransform canvasRect;

    // Death shake state
    RectTransform progressBarRect;
    Vector2 progressBarBasePos;
    float deathShakeTimer;
    bool deathShakeActive;
    bool deathShakeTriggered;
    RectTransform screenShakeWrapper;

    void Start()
    {
        if (progressBar != null)
        {
            progressFills = progressBar.GetComponentsInChildren<RawImage>();
            progressBarRect = progressBar.GetComponent<RectTransform>();
            progressBarBasePos = progressBarRect.anchoredPosition;
        }

        // Cache wolf animator
        if (wolfImage != null)
            wolfAnimator = wolfImage.GetComponent<Animator>();

        // Cache clock animator
        if (clockImage != null)
            clockAnimator = clockImage.GetComponent<Animator>();

        // Start hidden — DetectBossTransition will enable on first frame
        SetWolfActive(false);
        SetClockActive(false);

        CreateRotationWrapper();

        // Cache the canvas rect for gravity screen bounds
        Canvas rootCanvas = column.GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = rootCanvas.GetComponent<RectTransform>();

        // Create a wrapper inside the root canvas that holds ALL UI content
        // so we can shake everything on screen (overlay canvas root can't be moved)
        CreateScreenShakeWrapper();
    }

    // Groups the column and progress bar under a shared parent
    // so they rotate as a unit and maintain their relative positions
    void CreateRotationWrapper()
    {
        GameObject wrapper = new GameObject("RotationWrapper");
        rotationWrapper = wrapper.AddComponent<RectTransform>();

        // Place it in the canvas at center, same as column
        rotationWrapper.SetParent(column.parent, false);
        rotationWrapper.anchorMin = new Vector2(0.5f, 0.5f);
        rotationWrapper.anchorMax = new Vector2(0.5f, 0.5f);
        rotationWrapper.pivot = new Vector2(0.5f, 0.5f);
        rotationWrapper.anchoredPosition = Vector2.zero;
        rotationWrapper.sizeDelta = Vector2.zero;

        // Reparent column and progress bar under the wrapper
        column.SetParent(rotationWrapper, true);

        if (progressBar != null)
            progressBar.GetComponent<RectTransform>().SetParent(rotationWrapper, true);
    }

    void CreateScreenShakeWrapper()
    {
        Canvas rootCanvas = column.GetComponentInParent<Canvas>().rootCanvas;
        Transform canvasTransform = rootCanvas.transform;

        // Create a stretch-to-fill wrapper
        GameObject go = new GameObject("ScreenShakeWrapper");
        screenShakeWrapper = go.AddComponent<RectTransform>();
        screenShakeWrapper.SetParent(canvasTransform, false);
        screenShakeWrapper.anchorMin = Vector2.zero;
        screenShakeWrapper.anchorMax = Vector2.one;
        screenShakeWrapper.offsetMin = Vector2.zero;
        screenShakeWrapper.offsetMax = Vector2.zero;

        // Move wrapper to index 0, then take children in forward order
        // so the original hierarchy draw order is preserved
        screenShakeWrapper.SetAsFirstSibling();

        while (canvasTransform.childCount > 1)
        {
            Transform child = canvasTransform.GetChild(1);
            child.SetParent(screenShakeWrapper, true);
        }
    }

    void Update()
    {
        DetectBossTransition();

        UpdatePlayerSize();
        UpdatePlayerPosition();
        UpdateBossPosition();
        UpdateProgressBar();
        UpdateBossDefeatEffects();
        UpdateWolfAnimator();
        UpdateClockAnimator();
        UpdateRotation();
        UpdateGravityMechanic();
        UpdateDeathEffects();
        UpdateCounter();
    }

    // ---------------- PLAYER VISUAL SIZE ----------------

    void UpdatePlayerSize()
    {
        float visualHeight = column.rect.height * controller.barHeight;

        player.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            visualHeight
        );
    }

    // ---------------- PLAYER POSITION ----------------

    void UpdatePlayerPosition()
    {
        float halfColumn = column.rect.height / 2f;

        // barPosition is already clamped to [barHeight/2 .. 1-barHeight/2]
        // so Lerp maps it directly into the column's local space
        player.anchoredPosition = new Vector2(
            0f,
            Mathf.Lerp(-halfColumn, halfColumn, controller.barPosition)
        );
    }

    // ---------------- BOSS POSITION ----------------

    void UpdateBossPosition()
    {
        float halfColumn = column.rect.height / 2f;

        bossIcon.anchoredPosition = new Vector2(
            0f,
            Mathf.Lerp(-halfColumn, halfColumn, boss.position)
        );
    }

    // ---------------- PROGRESS BAR ----------------

    void UpdateProgressBar()
    {
        progressBar.SetProgress(controller.progress);

        // Tint all fill RawImages: hue 0 = red, hue 0.33 = green — pitch black on defeat
        if (progressFills != null)
        {
            Color tint = director.IsBossDying
                ? Color.black
                : Color.HSVToRGB(controller.progress * 0.33f, 1f, 0.9f);
            foreach (var fill in progressFills)
                fill.color = tint;
        }
    }

    // ---------------- BOSS DEFEAT EFFECTS (UNIVERSAL) ----------------

    void UpdateBossDefeatEffects()
    {
        if (!director.IsBossDying)
            return;

        // One-shot: kick off the shake on the first frame
        if (!defeatEffectsStarted)
        {
            defeatEffectsStarted = true;
            victoryShakeTimer = victoryShakeDuration;
        }

        // Brief punchy shake on column + progress bar
        if (victoryShakeTimer > 0f)
        {
            victoryShakeTimer -= Time.deltaTime;

            // Intensity decays linearly so it feels like an impact
            float t = Mathf.Clamp01(victoryShakeTimer / victoryShakeDuration);
            float intensity = t * victoryShakeIntensity;
            Vector2 shake = Random.insideUnitCircle * intensity;

            column.anchoredPosition = shake;

            if (progressBarRect != null)
                progressBarRect.anchoredPosition = progressBarBasePos + shake;
        }
        else
        {
            column.anchoredPosition = Vector2.zero;

            if (progressBarRect != null)
                progressBarRect.anchoredPosition = progressBarBasePos;
        }
    }

    // ---------------- ROTATION ----------------

    void DetectBossTransition()
    {
        if (director.bossesDefeated == lastBossCount)
            return;

        lastBossCount = director.bossesDefeated;

        // Reset universal defeat effects for the new boss
        defeatEffectsStarted = false;
        victoryShakeTimer = 0f;

        // Boss type cycle: Normal (0) -> Rotation (1) -> Gravity (2)
        int bossType = lastBossCount % 3;
        SetWolfActive(bossType == 0);
        SetClockActive(bossType == 1);
        SetRotationActive(bossType == 1);
        SetGravityActive(bossType == 2);
    }

    // ---------------- BOSS 1 — WOLF ----------------

    void SetWolfActive(bool active)
    {
        wolfActive = active;
        wolfDeathPhase = 0;

        if (wolfImage != null)
        {
            wolfImage.gameObject.SetActive(active);
            wolfImage.color = Color.white; // reset tint
        }

        if (boss1Background != null)
        {
            boss1Background.gameObject.SetActive(active);

            // Ensure background draws behind everything else
            if (active)
                boss1Background.transform.SetAsFirstSibling();
        }

        if (active && wolfAnimator != null)
        {
            // Reset to idle state
            wolfAnimator.ResetTrigger("IsHurt");
            wolfAnimator.ResetTrigger("IsDead");
            wolfAnimator.SetBool("IsAttacking", false);
            wolfAnimator.SetBool("IsRunning", false);
            wolfAnimator.Play("wolf_idle", 0, 0f);
            lastProgress = controller.progress;
        }
    }

    void UpdateWolfAnimator()
    {
        if (!wolfActive || wolfAnimator == null)
            return;

        // --- Death sequence takes over all animation control ---
        if (wolfDeathPhase > 0)
        {
            UpdateWolfDeathSequence();
            return;
        }

        // --- Before the player presses Space, just idle ---
        if (!controller.HasStarted)
            return;

        // --- Start the death sequence when boss is defeated ---
        if (director.IsBossDying)
        {
            StartWolfDeathSequence();
            return;
        }

        float half = controller.barHeight * 0.5f;
        bool bossInsideBar =
            boss.position > controller.barPosition - half &&
            boss.position < controller.barPosition + half;

        // Attack when the boss is OUTSIDE the bar (wolf is winning)
        wolfAnimator.SetBool("IsAttacking", !bossInsideBar);

        // Run when the boss AI is moving fast
        float bossSpeed = Mathf.Abs(boss.position - lastProgress);
        wolfAnimator.SetBool("IsRunning", bossSpeed > 0.01f && bossInsideBar);

        // Hurt flash whenever progress jumps up (player is catching the boss)
        if (controller.progress > lastProgress + 0.01f && bossInsideBar)
            wolfAnimator.SetTrigger("IsHurt");

        lastProgress = controller.progress;
    }

    // ---------------- WOLF DEATH SEQUENCE ----------------

    void StartWolfDeathSequence()
    {
        wolfDeathPhase = 1; // flashing
        wolfFlashCounter = 0;
        wolfFlashOn = false;
        wolfDeathTimer = wolfFlashInterval;

        // Stop all gameplay animations
        wolfAnimator.SetBool("IsAttacking", false);
        wolfAnimator.SetBool("IsRunning", false);
        wolfAnimator.Play("wolf_idle", 0, 0f);

        // Override director's default timer — we'll call FinishBossTransition ourselves
        director.SetDeathTimer(999f);
    }

    void UpdateWolfDeathSequence()
    {
        wolfDeathTimer -= Time.deltaTime;

        switch (wolfDeathPhase)
        {
            // Phase 1: Flash red/white
            case 1:
                if (wolfDeathTimer <= 0f)
                {
                    wolfFlashOn = !wolfFlashOn;
                    wolfImage.color = wolfFlashOn ? Color.red : Color.white;
                    wolfFlashCounter++;
                    wolfDeathTimer = wolfFlashInterval;

                    // Each on+off = 2 counts, so total flashes = wolfFlashCount * 2
                    if (wolfFlashCounter >= wolfFlashCount * 2)
                    {
                        // End on white, then start death anim
                        wolfImage.color = Color.white;
                        wolfDeathPhase = 2;
                        wolfDeathTimer = wolfDeathAnimDuration;
                        wolfAnimator.SetTrigger("IsDead");
                    }
                }
                break;

            // Phase 2: Death animation playing
            case 2:
                if (wolfDeathTimer <= 0f)
                {
                    // Wolf disappears, explosion takes its place
                    wolfImage.gameObject.SetActive(false);

                    SpawnBossExplosion(wolfDeathExplosionPrefab, wolfImage.rectTransform);
                    wolfDeathTimer = explosionWaitDuration;
                    wolfDeathPhase = 3;
                }
                break;

            // Phase 3: Explosion playing — wait for its duration, then transition
            case 3:
                if (wolfDeathTimer <= 0f)
                {
                    CleanUpExplosion();
                    wolfDeathPhase = 4;
                    director.FinishBossTransition();
                }
                break;
        }
    }

    const int ExplosionLayer = 31;

    void SpawnBossExplosion(GameObject prefab, RectTransform sourceRect)
    {
        if (prefab == null)
            return;

        // Get the true screen-space center of the source rect
        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        // Convert screen position to world z=0 plane
        Camera cam = Camera.main;
        float distToZero = Mathf.Abs(cam.transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(center.x, center.y, distToZero));

        // Instantiate and move to a dedicated layer
        bossExplosionObj = Instantiate(prefab, worldPos, Quaternion.identity);
        SetLayerRecursive(bossExplosionObj, ExplosionLayer);

        // Stop the main camera from rendering the explosion layer
        mainCamOriginalMask = cam.cullingMask;
        cam.cullingMask &= ~(1 << ExplosionLayer);

        // Create a RenderTexture matching the screen
        explosionRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        explosionRT.Create();

        // Dedicated camera that only renders the explosion to the RenderTexture
        GameObject camObj = new GameObject("ExplosionCam");
        Camera explosionCam = camObj.AddComponent<Camera>();
        explosionCam.orthographic = cam.orthographic;
        explosionCam.orthographicSize = cam.orthographicSize;
        explosionCam.fieldOfView = cam.fieldOfView;
        explosionCam.transform.position = cam.transform.position;
        explosionCam.transform.rotation = cam.transform.rotation;
        explosionCam.nearClipPlane = cam.nearClipPlane;
        explosionCam.farClipPlane = cam.farClipPlane;
        explosionCam.clearFlags = CameraClearFlags.SolidColor;
        explosionCam.backgroundColor = Color.clear;
        explosionCam.cullingMask = 1 << ExplosionLayer;
        explosionCam.targetTexture = explosionRT;
        camObj.transform.SetParent(bossExplosionObj.transform);

        // Overlay canvas with high sort order to render on top of the main UI
        GameObject canvasObj = new GameObject("ExplosionCanvas");
        Canvas expCanvas = canvasObj.AddComponent<Canvas>();
        expCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        expCanvas.sortingOrder = 100;
        canvasObj.transform.SetParent(bossExplosionObj.transform);

        // Fullscreen RawImage displaying the RenderTexture
        GameObject rawImgObj = new GameObject("ExplosionImage");
        rawImgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = rawImgObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        RawImage rawImg = rawImgObj.AddComponent<RawImage>();
        rawImg.texture = explosionRT;
        rawImg.raycastTarget = false;
    }

    void CleanUpExplosion()
    {
        // Restore main camera culling mask
        if (Camera.main != null)
            Camera.main.cullingMask = mainCamOriginalMask;

        // Destroy the explosion hierarchy (includes the camera and canvas)
        if (bossExplosionObj != null)
            Destroy(bossExplosionObj);
        bossExplosionObj = null;

        // Release the RenderTexture
        if (explosionRT != null)
        {
            explosionRT.Release();
            Destroy(explosionRT);
            explosionRT = null;
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // ---------------- BOSS 2 — CLOCK ----------------

    void SetClockActive(bool active)
    {
        clockActive = active;
        clockDeathPhase = 0;

        if (clockImage != null)
        {
            clockImage.gameObject.SetActive(active);
            clockImage.color = Color.white; // reset tint
        }

        if (boss2Background != null)
        {
            boss2Background.gameObject.SetActive(active);

            // Ensure background draws behind everything else
            if (active)
                boss2Background.transform.SetAsFirstSibling();
        }

        // Reset animation timers
        clockRotateTailTimer = 0f;
        clockHurtTimer = 0f;

        if (active && clockAnimator != null)
        {
            // Reset to idle state
            clockAnimator.ResetTrigger("IsHurt");
            clockAnimator.ResetTrigger("IsDead");
            clockAnimator.SetBool("IsRotating", false);
            clockAnimator.Play("clock_idle", 0, 0f);
            clockLastProgress = controller.progress;
        }
    }

    void UpdateClockAnimator()
    {
        if (!clockActive || clockAnimator == null)
            return;

        // --- Death sequence takes over all animation control ---
        if (clockDeathPhase > 0)
        {
            UpdateClockDeathSequence();
            return;
        }

        // --- Before the player presses Space, just idle ---
        if (!controller.HasStarted)
            return;

        // --- Start the death sequence when boss is defeated ---
        if (director.IsBossDying)
        {
            StartClockDeathSequence();
            return;
        }

        // Rotate animation: lead-in before rotation, active during, tail after
        bool rotationImminent = rotationActive && rotationTimer >= rotationInterval - clockRotateLeadIn;
        bool rotationInProgress = rotationActive && Mathf.Abs(currentAngle - targetAngle) > 0.5f;

        if (rotationInProgress || rotationImminent)
            clockRotateTailTimer = clockRotateTailOut;
        else if (clockRotateTailTimer > 0f)
            clockRotateTailTimer -= Time.deltaTime;

        bool showRotateAnim = rotationInProgress || rotationImminent || clockRotateTailTimer > 0f;

        // Priority: Rotate > Hurt > Idle
        float half = controller.barHeight * 0.5f;
        bool bossInsideBar =
            boss.position > controller.barPosition - half &&
            boss.position < controller.barPosition + half;

        bool catching = bossInsideBar && controller.progress > clockLastProgress;

        if (showRotateAnim)
        {
            // Rotate takes top priority — player must see the "cast"
            clockAnimator.SetBool("IsRotating", true);
        }
        else if (catching)
        {
            // Hurt while catching (only when not rotating)
            clockAnimator.SetBool("IsRotating", false);
            if (!clockAnimator.GetCurrentAnimatorStateInfo(0).IsName("clock_hurt"))
                clockAnimator.Play("clock_hurt", 0, 0f);
        }
        else
        {
            // Idle
            clockAnimator.SetBool("IsRotating", false);
        }

        clockLastProgress = controller.progress;
    }

    // ---------------- CLOCK DEATH SEQUENCE ----------------

    void StartClockDeathSequence()
    {
        clockDeathPhase = 1; // flashing
        clockFlashCounter = 0;
        clockFlashOn = false;
        clockDeathTimer = clockFlashInterval;

        // Stop rotation and shake immediately — snaps column back to normal
        SetRotationActive(false);

        // Play hurt animation during flash phase (not idle)
        clockAnimator.SetBool("IsRotating", false);
        clockAnimator.Play("clock_hurt", 0, 0f);

        // Override director's default timer — we'll call FinishBossTransition ourselves
        director.SetDeathTimer(999f);
    }

    void UpdateClockDeathSequence()
    {
        clockDeathTimer -= Time.deltaTime;

        switch (clockDeathPhase)
        {
            // Phase 1: Flash red/white (hurt animation keeps playing)
            case 1:
                // Keep hurt animation playing (prevent HasExitTime transition to idle)
                if (!clockAnimator.GetCurrentAnimatorStateInfo(0).IsName("clock_hurt"))
                    clockAnimator.Play("clock_hurt", 0, 0f);

                if (clockDeathTimer <= 0f)
                {
                    clockFlashOn = !clockFlashOn;
                    clockImage.color = clockFlashOn ? Color.red : Color.white;
                    clockFlashCounter++;
                    clockDeathTimer = clockFlashInterval;

                    // Each on+off = 2 counts, so total flashes = clockFlashCount * 2
                    if (clockFlashCounter >= clockFlashCount * 2)
                    {
                        // End on white, then start death anim
                        clockImage.color = Color.white;
                        clockDeathPhase = 2;
                        clockDeathTimer = clockDeathAnimDuration;
                        clockAnimator.SetTrigger("IsDead");
                    }
                }
                break;

            // Phase 2: Looping death animation playing
            case 2:
                if (clockDeathTimer <= 0f)
                {
                    // Clock disappears, explosion takes its place
                    clockImage.gameObject.SetActive(false);

                    SpawnBossExplosion(clockDeathExplosionPrefab, clockImage.rectTransform);
                    clockDeathTimer = clockExplosionWaitDuration;
                    clockDeathPhase = 3;
                }
                break;

            // Phase 3: Explosion playing — wait for its duration, then transition
            case 3:
                if (clockDeathTimer <= 0f)
                {
                    CleanUpExplosion();
                    clockDeathPhase = 4;
                    director.FinishBossTransition();
                }
                break;
        }
    }

    void SetRotationActive(bool active)
    {
        rotationActive = active;

        if (!active)
        {
            rotationTimer = 0f;
            targetAngle = 0f;
            currentAngle = 0f;
            tickTimer = 0f;
            tickShaking = false;

            rotationWrapper.localRotation = Quaternion.identity;
            bossIcon.localRotation = Quaternion.identity;
        }
        else
        {
            tickTimer = 0f;
        }
    }

    void UpdateRotation()
    {
        if (!rotationActive || !controller.HasStarted)
            return;

        rotationTimer += Time.deltaTime;

        if (rotationTimer >= rotationInterval)
        {
            rotationTimer -= rotationInterval;
            targetAngle += 90f;
        }

        // Smooth rotation
        currentAngle = Mathf.MoveTowards(
            currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        // Rotate the wrapper (column + progress bar move together)
        rotationWrapper.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

        // Counter-rotate boss icon so it stays visually upright
        bossIcon.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);

        // --- Tick shake: brief hit-shake every second as rotation warning ---
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickShakeInterval)
        {
            tickTimer -= tickShakeInterval;
            tickShaking = true;
            tickShakeTimer = tickShakeDuration;
        }

        if (tickShaking)
        {
            tickShakeTimer -= Time.deltaTime;
            if (tickShakeTimer > 0f)
            {
                Vector2 shake = Random.insideUnitCircle * tickShakeIntensity;
                column.anchoredPosition = shake;
            }
            else
            {
                column.anchoredPosition = Vector2.zero;
                tickShaking = false;
            }
        }
    }

    // ---------------- GRAVITY MECHANIC ----------------

    void SetGravityActive(bool active)
    {
        // Reset positions and rotations if gravity was previously active
        if (gravityMechanicActive && !active)
        {
            column.anchoredPosition = Vector2.zero;
            column.localRotation = Quaternion.identity;
            if (progressBarRect != null)
            {
                progressBarRect.anchoredPosition = progressBarBasePos;
                progressBarRect.localRotation = Quaternion.identity;
            }
        }

        // Ensure column renders on top of the health bar during gravity bosses
        if (active)
            column.SetAsLastSibling();
        else if (progressBarRect != null)
            progressBarRect.SetAsLastSibling();

        gravityMechanicActive = active;
        gravityPhase2 = false;
        gravityIsOn = false;
        gravityToggleTimer = 0f;
        columnVelocity = Vector2.zero;
        columnOffset = Vector2.zero;
        columnAngularVel = 0f;
        columnAngle = 0f;
        progressBarVelocity = Vector2.zero;
        progressBarOffset = Vector2.zero;
        progressBarAngularVel = 0f;
        progressBarAngle = 0f;
    }

    void UpdateGravityMechanic()
    {
        if (!gravityMechanicActive)
            return;

        // One-shot phase 2 trigger
        if (!gravityPhase2)
        {
            if (controller.progress >= gravityPhaseThreshold)
            {
                gravityPhase2 = true;
                gravityIsOn = true;
                gravityToggleTimer = gravityOnDuration;
                // Initial tumble impulse
                columnAngularVel = Random.Range(-tumbleImpulse, tumbleImpulse);
                progressBarAngularVel = Random.Range(-tumbleImpulse, tumbleImpulse);
            }
            return; // Phase 1 is normal gameplay
        }

        // --- Toggle timer ---
        gravityToggleTimer -= Time.deltaTime;
        if (gravityToggleTimer <= 0f)
        {
            gravityIsOn = !gravityIsOn;
            gravityToggleTimer = gravityIsOn ? gravityOnDuration : gravityOffDuration;

            // Fresh tumble impulse on each toggle
            columnAngularVel += Random.Range(-tumbleImpulse, tumbleImpulse);
            progressBarAngularVel += Random.Range(-tumbleImpulse, tumbleImpulse);
        }

        float dt = Time.deltaTime;

        // Acceleration direction: down when gravity on, up when floating
        Vector2 accel = gravityIsOn
            ? new Vector2(0f, -gravityAccel)
            : new Vector2(0f, floatAccel);

        // --- Simulate column ---
        SimulateElement(ref columnVelocity, ref columnOffset, ref columnAngularVel, ref columnAngle, accel, column, dt);
        column.anchoredPosition = columnOffset;
        column.localRotation = Quaternion.Euler(0f, 0f, columnAngle);

        // --- Simulate progress bar ---
        if (progressBarRect != null)
        {
            SimulateElement(ref progressBarVelocity, ref progressBarOffset, ref progressBarAngularVel, ref progressBarAngle, accel, progressBarRect, dt);
            progressBarRect.anchoredPosition = progressBarBasePos + progressBarOffset;
            progressBarRect.localRotation = Quaternion.Euler(0f, 0f, progressBarAngle);
        }
    }

    void SimulateElement(ref Vector2 velocity, ref Vector2 offset,
                         ref float angularVel, ref float angle,
                         Vector2 accel, RectTransform element, float dt)
    {
        // --- Linear physics ---
        velocity += accel * dt;
        offset += velocity * dt;

        // --- Angular physics ---
        // Dampen spin when floating (gravity off), free spin when falling
        if (!gravityIsOn)
            angularVel = Mathf.MoveTowards(angularVel, 0f, tumbleDamping * Mathf.Abs(angularVel) * dt + 10f * dt);

        angle += angularVel * dt;

        // Screen bounds (element coords are relative to screen center)
        float halfCanvasW = canvasRect.rect.width * 0.5f;
        float halfCanvasH = canvasRect.rect.height * 0.5f;
        float halfElemW = element.rect.width * 0.5f;
        float halfElemH = element.rect.height * 0.5f;

        float minX = -halfCanvasW + halfElemW;
        float maxX = halfCanvasW - halfElemW;
        float minY = -halfCanvasH + halfElemH;
        float maxY = halfCanvasH - halfElemH;

        // Bounce X
        if (offset.x < minX)
        {
            offset.x = minX;
            velocity.x = Mathf.Abs(velocity.x) * bounceFactor;
            angularVel = -angularVel * bounceFactor; // reverse spin on impact
        }
        else if (offset.x > maxX)
        {
            offset.x = maxX;
            velocity.x = -Mathf.Abs(velocity.x) * bounceFactor;
            angularVel = -angularVel * bounceFactor;
        }

        // Bounce Y
        if (offset.y < minY)
        {
            offset.y = minY;
            velocity.y = Mathf.Abs(velocity.y) * bounceFactor;
            angularVel = -angularVel * bounceFactor;
        }
        else if (offset.y > maxY)
        {
            offset.y = maxY;
            velocity.y = -Mathf.Abs(velocity.y) * bounceFactor;
            angularVel = -angularVel * bounceFactor;
        }
    }

    // ---------------- DEATH EFFECTS ----------------

    void UpdateDeathEffects()
    {
        float urgency = controller.LossUrgency;

        // Effective base accounts for gravity offset when active
        Vector2 effectiveBase = (gravityMechanicActive && gravityPhase2)
            ? progressBarBasePos + progressBarOffset
            : progressBarBasePos;

        // --- Grace period shake (health bar only) ---
        if (urgency > 0f && !director.IsGameOver)
        {
            // Ramp intensity: squared so it escalates near the end
            float intensity = urgency * urgency * graceShakeMax;
            Vector2 shake = Random.insideUnitCircle * intensity;

            // Offset progress bar from its effective base position
            if (progressBarRect != null)
                progressBarRect.anchoredPosition = effectiveBase + shake;
        }
        else if (progressBarRect != null && !deathShakeActive)
        {
            // Restore progress bar to effective base when not shaking
            progressBarRect.anchoredPosition = effectiveBase;
        }

        // --- Death screen shake (brief impact on the whole wrapper) ---
        if (director.IsGameOver && !deathShakeActive && !deathShakeTriggered)
        {
            deathShakeActive = true;
            deathShakeTimer = deathShakeDuration;
        }

        if (deathShakeActive && screenShakeWrapper != null)
        {
            // Uses unscaledDeltaTime so it works while timeScale == 0
            deathShakeTimer -= Time.unscaledDeltaTime;

            if (deathShakeTimer > 0f)
            {
                // Constant intensity for a punchy feel (like Week 2 project)
                Vector2 shake = Random.insideUnitCircle * deathShakeIntensity;
                screenShakeWrapper.anchoredPosition = shake;
            }
            else
            {
                screenShakeWrapper.anchoredPosition = Vector2.zero;
                deathShakeActive = false;
                deathShakeTriggered = true;
                director.ShowDeathScreen();
            }
        }
    }

    // ---------------- COUNTER ----------------

    void UpdateCounter()
    {
        enemyCounterText.text =
            $"Bosses Defeated: {director.bossesDefeated}";
    }
}
