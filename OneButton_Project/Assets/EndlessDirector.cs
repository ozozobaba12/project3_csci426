using UnityEngine;

public class EndlessDirector : MonoBehaviour
{
    public BossfightController controller;
    public BossAI boss;

    public int bossesDefeated = 0;
    float graceTimer = 0.75f;

    void Update()
    {
        if (graceTimer > 0f)
        {
            graceTimer -= Time.deltaTime;
            return;
        }

        if (controller.PlayerLost())
        {
            Time.timeScale = 0f;
            return;
        }

        if (controller.PlayerWon())
        {
            bossesDefeated++;
            boss.bossesDefeated = bossesDefeated;

            boss.ResetPosition();
            controller.ResetForNextBoss();

            graceTimer = 0.75f;
        }
    }
}
