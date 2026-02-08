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
    // ORIGINAL column height used as reference
    public float baseColumnHeight = 400f;
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
        // IMPORTANT:
        // We scale bar using the ORIGINAL column height,
        // not the current one, so it never grows visually
        float visualHeight = baseColumnHeight * controller.barHeight;

        player.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            visualHeight
        );
    }

    // ---------------- PLAYER POSITION ----------------

    void UpdatePlayerPosition()
    {
        float halfColumn = column.rect.height / 2f;
        float halfBar = (baseColumnHeight * controller.barHeight) / 2f;

        float minY = -halfColumn + halfBar;
        float maxY =  halfColumn - halfBar;

        player.anchoredPosition = new Vector2(
            player.anchoredPosition.x,
            Mathf.Lerp(
                minY,
                maxY,
                Mathf.InverseLerp(
                    controller.barHeight * 0.5f,
                    1f - controller.barHeight * 0.5f,
                    controller.barPosition
                )
            )
        );
    }

    // ---------------- BOSS POSITION ----------------

    void UpdateBossPosition()
    {
        float halfColumn = column.rect.height / 2f;

        bossIcon.anchoredPosition = new Vector2(
            bossIcon.anchoredPosition.x,
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
