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

    // Auto-discovered fill textures inside the progress bar
    RawImage[] progressFills;

    void Start()
    {
        if (progressBar != null)
            progressFills = progressBar.GetComponentsInChildren<RawImage>();
    }

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
        progressBar.SetProgress(controller.progress);

        // Tint all fill RawImages: hue 0 = red, hue 0.33 = green
        if (progressFills != null)
        {
            Color tint = Color.HSVToRGB(controller.progress * 0.33f, 1f, 0.9f);
            foreach (var fill in progressFills)
                fill.color = tint;
        }
    }

    // ---------------- COUNTER ----------------

    void UpdateCounter()
    {
        enemyCounterText.text =
            $"Bosses Defeated: {director.bossesDefeated}";
    }
}
