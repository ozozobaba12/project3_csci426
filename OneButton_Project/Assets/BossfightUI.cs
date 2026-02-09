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

    [Header("Death Effects")]
    [Tooltip("Max pixel displacement of the health bar during grace period.")]
    public float graceShakeMax = 8f;
    [Tooltip("Pixel displacement of the screen shake on death.")]
    public float deathShakeIntensity = 15f;
    [Tooltip("How long the death screen shake lasts (seconds, uses real time).")]
    public float deathShakeDuration = 0.25f;

    // Auto-discovered fill textures inside the progress bar
    RawImage[] progressFills;

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

        // Reparent all existing canvas children under the wrapper
        // (iterate in reverse so indices don't shift)
        int childCount = canvasTransform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = canvasTransform.GetChild(i);
            if (child != screenShakeWrapper.transform)
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

        // Tint all fill RawImages: hue 0 = red, hue 0.33 = green
        if (progressFills != null)
        {
            Color tint = Color.HSVToRGB(controller.progress * 0.33f, 1f, 0.9f);
            foreach (var fill in progressFills)
                fill.color = tint;
        }
    }

    // ---------------- ROTATION ----------------

    void DetectBossTransition()
    {
        if (director.bossesDefeated == lastBossCount)
            return;

        lastBossCount = director.bossesDefeated;

        // Boss type cycle: Normal (0) -> Rotation (1) -> Gravity (2)
        int bossType = lastBossCount % 3;
        SetRotationActive(bossType == 1);
        SetGravityActive(bossType == 2);
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
        if (!rotationActive)
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
