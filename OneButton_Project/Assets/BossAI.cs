using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Range(0f, 1f)]
    public float position;

    [Header("Base Difficulty")]
    public float baseSpeed = 0.5f;

    [Header("Scaling")]
    public float speedPerDifficulty = 0.35f;
    public float difficultyCurve = 0.65f; // 🔴 slows early ramp

    [Header("Movement Behavior")]
    public float baseDecisionTime = 1.4f;
    public float minDecisionTime = 0.6f;

    [HideInInspector]
    public int bossesDefeated;

    public bool isActive;

    float target;
    float timer;

    void Start()
    {
        ResetPosition();
    }

    void Update()
    {
        if (!isActive)
            return;

        timer -= Time.deltaTime;

        float difficulty =
            Mathf.Pow(bossesDefeated, difficultyCurve);

        float speed =
            baseSpeed + difficulty * speedPerDifficulty;

        position = Mathf.MoveTowards(
            position,
            target,
            speed * Time.deltaTime
        );

        if (timer <= 0f && Mathf.Abs(position - target) < 0.02f)
            PickTarget(difficulty);
    }

    void PickTarget(float difficulty)
    {
        // More extremes at higher difficulty
        float extremeChance = Mathf.Lerp(0.3f, 0.7f, difficulty / 5f);

        float roll = Random.value;

        if (roll < extremeChance * 0.5f)
            target = 0.05f; // bottom
        else if (roll < extremeChance)
            target = 0.95f; // top
        else
            target = Random.Range(0.15f, 0.85f);

        timer = Mathf.Max(
            minDecisionTime,
            baseDecisionTime - difficulty * 0.25f
        );
    }

    public void ResetPosition()
    {
        position = 0.05f;
        PickTarget(0f);
    }
}
