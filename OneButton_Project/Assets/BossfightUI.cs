using UnityEngine;
using TMPro;

public class BossfightUI : MonoBehaviour
{
    public BossfightController controller;
    public BossAI boss;
    public EndlessDirector director;

    [Header("UI References")]
    public RectTransform column;
    public RectTransform player;
    public RectTransform bossIcon;
    public RectTransform progressFill;
    public TextMeshProUGUI enemyCounterText;

    [Header("Layout")]
    public float progressMaxWidth = 220f;

    void Update()
    {
        UpdatePlayerSize();
        UpdatePlayerPosition();
        UpdateBossPosition();
        UpdateProgressBar();
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
        float width = progressMaxWidth * controller.progress;

        progressFill.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width
        );
    }

    // ---------------- COUNTER ----------------

    void UpdateCounter()
    {
        enemyCounterText.text =
            $"Bosses Defeated: {director.bossesDefeated}";
    }
}
