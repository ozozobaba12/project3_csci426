using UnityEngine;

public class BossfightController : MonoBehaviour
{
    // ---------------- PLAYER BAR LOGIC ----------------

    [Header("Player Bar (LOGIC SIZE ONLY)")]
    // Fraction of the ORIGINAL column height that the bar occupies
    // This NEVER changes during the game
    public float barHeight = 0.325f;

    // Normalized vertical position (0–1)
    public float barPosition;

    // Vertical velocity of the bar (physics-like control)
    public float barVelocity;

    // ---------------- MOVEMENT FEEL ----------------

    [Header("Movement")]
    // Continuous upward force while holding Space
    public float riseForce = 4.5f;

    // Continuous downward force when Space is released
    public float gravity = 5.5f;

    // Hard cap on velocity so things never explode
    public float maxSpeed = 3.2f;

    // Small impulse applied on each tap
    public float tapImpulse = 0.8f;

    // ---------------- PROGRESS ----------------

    [Header("Progress")]
    // Starts low so pressure is immediate
    [Range(0f, 1f)]
    public float progress = 0.20f;

    // How fast progress increases when boss is inside bar
    public float catchRate = 0.50f;

    // How fast progress drains when boss is outside bar
    public float escapeRate = 0.50f;

    // ---------------- DRAIN DELAY ----------------

    [Header("Drain Delay")]
    // Small delay before draining starts when boss leaves bar
    public float drainDelay = 0.8f;

    float drainTimer = 0f;

    // ---------------- LOSS ----------------

    [Header("Loss Delay")]
    // Gives the player a clutch moment at 0 progress
    public float lossDelay = 0.8f;
    float lossTimer;

    // ---------------- REFERENCES ----------------

    [Header("References")]
    public BossAI boss;

    bool hasStarted;

    void Awake()
    {
        // Safety: ensure game isn't paused from a previous run
        Time.timeScale = 1f;

        ResetForNextBoss();
    }

    void Update()
    {
        UpdateBar();
        UpdateProgress();
    }

    // ---------------- BAR MOVEMENT ----------------

    void UpdateBar()
    {
        // First Space press starts the round
        if (!hasStarted && Input.GetKeyDown(KeyCode.Space))
        {
            hasStarted = true;
            boss.isActive = true;
        }

        // Do nothing until the round starts
        if (!hasStarted)
            return;

        // Apply a small upward impulse on each tap
        if (Input.GetKeyDown(KeyCode.Space))
            barVelocity = Mathf.Max(barVelocity, tapImpulse);

        // Apply continuous forces
        if (Input.GetKey(KeyCode.Space))
            barVelocity += riseForce * Time.deltaTime;
        else
            barVelocity -= gravity * Time.deltaTime;

        // Clamp velocity so it never goes crazy
        barVelocity = Mathf.Clamp(barVelocity, -maxSpeed, maxSpeed);

        // Integrate velocity into position
        barPosition += barVelocity * Time.deltaTime;

        // Compute valid range so the BAR EDGES stay inside column
        float half = barHeight * 0.5f;
        float top = 1f - half;
        float bottom = half;

        // Hard ceiling
        if (barPosition >= top)
        {
            barPosition = top;
            if (barVelocity > 0f)
                barVelocity = 0f; // kill upward momentum
        }
        // Hard floor
        else if (barPosition <= bottom)
        {
            barPosition = bottom;
            if (barVelocity < 0f)
                barVelocity = 0f;
        }
    }

    // ---------------- PROGRESS LOGIC ----------------

    void UpdateProgress()
    {
        if (!hasStarted)
            return;

        float half = barHeight * 0.5f;

        // Check if boss is inside the bar
        bool inside =
            boss.position > barPosition - half &&
            boss.position < barPosition + half;

        if (inside)
        {
            // Reset drain timer while successful
            drainTimer = 0f;

            // Gain progress
            progress += catchRate * Time.deltaTime;
        }
        else
        {
            // Count time since boss left bar
            drainTimer += Time.deltaTime;

            // Only drain after delay
            if (drainTimer >= drainDelay)
                progress -= escapeRate * Time.deltaTime;
        }

        // Keep progress valid
        progress = Mathf.Clamp01(progress);
    }

    // ---------------- GAME STATE ----------------

    public bool PlayerLost()
    {
        if (progress > 0f)
        {
            lossTimer = 0f;
            return false;
        }

        lossTimer += Time.deltaTime;
        return lossTimer >= lossDelay;
    }

    public bool PlayerWon() => progress >= 1f;

    // ---------------- RESET ----------------

    public void ResetForNextBoss()
    {
        progress = 0.20f;

        // Start at bottom (bar center)
        barPosition = barHeight * 0.5f;

        barVelocity = 0f;
        drainTimer = 0f;
        lossTimer = 0f;
        hasStarted = false;

        boss.isActive = false;
    }
}
